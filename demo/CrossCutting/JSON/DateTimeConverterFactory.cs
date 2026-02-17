using System.Text.Json;
using System.Text.Json.Serialization;
using CrossCutting.Constants;

namespace CrossCutting.JSON;

public class DateTimeConverterFactory: JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(DateTime) 
           || typeToConvert == typeof(DateTime?)
           || typeToConvert == typeof(DateOnly) 
           || typeToConvert == typeof(DateOnly?)
           || typeToConvert == typeof(TimeOnly)
           || typeToConvert == typeof(TimeOnly?);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(DateTime))
            return new DateTimeConverter();

        if (typeToConvert == typeof(DateTime?))
            return new NullableDateTimeConverter();

        if (typeToConvert == typeof(DateOnly))
            return new DateOnlyConverter();
        
        if (typeToConvert == typeof(DateOnly?))
            return new NullableDateOnlyConverter();
        
        if (typeToConvert == typeof(TimeOnly))
            return new TimeOnlyConverter();
        
        if (typeToConvert == typeof(TimeOnly?))
            return new NullableTimeOnlyConverter();
        
        throw new NotSupportedException(typeToConvert.FullName);
    }
    
    // --------- Inner Converters ---------

    private class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTime.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(Format(value));
    }

    private class DateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) 
            => DateOnly.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) 
            => writer.WriteStringValue(Format(value));
    }

    private class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeOnly.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(Format(value));
    }

    private class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (string.IsNullOrWhiteSpace(reader.GetString()))
                return null;

            return DateTime.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value is null ? null : Format(value.Value));
    }

    private class NullableDateOnlyConverter : JsonConverter<DateOnly?>
    {
        public override DateOnly? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (string.IsNullOrWhiteSpace(reader.GetString()))
                return null;

            return DateOnly.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value is null ? null : Format(value.Value));
    }
    
    private class NullableTimeOnlyConverter : JsonConverter<TimeOnly?>
    {
        public override TimeOnly? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (string.IsNullOrWhiteSpace(reader.GetString()))
                return null;

            return TimeOnly.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value is null ? null : Format(value.Value));
    }

    private static string Format(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc =>
                value.ToUniversalTime().ToString(DateTimeFormats.ISO_8601),

            DateTimeKind.Local =>
                value.ToLocalTime().ToString(DateTimeFormats.LOCAL_DATE_TIME),

            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(value, DateTimeKind.Local)
                    .ToString(DateTimeFormats.LOCAL_DATE_TIME),

            _ => value.ToString("o")
        };

    private static string Format(DateOnly value)
        => value.ToString(DateTimeFormats.LOCAL_DATE);
    
    private static string Format(TimeOnly value)
        => value.ToString(DateTimeFormats.LOCAL_TIME);
}