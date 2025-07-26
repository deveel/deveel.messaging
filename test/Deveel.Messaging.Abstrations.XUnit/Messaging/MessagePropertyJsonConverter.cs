using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deveel.Messaging;

/// <summary>
/// Custom JSON converter for MessageProperty that handles serialization and deserialization
/// with proper type conversion for JsonElement values.
/// </summary>
public class MessagePropertyJsonConverter : JsonConverter<MessageProperty>
{
    public override MessageProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string? name = null;
        object? value = null;
        bool isSensitive = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName.ToLowerInvariant())
            {
                case "name":
                    name = reader.GetString();
                    break;
                case "value":
                    value = ReadValue(ref reader, options);
                    break;
                case "issensitive":
                    isSensitive = reader.GetBoolean();
                    break;
                default:
                    // Skip unknown properties
                    reader.Skip();
                    break;
            }
        }

        return new MessageProperty(name ?? "", value)
        {
            IsSensitive = isSensitive
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageProperty value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString("name", value.Name);
        
        writer.WritePropertyName("value");
        JsonSerializer.Serialize(writer, value.Value, value.Value?.GetType() ?? typeof(object), options);
        
        writer.WriteBoolean("isSensitive", value.IsSensitive);
        
        writer.WriteEndObject();
    }

    private static object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt32(out int intValue) ? intValue :
                                   reader.TryGetInt64(out long longValue) ? longValue :
                                   reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize<object[]>(ref reader, options),
            _ => throw new JsonException($"Unsupported token type: {reader.TokenType}")
        };
    }
}

/// <summary>
/// Custom JSON converter for System.Object that converts JsonElement values to primitive types.
/// This handles the case where JSON deserialization creates JsonElement objects instead of primitive types.
/// </summary>
public class ObjectJsonConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadValue(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
    }

    private static object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt32(out int intValue) ? intValue :
                                   reader.TryGetInt64(out long longValue) ? longValue :
                                   reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize<object[]>(ref reader, options),
            _ => throw new JsonException($"Unsupported token type: {reader.TokenType}")
        };
    }
}