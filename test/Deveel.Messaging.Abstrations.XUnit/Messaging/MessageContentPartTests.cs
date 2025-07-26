namespace Deveel.Messaging;

public class MessageContentPartTests
{
    [Fact]
    public void MessageContentPart_Create_WithTextContentPart_ReturnsTextContentPart()
    {
        // Arrange
        var originalPart = new TextContentPart("Hello, World!", "utf-8");

        // Act
        var result = MessageContentPart.Create(originalPart);

        // Assert
        var text = Assert.IsType<TextContentPart>(result);
        Assert.NotSame(originalPart, result);
        Assert.Equal(originalPart.Text, text.Text);
        Assert.Equal(originalPart.Encoding, text.Encoding);
    }

    [Fact]
    public void MessageContentPart_Create_WithHtmlContentPart_ReturnsHtmlContentPart()
    {
        // Arrange
        var originalPart = new HtmlContentPart("<p>Hello, World!</p>");

        // Act
        var result = MessageContentPart.Create(originalPart);

        // Assert
        var part = Assert.IsType<HtmlContentPart>(result);
        Assert.NotSame(originalPart, result);
        Assert.Equal(originalPart.Html, part.Html);
        Assert.Empty(part.Attachments);
    }

    [Fact]
    public void MessageContentPart_Create_WithUnsupportedContentPart_ThrowsArgumentException()
    {
        // Arrange
        var unsupportedPart = new UnsupportedContentPart();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => MessageContentPart.Create(unsupportedPart));
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void MessageContentPart_Create_WithNullContentPart_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => MessageContentPart.Create(null!));
    }

    // Helper class for testing unsupported content part
    private class UnsupportedContentPart : IMessageContentPart
    {
        public MessageContentType ContentType => (MessageContentType)999; // Non-standard type
    }
}

public class TextContentPartTests
{
    [Fact]
    public void TextContentPart_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var part = new TextContentPart();

        // Assert
        Assert.Null(part.Text);
        Assert.Null(part.Encoding);
        Assert.Equal(MessageContentType.PlainText, part.ContentType);
    }

    [Fact]
    public void TextContentPart_ConstructorWithTextAndEncoding_SetsProperties()
    {
        // Arrange
        var text = "Hello, World!";
        var encoding = "utf-8";

        // Act
        var part = new TextContentPart(text, encoding);

        // Assert
        Assert.Equal(text, part.Text);
        Assert.Equal(encoding, part.Encoding);
        Assert.Equal(MessageContentType.PlainText, part.ContentType);
    }

    [Fact]
    public void TextContentPart_ConstructorWithTextOnly_SetsTextAndNullEncoding()
    {
        // Arrange
        var text = "Hello, World!";

        // Act
        var part = new TextContentPart(text);

        // Assert
        Assert.Equal(text, part.Text);
        Assert.Null(part.Encoding);
        Assert.Equal(MessageContentType.PlainText, part.ContentType);
    }

    [Fact]
    public void TextContentPart_ConstructorWithNullText_AcceptsNull()
    {
        // Arrange & Act
        var part = new TextContentPart(null);

        // Assert
        Assert.Null(part.Text);
        Assert.Null(part.Encoding);
        Assert.Equal(MessageContentType.PlainText, part.ContentType);
    }

    [Fact]
    public void TextContentPart_ConstructorWithITextContentPart_CopiesProperties()
    {
        // Arrange
        var sourcePart = new TextContentPart("Source text", "utf-16");

        // Act
        var part = new TextContentPart(sourcePart);

        // Assert
        Assert.Equal("Source text", part.Text);
        Assert.Equal("utf-16", part.Encoding);
        Assert.Equal(MessageContentType.PlainText, part.ContentType);
    }

    [Fact]
    public void TextContentPart_ConstructorWithNullITextContentPart_HandlesNull()
    {
        // Arrange & Act
        var part = new TextContentPart(null!);

        // Assert
        Assert.Null(part.Text);
        Assert.Null(part.Encoding);
    }

    [Fact]
    public void TextContentPart_PropertySetters_UpdateValues()
    {
        // Arrange
        var part = new TextContentPart();

        // Act
        part.Text = "Updated text";
        part.Encoding = "iso-8859-1";

        // Assert
        Assert.Equal("Updated text", part.Text);
        Assert.Equal("iso-8859-1", part.Encoding);
    }

    [Fact]
    public void ITextContentPart_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var part = new TextContentPart("Test text", "utf-8");

        // Act & Assert
        ITextContentPart iTextContentPart = part;
        Assert.Equal("Test text", iTextContentPart.Text);
        Assert.Equal("utf-8", iTextContentPart.Encoding);
    }

    [Fact]
    public void IMessageContentPart_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var part = new TextContentPart("Test text");

        // Act & Assert
        IMessageContentPart iMessageContentPart = part;
        Assert.Equal(MessageContentType.PlainText, iMessageContentPart.ContentType);
    }

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with special characters: אבגדהוזחטיךכ")]
    [InlineData("Text with numbers: 123456789")]
    [InlineData("Text with symbols: !@#$%^&*()")]
    [InlineData("")]
    public void TextContentPart_VariousTextValues_HandlesCorrectly(string text)
    {
        // Arrange & Act
        var part = new TextContentPart(text);

        // Assert
        Assert.Equal(text, part.Text);
        Assert.Equal(MessageContentType.PlainText, part.ContentType);
    }
}

