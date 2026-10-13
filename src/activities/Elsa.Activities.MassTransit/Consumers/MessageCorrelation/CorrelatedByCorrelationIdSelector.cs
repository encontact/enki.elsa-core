using System;
using MassTransit;

namespace Elsa.Activities.MassTransit.Consumers.MessageCorrelation
{
    public class CorrelatedByCorrelationIdSelector<T>
        : ICorrelationIdSelector<T>
        where T : class
    {
        public bool TryGetCorrelationId(T message, out Guid? correlationId)
        {
            if (typeof(CorrelatedBy<Guid>).IsAssignableFrom(typeof(T)))
            {
                var correlatedBy = (CorrelatedBy<Guid>)message;
                correlationId = correlatedBy.CorrelationId;
                return true;
            }

            correlationId = null;
            return false;
        }
    }
}