using System.Collections.Generic;

namespace Deveel.Messaging;

public class MessageBuilderTests
{
    [Fact]
    public void Message_DefaultConstructor_CreatesEmptyMessage()
    {
        // Arrange & Act
        var message = new Message();

        // Assert
        Assert.NotNull(message);
        Assert.Equal("", message.Id);
        Assert.Null(message.Sender);
        Assert.Null(message.Receiver);
        Assert.Null(message.Content);
        Assert.Null(message.Properties);
    }

    [Fact]
    public void Message_WithExistingMessage_CopiesMessage()
    {
        // Arrange
        var originalMessage = new Message
        {
            Id = "test-id",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@test.com"),
            Receiver = new Endpoint(EndpointType.EmailAddress, "receiver@test.com"),
            Content = new TextContent("Test content"),
            Properties = new Dictionary<string, MessageProperty> { { "key", new MessageProperty("key", "value") } }
        };

        // Act
        var copiedMessage = new Message(originalMessage);

        // Assert
        Assert.Equal("test-id", copiedMessage.Id);
        Assert.Equal("sender@test.com", copiedMessage.Sender!.Address);
        Assert.Equal("receiver@test.com", copiedMessage.Receiver!.Address);
        Assert.IsType<TextContent>(copiedMessage.Content);
        Assert.Equal("Test content", ((TextContent)copiedMessage.Content).Text);
        Assert.Contains("key", copiedMessage.Properties!.Keys);
        Assert.Equal("value", copiedMessage.Properties["key"].Value);
    }

    [Fact]
    public void WithId_SetsMessageId()
    {
        // Arrange
        var message = new Message();
        var messageId = "unique-message-id";

        // Act
        var result = message.WithId(messageId);

        // Assert
        Assert.Same(message, result); // Should return same instance for chaining
        Assert.Equal(messageId, message.Id);
    }

