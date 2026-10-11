using System.Collections.Generic;
using AutoMapper;
using Elsa.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elsa.Core.Tests.Services
{
    public class AutoMapperMapperTests
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
            public TestProfile() => CreateMap<Source, Destination>();
        }

        [Fact]
        public void Map_GenericSourceAndDestination_MapsMatchingProperties()
        {
            var mapper = new AutoMapperMapper(new[] { new TestProfile() }, NullLoggerFactory.Instance);

            var result = mapper.Map<Source, Destination>(new Source { Name = "hello" });

            Assert.Equal("hello", result.Name);
        }

        [Fact]
        public void Map_WithExistingDestinationInstance_UpdatesInPlace()
        {
            var mapper = new AutoMapperMapper(new[] { new TestProfile() }, NullLoggerFactory.Instance);
            var destination = new Destination { Name = "old" };

            var result = mapper.Map(new Source { Name = "new" }, destination);

            Assert.Same(destination, result);
            Assert.Equal("new", result.Name);
        }

        [Fact]
        public void Map_SourceAsObject_ResolvesSourceTypeAtRuntime()
        {
            var mapper = new AutoMapperMapper(new[] { new TestProfile() }, NullLoggerFactory.Instance);
            object source = new Source { Name = "hello" };

            var result = mapper.Map<Destination>(source);

            Assert.Equal("hello", result.Name);
        }

        [Fact]
        public void Constructor_WithoutExplicitLoggerFactory_DoesNotThrow()
        {
            var mapper = new AutoMapperMapper(new List<MappingProfile> { new TestProfile() });

            var result = mapper.Map<Source, Destination>(new Source { Name = "hello" });

            Assert.Equal("hello", result.Name);
        }
    }
}
