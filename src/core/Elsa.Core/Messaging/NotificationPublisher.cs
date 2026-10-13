using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Messaging
{
    public class NotificationPublisher : INotificationPublisher
    {
        private readonly IServiceProvider serviceProvider;

        public NotificationPublisher(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
            var invokedHandlerTypes = new HashSet<Type>();

            foreach (var handler in handlers)
            {
                // The same handler type may be registered more than once (e.g. by assembly scanning and by explicit registration).
                if (!invokedHandlerTypes.Add(handler.GetType()))
                    continue;

                await handler.Handle(notification, cancellationToken);
            }
        }
    }
}
