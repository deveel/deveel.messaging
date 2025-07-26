namespace Deveel.Messaging;

public class TextContentTests
{
    [Fact]
    public void TextContent_Constructor_SetsTextAndEncoding()
    {
        // Arrange
        var text = "Hello, World!";
        var encoding = "utf-8";

        // Act
        var content = new TextContent(text, encoding);

        // Assert
        Assert.Equal(text, content.Text);
        Assert.Equal(encoding, content.Encoding);
        Assert.Equal(MessageContentType.PlainText, content.ContentType);
    }

    [Fact]
    public void TextContent_ConstructorWithoutEncoding_SetsTextOnly()
    {
        // Arrange
        var text = "Hello, World!";

        // Act
        var content = new TextContent(text);

        // Assert
        Assert.Equal(text, content.Text);
        Assert.Null(content.Encoding);
        Assert.Equal(MessageContentType.PlainText, content.ContentType);
    }

    [Fact]
    public void TextContent_ConstructorWithNullText_AcceptsNull()
    {
        // Arrange & Act
        var content = new TextContent(null);

        // Assert
        Assert.Null(content.Text);
        Assert.Null(content.Encoding);
        Assert.Equal(MessageContentType.PlainText, content.ContentType);
    }

    [Fact]
    public void TextContent_ConstructorWithITextContent_CopiesProperties()
    {
        // Arrange
        var sourceContent = new TextContent("Source text", "utf-8");

        // Act
        var content = new TextContent(sourceContent);

        // Assert
        Assert.Equal("Source text", content.Text);
        Assert.Equal("utf-8", content.Encoding);
        Assert.Equal(MessageContentType.PlainText, content.ContentType);
    }

    [Fact]
    public void TextContent_PropertySetters_UpdateValues()
    {
        // Arrange
        var content = new TextContent("Initial text");

        // Act
        content.Text = "Updated text";
        content.Encoding = "utf-16";

        // Assert
        Assert.Equal("Updated text", content.Text);
        Assert.Equal("utf-16", content.Encoding);
    }

    [Fact]
    public void ITextContent_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var content = new TextContent("Test text", "utf-8");

        // Act & Assert
        ITextContent iTextContent = content;
        Assert.Equal("Test text", iTextContent.Text);
        Assert.Equal("utf-8", iTextContent.Encoding);
    }

    [Fact]
    public void IMessageContent_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var content = new TextContent("Test text");

        // Act & Assert
        IMessageContent iMessageContent = content;
        Assert.Equal(MessageContentType.PlainText, iMessageContent.ContentType);
    }

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with special characters: אבגדהוזחטיךכ")]
    [InlineData("Text with numbers: 123456789")]
    [InlineData("Text with symbols: !@#$%^&*()")]
    [InlineData("")]
    public void TextContent_VariousTextValues_HandlesCorrectly(string text)
    {
        // Arrange & Act
        var content = new TextContent(text);

        // Assert
        Assert.Equal(text, content.Text);
        Assert.Equal(MessageContentType.PlainText, content.ContentType);
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-16")]
    [InlineData("ascii")]
    [InlineData("iso-8859-1")]
    public void TextContent_VariousEncodings_HandlesCorrectly(string encoding)
    {
        // Arrange & Act
        var content = new TextContent("Test text", encoding);

        // Assert
        Assert.Equal(encoding, content.Encoding);
        Assert.Equal("Test text", content.Text);
    }
}