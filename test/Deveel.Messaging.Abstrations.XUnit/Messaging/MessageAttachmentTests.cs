namespace Deveel.Messaging;

public class MessageAttachmentTests
{
    [Fact]
    public void MessageAttachment_DefaultConstructor_CreatesEmptyAttachment()
    {
        // Arrange & Act
        var attachment = new MessageAttachment();

        // Assert
        Assert.Equal("", attachment.Id);
        Assert.Equal("", attachment.FileName);
        Assert.Equal("", attachment.MimeType);
        Assert.Equal("", attachment.Content);
    }

    [Fact]
    public void MessageAttachment_ConstructorWithParameters_SetsProperties()
    {
        // Arrange
        var id = "attachment-1";
        var fileName = "document.pdf";
        var mimeType = "application/pdf";
        var content = "base64encodedcontent";

        // Act
        var attachment = new MessageAttachment(id, fileName, mimeType, content);

        // Assert
        Assert.Equal(id, attachment.Id);
        Assert.Equal(fileName, attachment.FileName);
        Assert.Equal(mimeType, attachment.MimeType);
        Assert.Equal(content, attachment.Content);
    }

    [Fact]
    public void MessageAttachment_ConstructorWithIAttachment_CopiesProperties()
    {
        // Arrange
        var sourceAttachment = new MessageAttachment("source-id", "source.txt", "text/plain", "sourcecontent");

        // Act
        var attachment = new MessageAttachment(sourceAttachment);

        // Assert
        Assert.Equal("source-id", attachment.Id);
        Assert.Equal("source.txt", attachment.FileName);
        Assert.Equal("text/plain", attachment.MimeType);
        Assert.Equal("sourcecontent", attachment.Content);
    }

    [Fact]
    public void MessageAttachment_PropertySetters_UpdateValues()
    {
        // Arrange
        var attachment = new MessageAttachment();

        // Act
        attachment.Id = "new-id";
        attachment.FileName = "newfile.jpg";
        attachment.MimeType = "image/jpeg";
        attachment.Content = "newbase64content";

        // Assert
        Assert.Equal("new-id", attachment.Id);
        Assert.Equal("newfile.jpg", attachment.FileName);
        Assert.Equal("image/jpeg", attachment.MimeType);
        Assert.Equal("newbase64content", attachment.Content);
    }

    [Fact]
    public void IAttachment_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var attachment = new MessageAttachment("test-id", "test.doc", "application/msword", "testcontent");

        // Act & Assert
        IAttachment iAttachment = attachment;
        Assert.Equal("test-id", iAttachment.Id);
        Assert.Equal("test.doc", iAttachment.FileName);
        Assert.Equal("application/msword", iAttachment.MimeType);
        Assert.Equal("testcontent", iAttachment.Content);
    }

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("text.txt", "text/plain")]
    [InlineData("video.mp4", "video/mp4")]
    public void MessageAttachment_VariousFileTypes_HandlesCorrectly(string fileName, string mimeType)
    {
        // Arrange & Act
        var attachment = new MessageAttachment("1", fileName, mimeType, "content");

        // Assert
        Assert.Equal(fileName, attachment.FileName);
        Assert.Equal(mimeType, attachment.MimeType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SGVsbG8gV29ybGQ=")]
    [InlineData("VGhpcyBpcyBhIHRlc3QgZmlsZSBjb250ZW50")]
    public void MessageAttachment_VariousContentValues_HandlesCorrectly(string content)
    {
        // Arrange & Act
        var attachment = new MessageAttachment("1", "test.txt", "text/plain", content);

        // Assert
        Assert.Equal(content, attachment.Content);
    }

    [Fact]
    public void MessageAttachment_WithEmptyValues_HandlesCorrectly()
    {
        // Arrange & Act
        var attachment = new MessageAttachment("", "", "", "");

        // Assert
        Assert.Equal("", attachment.Id);
        Assert.Equal("", attachment.FileName);
        Assert.Equal("", attachment.MimeType);
        Assert.Equal("", attachment.Content);
    }
}