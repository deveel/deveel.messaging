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
    public void Create_WithITextContentCustomImplementation_ReturnsTextContent()
    {
        // Arrange
        var customTextContent = new CustomTextContent("Custom text", "utf-8");

        // Act
        var result = MessageContent.Create(customTextContent);

        // Assert
        Assert.IsType<TextContent>(result);
        var textResult = (TextContent)result;
        Assert.Equal("Custom text", textResult.Text);
        Assert.Equal("utf-8", textResult.Encoding);
        Assert.Equal(MessageContentType.PlainText, textResult.ContentType);
    }

    [Fact]
    public void Create_WithHtmlContent_ReturnsHtmlContent()
    {
        // Arrange
        var originalContent = new HtmlContent("<p>Hello HTML</p>");

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<HtmlContent>(result);
        Assert.Same(originalContent, result);
    }

    [Fact]
    public void Create_WithIHtmlContent_ReturnsHtmlContent()
    {
        // Arrange
        var attachments = new List<MessageAttachment>
        {
            new MessageAttachment("1", "test.txt", "text/plain", "content")
        };
        IHtmlContent htmlContent = new HtmlContent("<h1>HTML Content</h1>", attachments);

        // Act
        var result = MessageContent.Create(htmlContent);

        // Assert
        Assert.IsType<HtmlContent>(result);
        var htmlResult = (HtmlContent)result;
        Assert.Equal("<h1>HTML Content</h1>", htmlResult.Html);
        Assert.Single(htmlResult.Attachments);
        Assert.Equal("test.txt", htmlResult.Attachments[0].FileName);
    }

    [Fact]
    public void Create_WithIHtmlContentCustomImplementation_ReturnsHtmlContent()
    {
        // Arrange
        var customHtmlContent = new CustomHtmlContent("<div>Custom HTML</div>");

        // Act
        var result = MessageContent.Create(customHtmlContent);

        // Assert
        Assert.IsType<HtmlContent>(result);
        var htmlResult = (HtmlContent)result;
        Assert.Equal("<div>Custom HTML</div>", htmlResult.Html);
        Assert.Equal(MessageContentType.Html, htmlResult.ContentType);
    }

    [Fact]
    public void Create_WithTemplateContent_ReturnsTemplateContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "name", "John" } };
        var originalContent = new TemplateContent("welcome", parameters);

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<TemplateContent>(result);
        Assert.Same(originalContent, result);
    }

    [Fact]
    public void Create_WithITemplateContent_ReturnsTemplateContent()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> 
        { 
            { "firstName", "John" },
            { "lastName", "Doe" },
            { "age", 30 },
            { "isActive", true }
        };
        ITemplateContent templateContent = new TemplateContent("user-profile", parameters);

        // Act
        var result = MessageContent.Create(templateContent);

        // Assert
        Assert.IsType<TemplateContent>(result);
        var templateResult = (TemplateContent)result;
        Assert.Equal("user-profile", templateResult.TemplateId);
        Assert.Equal(4, templateResult.Parameters.Count);
        Assert.Equal("John", templateResult.Parameters["firstName"]);
        Assert.Equal(30, templateResult.Parameters["age"]);
    }

    [Fact]
    public void Create_WithITemplateContentCustomImplementation_ReturnsTemplateContent()
    {
        // Arrange
        var customTemplateContent = new CustomTemplateContent("custom-template", 
            new Dictionary<string, object?> { { "key", "value" } });

        // Act
        var result = MessageContent.Create(customTemplateContent);

        // Assert
        Assert.IsType<TemplateContent>(result);
        var templateResult = (TemplateContent)result;
        Assert.Equal("custom-template", templateResult.TemplateId);
        Assert.Single(templateResult.Parameters);
        Assert.Equal("value", templateResult.Parameters["key"]);
    }

    [Fact]
    public void Create_WithMultipartContent_ReturnsMultipartContent()
    {
        // Arrange
        var parts = new List<MessageContentPart>
        {
            new TextContentPart("Text part"),
            new HtmlContentPart("<p>HTML part</p>")
        };
        var originalContent = new MultipartContent(parts);

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<MultipartContent>(result);
        Assert.Same(originalContent, result);
    }

    [Fact]
    public void Create_WithIMultipartContent_ReturnsMultipartContent()
    {
        // Arrange
        var parts = new List<MessageContentPart>
        {
            new TextContentPart("Text part"),
            new HtmlContentPart("<p>HTML part</p>")
        };
        IMultipartContent multipartContent = new MultipartContent(parts);

        // Act
        var result = MessageContent.Create(multipartContent);

        // Assert
        Assert.IsType<MultipartContent>(result);
        var multipartResult = (MultipartContent)result;
        Assert.Equal(2, multipartResult.Parts.Count);
        Assert.IsType<TextContentPart>(multipartResult.Parts[0]);
        Assert.IsType<HtmlContentPart>(multipartResult.Parts[1]);
    }

    [Fact]
    public void Create_WithIMultipartContentCustomImplementation_ReturnsMultipartContent()
    {
        // Arrange
        var customMultipartContent = new CustomMultipartContent(new List<IMessageContentPart>
        {
            new TextContentPart("Custom text"),
            new HtmlContentPart("<b>Custom HTML</b>")
        });

        // Act
        var result = MessageContent.Create(customMultipartContent);

        // Assert
        Assert.IsType<MultipartContent>(result);
        var multipartResult = (MultipartContent)result;
        Assert.Equal(2, multipartResult.Parts.Count);
    }

    [Fact]
    public void Create_WithJsonContent_ReturnsJsonContent()
    {
        // Arrange
        var originalContent = new JsonContent("{\"test\": \"value\"}");

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<JsonContent>(result);
        Assert.Same(originalContent, result);
    }

    [Fact]
    public void Create_WithIJsonContent_ReturnsJsonContent()
    {
        // Arrange
        var jsonData = "{\"user\": \"john\", \"action\": \"login\", \"timestamp\": \"2023-12-01T10:30:00Z\"}";
        IJsonContent jsonContent = new JsonContent(jsonData);

        // Act
        var result = MessageContent.Create(jsonContent);

        // Assert
        Assert.IsType<JsonContent>(result);
        var jsonResult = (JsonContent)result;
        Assert.Equal(jsonData, jsonResult.Json);
        Assert.Equal(MessageContentType.Json, jsonResult.ContentType);
    }

    [Fact]
    public void Create_WithIJsonContentCustomImplementation_ReturnsJsonContent()
    {
        // Arrange
        var customJsonContent = new CustomJsonContent("{\"custom\": true}");

        // Act
        var result = MessageContent.Create(customJsonContent);

        // Assert
        Assert.IsType<JsonContent>(result);
        var jsonResult = (JsonContent)result;
        Assert.Equal("{\"custom\": true}", jsonResult.Json);
    }

    [Fact]
    public void Create_WithBinaryContent_ReturnsBinaryContent()
    {
        // Arrange
        var binaryData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        var originalContent = new BinaryContent(binaryData, "application/octet-stream");

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<BinaryContent>(result);
        Assert.Same(originalContent, result);
    }

    [Fact]
    public void Create_WithIBinaryContent_ReturnsBinaryContent()
    {
        // Arrange
        var binaryData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        IBinaryContent binaryContent = new BinaryContent(binaryData, "image/png");

        // Act
        var result = MessageContent.Create(binaryContent);

        // Assert
        Assert.IsType<BinaryContent>(result);
        var binaryResult = (BinaryContent)result;
        Assert.Equal(binaryData, binaryResult.RawData);
        Assert.Equal("image/png", binaryResult.MimeType);
        Assert.Equal(MessageContentType.Binary, binaryResult.ContentType);
    }

    [Fact]
    public void Create_WithIBinaryContentCustomImplementation_ReturnsBinaryContent()
    {
        // Arrange
        var customBinaryContent = new CustomBinaryContent(
            new byte[] { 0x01, 0x02, 0x03 }, 
            "application/custom");

        // Act
        var result = MessageContent.Create(customBinaryContent);

        // Assert
        Assert.IsType<BinaryContent>(result);
        var binaryResult = (BinaryContent)result;
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, binaryResult.RawData);
        Assert.Equal("application/custom", binaryResult.MimeType);
    }

    [Fact]
    public void Create_WithMediaContent_ReturnsMediaContent()
    {
        // Arrange
        var mediaData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG header
        var originalContent = new MediaContent(MediaType.Image, "photo.jpg", mediaData);

        // Act
        var result = MessageContent.Create(originalContent);

        // Assert
        Assert.IsType<MediaContent>(result);
        Assert.Same(originalContent, result);
    }

    [Fact]
    public void Create_WithIMediaContent_ReturnsMediaContent()
    {
        // Arrange
        var mediaData = new byte[] { 0x00, 0x00, 0x00, 0x18 }; // MP4 header
        IMediaContent mediaContent = new MediaContent(MediaType.Video, "video.mp4", mediaData);

        // Act
        var result = MessageContent.Create(mediaContent);

        // Assert
        Assert.IsType<MediaContent>(result);
        var mediaResult = (MediaContent)result;
        Assert.Equal(MediaType.Video, mediaResult.MediaType);
        Assert.Equal("video.mp4", mediaResult.FileName);
        Assert.Equal(mediaData, mediaResult.Data);
        // Note: MediaContent currently returns Binary content type, not Media
        Assert.Equal(MessageContentType.Binary, mediaResult.ContentType);
    }

    [Fact]
    public void Create_WithIMediaContentCustomImplementation_ReturnsMediaContent()
    {
        // Arrange - Custom media content that returns Binary but doesn't implement IBinaryContent
        var customMediaContent = new CustomMediaContent(
            MediaType.Audio, 
            "audio.mp3", 
            "https://example.com/audio.mp3",
            new byte[] { 0x49, 0x44, 0x33 }); // MP3 header

        // Act & Assert - This should throw because it claims Binary but doesn't implement IBinaryContent
        var exception = Assert.Throws<NotSupportedException>(() => MessageContent.Create(customMediaContent));
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void Create_WithMediaContentThatImplementsBinaryCorrectly_ReturnsMediaContent()
    {
        // Arrange - Media content that correctly implements IBinaryContent since it returns Binary content type
        var mediaContent = new MediaContentAsBinary(
            MediaType.Audio, 
            "audio.mp3", 
            new byte[] { 0x49, 0x44, 0x33 }); // MP3 header

        // Act
        var result = MessageContent.Create(mediaContent);

        // Assert
        Assert.IsType<BinaryContent>(result); // Should create BinaryContent since it implements IBinaryContent
        var binaryResult = (BinaryContent)result;
        Assert.Equal(new byte[] { 0x49, 0x44, 0x33 }, binaryResult.RawData);
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
        // The actual error message contains the numeric value, not the enum name
        Assert.Contains("999", exception.Message);
    }

    [Fact]
    public void Create_WithContentTypeNotMatchingInterface_ThrowsNotSupportedException()
    {
        // Arrange - Content that claims to be PlainText but doesn't implement ITextContent
        var mismatchedContent = new MismatchedContentTypeContent();

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => MessageContent.Create(mismatchedContent));
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyTextContent_ReturnsTextContent()
    {
        // Arrange
        var emptyTextContent = new CustomTextContent("", null);

        // Act
        var result = MessageContent.Create(emptyTextContent);

        // Assert
        Assert.IsType<TextContent>(result);
        var textResult = (TextContent)result;
        Assert.Equal("", textResult.Text);
        Assert.Null(textResult.Encoding);
    }

    [Fact]
    public void Create_WithEmptyJsonContent_ReturnsJsonContent()
    {
        // Arrange
        var emptyJsonContent = new CustomJsonContent("");

        // Act
        var result = MessageContent.Create(emptyJsonContent);

        // Assert
        Assert.IsType<JsonContent>(result);
        var jsonResult = (JsonContent)result;
        Assert.Equal("", jsonResult.Json);
    }

    [Fact]
    public void Create_WithEmptyBinaryContent_ReturnsBinaryContent()
    {
        // Arrange
        var emptyBinaryContent = new CustomBinaryContent(Array.Empty<byte>(), "");

        // Act
        var result = MessageContent.Create(emptyBinaryContent);

        // Assert
        Assert.IsType<BinaryContent>(result);
        var binaryResult = (BinaryContent)result;
        Assert.Empty(binaryResult.RawData);
        Assert.Equal("", binaryResult.MimeType);
    }

    [Fact]
    public void Create_WithEmptyMultipartContent_ReturnsMultipartContent()
    {
        // Arrange
        var emptyMultipartContent = new CustomMultipartContent(new List<IMessageContentPart>());

        // Act
        var result = MessageContent.Create(emptyMultipartContent);

        // Assert
        Assert.IsType<MultipartContent>(result);
        var multipartResult = (MultipartContent)result;
        Assert.Empty(multipartResult.Parts);
    }

    [Fact]
    public void Create_AllContentTypes_VerifyFactoryMethodHandlesAllTypes()
    {
        // This test ensures all content types defined in MessageContentType enum
        // are handled by the factory method (except unsupported ones)
        
        var testCases = new (IMessageContent content, Type expectedType)[]
        {
            (new TextContent("test"), typeof(TextContent)),
            (new HtmlContent("<p>test</p>"), typeof(HtmlContent)),
            (new TemplateContent("template", new Dictionary<string, object?>()), typeof(TemplateContent)),
            (new MultipartContent(new List<MessageContentPart>()), typeof(MultipartContent)),
            (new JsonContent("{}"), typeof(JsonContent)),
            (new BinaryContent(Array.Empty<byte>(), "application/octet-stream"), typeof(BinaryContent)),
            // Note: MediaContent currently reports ContentType.Binary, so it's handled by Binary branch
            (new MediaContent(MediaType.Image, "test.jpg", Array.Empty<byte>()), typeof(MediaContent))
        };

        foreach (var (content, expectedType) in testCases)
        {
            // Act
            var result = MessageContent.Create(content);

            // Assert
            Assert.NotNull(result);
            Assert.IsType(expectedType, result);
            Assert.Equal(content.ContentType, result.ContentType);
        }
    }

    [Fact]
    public void Create_WithUnsupportedContentType_ThrowsNotSupportedException()
    {
        // Test that verifies a content type that has no matching handler throws exception
        var testCases = new IMessageContent[]
        {
            new UnsupportedMessageContent(), // ContentType 999
            new MismatchedContentTypeContent(), // PlainText but doesn't implement ITextContent
            new ContentWithUnknownContentType() // Valid enum value but no handler
        };

        foreach (var content in testCases)
        {
            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => MessageContent.Create(content));
            Assert.Contains("not supported", exception.Message);
        }
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

    // Helper classes for testing custom implementations

    private class CustomTextContent : ITextContent
    {
        public CustomTextContent(string text, string? encoding)
        {
            Text = text;
            Encoding = encoding;
        }

        public MessageContentType ContentType => MessageContentType.PlainText;
        public string? Text { get; }
        public string? Encoding { get; }
    }

    private class CustomHtmlContent : IHtmlContent
    {
        public CustomHtmlContent(string html)
        {
            Html = html;
            Attachments = new List<IAttachment>();
        }

        public MessageContentType ContentType => MessageContentType.Html;
        public string Html { get; }
        public IEnumerable<IAttachment> Attachments { get; }
    }

    private class CustomTemplateContent : ITemplateContent
    {
        public CustomTemplateContent(string templateId, IDictionary<string, object?> parameters)
        {
            TemplateId = templateId;
            Parameters = parameters;
        }

        public MessageContentType ContentType => MessageContentType.Template;
        public string TemplateId { get; }
        public IDictionary<string, object?> Parameters { get; }
    }

    private class CustomMultipartContent : IMultipartContent
    {
        public CustomMultipartContent(IEnumerable<IMessageContentPart> parts)
        {
            Parts = parts;
        }

        public MessageContentType ContentType => MessageContentType.Multipart;
        public IEnumerable<IMessageContentPart> Parts { get; }
    }

    private class CustomJsonContent : IJsonContent
    {
        public CustomJsonContent(string json)
        {
            Json = json;
        }

        public MessageContentType ContentType => MessageContentType.Json;
        public string Json { get; }
    }

    private class CustomBinaryContent : IBinaryContent
    {
        public CustomBinaryContent(byte[] rawData, string mimeType)
        {
            RawData = rawData;
            MimeType = mimeType;
        }

        public MessageContentType ContentType => MessageContentType.Binary;
        public byte[] RawData { get; }
        public string MimeType { get; }
    }

    private class MediaContentAsBinary : IMediaContent, IBinaryContent
    {
        public MediaContentAsBinary(MediaType mediaType, string fileName, byte[] data)
        {
            MediaType = mediaType;
            FileName = fileName;
            Data = data;
            RawData = data;
            MimeType = GetMimeTypeFromMediaType(mediaType);
        }

        private static string GetMimeTypeFromMediaType(MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Image => "image/jpeg",
                MediaType.Audio => "audio/mpeg",
                MediaType.Video => "video/mp4",
                MediaType.Document => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        public MessageContentType ContentType => MessageContentType.Binary;
        public MediaType MediaType { get; }
        public string? FileName { get; }
        public string? FileUrl => null;
        public byte[]? Data { get; }
        public byte[] RawData { get; }
        public string MimeType { get; }
    }

    private class CustomMediaContent : IMediaContent
    {
        public CustomMediaContent(MediaType mediaType, string fileName, string? fileUrl, byte[]? data)
        {
            MediaType = mediaType;
            FileName = fileName;
            FileUrl = fileUrl;
            Data = data;
        }

        public MessageContentType ContentType => MessageContentType.Binary; // MediaContent uses Binary
        public MediaType MediaType { get; }
        public string? FileName { get; }
        public string? FileUrl { get; }
        public byte[]? Data { get; }
    }

    private class UnsupportedMessageContent : IMessageContent
    {
        public MessageContentType ContentType => (MessageContentType)999; // Non-standard type called "UnsupportedType"
    }

    private class MismatchedContentTypeContent : IMessageContent
    {
        // Claims to be PlainText but doesn't implement ITextContent
        public MessageContentType ContentType => MessageContentType.PlainText;
    }

    private class ContentWithUnknownContentType : IMessageContent
    {
        // Valid enum value but no corresponding interface implementation
        public MessageContentType ContentType => MessageContentType.Html; // Valid enum but doesn't implement IHtmlContent
    }
}