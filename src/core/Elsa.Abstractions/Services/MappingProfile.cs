using System;
using System.Collections.Generic;

namespace Elsa.Services
{
    /// <summary>
    /// Base class for registering plain-C# mapping functions between a source and destination type.
    /// Replaces the former AutoMapper <c>Profile</c> base class; see GHSA-rvv3-g6hj-g44x.
    /// </summary>
    public abstract class MappingProfile
    {
        private readonly List<MapRegistration> registrations = new List<MapRegistration>();

        /// <summary>
        /// Registers how to map from <typeparamref name="TSource"/> to a new <typeparamref name="TDestination"/>,
        /// and, optionally, how to map into an existing <typeparamref name="TDestination"/> instance.
        /// </summary>
        protected void CreateMap<TSource, TDestination>(
            Func<TSource, TDestination> map,
            Func<TSource, TDestination, TDestination> mapInto = null)
        {
            registrations.Add(new MapRegistration(
                typeof(TSource),
                typeof(TDestination),
                source => map((TSource) source),
                mapInto == null ? (Func<object, object, object>) null : (source, destination) => mapInto((TSource) source, (TDestination) destination)));
        }

        public IReadOnlyList<MapRegistration> Registrations => registrations;
    }

    /// <summary>
    /// A single source-to-destination mapping registered by a <see cref="MappingProfile"/>.
    /// </summary>
    public sealed class MapRegistration
    {
        public MapRegistration(Type sourceType, Type destinationType, Func<object, object> map, Func<object, object, object> mapInto)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            Map = map;
            MapInto = mapInto;
        }

        public Type SourceType { get; }
        public Type DestinationType { get; }
        public Func<object, object> Map { get; }
        public Func<object, object, object> MapInto { get; }
    }
}
