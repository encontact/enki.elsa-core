using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Extensions;
using Elsa.Messages;
using Elsa.Messages.Handlers;
using Elsa.Messaging;
using Elsa.Models;
using Elsa.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Core.Tests.Extensions
{
    public class ElsaServiceCollectionExtensionsTests
    {
        // Same value as WorkflowRegistry.CacheKey (internal).
        private const string WorkflowRegistryCacheKey = "elsa:workflow-registry";

        private static ServiceProvider BuildElsaCoreProvider(IServiceCollection? services = null) =>
            (services ?? new ServiceCollection()).AddElsaCore().BuildServiceProvider();

        [Fact]
        public void AddElsaCore_RegistersSingletonNotificationPublisher()
        {
            using var provider = BuildElsaCoreProvider();

            var publisher1 = provider.GetRequiredService<INotificationPublisher>();
            var publisher2 = provider.GetRequiredService<INotificationPublisher>();

            Assert.IsType<NotificationPublisher>(publisher1);
            Assert.Same(publisher1, publisher2);
        }

        [Fact]
        public void AddElsaCore_RegistersCoreNotificationHandlers()
        {
            using var provider = BuildElsaCoreProvider();

            var handlers = provider.GetServices<INotificationHandler<WorkflowDefinitionStoreUpdated>>();

            Assert.Contains(handlers, x => x is EvictWorkflowRegistryHandler);
        }

        [Fact]
        public async Task AddElsaCore_SavingWorkflowDefinition_EvictsWorkflowRegistryCache()
        {
            using var provider = BuildElsaCoreProvider();
            var signal = provider.GetRequiredService<ISignal>();
            var store = provider.GetRequiredService<IWorkflowDefinitionStore>();
            var token = signal.GetToken(WorkflowRegistryCacheKey);

            await store.SaveAsync(new WorkflowDefinitionVersion { Id = "definition-1", DefinitionId = "definition-1", Version = 1 });

            Assert.True(token.HasChanged);
        }

        [Fact]
        public async Task AddElsaCore_WithConsumerHandler_InvokesConsumerHandlerOnStoreUpdate()
        {
            var services = new ServiceCollection().AddSingleton<ConsumerHandlerCalls>();
            services.AddNotificationHandler<WorkflowDefinitionStoreUpdated, ConsumerStoreUpdatedHandler>();
            using var provider = BuildElsaCoreProvider(services);
            var store = provider.GetRequiredService<IWorkflowDefinitionStore>();

            await store.SaveAsync(new WorkflowDefinitionVersion { Id = "definition-1", DefinitionId = "definition-1", Version = 1 });

            Assert.Equal(1, provider.GetRequiredService<ConsumerHandlerCalls>().Count);
        }

        [Fact]
        public void ElsaAssemblies_DoNotReferenceMediatR()
        {
            var assemblies = new[] { typeof(INotification).Assembly, typeof(NotificationPublisher).Assembly };

            var references = assemblies.SelectMany(x => x.GetReferencedAssemblies()).Select(x => x.Name);

            Assert.DoesNotContain("MediatR", references);
            Assert.DoesNotContain("MediatR.Contracts", references);
        }

        public class ConsumerHandlerCalls
        {
            public int Count { get; set; }
        }

        public class ConsumerStoreUpdatedHandler : INotificationHandler<WorkflowDefinitionStoreUpdated>
        {
            private readonly ConsumerHandlerCalls calls;

            public ConsumerStoreUpdatedHandler(ConsumerHandlerCalls calls)
            {
                this.calls = calls;
            }

            public Task Handle(WorkflowDefinitionStoreUpdated notification, CancellationToken cancellationToken)
            {
                calls.Count++;
                return Task.CompletedTask;
            }
        }
    }
}
