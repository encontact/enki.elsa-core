using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.MassTransit
{
    public abstract class MassTransitBuilderBase<TOptions> : IMassTransitBuilder<TOptions> where TOptions : class
    {
        protected MassTransitBuilderBase() { }

        public IServiceCollection Build(IServiceCollection services)
        {
            var optionsBuilder = services.AddOptions<TOptions>();
            Options?.Invoke(optionsBuilder);

            services.AddMassTransit(configurator =>
            {
                ConfigureMassTransit(configurator);
                ConfigureTransport(configurator);
            });

            return services;
        }

        protected abstract void ConfigureMassTransit(IBusRegistrationConfigurator configurator);

        // Neither this base class nor the Default*MassTransitBuilder implementations call
        // cfg.UseMessageScheduler(address) inside this method. Without it, IMessageScheduler (used by
        // ScheduleSendMassTransitMessage/CancelScheduledMassTransitMessage) has nothing configured to route
        // scheduled messages to. A builder that wants scheduling must call UseMessageScheduler itself, inside
        // its UsingRabbitMq/UsingAzureServiceBus callback here - this was also true of the pre-MassTransit-9
        // code (only the old sample builders did it, never the Default* builders).
        protected abstract void ConfigureTransport(IBusRegistrationConfigurator configurator);

        public Action<OptionsBuilder<TOptions>> Options { get; set; }
    }
}