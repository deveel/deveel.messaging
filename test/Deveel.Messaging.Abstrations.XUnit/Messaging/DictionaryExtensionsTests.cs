using System.Text.Json;

namespace Deveel.Messaging;

public class DictionaryExtensionsTests
{
    [Fact]
    public void TryGetValue_ExistingKeyWithCorrectType_ReturnsTrue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "stringKey", "stringValue" },
            { "intKey", 42 }
        };

        // Act
        var stringResult = dictionary.TryGetValue<string>("stringKey", out var stringValue);
        var intResult = dictionary.TryGetValue<int>("intKey", out var intValue);

        // Assert
        Assert.True(stringResult);
        Assert.Equal("stringValue", stringValue);
        Assert.True(intResult);
        Assert.Equal(42, intValue);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>();

        // Act
        var result = dictionary.TryGetValue<string>("nonExistingKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_ExistingKeyWithWrongType_ReturnsFalse()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "stringKey", "stringValue" }
        };

        // Act
        var result = dictionary.TryGetValue<int>("stringKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetValue_EnumValue_ConvertsFromString()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "statusKey", "PlainText" }
        };

        // Act
        var result = dictionary.TryGetValue<MessageContentType>("statusKey", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(MessageContentType.PlainText, value);
    }

    [Fact]
    public void TryGetValue_ConvertibleValue_ConvertsType()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "doubleKey", 42 } // int to double conversion
        };

        // Act
        var result = dictionary.TryGetValue<double>("doubleKey", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(42.0, value);
    }

    [Fact]
    public void TryGetValue_NullableType_HandlesConversion()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "nullableIntKey", 42 }
        };

        // Act
        var result = dictionary.TryGetValue<int?>("nullableIntKey", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_JsonElement_DeserializesCorrectly()
    {
        // Arrange
        var jsonString = "\"test value\"";
        var jsonDocument = JsonDocument.Parse(jsonString);
        var dictionary = new Dictionary<string, object>
        {
            { "jsonKey", jsonDocument.RootElement }
        };

        // Act
        var result = dictionary.TryGetValue<string>("jsonKey", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("test value", value);
        
        // Cleanup
        jsonDocument.Dispose();
    }

    [Fact]
    public void Merge_BothDictionariesNull_ReturnsEmptyDictionary()
    {
        // Arrange
        IDictionary<string, object>? dict1 = null;
        IDictionary<string, object>? dict2 = null;

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Merge_FirstDictionaryNull_ReturnsSecondDictionary()
    {
        // Arrange
        IDictionary<string, object>? dict1 = null;
        var dict2 = new Dictionary<string, object> { { "key1", "value1" } };

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("value1", result["key1"]);
        Assert.NotSame(dict2, result); // Should be a copy
    }

    [Fact]
    public void Merge_SecondDictionaryNull_ReturnsFirstDictionary()
    {
        // Arrange
        var dict1 = new Dictionary<string, object> { { "key1", "value1" } };
        IDictionary<string, object>? dict2 = null;

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("value1", result["key1"]);
        Assert.NotSame(dict1, result); // Should be a copy
    }

    [Fact]
    public void Merge_TwoDictionaries_MergesCorrectly()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var dict2 = new Dictionary<string, object>
        {
            { "key2", "newValue2" }, // Override existing
            { "key3", "value3" }     // Add new
        };

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("value1", result["key1"]); // From first dictionary
        Assert.Equal("newValue2", result["key2"]); // Overridden by second dictionary
        Assert.Equal("value3", result["key3"]); // From second dictionary
    }

    [Fact]
    public void Merge_SecondDictionaryHasNullValue_RemovesKey()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var dict2 = new Dictionary<string, object>
        {
            { "key2", null! } // Remove this key
        };

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("value1", result["key1"]);
        Assert.DoesNotContain("key2", result.Keys);
    }

    [Fact]
    public void Merge_AddingNewKeysFromSecondDictionary_AddsCorrectly()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>
        {
            { "existingKey", "existingValue" }
        };
        var dict2 = new Dictionary<string, object>
        {
            { "newKey1", "newValue1" },
            { "newKey2", "newValue2" }
        };

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("existingValue", result["existingKey"]);
        Assert.Equal("newValue1", result["newKey1"]);
        Assert.Equal("newValue2", result["newKey2"]);
    }

    [Fact]
    public void Merge_EmptyDictionaries_ReturnsEmptyDictionary()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>();
        var dict2 = new Dictionary<string, object>();

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}