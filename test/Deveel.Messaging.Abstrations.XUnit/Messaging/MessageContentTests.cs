namespace Deveel.Messaging;

public class MessageContentTests
{
    [Fact]
    public void Create_WithTextContent_ReturnsTextContent()
    {
        // Arrange
        var originalContent = new TextContent("Hello, World!");

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<TextContent>(result);
        Assert.Same(originalContent, result); // Should return same instance if already MessageContent
    }

    [Fact]
    public void Create_WithITextContent_ReturnsTextContent()
    {
        // Arrange
        ITextContent textContent = new TextContent("Hello, World!");

        // Act
        var result = MessageContent.Create(textContent);

        // Assert
        Assert.IsType<TextContent>(result);
        var textResult = (TextContent)result;
        Assert.Equal("Hello, World!", textResult.Text);
    }

    [Fact]
    public void Create_WithNullContent_ReturnsNull()
    {
        // Arrange & Act
        var result = MessageContent.Create(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_WithUnsupportedContent_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedContent = new UnsupportedMessageContent();

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => MessageContent.Create(unsupportedContent));
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void IMessageContent_ContentType_ReturnsCorrectType()
    {
        // Arrange
        var content = new TextContent("Test");

        // Act
        IMessageContent iContent = content;

        // Assert
        Assert.Equal(MessageContentType.PlainText, iContent.ContentType);
    }

    // Helper class for testing unsupported content
    private class UnsupportedMessageContent : IMessageContent
    {
        public MessageContentType ContentType => (MessageContentType)999; // Non-standard type
    }
}