public class HtmlContentPartTests
{
    [Fact]
    public void HtmlContentPart_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var part = new HtmlContentPart();

        // Assert
        Assert.Equal("", part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Empty(part.Attachments);
        Assert.Equal(MessageContentType.Html, part.ContentType);
    }

    [Fact]
    public void HtmlContentPart_ConstructorWithHtml_SetsProperties()
    {
        // Arrange
        var html = "<p>Hello, World!</p>";

        // Act
        var part = new HtmlContentPart(html);

        // Assert
        Assert.Equal(html, part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Empty(part.Attachments);
        Assert.Equal(MessageContentType.Html, part.ContentType);
    }

    [Fact]
    public void HtmlContentPart_ConstructorWithHtmlAndAttachments_SetsProperties()
    {
        // Arrange
        var html = "<p>Hello with attachment!</p>";
        var attachments = new List<MessageAttachment>
        {
            new MessageAttachment("1", "file1.txt", "text/plain", "content1"),
            new MessageAttachment("2", "file2.pdf", "application/pdf", "content2")
        };

        // Act
        var part = new HtmlContentPart(html, attachments);

        // Assert
        Assert.Equal(html, part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Equal(2, part.Attachments.Count);
        Assert.Equal("file1.txt", part.Attachments[0].FileName);
        Assert.Equal("file2.pdf", part.Attachments[1].FileName);
    }

    [Fact]
    public void HtmlContentPart_ConstructorWithNullHtml_SetsEmptyHtml()
    {
        // Arrange & Act
        var part = new HtmlContentPart(null);

        // Assert
        Assert.NotNull(part.Html);
        Assert.Equal("", part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Empty(part.Attachments);
    }

    [Fact]
    public void HtmlContentPart_ConstructorWithNullAttachments_CreatesEmptyList()
    {
        // Arrange
        var html = "<p>Test</p>";

        // Act
        var part = new HtmlContentPart(html, null);

        // Assert
        Assert.Equal(html, part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Empty(part.Attachments);
    }

    [Fact]
    public void HtmlContentPart_ConstructorWithIHtmlContentPart_CopiesProperties()
    {
        // Arrange
        var sourceAttachments = new List<MessageAttachment>
        {
            new MessageAttachment("1", "source.txt", "text/plain", "content")
        };
        var sourcePart = new HtmlContentPart("<div>Source HTML</div>", sourceAttachments);

        // Act
        var part = new HtmlContentPart(sourcePart);

        // Assert
        Assert.Equal("<div>Source HTML</div>", part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Single(part.Attachments);
        Assert.Equal("source.txt", part.Attachments[0].FileName);
        Assert.NotSame(sourcePart.Attachments, part.Attachments); // Should be a copy
    }

    [Fact]
    public void HtmlContentPart_ConstructorWithNullIHtmlContentPart_HandlesNull()
    {
        // Arrange & Act
        var part = new HtmlContentPart(null!);

        // Assert
        Assert.Equal("", part.Html);
        Assert.NotNull(part.Attachments);
        Assert.Empty(part.Attachments);
    }

    [Fact]
    public void HtmlContentPart_PropertySetters_UpdateValues()
    {
        // Arrange
        var part = new HtmlContentPart();

        // Act
        part.Html = "<p>Updated HTML</p>";
        part.Attachments = new List<MessageAttachment>
        {
            new MessageAttachment("new", "new.txt", "text/plain", "new content")
        };

        // Assert
        Assert.Equal("<p>Updated HTML</p>", part.Html);
        Assert.Single(part.Attachments);
        Assert.Equal("new.txt", part.Attachments[0].FileName);
    }

    [Fact]
    public void IHtmlContentPart_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var attachments = new List<MessageAttachment>
        {
            new MessageAttachment("1", "test.txt", "text/plain", "content")
        };
        var part = new HtmlContentPart("<p>Test HTML</p>", attachments);

        // Act & Assert
        IHtmlContentPart iHtmlContentPart = part;
        Assert.Equal("<p>Test HTML</p>", iHtmlContentPart.Html);
        Assert.NotNull(iHtmlContentPart.Attachments);
        Assert.Single(iHtmlContentPart.Attachments);
        Assert.IsAssignableFrom<IAttachment>(iHtmlContentPart.Attachments.First());
    }

    [Fact]
    public void IMessageContentPart_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var part = new HtmlContentPart("<p>Test</p>");

        // Act & Assert
        IMessageContentPart iMessageContentPart = part;
        Assert.Equal(MessageContentType.Html, iMessageContentPart.ContentType);
    }

    [Theory]
    [InlineData("<p>Simple paragraph</p>")]
    [InlineData("<div><h1>Title</h1><p>Content</p></div>")]
    [InlineData("<html><body>Full HTML document</body></html>")]
    [InlineData("")]
    public void HtmlContentPart_VariousHtmlValues_HandlesCorrectly(string html)
    {
        // Arrange & Act
        var part = new HtmlContentPart(html);

        // Assert
        Assert.Equal(html, part.Html);
        Assert.Equal(MessageContentType.Html, part.ContentType);
    }
}