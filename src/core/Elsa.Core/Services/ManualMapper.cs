using System;
using System.Collections;
using System.Collections.Generic;

namespace Elsa.Services
{
    /// <summary>
    /// An <see cref="IMapper"/> implementation that dispatches to the plain-C# functions registered via
    /// <see cref="MappingProfile.CreateMap{TSource,TDestination}"/>. <see cref="Map{TDestination}"/> additionally
    /// understands <see cref="IEnumerable{T}"/>/<see cref="ICollection{T}"/> destinations, mapping each element
    /// through its own registered scalar mapping, so collection call sites keep working unchanged.
    /// Replaces the former AutoMapper-based implementation; see GHSA-rvv3-g6hj-g44x.
    /// </summary>
    public class ManualMapper : IMapper
    {
        private readonly Dictionary<(Type Source, Type Destination), MapRegistration> registrations;

        public ManualMapper(IEnumerable<MappingProfile> profiles)
        {
            registrations = new Dictionary<(Type, Type), MapRegistration>();

            foreach (var profile in profiles)
            foreach (var registration in profile.Registrations)
                registrations[(registration.SourceType, registration.DestinationType)] = registration;
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source == null)
                return default;

            return (TDestination) MapObject(typeof(TSource), typeof(TDestination), source);
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null)
                return destination;

            var registration = GetRegistration(typeof(TSource), typeof(TDestination));

            if (registration.MapInto == null)
                throw new InvalidOperationException($"No in-place mapping registered from {typeof(TSource)} to {typeof(TDestination)}.");

            return (TDestination) registration.MapInto(source, destination);
        }

        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
                return default;

            return (TDestination) MapObject(source.GetType(), typeof(TDestination), source);
        }

        private object MapObject(Type sourceType, Type destinationType, object source)
        {
            if (TryGetRegistration(sourceType, destinationType, out var registration))
                return registration.Map(source);

            if (TryMapCollection(destinationType, source, out var mapped))
                return mapped;

            throw new InvalidOperationException($"No mapping registered from {sourceType} to {destinationType}.");
        }

        // A non-null Nullable<T> boxes as a plain boxed T, so a runtime-resolved source type (via
        // source.GetType()) never comes back as Nullable<T>. When the caller asks for a Nullable<T>
        // destination, fall back to the registration for the underlying T: the unboxing conversion to
        // Nullable<T> on the way out (via the (TDestination) cast in the Map overloads) handles the rest.
        private bool TryGetRegistration(Type sourceType, Type destinationType, out MapRegistration registration)
        {
            if (registrations.TryGetValue((sourceType, destinationType), out registration))
                return true;

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);

            return underlyingDestinationType != null && registrations.TryGetValue((sourceType, underlyingDestinationType), out registration);
        }

        private bool TryMapCollection(Type destinationType, object source, out object result)
        {
            result = null;

            if (source is string || !(source is IEnumerable sourceEnumerable))
                return false;

            var destinationElementType = GetEnumerableElementType(destinationType);

            if (destinationElementType == null)
                return false;

            var list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(destinationElementType));

            foreach (var item in sourceEnumerable)
                list.Add(item == null ? null : MapObject(item.GetType(), destinationElementType, item));

            result = list;
            return true;
        }

        private static Type GetEnumerableElementType(Type type)
        {
            if (!type.IsGenericType)
                return null;

            var definition = type.GetGenericTypeDefinition();

            return definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>) || definition == typeof(IList<>) || definition == typeof(List<>)
                ? type.GetGenericArguments()[0]
                : null;
        }

        private MapRegistration GetRegistration(Type sourceType, Type destinationType)
        {
            if (!registrations.TryGetValue((sourceType, destinationType), out var registration))
                throw new InvalidOperationException($"No mapping registered from {sourceType} to {destinationType}.");

            return registration;
        }
    }
}
