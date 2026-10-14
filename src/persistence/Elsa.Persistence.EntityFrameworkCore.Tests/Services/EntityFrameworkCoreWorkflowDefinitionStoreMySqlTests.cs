using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Elsa.Persistence.EntityFrameworkCore.Tests.Services
{
    /// <summary>
    /// Saving an existing workflow definition deletes its activities and connections before inserting the new ones.
    /// MariaDB rejects a DELETE whose subquery reads the table being deleted (error 1093), which is what EF Core 10
    /// generated for the previous navigation-based filter.
    /// </summary>
    public class EntityFrameworkCoreWorkflowDefinitionStoreMySqlTests
    {
        private static ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddElsaCore(elsa => elsa.AddEntityFrameworkStores<MySqlContext>(options => options.UseMySql(
                    MySqlFactAttribute.ConnectionString,
                    MariaDbServerVersion.LatestSupportedServerVersion)));

            return services.BuildServiceProvider();
        }

        private static async Task<T> InScopeAsync<T>(ServiceProvider provider, Func<IWorkflowDefinitionStore, Task<T>> action)
        {
            using var scope = provider.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>());
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ElsaContext>().Database.MigrateAsync();
        }

        private static WorkflowDefinitionVersion CreateDefinition(string definitionId, string versionId, string name, int activityCount)
        {
            var definition = new WorkflowDefinitionVersion
            {
                Id = versionId,
                DefinitionId = definitionId,
                Version = 1,
                Name = name,
                IsPublished = true,
                IsLatest = true
            };

            for (var i = 1; i <= activityCount; i++)
                definition.Activities.Add(new ActivityDefinition($"activity-{i}", "Type", new JObject { ["index"] = i }, 0, 0));

            for (var i = 1; i < activityCount; i++)
                definition.Connections.Add(new ConnectionDefinition($"activity-{i}", $"activity-{i + 1}", "Done"));

            return definition;
        }

        [MySqlFact]
        public async Task SaveAsync_ExistingDefinition_ReplacesActivitiesAndConnections()
        {
            await using var provider = CreateServiceProvider();
            await MigrateAsync(provider);
            var definitionId = Guid.NewGuid().ToString("N");
            var versionId = Guid.NewGuid().ToString("N");

            await InScopeAsync(provider, store => store.SaveAsync(CreateDefinition(definitionId, versionId, "Original", 2)));
            await InScopeAsync(provider, store => store.SaveAsync(CreateDefinition(definitionId, versionId, "Renamed", 3)));

            var saved = await InScopeAsync(provider, store => store.GetByIdReadOnlyAsync(definitionId, VersionOptions.Latest));
            Assert.Equal("Renamed", saved.Name);
            Assert.Equal(3, saved.Activities.Count);
            Assert.Equal(2, saved.Connections.Count);
            Assert.Equal(3, saved.Activities.Select(x => x.Id).Distinct().Count());
        }

        [MySqlFact]
        public async Task UpdateAsync_ExistingDefinition_ReplacesActivitiesAndConnections()
        {
            await using var provider = CreateServiceProvider();
            await MigrateAsync(provider);
            var definitionId = Guid.NewGuid().ToString("N");
            var versionId = Guid.NewGuid().ToString("N");

            await InScopeAsync(provider, store => store.AddAsync(CreateDefinition(definitionId, versionId, "Original", 3)));
            await InScopeAsync(provider, store => store.UpdateAsync(CreateDefinition(definitionId, versionId, "Renamed", 2)));

            var saved = await InScopeAsync(provider, store => store.GetByIdReadOnlyAsync(definitionId, VersionOptions.Latest));
            Assert.Equal("Renamed", saved.Name);
            Assert.Equal(2, saved.Activities.Count);
            Assert.Single(saved.Connections);
        }
    }
}
