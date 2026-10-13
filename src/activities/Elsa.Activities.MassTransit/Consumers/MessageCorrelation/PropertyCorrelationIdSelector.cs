using System;

namespace Elsa.Activities.MassTransit.Consumers.MessageCorrelation
{
    public class PropertyCorrelationIdSelector<T> :
        ICorrelationIdSelector<T>
        where T : class
    {
        readonly string _propertyName;

        public PropertyCorrelationIdSelector(string propertyName)
        {
            _propertyName = propertyName;
        }

        public bool TryGetCorrelationId(T message, out Guid? correlationId)
        {
            var propertyInfo = typeof(T).GetProperty(_propertyName);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(Guid))
            {
                correlationId = (Guid)propertyInfo.GetValue(message);
                return true;
            }

            if (propertyInfo != null && propertyInfo.PropertyType == typeof(Guid?))
            {
                correlationId = (Guid?)propertyInfo.GetValue(message);
                return true;
            }

            correlationId = null;
            return false;
        }
    }
}