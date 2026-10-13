using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Core.Tests.Extensions
{
    public class MessageHandlerServiceCollectionExtensionsTests
    {
        public class ScannedNotification : INotification
        {
            public int Calls { get; set; }
        }

        public class OtherScannedNotification : INotification
        {
            public int Calls { get; set; }
        }

        public class ScannedHandler : INotificationHandler<ScannedNotification>
        {
            public Task Handle(ScannedNotification notification, CancellationToken cancellationToken)
            {
                notification.Calls++;
                return Task.CompletedTask;
            }
        }

        public class MultiHandler : INotificationHandler<ScannedNotification>, INotificationHandler<OtherScannedNotification>
        {
            public Task Handle(ScannedNotification notification, CancellationToken cancellationToken)
            {
                notification.Calls++;
                return Task.CompletedTask;
            }

            public Task Handle(OtherScannedNotification notification, CancellationToken cancellationToken)
            {
                notification.Calls++;
                return Task.CompletedTask;
            }
        }

        public class ScopedNotification : INotification
        {
        }

        public class RequestScopedDependency
        {
            public bool Handled { get; set; }
        }

        public class ScopedNotificationHandler : INotificationHandler<ScopedNotification>
        {
            private readonly RequestScopedDependency dependency;

            public ScopedNotificationHandler(RequestScopedDependency dependency)
            {
                this.dependency = dependency;
            }

            public Task Handle(ScopedNotification notification, CancellationToken cancellationToken)
            {
                dependency.Handled = true;
                return Task.CompletedTask;
            }
        }

        public abstract class AbstractHandler : INotificationHandler<ScannedNotification>
        {
            public abstract Task Handle(ScannedNotification notification, CancellationToken cancellationToken);
        }

        public class OpenGenericHandler<T> : INotificationHandler<ScannedNotification>
        {
            public Task Handle(ScannedNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private static ServiceDescriptor[] GetHandlerDescriptors<TNotification>(IServiceCollection services) where TNotification : INotification =>
            services.Where(x => x.ServiceType == typeof(INotificationHandler<TNotification>)).ToArray();

        [Fact]
        public void AddNotificationHandlers_RegistersConcreteHandlersFromMarkerAssembly_AsTransient()
        {
            var services = new ServiceCollection();

            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));

            var descriptors = GetHandlerDescriptors<ScannedNotification>(services);
            Assert.Contains(descriptors, x => x.ImplementationType == typeof(ScannedHandler));
            Assert.Contains(descriptors, x => x.ImplementationType == typeof(MultiHandler));
            Assert.All(descriptors, x => Assert.Equal(ServiceLifetime.Transient, x.Lifetime));
        }

        [Fact]
        public void AddNotificationHandlers_SkipsAbstractAndOpenGenericTypes()
        {
            var services = new ServiceCollection();

            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));

            var implementationTypes = GetHandlerDescriptors<ScannedNotification>(services).Select(x => x.ImplementationType).ToArray();
            Assert.DoesNotContain(typeof(AbstractHandler), implementationTypes);
            Assert.DoesNotContain(implementationTypes, x => x!.IsGenericType);
        }

        [Fact]
        public void AddNotificationHandlers_WithHandlerImplementingSeveralNotifications_RegistersEachInterface()
        {
            var services = new ServiceCollection();

            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));

            Assert.Contains(GetHandlerDescriptors<OtherScannedNotification>(services), x => x.ImplementationType == typeof(MultiHandler));
        }

        [Fact]
        public void AddNotificationHandlers_CalledTwice_DoesNotDuplicateRegistrations()
        {
            var services = new ServiceCollection();

            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));
            var countAfterFirstCall = GetHandlerDescriptors<ScannedNotification>(services).Length;
            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests), typeof(MessageHandlerServiceCollectionExtensionsTests));

            Assert.Equal(countAfterFirstCall, GetHandlerDescriptors<ScannedNotification>(services).Length);
            Assert.Single(services, x => x.ServiceType == typeof(INotificationPublisher));
        }

        [Fact]
        public void AddNotificationHandlers_RegistersPublisherAsTransient()
        {
            var services = new ServiceCollection();

            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));

            var descriptor = Assert.Single(services, x => x.ServiceType == typeof(INotificationPublisher));
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
            Assert.Equal(typeof(NotificationPublisher), descriptor.ImplementationType);
        }

        [Fact]
        public async Task Publish_FromScope_ResolvesHandlerDependenciesFromThatScope()
        {
            var services = new ServiceCollection().AddScoped<RequestScopedDependency>();
            services.AddNotificationHandler<ScopedNotification, ScopedNotificationHandler>();
            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            using var scope = provider.CreateScope();

            await scope.ServiceProvider.GetRequiredService<INotificationPublisher>().Publish(new ScopedNotification());

            Assert.True(scope.ServiceProvider.GetRequiredService<RequestScopedDependency>().Handled);
        }

        [Fact]
        public void AddNotificationPublisher_WithCustomPublisherAlreadyRegistered_KeepsCustomPublisher()
        {
            var services = new ServiceCollection();
            services.AddSingleton<INotificationPublisher, CustomPublisher>();

            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));

            using var provider = services.BuildServiceProvider();
            Assert.IsType<CustomPublisher>(provider.GetRequiredService<INotificationPublisher>());
        }

        [Fact]
        public async Task AddNotificationHandlers_ThenPublish_InvokesEachScannedHandlerOnce()
        {
            var services = new ServiceCollection();
            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));
            services.AddNotificationHandlers(typeof(MessageHandlerServiceCollectionExtensionsTests));
            using var provider = services.BuildServiceProvider();
            var notification = new ScannedNotification();

            await provider.GetRequiredService<INotificationPublisher>().Publish(notification);

            Assert.Equal(2, notification.Calls); // ScannedHandler + MultiHandler.
        }

        [Fact]
        public async Task AddNotificationHandler_RegistersHandlerAndPublisher()
        {
            var services = new ServiceCollection();
            services.AddNotificationHandler<OtherScannedNotification, MultiHandler>();
            using var provider = services.BuildServiceProvider();
            var notification = new OtherScannedNotification();

            await provider.GetRequiredService<INotificationPublisher>().Publish(notification);

            Assert.Equal(1, notification.Calls);
        }

        private class CustomPublisher : INotificationPublisher
        {
            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        }
    }
}
