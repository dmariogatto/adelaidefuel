using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Shared
{
    public class DateUtcJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(DateTime))
                throw new ArgumentException($"Unexpected type '{typeToConvert}'.");
            var dt = reader.GetDateTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var dt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            writer.WriteStringValue(dt);
        }
    }
}