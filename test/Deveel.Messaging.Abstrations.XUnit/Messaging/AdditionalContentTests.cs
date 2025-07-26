namespace Deveel.Messaging;

public class BinaryContentTests
{
    [Fact]
    public void BinaryContent_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var content = new BinaryContent();

        // Assert
        Assert.NotNull(content.RawData);
        Assert.Empty(content.RawData);
        Assert.Equal(string.Empty, content.MimeType);
        Assert.Equal(MessageContentType.Binary, content.ContentType);
    }

    [Fact]
    public void BinaryContent_ConstructorWithRawDataAndMimeType_SetsProperties()
    {
        // Arrange
        var rawData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" in bytes
        var mimeType = "application/octet-stream";

        // Act
        var content = new BinaryContent(rawData, mimeType);

        // Assert
        Assert.Equal(rawData, content.RawData);
        Assert.Equal(mimeType, content.MimeType);
        Assert.Equal(MessageContentType.Binary, content.ContentType);
    }

    [Fact]
    public void BinaryContent_ConstructorWithIBinaryContent_CopiesProperties()
    {
        // Arrange
        var sourceData = new byte[] { 0x01, 0x02, 0x03 };
        var sourceMimeType = "image/png";
        var sourceContent = new BinaryContent(sourceData, sourceMimeType);

        // Act
        var content = new BinaryContent(sourceContent);

        // Assert
        Assert.Equal(sourceData, content.RawData);
        Assert.Equal(sourceMimeType, content.MimeType);
        Assert.Equal(MessageContentType.Binary, content.ContentType);
    }

    [Fact]
    public void BinaryContent_ConstructorWithNullIBinaryContent_SetsDefaults()
    {
        // Arrange & Act
        var content = new BinaryContent(null!);

        // Assert
        Assert.NotNull(content.RawData);
        Assert.Empty(content.RawData);
        Assert.Equal(string.Empty, content.MimeType);
    }

    [Fact]
    public void BinaryContent_PropertySetters_UpdateValues()
    {
        // Arrange
        var content = new BinaryContent();
        var newData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var newMimeType = "application/pdf";

        // Act
        content.RawData = newData;
        content.MimeType = newMimeType;

        // Assert
        Assert.Equal(newData, content.RawData);
        Assert.Equal(newMimeType, content.MimeType);
    }

    [Fact]
    public void IBinaryContent_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var rawData = new byte[] { 0xFF, 0x00, 0xFF };
        var mimeType = "image/jpeg";
        var content = new BinaryContent(rawData, mimeType);

        // Act & Assert
        IBinaryContent iBinaryContent = content;
        Assert.Equal(rawData, iBinaryContent.RawData);
        Assert.Equal(mimeType, iBinaryContent.MimeType);
    }

    [Fact]
    public void IMessageContent_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var content = new BinaryContent();

        // Act & Assert
        IMessageContent iMessageContent = content;
        Assert.Equal(MessageContentType.Binary, iMessageContent.ContentType);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    [InlineData("audio/mpeg")]
    public void BinaryContent_VariousMimeTypes_HandlesCorrectly(string mimeType)
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var content = new BinaryContent(data, mimeType);

        // Assert
        Assert.Equal(mimeType, content.MimeType);
        Assert.Equal(data, content.RawData);
    }

    [Fact]
    public void BinaryContent_WithEmptyByteArray_HandlesCorrectly()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        var mimeType = "application/empty";

        // Act
        var content = new BinaryContent(emptyData, mimeType);

        // Assert
        Assert.Equal(emptyData, content.RawData);
        Assert.Empty(content.RawData);
        Assert.Equal(mimeType, content.MimeType);
    }

    [Fact]
    public void BinaryContent_WithLargeByteArray_HandlesCorrectly()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }
        var mimeType = "application/large-file";

        // Act
        var content = new BinaryContent(largeData, mimeType);

        // Assert
        Assert.Equal(largeData, content.RawData);
        Assert.Equal(1024 * 1024, content.RawData.Length);
        Assert.Equal(mimeType, content.MimeType);
    }
}

public class MediaContentTests
{
    [Fact]
    public void MediaContent_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var content = new MediaContent();

