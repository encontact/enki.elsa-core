using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Elsa.Activities.MassTransit.Options;
using Elsa.Activities.MassTransit.Extensions;
using System.Collections.Generic;

namespace Elsa.Activities.MassTransit
{
    public sealed class DefaultAzureServiceBusMassTransitBuilder : MassTransitBuilderBase<AzureServiceBusOptions>
    {
        protected override void ConfigureTransport(IBusRegistrationConfigurator configurator)
        {
            configurator.UsingAzureServiceBus((context, bus) =>
            {
                var options = context.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;

                bus.Host(options.ConnectionString);

                foreach (var messageType in MessageTypes)
                {
                    var queueName = messageType.Name;
                    var consumerType = messageType.CreateConsumerType();

                    bus.ReceiveEndpoint(queueName, endpoint =>
                    {
                        endpoint.PrefetchCount = 16;
                        endpoint.ConfigureConsumer(context, consumerType);
                        messageType.MapEndpointConvention(endpoint.InputAddress);
                    });
                }
            });
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
        {
            foreach (var messageType in MessageTypes)
            {
                configurator.AddConsumer(messageType.CreateConsumerType());
            }
        }

        public IEnumerable<Type> MessageTypes { get; set; }
    }
}