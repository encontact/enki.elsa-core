using Elsa.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elsa.Core.Tests.Extensions
{
    public class MapperServiceCollectionExtensionsTests
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
        public void AddMapperProfile_RegistersResolvableMapper_ThatUsesRegisteredProfile()
        {
            var services = new ServiceCollection();
            services.AddSingleton(NullLoggerFactory.Instance as Microsoft.Extensions.Logging.ILoggerFactory);
            services.AddMapperProfile<TestProfile>(ServiceLifetime.Singleton);

            using var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            var result = mapper.Map<Source, Destination>(new Source { Name = "hello" });

            Assert.Equal("hello", result.Name);
        }

        [Fact]
        public void AddMapperProfile_WithoutRegisteredLoggerFactory_StillResolvesMapper()
        {
            var services = new ServiceCollection();
            services.AddMapperProfile<TestProfile>(ServiceLifetime.Singleton);

            using var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            var result = mapper.Map<Source, Destination>(new Source { Name = "hello" });

            Assert.Equal("hello", result.Name);
        }

        [Fact]
        public void AddMapperProfile_RegistersSingleMapperInstance()
        {
            var services = new ServiceCollection();
            services.AddMapperProfile<TestProfile>(ServiceLifetime.Singleton);

            using var provider = services.BuildServiceProvider();
            var mapper1 = provider.GetRequiredService<IMapper>();
            var mapper2 = provider.GetRequiredService<IMapper>();

            Assert.Same(mapper1, mapper2);
        }
    }
}
