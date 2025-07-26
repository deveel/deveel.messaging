namespace Deveel.Messaging;

public class JsonContentTests
{
    [Fact]
    public void JsonContent_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var content = new JsonContent();

        // Assert
        Assert.Equal("", content.Json);
        Assert.Equal(MessageContentType.Json, content.ContentType);
    }

    [Fact]
    public void JsonContent_ConstructorWithJsonString_SetsProperty()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}";

        // Act
        var content = new JsonContent(json);

        // Assert
        Assert.Equal(json, content.Json);
        Assert.Equal(MessageContentType.Json, content.ContentType);
    }

    [Fact]
    public void JsonContent_ConstructorWithIJsonContent_CopiesProperty()
    {
        // Arrange
        var sourceContent = new JsonContent("{\"source\":\"data\"}");

        // Act
        var content = new JsonContent(sourceContent);

        // Assert
        Assert.Equal("{\"source\":\"data\"}", content.Json);
        Assert.Equal(MessageContentType.Json, content.ContentType);
    }

    [Fact]
    public void JsonContent_ConstructorWithNullIJsonContent_SetsEmptyString()
    {
        // Arrange & Act
        var content = new JsonContent((IJsonContent)null!);

        // Assert
        Assert.Equal("", content.Json);
        Assert.Equal(MessageContentType.Json, content.ContentType);
    }

    [Fact]
    public void JsonContent_PropertySetter_UpdatesJson()
    {
        // Arrange
        var content = new JsonContent();

        // Act
        content.Json = "{\"updated\":\"value\"}";

        // Assert
        Assert.Equal("{\"updated\":\"value\"}", content.Json);
    }

    [Fact]
    public void IJsonContent_Implementation_ExposesCorrectProperty()
    {
        // Arrange
        var content = new JsonContent("{\"interface\":\"test\"}");

        // Act & Assert
        IJsonContent iJsonContent = content;
        Assert.Equal("{\"interface\":\"test\"}", iJsonContent.Json);
    }

    [Fact]
    public void IMessageContent_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var content = new JsonContent();

        // Act & Assert
        IMessageContent iMessageContent = content;
        Assert.Equal(MessageContentType.Json, iMessageContent.ContentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("{\"simple\":\"object\"}")]
    [InlineData("{\"nested\":{\"object\":{\"value\":123}}}")]
    [InlineData("[1,2,3,4,5]")]
    [InlineData("[{\"id\":1,\"name\":\"John\"},{\"id\":2,\"name\":\"Jane\"}]")]
    [InlineData("\"simple string\"")]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("null")]
    public void JsonContent_VariousJsonValues_HandlesCorrectly(string json)
    {
        // Arrange & Act
        var content = new JsonContent(json);

        // Assert
        Assert.Equal(json, content.Json);
        Assert.Equal(MessageContentType.Json, content.ContentType);
    }

    [Fact]
    public void JsonContent_ComplexJsonObject_HandlesCorrectly()
    {
        // Arrange
        var complexJson = @"{
            ""user"": {
                ""id"": 123,
                ""name"": ""John Doe"",
                ""email"": ""john@example.com"",
                ""preferences"": {
                    ""theme"": ""dark"",
                    ""notifications"": true
                },
                ""tags"": [""admin"", ""power-user""]
            },
            ""timestamp"": ""2023-12-01T10:30:00Z""
        }";

        // Act
        var content = new JsonContent(complexJson);

        // Assert
        Assert.Equal(complexJson, content.Json);
        Assert.Equal(MessageContentType.Json, content.ContentType);
    }

    [Fact]
    public void JsonContent_WithMockIJsonContent_CopiesCorrectly()
    {
        // Arrange
        var mockJsonContent = new MockJsonContent
        {
            Json = "{\"mock\":\"content\"}"
        };

        // Act
        var content = new JsonContent(mockJsonContent);

        // Assert
        Assert.Equal("{\"mock\":\"content\"}", content.Json);
    }

    // Helper class for testing
    private class MockJsonContent : IJsonContent
    {
        public string Json { get; set; } = "";
        public MessageContentType ContentType => MessageContentType.Json;
    }
}