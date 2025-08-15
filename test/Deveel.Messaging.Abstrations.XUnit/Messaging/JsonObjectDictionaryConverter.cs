using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deveel.Messaging;

/// <summary>
/// Custom JSON converter for Dictionary<string, object> that converts JsonElement values to primitive types.
/// </summary>
public class JsonObjectDictionaryConverter : JsonConverter<IDictionary<string, object?>>
{
    public override IDictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        var dictionary = new Dictionary<string, object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dictionary;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            string propertyName = reader.GetString()!;
            reader.Read();

            dictionary[propertyName] = ReadValue(ref reader, options);
        }

        throw new JsonException("Expected EndObject token");
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<string, object?> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, kvp.Value?.GetType() ?? typeof(object), options);
        }

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