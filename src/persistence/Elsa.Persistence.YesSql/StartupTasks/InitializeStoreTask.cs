using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Schema;
using Elsa.Runtime;
using YesSql;
using YesSql.Sql;

namespace Elsa.Persistence.YesSql.StartupTasks
{
    public class InitializeStoreTask : IStartupTask
    {
        private readonly IStore store;
        private readonly ISchemaVersionStore schemaVersionStore;
        private readonly SchemaUpdate[] schemaVersionUpdates;

        public InitializeStoreTask(IStore store,
            ISchemaVersionStore schemaVersionStore)
        {
            this.store = store;
            this.schemaVersionStore = schemaVersionStore;

            schemaVersionUpdates = new[]
            {
                new SchemaUpdate {Version = 1, Update = UpdateToVersion1}
            };
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            int currentVersion = await schemaVersionStore.GetVersionAsync();

            foreach (var schemaUpdate in schemaVersionUpdates.Where(x => x.Version > currentVersion))
            {
                await schemaUpdate.Update();
                await schemaVersionStore.SaveVersionAsync(schemaUpdate.Version);
            }
        }

        private Task UpdateToVersion1()
        {
            return PerformUpdates(async builder =>
            {
                await builder.CreateMapIndexTableAsync(typeof(WorkflowDefinitionIndex), table => table
                    .Column<string>("WorkflowDefinitionId")
                    .Column<int>("Version")
                    .Column<bool>("IsPublished")
                    .Column<bool>("IsLatest")
                    .Column<bool>("IsDisabled"),
                    collection: null);

                await builder.CreateMapIndexTableAsync(typeof(WorkflowDefinitionStartActivitiesIndex), table => table
                    .Column<string>("StartActivityId")
                    .Column<string>("StartActivityType")
                    .Column<bool>("IsDisabled"),
                    collection: null);

                await builder.CreateMapIndexTableAsync(typeof(WorkflowInstanceIndex), table => table
                    .Column<string>("WorkflowInstanceId")
                    .Column<string>("WorkflowDefinitionId")
                    .Column<string>("CorrelationId")
                    .Column<string>("WorkflowStatus")
                    .Column<DateTime>("CreatedAt"),
                    collection: null);

                await builder.CreateMapIndexTableAsync(typeof(WorkflowInstanceBlockingActivitiesIndex), table => table
                    .Column<string>("ActivityId")
                    .Column<string>("ActivityType")
                    .Column<string>("CorrelationId")
                    .Column<string>("WorkflowStatus")
                    .Column<DateTime>("CreatedAt"),
                    collection: null);
            });
        }

        private async Task PerformUpdates(Func<ISchemaBuilder, Task> builder)
        {
            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    var schemaBuilder = new SchemaBuilder(store.Configuration, transaction, false);

                    await builder(schemaBuilder);

                    transaction.Commit();
                }
            }
        }

        private class SchemaUpdate
        {
            internal int Version { get; set; }
            internal Func<Task> Update { get; set; }
        }
    }
}