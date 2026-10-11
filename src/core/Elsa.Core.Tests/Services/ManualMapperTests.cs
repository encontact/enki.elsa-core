using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Xunit;

namespace Elsa.Core.Tests.Services
{
    public class ManualMapperTests
    {
        private class Source
        {
            public string Name { get; set; } = "";
        }

        private class Destination
        {
            public string Name { get; set; } = "";
        }

        private class TestProfile : MappingProfile
        {
            public TestProfile() => CreateMap<Source, Destination>(
                source => new Destination { Name = source.Name },
                (source, destination) =>
                {
                    destination.Name = source.Name;
                    return destination;
                });
        }

        private class NullableProfile : MappingProfile
        {
            public NullableProfile() => CreateMap<int, int?>(source => source);
        }

        [Fact]
        public void Map_GenericSourceAndDestination_MapsMatchingProperties()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });

            var result = mapper.Map<Source, Destination>(new Source { Name = "hello" });

            Assert.Equal("hello", result.Name);
        }

        [Fact]
        public void Map_WithExistingDestinationInstance_UpdatesInPlace()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });
            var destination = new Destination { Name = "old" };

            var result = mapper.Map(new Source { Name = "new" }, destination);

            Assert.Same(destination, result);
            Assert.Equal("new", result.Name);
        }

        [Fact]
        public void Map_SourceAsObject_ResolvesSourceTypeAtRuntime()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });
            object source = new Source { Name = "hello" };

            var result = mapper.Map<Destination>(source);

            Assert.Equal("hello", result.Name);
        }

        [Fact]
        public void Map_NullSource_ReturnsDefaultWithoutThrowing()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });

            var result = mapper.Map<Source, Destination>(null);

            Assert.Null(result);
        }

        [Fact]
        public void Map_GenericDestinationAsEnumerable_MapsEachElement()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });
            var sources = new List<Source> { new Source { Name = "a" }, new Source { Name = "b" } };

            var result = mapper.Map<IEnumerable<Destination>>(sources);

            Assert.Equal(new[] { "a", "b" }, result.Select(x => x.Name));
        }

        [Fact]
        public void Map_GenericDestinationAsCollection_MapsEachElement()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });
            var sources = new List<Source> { new Source { Name = "a" } };

            var result = mapper.Map<ICollection<Destination>>(sources);

            Assert.Single(result);
            Assert.Equal("a", result.First().Name);
        }

        [Fact]
        public void Map_BoxedValueTypeToNullableDestination_FallsBackToUnderlyingTypeMapping()
        {
            // A non-null Nullable<T> boxes as a plain T, so source.GetType() never resolves to Nullable<T>.
            // ManualMapper must fall back to the registration for the underlying type.
            var mapper = new ManualMapper(new[] { new NullableProfile() });
            object source = 42;

            var result = mapper.Map<int?>(source);

            Assert.Equal(42, result);
        }

        [Fact]
        public void Map_NoRegistrationForPair_ThrowsInvalidOperationException()
        {
            var mapper = new ManualMapper(new[] { new TestProfile() });

            Assert.Throws<InvalidOperationException>(() => mapper.Map<Destination, Source>(new Destination()));
        }
    }
}