    [Fact]
    public void WithId_NullId_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new Message();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.WithId(null!));
    }

    [Fact]
    public void WithSender_SetsMessageSender()
    {
        // Arrange
        var message = new Message();
        var sender = new Endpoint(EndpointType.EmailAddress, "sender@test.com");

        // Act
        var result = message.WithSender(sender);

        // Assert
        Assert.Same(message, result);
        Assert.Equal(EndpointType.EmailAddress, message.Sender!.Type);
        Assert.Equal("sender@test.com", message.Sender.Address);
    }

    [Fact]
    public void WithSender_NullSender_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new Message();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.WithSender(null!));
    }

    [Fact]
    public void WithEmailSender_SetsEmailSender()
    {
        // Arrange
        var message = new Message();
        var email = "sender@test.com";

        // Act
        var result = message.WithEmailSender(email);

        // Assert
        Assert.Same(message, result);
        Assert.Equal(EndpointType.EmailAddress, message.Sender!.Type);
        Assert.Equal(email, message.Sender.Address);
    }

    [Fact]
    public void WithPhoneSender_SetsPhoneSender()
    {
        // Arrange
        var message = new Message();
        var phone = "+1234567890";

        // Act
        var result = message.WithPhoneSender(phone);

        // Assert
        Assert.Same(message, result);
        Assert.Equal(EndpointType.PhoneNumber, message.Sender!.Type);
        Assert.Equal(phone, message.Sender.Address);
    }

    [Fact]
    public void WithReceiver_SetsMessageReceiver()
    {
        // Arrange
        var message = new Message();
        var receiver = new Endpoint(EndpointType.EmailAddress, "receiver@test.com");

        // Act
        var result = message.WithReceiver(receiver);

        // Assert
        Assert.Same(message, result);
        Assert.Equal(EndpointType.EmailAddress, message.Receiver!.Type);
        Assert.Equal("receiver@test.com", message.Receiver.Address);
    }

    [Fact]
    public void WithEmailReceiver_SetsEmailReceiver()
    {
        // Arrange
        var message = new Message();
        var email = "receiver@test.com";

        // Act
        var result = message.WithEmailReceiver(email);

        // Assert
        Assert.Same(message, result);
        Assert.Equal(EndpointType.EmailAddress, message.Receiver!.Type);
        Assert.Equal(email, message.Receiver.Address);
    }

    [Fact]
    public void WithPhoneReceiver_SetsPhoneReceiver()
    {
        // Arrange
        var message = new Message();
        var phone = "+1234567890";

        // Act
        var result = message.WithPhoneReceiver(phone);

        // Assert
        Assert.Same(message, result);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver!.Type);
        Assert.Equal(phone, message.Receiver.Address);
    }

    [Fact]
    public void WithContent_SetsMessageContent()
    {
        // Arrange
        var message = new Message();
        var content = new TextContent("Test content");

        // Act
        var result = message.WithContent(content);

        // Assert
        Assert.Same(message, result);
        Assert.IsType<TextContent>(message.Content);
        Assert.Equal("Test content", ((TextContent)message.Content).Text);
    }

    [Fact]
    public void WithContent_NullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new Message();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.WithContent(null!));
    }

    [Fact]
    public void WithTextContent_SetsTextContent()
    {
        // Arrange
        var message = new Message();
        var text = "Hello, World!";

        // Act
        var result = message.WithTextContent(text);

        // Assert
        Assert.Same(message, result);
        Assert.IsType<TextContent>(message.Content);
        Assert.Equal(text, ((TextContent)message.Content).Text);
    }

    [Fact]
    public void WithTextContent_WithEncoding_SetsTextContentWithEncoding()
    {
        // Arrange
        var message = new Message();
        var text = "Hello, World!";
        var encoding = "utf-8";

        // Act
        var result = message.WithTextContent(text, encoding);

        // Assert
        Assert.Same(message, result);
        Assert.IsType<TextContent>(message.Content);
        var textContent = (TextContent)message.Content;
        Assert.Equal(text, textContent.Text);
        Assert.Equal(encoding, textContent.Encoding);
    }

    [Fact]
    public void With_Properties_SetsMessageProperties()
    {
        // Arrange
        var message = new Message();
        var properties = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        // Act
        var result = message.With(properties);

        // Assert
        Assert.Same(message, result);
        Assert.NotNull(message.Properties);
        Assert.Equal("value1", message.Properties["key1"].Value);
        Assert.Equal(123, message.Properties["key2"].Value);
    }

    [Fact]
    public void With_SingleProperty_SetsMessageProperty()
    {
        // Arrange
        var message = new Message();

        // Act
        var result = message.With("testKey", "testValue");

        // Assert
        Assert.Same(message, result);
        Assert.NotNull(message.Properties);
        Assert.Equal("testValue", message.Properties["testKey"].Value);
    }

    [Fact]
    public void With_MergesExistingProperties()
    {
        // Arrange
        var message = new Message();
        message.With("existingKey", "existingValue");

        // Act
        var result = message.With("newKey", "newValue");

        // Assert
        Assert.Same(message, result);
        Assert.NotNull(message.Properties);
        Assert.Equal("existingValue", message.Properties["existingKey"].Value);
        Assert.Equal("newValue", message.Properties["newKey"].Value);
    }

    [Fact]
    public void WithSubject_SetsSubjectProperty()
    {
        // Arrange
        var message = new Message();
        var subject = "Test Subject";

        // Act
        var result = message.WithSubject(subject);

        // Assert
        Assert.Same(message, result);
        Assert.NotNull(message.Properties);
        Assert.Equal(subject, message.Properties[KnownMessageProperties.Subject].Value);
    }

    [Fact]
    public void WithRemoteId_SetsRemoteMessageIdProperty()
    {
        // Arrange
        var message = new Message();
        var remoteId = "remote-message-id";

        // Act
        var result = message.WithRemoteId(remoteId);

        // Assert
        Assert.Same(message, result);
        Assert.NotNull(message.Properties);
        Assert.Equal(remoteId, message.Properties[KnownMessageProperties.RemoteMessageId].Value);
    }

    [Fact]
    public void WithReplyTo_SetsReplyToProperty()
    {
        // Arrange
        var message = new Message();
        var replyToId = "reply-to-message-id";

        // Act
        var result = message.WithReplyTo(replyToId);

        // Assert
        Assert.Same(message, result);
        Assert.NotNull(message.Properties);
        Assert.Equal(replyToId, message.Properties[KnownMessageProperties.ReplyTo].Value);
    }

    [Fact]
    public void FluentInterface_MethodChaining_BuildsCompleteMessage()
    {
        // Arrange & Act
        var message = new Message()
            .WithId("test-message-id")
            .WithEmailSender("sender@test.com")
            .WithEmailReceiver("receiver@test.com")
            .WithTextContent("Hello, World!")
            .WithSubject("Test Subject")
            .WithRemoteId("remote-123")
            .WithReplyTo("original-message-id");

        // Assert
        Assert.Equal("test-message-id", message.Id);
        Assert.Equal("sender@test.com", message.Sender!.Address);
        Assert.Equal("receiver@test.com", message.Receiver!.Address);
        Assert.Equal("Hello, World!", ((TextContent)message.Content!).Text);
        Assert.Equal("Test Subject", message.Properties![KnownMessageProperties.Subject].Value);
        Assert.Equal("remote-123", message.Properties[KnownMessageProperties.RemoteMessageId].Value);
        Assert.Equal("original-message-id", message.Properties[KnownMessageProperties.ReplyTo].Value);
    }

    [Fact]
    public void IMessage_Implementation_ReturnsCorrectValues()
    {
        // Arrange
        var message = new Message()
            .WithId("test-id")
            .WithEmailSender("sender@test.com")
            .WithEmailReceiver("receiver@test.com")
            .WithTextContent("Test content")
            .With("testProp", "testValue");

        // Act & Assert (testing IMessage interface)
        IMessage iMessage = message;
        Assert.Equal("test-id", iMessage.Id);
        Assert.Equal("sender@test.com", iMessage.Sender!.Address);
        Assert.Equal("receiver@test.com", iMessage.Receiver!.Address);
        Assert.IsAssignableFrom<IMessageContent>(iMessage.Content);
        Assert.NotNull(iMessage.Properties);
        Assert.Equal("testValue", iMessage.Properties["testProp"].Value);
    }
}