namespace Deveel.Messaging;

/// <summary>
/// Tests for messaging interfaces to ensure they have the correct structure and contracts.
/// Since these are interfaces without concrete implementations in the abstractions project,
/// these tests verify the interface contracts using mock implementations.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IMessageBatch_HasCorrectProperties()
    {
        // Arrange
        var mockBatch = new MockMessageBatch
        {
            Id = "batch-123",
            Properties = new Dictionary<string, object> { { "key", "value" } },
            Messages = new List<IMessage>
            {
                new Message { Id = "msg-1" },
                new Message { Id = "msg-2" }
            }
        };

        // Act & Assert
        IMessageBatch batch = mockBatch;
        Assert.Equal("batch-123", batch.Id);
        Assert.NotNull(batch.Properties);
        Assert.Equal("value", batch.Properties["key"]);
        Assert.NotNull(batch.Messages);
        Assert.Equal(2, batch.Messages.Count());
    }

    [Fact]
    public void IMessageChannel_HasCorrectProperties()
    {
        // Arrange
        var mockChannel = new MockMessageChannel
        {
            Id = "channel-456",
            Type = "email",
            Provider = "smtp-provider",
            Name = "Email Channel"
        };

        // Act & Assert
        IMessageChannel channel = mockChannel;
        Assert.Equal("channel-456", channel.Id);
        Assert.Equal("email", channel.Type);
        Assert.Equal("smtp-provider", channel.Provider);
        Assert.Equal("Email Channel", channel.Name);
    }

    [Fact]
    public void IMessageChannel_SupportsNullValues()
    {
        // Arrange
        var mockChannel = new MockMessageChannel
        {
            Id = null,
            Type = "sms",
            Provider = "sms-provider",
            Name = null
        };

        // Act & Assert
        IMessageChannel channel = mockChannel;
        Assert.Null(channel.Id);
        Assert.Equal("sms", channel.Type);
        Assert.Equal("sms-provider", channel.Provider);
        Assert.Null(channel.Name);
    }

    [Fact]
    public void IMessageState_HasCorrectProperties()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var error = new MockMessageError { Code = "ERR001", Message = "Test error" };
        var remoteError = new MockMessageError { Code = "REMOTE001", Message = "Remote error" };
        var mockState = new MockMessageState
        {
            Id = "state-789",
            MessageId = "msg-123",
            Status = MessageStatus.Delivered,
            Error = error,
            RemoteError = remoteError,
            TimeStamp = timestamp,
            Properties = new Dictionary<string, object> { { "attempt", 1 } }
        };

        // Act & Assert
        IMessageState state = mockState;
        Assert.Equal("state-789", state.Id);
        Assert.Equal("msg-123", state.MessageId);
        Assert.Equal(MessageStatus.Delivered, state.Status);
        Assert.Same(error, state.Error);
        Assert.Same(remoteError, state.RemoteError);
        Assert.Equal(timestamp, state.TimeStamp);
        Assert.NotNull(state.Properties);
        Assert.Equal(1, state.Properties["attempt"]);
    }

    [Fact]
    public void IMessageState_SupportsNullErrors()
    {
        // Arrange
        var mockState = new MockMessageState
        {
            Id = "state-success",
            MessageId = "msg-success",
            Status = MessageStatus.Delivered,
            Error = null,
            RemoteError = null,
            TimeStamp = DateTimeOffset.UtcNow,
            Properties = null
        };

        // Act & Assert
        IMessageState state = mockState;
        Assert.Equal("state-success", state.Id);
        Assert.Equal("msg-success", state.MessageId);
        Assert.Equal(MessageStatus.Delivered, state.Status);
        Assert.Null(state.Error);
        Assert.Null(state.RemoteError);
        Assert.Null(state.Properties);
    }

    [Fact]
    public void IMessageError_HasCorrectProperties()
    {
        // Arrange
        var innerError = new MockMessageError { Code = "INNER001", Message = "Inner error" };
        var mockError = new MockMessageError
        {
            Code = "ERR001",
            Message = "Test error message",
            InnerError = innerError
        };

        // Act & Assert
        IMessageError error = mockError;
        Assert.Equal("ERR001", error.Code);
        Assert.Equal("Test error message", error.Message);
        Assert.Same(innerError, error.InnerError);
    }

    [Fact]
    public void IMessageError_SupportsNullValues()
    {
        // Arrange
        var mockError = new MockMessageError
        {
            Code = "ERR002",
            Message = null,
            InnerError = null
        };

        // Act & Assert
        IMessageError error = mockError;
        Assert.Equal("ERR002", error.Code);
        Assert.Null(error.Message);
        Assert.Null(error.InnerError);
    }

    [Fact]
    public void IMessageError_SupportsNestedErrors()
    {
        // Arrange
        var level3Error = new MockMessageError { Code = "L3", Message = "Level 3 error" };
        var level2Error = new MockMessageError { Code = "L2", Message = "Level 2 error", InnerError = level3Error };
        var level1Error = new MockMessageError { Code = "L1", Message = "Level 1 error", InnerError = level2Error };

        // Act & Assert
        IMessageError error = level1Error;
        Assert.Equal("L1", error.Code);
        Assert.Equal("Level 1 error", error.Message);
        Assert.NotNull(error.InnerError);
        Assert.Equal("L2", error.InnerError.Code);
        Assert.NotNull(error.InnerError.InnerError);
        Assert.Equal("L3", error.InnerError.InnerError.Code);
    }

    // Mock implementations for testing interface contracts
    private class MockMessageBatch : IMessageBatch
    {
        public string Id { get; set; } = "";
        public IDictionary<string, object>? Properties { get; set; }
        public IEnumerable<IMessage> Messages { get; set; } = Array.Empty<IMessage>();
    }

    private class MockMessageChannel : IMessageChannel
    {
        public string? Id { get; set; }
        public string Type { get; set; } = "";
        public string Provider { get; set; } = "";
        public string? Name { get; set; }
    }

    private class MockMessageState : IMessageState
    {
        public string Id { get; set; } = "";
        public string MessageId { get; set; } = "";
        public MessageStatus Status { get; set; }
        public IMessageError? Error { get; set; }
        public IMessageError? RemoteError { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public IDictionary<string, object>? Properties { get; set; }
    }

    private class MockMessageError : IMessageError
    {
        public string Code { get; set; } = "";
        public string? Message { get; set; }
        public IMessageError? InnerError { get; set; }
    }
}

