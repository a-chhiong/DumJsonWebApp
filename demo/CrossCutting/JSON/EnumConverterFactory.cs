using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrossCutting.Extensions;

namespace CrossCutting.JSON;

public class EnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var t = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return t.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        
        // If the enum has JsonStringEnumConverter attribute, use that
        var hasStringEnumConverter = enumType
            .GetCustomAttributes(typeof(JsonConverterAttribute), false)
            .OfType<JsonConverterAttribute>()
            .Any(attr => attr.ConverterType == typeof(JsonStringEnumConverter));

        if (Nullable.GetUnderlyingType(typeToConvert) != null)
        {
            // Nullable enum → use NullableEnumConverter<TEnum>
            if (hasStringEnumConverter)
            {
                var converterType = typeof(NullableStringEnumConverter<>).MakeGenericType(enumType);
                return (JsonConverter)Activator.CreateInstance(converterType)!;
            }
            else
            {
                var converterType = typeof(NullableEnumConverter<>).MakeGenericType(enumType);
                return (JsonConverter)Activator.CreateInstance(converterType)!;
            }
        }
        else
        {
            // Non-nullable enum → use EnumConverter<TEnum>
            if (hasStringEnumConverter)
            {
                var nonNullType = typeof(StringEnumConverter<>).MakeGenericType(enumType);
                return (JsonConverter)Activator.CreateInstance(nonNullType)!;
            }
            else
            {   
                var nonNullType = typeof(EnumConverter<>).MakeGenericType(enumType);
                return (JsonConverter)Activator.CreateInstance(nonNullType)!;
            }
        }
    }

    // -----------------------
    // Non-nullable enum converter
    // -----------------------
    
    private class EnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                {
                    var raw = reader.GetInt32();
                    return (TEnum)Enum.ToObject(typeof(TEnum), raw);
                }
                case JsonTokenType.String when
                    int.TryParse(reader.GetString(), out var val):
                    return (TEnum)Enum.ToObject(typeof(TEnum), val);
                default:
                    throw new JsonException($"Invalid value for enum {typeof(TEnum).Name}");
            }
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt32(value));
        }
    }
    
    private class StringEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();

            // Try to match EnumMemberAttribute first
            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                if (attr?.Value == str)
                    return (TEnum)field.GetValue(null)!;
            }

            // Fallback to enum name
            return Enum.TryParse<TEnum>(str, ignoreCase: true, out var parsed) ? parsed 
                : throw new JsonException($"Invalid value '{str}' for enum {typeof(TEnum).Name}");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.GetString());
        }
    }
    
    // -----------------------
    // Nullable enum converter
    // -----------------------
    
    private class NullableEnumConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                {
                    var raw = reader.GetInt32();
                    return (TEnum)Enum.ToObject(typeof(TEnum), raw);
                }
                case JsonTokenType.String when
                    int.TryParse(reader.GetString(), out var val):
                    return (TEnum)Enum.ToObject(typeof(TEnum), val);
                default:
                    return null; // fallback
            }
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteNumberValue(Convert.ToInt32(value.Value));
        }
    }
    
    private class NullableStringEnumConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {   
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var str = reader.GetString();

            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                if (attr?.Value == str)
                    return (TEnum)field.GetValue(null)!;
            }

            return Enum.TryParse<TEnum>(str, ignoreCase: true, out var parsed) ? parsed 
                : throw new JsonException($"Invalid value '{str}' for enum {typeof(TEnum).Name}");
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }
            
            writer.WriteStringValue(value.GetString());
        }
    }
}