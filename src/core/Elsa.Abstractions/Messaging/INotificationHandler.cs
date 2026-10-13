using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Messaging
{
    /// <summary>
    /// Handles notifications of type <typeparamref name="TNotification"/>.
    /// </summary>
    public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
