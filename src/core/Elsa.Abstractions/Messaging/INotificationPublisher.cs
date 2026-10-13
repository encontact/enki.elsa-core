using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Messaging
{
    /// <summary>
    /// Publishes notifications to every registered <see cref="INotificationHandler{TNotification}"/>.
    /// </summary>
    public interface INotificationPublisher
    {
        /// <summary>
        /// Invokes the handlers sequentially, in registration order. Each handler type is invoked at most once,
        /// and the first exception thrown by a handler stops the publication and is propagated to the caller.
        /// </summary>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
    }
}