/// <summary>
/// Additional tests to verify interface usage patterns and compatibility.
/// </summary>
public class InterfaceUsageTests
{
    [Fact]
    public void AllContentInterfaces_CanBeAssignedToIMessageContent()
    {
        // Arrange & Act
        IMessageContent textContent = new TextContent("test");
        IMessageContent htmlContent = new HtmlContent("<p>test</p>");
        IMessageContent jsonContent = new JsonContent("{}");
        IMessageContent templateContent = new TemplateContent("template-id", new Dictionary<string, object?>());
        IMessageContent binaryContent = new BinaryContent(new byte[] { 1, 2, 3 }, "application/octet-stream");
        IMessageContent mediaContent = new MediaContent(MediaType.Image, "image.png", new byte[] { 1, 2, 3 });
        IMessageContent multipartContent = new MultipartContent();

        // Assert
        Assert.Equal(MessageContentType.PlainText, textContent.ContentType);
        Assert.Equal(MessageContentType.Html, htmlContent.ContentType);
        Assert.Equal(MessageContentType.Json, jsonContent.ContentType);
        Assert.Equal(MessageContentType.Template, templateContent.ContentType);
        Assert.Equal(MessageContentType.Binary, binaryContent.ContentType);
        Assert.Equal(MessageContentType.Binary, mediaContent.ContentType);
        Assert.Equal(MessageContentType.Multipart, multipartContent.ContentType);
    }

    [Fact]
    public void AllContentPartInterfaces_CanBeAssignedToIMessageContentPart()
    {
        // Arrange & Act
        IMessageContentPart textPart = new TextContentPart("test");
        IMessageContentPart htmlPart = new HtmlContentPart("<p>test</p>");

        // Assert
        Assert.Equal(MessageContentType.PlainText, textPart.ContentType);
        Assert.Equal(MessageContentType.Html, htmlPart.ContentType);
    }

    [Fact]
    public void IEndpoint_CanBeImplementedByEndpoint()
    {
        // Arrange & Act
        IEndpoint endpoint = new Endpoint("email", "test@example.com");

        // Assert
        Assert.Equal("email", endpoint.Type);
        Assert.Equal("test@example.com", endpoint.Address);
    }

    [Fact]
    public void IMessage_CanBeImplementedByMessage()
    {
        // Arrange & Act
        IMessage message = new Message
        {
            Id = "test-id",
            Sender = new Endpoint("email", "sender@test.com"),
            Receiver = new Endpoint("email", "receiver@test.com"),
            Content = new TextContent("test content"),
            Properties = new Dictionary<string, MessageProperty> { { "key", new MessageProperty("key", "value") } }
        };

        // Assert
        Assert.Equal("test-id", message.Id);
        Assert.Equal("sender@test.com", message.Sender!.Address);
        Assert.Equal("receiver@test.com", message.Receiver!.Address);
        Assert.Equal(MessageContentType.PlainText, message.Content!.ContentType);
        Assert.Equal("value", message.Properties!["key"].Value);
    }

    [Fact]
    public void IAttachment_CanBeImplementedByMessageAttachment()
    {
        // Arrange & Act
        IAttachment attachment = new MessageAttachment("att-1", "file.txt", "text/plain", "content");

        // Assert
        Assert.Equal("att-1", attachment.Id);
        Assert.Equal("file.txt", attachment.FileName);
        Assert.Equal("text/plain", attachment.MimeType);
        Assert.Equal("content", attachment.Content);
    }
}