using System;
using System.Linq;
using System.Reflection;
using Elsa.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions
{
    public static class MessageHandlerServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationPublisher(this IServiceCollection services)
        {
            services.TryAddSingleton<INotificationPublisher, NotificationPublisher>();
            return services;
        }

        public static IServiceCollection AddNotificationHandler<T, THandler>(this IServiceCollection services)
            where T : INotification
            where THandler : INotificationHandler<T>
        {
            return services
                .AddNotificationPublisher()
                .AddTransient(typeof(INotificationHandler<T>), typeof(THandler));
        }

        /// <summary>
        /// Registers the notification publisher and every concrete <see cref="INotificationHandler{TNotification}"/> found in the assemblies of the specified marker types.
        /// </summary>
        public static IServiceCollection AddNotificationHandlers(this IServiceCollection services, params Type[] markerTypes)
        {
            services.AddNotificationPublisher();

            var handlerTypes = markerTypes
                .Select(x => x.GetTypeInfo().Assembly)
                .Distinct()
                .SelectMany(x => x.DefinedTypes)
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition);

            foreach (var handlerType in handlerTypes)
            {
                var handlerInterfaces = handlerType.ImplementedInterfaces
                    .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

                foreach (var handlerInterface in handlerInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Transient(handlerInterface, handlerType));
            }

            return services;
        }
    }
}
