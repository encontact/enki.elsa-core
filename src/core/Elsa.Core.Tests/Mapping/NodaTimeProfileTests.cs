using System;
using Elsa.Mapping;
using Elsa.Services;
using NodaTime;
using Xunit;

namespace Elsa.Core.Tests.Mapping
{
    public class NodaTimeProfileTests
    {
        private static IMapper CreateMapper() => new ManualMapper(new MappingProfile[] { new NodaTimeProfile() });

        [Fact]
        public void Map_InstantToDateTime_ReturnsEquivalentUtcDateTime()
        {
            var mapper = CreateMapper();
            var instant = Instant.FromUtc(2024, 3, 15, 10, 30, 0);

            var result = mapper.Map<DateTime>(instant);

            Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void Map_NullableInstantToNullableDateTime_WithValue_ReturnsEquivalentUtcDateTime()
        {
            var mapper = CreateMapper();
            Instant? instant = Instant.FromUtc(2024, 3, 15, 10, 30, 0);

            var result = mapper.Map<DateTime?>(instant);

            Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void Map_NullableInstantToNullableDateTime_WithNull_ReturnsNull()
        {
            var mapper = CreateMapper();
            Instant? instant = null;

            var result = mapper.Map<DateTime?>(instant);

            Assert.Null(result);
        }

        [Fact]
        public void Map_UtcDateTimeToInstant_ReturnsEquivalentInstant()
        {
            var mapper = CreateMapper();
            var dateTime = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);

            var result = mapper.Map<Instant>(dateTime);

            Assert.Equal(Instant.FromUtc(2024, 3, 15, 10, 30, 0), result);
        }

        [Fact]
        public void Map_UnspecifiedKindDateTimeToInstant_TreatsAsUtc()
        {
            var mapper = CreateMapper();
            var dateTime = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Unspecified);

            var result = mapper.Map<Instant>(dateTime);

            Assert.Equal(Instant.FromUtc(2024, 3, 15, 10, 30, 0), result);
        }

        [Fact]
        public void Map_LocalDateTimeToInstant_ConvertsToUniversalTime()
        {
            var mapper = CreateMapper();
            var localDateTime = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Local);

            var result = mapper.Map<Instant>(localDateTime);

            Assert.Equal(Instant.FromDateTimeUtc(localDateTime.ToUniversalTime()), result);
        }

        [Fact]
        public void Map_NullableDateTimeToNullableInstant_WithNull_ReturnsNull()
        {
            var mapper = CreateMapper();
            DateTime? dateTime = null;

            var result = mapper.Map<Instant?>(dateTime);

            Assert.Null(result);
        }
    }
}
