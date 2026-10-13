using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Core.Tests.Messaging
{
    public class NotificationPublisherTests
    {
        private class TestNotification : INotification
        {
            public List<string> Calls { get; } = new List<string>();
            public CancellationToken? ReceivedToken { get; set; }
        }

        private class OtherNotification : INotification
        {
        }

        private class FirstHandler : INotificationHandler<TestNotification>
        {
            public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                notification.Calls.Add(nameof(FirstHandler));
                notification.ReceivedToken = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private class SecondHandler : INotificationHandler<TestNotification>
        {
            public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                notification.Calls.Add(nameof(SecondHandler));
                return Task.CompletedTask;
            }
        }

        private class SlowHandler : INotificationHandler<TestNotification>
        {
            public async Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                await Task.Delay(50, cancellationToken);
                notification.Calls.Add(nameof(SlowHandler));
            }
        }

        private class ThrowingHandler : INotificationHandler<TestNotification>
        {
            public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                notification.Calls.Add(nameof(ThrowingHandler));
                throw new InvalidOperationException("handler failure");
            }
        }

        private class OtherNotificationHandler : INotificationHandler<OtherNotification>
        {
            public static int Calls;

            public Task Handle(OtherNotification notification, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref Calls);
                return Task.CompletedTask;
            }
        }

        private static INotificationPublisher CreatePublisher(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            return new NotificationPublisher(services.BuildServiceProvider());
        }

        [Fact]
        public async Task Publish_WithMultipleHandlers_InvokesAllInRegistrationOrder()
        {
            var publisher = CreatePublisher(services => services
                .AddTransient<INotificationHandler<TestNotification>, SecondHandler>()
                .AddTransient<INotificationHandler<TestNotification>, FirstHandler>());
            var notification = new TestNotification();

            await publisher.Publish(notification);

            Assert.Equal(new[] { nameof(SecondHandler), nameof(FirstHandler) }, notification.Calls);
        }

        [Fact]
        public async Task Publish_WithAsyncHandler_AwaitsEachHandlerBeforeInvokingTheNext()
        {
            var publisher = CreatePublisher(services => services
                .AddTransient<INotificationHandler<TestNotification>, SlowHandler>()
                .AddTransient<INotificationHandler<TestNotification>, SecondHandler>());
            var notification = new TestNotification();

            await publisher.Publish(notification);

            Assert.Equal(new[] { nameof(SlowHandler), nameof(SecondHandler) }, notification.Calls);
        }

        [Fact]
        public async Task Publish_WhenHandlerThrows_PropagatesExceptionAndSkipsRemainingHandlers()
        {
            var publisher = CreatePublisher(services => services
                .AddTransient<INotificationHandler<TestNotification>, FirstHandler>()
                .AddTransient<INotificationHandler<TestNotification>, ThrowingHandler>()
                .AddTransient<INotificationHandler<TestNotification>, SecondHandler>());
            var notification = new TestNotification();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.Publish(notification));

            Assert.Equal("handler failure", exception.Message);
            Assert.Equal(new[] { nameof(FirstHandler), nameof(ThrowingHandler) }, notification.Calls);
        }

        [Fact]
        public async Task Publish_WithoutHandlers_CompletesWithoutError()
        {
            var publisher = CreatePublisher(services => { });

            await publisher.Publish(new TestNotification());
        }

        [Fact]
        public async Task Publish_WithNullNotification_ThrowsArgumentNullException()
        {
            var publisher = CreatePublisher(services => { });

            await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.Publish<TestNotification>(null!));
        }

        [Fact]
        public async Task Publish_WithCancellationToken_PassesTokenToHandlers()
        {
            var publisher = CreatePublisher(services => services.AddTransient<INotificationHandler<TestNotification>, FirstHandler>());
            var notification = new TestNotification();
            using var cancellationTokenSource = new CancellationTokenSource();

            await publisher.Publish(notification, cancellationTokenSource.Token);

            Assert.Equal(cancellationTokenSource.Token, notification.ReceivedToken);
        }

        [Fact]
        public async Task Publish_WithSameHandlerTypeRegisteredTwice_InvokesHandlerOnce()
        {
            var publisher = CreatePublisher(services => services
                .AddTransient<INotificationHandler<TestNotification>, FirstHandler>()
                .AddTransient<INotificationHandler<TestNotification>, SecondHandler>()
                .AddTransient<INotificationHandler<TestNotification>, FirstHandler>());
            var notification = new TestNotification();

            await publisher.Publish(notification);

            Assert.Equal(new[] { nameof(FirstHandler), nameof(SecondHandler) }, notification.Calls);
        }

        [Fact]
        public async Task Publish_WithHandlersForOtherNotification_DoesNotInvokeThem()
        {
            var publisher = CreatePublisher(services => services
                .AddTransient<INotificationHandler<TestNotification>, FirstHandler>()
                .AddTransient<INotificationHandler<OtherNotification>, OtherNotificationHandler>());
            var callsBefore = OtherNotificationHandler.Calls;

            await publisher.Publish(new TestNotification());

            Assert.Equal(callsBefore, OtherNotificationHandler.Calls);
        }
    }
}
