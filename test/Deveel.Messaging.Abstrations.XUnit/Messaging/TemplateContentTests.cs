namespace Deveel.Messaging;

public class TemplateContentTests
{
    [Fact]
    public void TemplateContent_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var content = new TemplateContent();

        // Assert
        Assert.Equal("", content.TemplateId);
        Assert.NotNull(content.Parameters);
        Assert.Empty(content.Parameters);
        Assert.Equal(MessageContentType.Template, content.ContentType);
    }

    [Fact]
    public void TemplateContent_ConstructorWithTemplateIdAndParameters_SetsProperties()
    {
        // Arrange
        var templateId = "welcome-template";
        var parameters = new Dictionary<string, object?>
        {
            { "firstName", "John" },
            { "lastName", "Doe" },
            { "age", 30 },
            { "isActive", true },
            { "nullValue", null }
        };

        // Act
        var content = new TemplateContent(templateId, parameters);

        // Assert
        Assert.Equal(templateId, content.TemplateId);
        Assert.NotNull(content.Parameters);
        Assert.Equal(5, content.Parameters.Count);
        Assert.Equal("John", content.Parameters["firstName"]);
        Assert.Equal("Doe", content.Parameters["lastName"]);
        Assert.Equal(30, content.Parameters["age"]);
        Assert.Equal(true, content.Parameters["isActive"]);
        Assert.Null(content.Parameters["nullValue"]);
        Assert.NotSame(parameters, content.Parameters); // Should be a copy
    }

    [Fact]
    public void TemplateContent_ConstructorWithNullParameters_CreatesEmptyDictionary()
    {
        // Arrange
        var templateId = "test-template";

        // Act
        var content = new TemplateContent(templateId, null!);

        // Assert
        Assert.Equal(templateId, content.TemplateId);
        Assert.NotNull(content.Parameters);
        Assert.Empty(content.Parameters);
    }

    [Fact]
    public void TemplateContent_ConstructorWithITemplateContent_CopiesProperties()
    {
        // Arrange
        var sourceParameters = new Dictionary<string, object?>
        {
            { "param1", "value1" },
            { "param2", 42 }
        };
        var sourceContent = new TemplateContent("source-template", sourceParameters);

        // Act
        var content = new TemplateContent(sourceContent);

        // Assert
        Assert.Equal("source-template", content.TemplateId);
        Assert.NotNull(content.Parameters);
        Assert.Equal(2, content.Parameters.Count);
        Assert.Equal("value1", content.Parameters["param1"]);
        Assert.Equal(42, content.Parameters["param2"]);
        Assert.NotSame(sourceContent.Parameters, content.Parameters); // Should be a copy
    }

    [Fact]
    public void TemplateContent_ConstructorWithITemplateContentNullParameters_CreatesEmptyDictionary()
    {
        // Arrange
        var mockTemplateContent = new MockTemplateContent
        {
            TemplateId = "mock-template",
            Parameters = null!
        };

        // Act
        var content = new TemplateContent(mockTemplateContent);

        // Assert
        Assert.Equal("mock-template", content.TemplateId);
        Assert.NotNull(content.Parameters);
        Assert.Empty(content.Parameters);
    }

    [Fact]
    public void TemplateContent_ParametersProperty_CanBeModified()
    {
        // Arrange
        var content = new TemplateContent("test-template", new Dictionary<string, object?>());

        // Act
        content.Parameters["newParam"] = "newValue";

        // Assert
        Assert.Single(content.Parameters);
        Assert.Equal("newValue", content.Parameters["newParam"]);
    }

    [Fact]
    public void TemplateContent_ParametersProperty_CanBeReplaced()
    {
        // Arrange
        var content = new TemplateContent("test-template", new Dictionary<string, object?> { { "old", "value" } });
        var newParameters = new Dictionary<string, object?> { { "new", "parameter" } };

        // Act
        content.Parameters = newParameters;

        // Assert
        Assert.Same(newParameters, content.Parameters);
        Assert.Single(content.Parameters);
        Assert.Equal("parameter", content.Parameters["new"]);
    }

    [Fact]
    public void ITemplateContent_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "key", "value" } };
        var content = new TemplateContent("interface-template", parameters);

        // Act & Assert
        ITemplateContent iTemplateContent = content;
        Assert.Equal("interface-template", iTemplateContent.TemplateId);
        Assert.NotNull(iTemplateContent.Parameters);
        Assert.Equal("value", iTemplateContent.Parameters["key"]);
    }

    [Fact]
    public void IMessageContent_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var content = new TemplateContent("test-template", new Dictionary<string, object?>());

        // Act & Assert
        IMessageContent iMessageContent = content;
        Assert.Equal(MessageContentType.Template, iMessageContent.ContentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("simple-template")]
    [InlineData("template_with_underscores")]
    [InlineData("template-with-dashes")]
    [InlineData("TemplateWithCamelCase")]
    [InlineData("template.with.dots")]
    public void TemplateContent_VariousTemplateIds_HandlesCorrectly(string templateId)
    {
        // Arrange & Act
        var content = new TemplateContent(templateId, new Dictionary<string, object?>());

        // Assert
        Assert.Equal(templateId, content.TemplateId);
    }

    // Helper class for testing
    private class MockTemplateContent : ITemplateContent
    {
        public string TemplateId { get; set; } = "";
        public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
        public MessageContentType ContentType => MessageContentType.Template;
    }
}