        // Assert
        Assert.Equal(default, content.MediaType);
        Assert.Null(content.FileName);
        Assert.Null(content.FileUrl);
        Assert.Null(content.Data);
        Assert.Equal(MessageContentType.Binary, content.ContentType);
    }

    [Fact]
    public void MediaContent_ConstructorWithMediaTypeFileNameAndData_SetsProperties()
    {
        // Arrange
        var mediaType = MediaType.Image;
        var fileName = "image.png";
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var content = new MediaContent(mediaType, fileName, data);

        // Assert
        Assert.Equal(mediaType, content.MediaType);
        Assert.Equal(fileName, content.FileName);
        Assert.Equal(data, content.Data);
        Assert.Null(content.FileUrl);
        Assert.Equal(MessageContentType.Binary, content.ContentType);
    }

    [Fact]
    public void MediaContent_ConstructorWithMediaTypeFileNameAndUrl_SetsProperties()
    {
        // Arrange
        var mediaType = MediaType.Video;
        var fileName = "video.mp4";
        var fileUrl = "https://example.com/video.mp4";

        // Act
        var content = new MediaContent(mediaType, fileName, fileUrl);

        // Assert
        Assert.Equal(mediaType, content.MediaType);
        Assert.Equal(fileName, content.FileName);
        Assert.Equal(fileUrl, content.FileUrl);
        Assert.Null(content.Data);
        Assert.Equal(MessageContentType.Binary, content.ContentType);
    }

    [Fact]
    public void MediaContent_ConstructorWithIMediaContent_CopiesProperties()
    {
        // Arrange
        var sourceData = new byte[] { 0x01, 0x02, 0x03 };
        var sourceContent = new MediaContent(MediaType.Audio, "audio.mp3", sourceData);

        // Act
        var content = new MediaContent(sourceContent);

        // Assert
        Assert.Equal(MediaType.Audio, content.MediaType);
        Assert.Equal("audio.mp3", content.FileName);
        Assert.Equal(sourceData, content.Data);
        Assert.Equal("", content.FileUrl);
    }

    [Fact]
    public void MediaContent_ConstructorWithNullIMediaContent_SetsDefaults()
    {
        // Arrange & Act
        var content = new MediaContent(null!);

        // Assert
        Assert.Equal(default, content.MediaType);
        Assert.Equal("", content.FileName);
        Assert.Equal("", content.FileUrl);
        Assert.NotNull(content.Data);
        Assert.Empty(content.Data);
    }

    [Fact]
    public void MediaContent_PropertySetters_UpdateValues()
    {
        // Arrange
        var content = new MediaContent();

        // Act
        content.MediaType = MediaType.Document;
        content.FileName = "document.pdf";
        content.FileUrl = "https://example.com/doc.pdf";
        content.Data = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header

        // Assert
        Assert.Equal(MediaType.Document, content.MediaType);
        Assert.Equal("document.pdf", content.FileName);
        Assert.Equal("https://example.com/doc.pdf", content.FileUrl);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, content.Data);
    }

    [Fact]
    public void IMediaContent_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var mediaType = MediaType.Image;
        var fileName = "test.jpg";
        var fileUrl = "https://example.com/test.jpg";
        var data = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG header
        var content = new MediaContent(mediaType, fileName, data);
        content.FileUrl = fileUrl;

        // Act & Assert
        IMediaContent iMediaContent = content;
        Assert.Equal(mediaType, iMediaContent.MediaType);
        Assert.Equal(fileName, iMediaContent.FileName);
        Assert.Equal(fileUrl, iMediaContent.FileUrl);
        Assert.Equal(data, iMediaContent.Data);
    }

    [Fact]
    public void IMessageContent_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var content = new MediaContent();

        // Act & Assert
        IMessageContent iMessageContent = content;
        Assert.Equal(MessageContentType.Binary, iMessageContent.ContentType);
    }

    [Theory]
    [InlineData(MediaType.Image, "image.png")]
    [InlineData(MediaType.Audio, "audio.mp3")]
    [InlineData(MediaType.Video, "video.mp4")]
    [InlineData(MediaType.Document, "document.pdf")]
    [InlineData(MediaType.File, "file.bin")]
    public void MediaContent_VariousMediaTypes_HandlesCorrectly(MediaType mediaType, string fileName)
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var content = new MediaContent(mediaType, fileName, data);

        // Assert
        Assert.Equal(mediaType, content.MediaType);
        Assert.Equal(fileName, content.FileName);
        Assert.Equal(data, content.Data);
    }

    [Fact]
    public void MediaContent_WithNullValues_HandlesCorrectly()
    {
        // Arrange & Act
        var content = new MediaContent(MediaType.File, null, (byte[])null!);

        // Assert
        Assert.Equal(MediaType.File, content.MediaType);
        Assert.Null(content.FileName);
        Assert.Null(content.Data);
    }
}

public class MultipartContentTests
{
    [Fact]
    public void MultipartContent_DefaultConstructor_CreatesEmptyInstance()
    {
        // Arrange & Act
        var content = new MultipartContent();

        // Assert
        Assert.NotNull(content.Parts);
        Assert.Empty(content.Parts);
        Assert.Equal(MessageContentType.Multipart, content.ContentType);
    }

