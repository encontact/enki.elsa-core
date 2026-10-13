using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Messaging;
using Elsa.Models;
using Elsa.Persistence;
using Xunit;

namespace Elsa.Core.Tests.Persistence
{
    public class PublishingWorkflowDefinitionStoreTests
    {
        private class RecordingPublisher : INotificationPublisher
        {
            public List<(INotification Notification, CancellationToken CancellationToken)> Published { get; } = new List<(INotification, CancellationToken)>();

            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            {
                Published.Add((notification, cancellationToken));
                return Task.CompletedTask;
            }
        }

        private class StubWorkflowDefinitionStore : IWorkflowDefinitionStore
        {
            public Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default) => Task.FromResult(definition);
            public Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default) => Task.FromResult(definition);
            public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default) => Task.FromResult(new WorkflowDefinitionVersion { Id = id });
            public Task<WorkflowDefinitionVersion> GetByIdReadOnlyAsync(string id, VersionOptions version, CancellationToken cancellationToken = default) => Task.FromResult(new WorkflowDefinitionVersion { Id = id });
            public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<WorkflowDefinitionVersion>());
            public Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default) => Task.FromResult(definition);
            public Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(1);
        }

        private readonly RecordingPublisher publisher = new RecordingPublisher();
        private readonly PublishingWorkflowDefinitionStore store;
        private readonly WorkflowDefinitionVersion definition = new WorkflowDefinitionVersion { Id = "definition-1" };

        public PublishingWorkflowDefinitionStoreTests()
        {
            store = new PublishingWorkflowDefinitionStore(new StubWorkflowDefinitionStore(), publisher);
        }

        [Fact]
        public async Task SaveAsync_PublishesWorkflowDefinitionStoreUpdatedWithCancellationToken()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var result = await store.SaveAsync(definition, cancellationTokenSource.Token);

            Assert.Same(definition, result);
            var published = Assert.Single(publisher.Published);
            Assert.IsType<WorkflowDefinitionStoreUpdated>(published.Notification);
            Assert.Equal(cancellationTokenSource.Token, published.CancellationToken);
        }

        [Fact]
        public async Task AddAsync_PublishesWorkflowDefinitionStoreUpdated()
        {
            await store.AddAsync(definition);

            Assert.IsType<WorkflowDefinitionStoreUpdated>(Assert.Single(publisher.Published).Notification);
        }

        [Fact]
        public async Task UpdateAsync_PublishesWorkflowDefinitionStoreUpdated()
        {
            await store.UpdateAsync(definition);

            Assert.IsType<WorkflowDefinitionStoreUpdated>(Assert.Single(publisher.Published).Notification);
        }

        [Fact]
        public async Task DeleteAsync_PublishesWorkflowDefinitionStoreUpdatedAndReturnsDeletedCount()
        {
            var count = await store.DeleteAsync("definition-1");

            Assert.Equal(1, count);
            Assert.IsType<WorkflowDefinitionStoreUpdated>(Assert.Single(publisher.Published).Notification);
        }

        [Fact]
        public async Task ReadOperations_DoNotPublishNotifications()
        {
            await store.GetByIdAsync("definition-1", VersionOptions.Latest);
            await store.GetByIdReadOnlyAsync("definition-1", VersionOptions.Latest);
            await store.ListAsync(VersionOptions.Latest);

            Assert.Empty(publisher.Published);
        }
    }
}
