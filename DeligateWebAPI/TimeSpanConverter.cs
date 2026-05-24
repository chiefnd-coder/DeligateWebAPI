using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Try to parse the string as a TimeSpan
        if (reader.TokenType == JsonTokenType.String && TimeSpan.TryParse(reader.GetString(), out var timeSpan))
        {
            return timeSpan;
        }

        // Fallback if parsing fails
        throw new JsonException("Invalid TimeSpan format");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Write TimeSpan as string (ISO 8601 format or your preferred format)
        writer.WriteStringValue(value.ToString("c")); // "c" is the standard round-trip format for TimeSpan
    }
}