    [Fact]
    public void MultipartContent_ConstructorWithParts_SetsProperties()
    {
        // Arrange
        var parts = new List<MessageContentPart>
        {
            new TextContentPart("Text part"),
            new HtmlContentPart("<p>HTML part</p>")
        };

        // Act
        var content = new MultipartContent(parts);

        // Assert
        Assert.NotNull(content.Parts);
        Assert.Equal(2, content.Parts.Count);
        Assert.IsType<TextContentPart>(content.Parts[0]);
        Assert.IsType<HtmlContentPart>(content.Parts[1]);
        Assert.Equal(MessageContentType.Multipart, content.ContentType);
    }

    [Fact]
    public void MultipartContent_ConstructorWithIMultipartContent_CopiesParts()
    {
        // Arrange
        var sourceParts = new List<MessageContentPart>
        {
            new TextContentPart("Source text"),
            new HtmlContentPart("<div>Source HTML</div>")
        };
        var sourceContent = new MultipartContent(sourceParts);

        // Act
        var content = new MultipartContent(sourceContent);

        // Assert
        Assert.NotNull(content.Parts);
        Assert.Equal(2, content.Parts.Count);
        Assert.IsType<TextContentPart>(content.Parts[0]);
        Assert.IsType<HtmlContentPart>(content.Parts[1]);
        Assert.NotSame(sourceContent.Parts, content.Parts); // Should be a copy
    }

    [Fact]
    public void MultipartContent_ConstructorWithNullIMultipartContent_CreatesEmptyList()
    {
        // Arrange
        var mockMultipartContent = new MockMultipartContent { Parts = null! };

        // Act
        var content = new MultipartContent(mockMultipartContent);

        // Assert
        Assert.NotNull(content.Parts);
        Assert.Empty(content.Parts);
    }

    [Fact]
    public void MultipartContent_PartsProperty_CanBeModified()
    {
        // Arrange
        var content = new MultipartContent();

        // Act
        content.Parts.Add(new TextContentPart("Added text"));
        content.Parts.Add(new HtmlContentPart("<p>Added HTML</p>"));

        // Assert
        Assert.Equal(2, content.Parts.Count);
        Assert.IsType<TextContentPart>(content.Parts[0]);
        Assert.IsType<HtmlContentPart>(content.Parts[1]);
    }

    [Fact]
    public void MultipartContent_PartsProperty_CanBeReplaced()
    {
        // Arrange
        var content = new MultipartContent();
        var newParts = new List<MessageContentPart>
        {
            new TextContentPart("Replacement text")
        };

        // Act
        content.Parts = newParts;

        // Assert
        Assert.Same(newParts, content.Parts);
        Assert.Single(content.Parts);
    }

    [Fact]
    public void IMultipartContent_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var parts = new List<MessageContentPart>
        {
            new TextContentPart("Interface text"),
            new HtmlContentPart("<span>Interface HTML</span>")
        };
        var content = new MultipartContent(parts);

        // Act & Assert
        IMultipartContent iMultipartContent = content;
        Assert.NotNull(iMultipartContent.Parts);
        Assert.Equal(2, iMultipartContent.Parts.Count());
        Assert.All(iMultipartContent.Parts, part => Assert.IsAssignableFrom<IMessageContentPart>(part));
    }

    [Fact]
    public void IMessageContent_Implementation_ExposesCorrectContentType()
    {
        // Arrange
        var content = new MultipartContent();

        // Act & Assert
        IMessageContent iMessageContent = content;
        Assert.Equal(MessageContentType.Multipart, iMessageContent.ContentType);
    }

    [Fact]
    public void MultipartContent_WithMixedContentTypes_HandlesCorrectly()
    {
        // Arrange
        var parts = new List<MessageContentPart>
        {
            new TextContentPart("Plain text part", "utf-8"),
            new HtmlContentPart("<html><body>HTML part</body></html>"),
            new TextContentPart("Another text part"),
            new HtmlContentPart("<div>Another HTML part</div>")
        };

        // Act
        var content = new MultipartContent(parts);

        // Assert
        Assert.Equal(4, content.Parts.Count);
        Assert.Equal(2, content.Parts.Count(p => p is TextContentPart));
        Assert.Equal(2, content.Parts.Count(p => p is HtmlContentPart));
    }

    [Fact]
    public void MultipartContent_WithEmptyPartsList_HandlesCorrectly()
    {
        // Arrange
        var emptyParts = new List<MessageContentPart>();

        // Act
        var content = new MultipartContent(emptyParts);

        // Assert
        Assert.NotNull(content.Parts);
        Assert.Empty(content.Parts);
    }

    // Helper class for testing
    private class MockMultipartContent : IMultipartContent
    {
        public IEnumerable<IMessageContentPart> Parts { get; set; } = Array.Empty<IMessageContentPart>();
        public MessageContentType ContentType => MessageContentType.Multipart;
    }
}