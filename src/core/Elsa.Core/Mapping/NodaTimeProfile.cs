using System;
using Elsa.Services;
using NodaTime;

namespace Elsa.Mapping
{
    public class NodaTimeProfile : MappingProfile
    {
        public NodaTimeProfile()
        {
            CreateMap<Instant, DateTime>(ToDateTime);
            CreateMap<Instant?, DateTime?>(ToNullableDateTime);
            CreateMap<DateTime, Instant>(ToInstant);
            CreateMap<DateTime?, Instant?>(ToNullableInstant);
        }

        public static DateTime ToDateTime(Instant source) => source.ToDateTimeUtc();

        public static DateTime? ToNullableDateTime(Instant? source) => source?.ToDateTimeUtc();

        public static Instant ToInstant(DateTime source)
        {
            var utcDateTime = source.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(source, DateTimeKind.Utc)
                : source.ToUniversalTime();

            return Instant.FromDateTimeUtc(utcDateTime);
        }

        public static Instant? ToNullableInstant(DateTime? source) => source != null ? ToInstant(source.Value) : default(Instant?);
    }
}
