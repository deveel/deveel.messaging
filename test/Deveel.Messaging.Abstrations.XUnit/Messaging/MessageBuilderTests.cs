using System.Collections.Generic;

namespace Deveel.Messaging;

public class MessageBuilderTests
{
    [Fact]
    public void MessageBuilder_DefaultConstructor_CreatesEmptyMessage()
    {
        // Arrange & Act
        var builder = new MessageBuilder();

        // Assert
        Assert.NotNull(builder.Message);
        Assert.Equal("", builder.Message.Id);
        Assert.Null(builder.Message.Sender);
        Assert.Null(builder.Message.Receiver);
        Assert.Null(builder.Message.Content);
        Assert.Null(builder.Message.Properties);
    }

    [Fact]
    public void MessageBuilder_WithExistingMessage_CopiesMessage()
    {
        // Arrange
        var originalMessage = new Message
        {
            Id = "test-id",
            Sender = new Endpoint(KnownEndpointTypes.Email, "sender@test.com"),
            Receiver = new Endpoint(KnownEndpointTypes.Email, "receiver@test.com"),
            Content = new TextContent("Test content"),
            Properties = new Dictionary<string, MessageProperty> { { "key", new MessageProperty("key", "value") } }
        };

        // Act
        var builder = new MessageBuilder(originalMessage);

        // Assert
        Assert.Equal("test-id", builder.Message.Id);
        Assert.Equal("sender@test.com", builder.Message.Sender!.Address);
        Assert.Equal("receiver@test.com", builder.Message.Receiver!.Address);
        Assert.IsType<TextContent>(builder.Message.Content);
        Assert.Equal("Test content", ((TextContent)builder.Message.Content).Text);
        Assert.Contains("key", builder.Message.Properties!.Keys);
        Assert.Equal("value", builder.Message.Properties["key"].Value);
    }

    [Fact]
    public void WithId_SetsMessageId()
    {
        // Arrange
        var builder = new MessageBuilder();
        var messageId = "unique-message-id";

        // Act
        var result = builder.WithId(messageId);

        // Assert
        Assert.Same(builder, result); // Should return same instance for chaining
        Assert.Equal(messageId, builder.Message.Id);
    }

    [Fact]
    public void WithId_NullId_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MessageBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithId(null!));
    }

    [Fact]
    public void WithSender_SetsMessageSender()
    {
        // Arrange
        var builder = new MessageBuilder();
        var sender = new Endpoint(KnownEndpointTypes.Email, "sender@test.com");

        // Act
        var result = builder.WithSender(sender);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(KnownEndpointTypes.Email, builder.Message.Sender!.Type);
        Assert.Equal("sender@test.com", builder.Message.Sender.Address);
    }

    [Fact]
    public void WithSender_NullSender_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MessageBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithSender(null!));
    }

    [Fact]
    public void WithEmailSender_SetsEmailSender()
    {
        // Arrange
        var builder = new MessageBuilder();
        var email = "sender@test.com";

        // Act
        var result = builder.WithEmailSender(email);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(KnownEndpointTypes.Email, builder.Message.Sender!.Type);
        Assert.Equal(email, builder.Message.Sender.Address);
    }

    [Fact]
    public void WithPhoneSender_SetsPhoneSender()
    {
        // Arrange
        var builder = new MessageBuilder();
        var phone = "+1234567890";

        // Act
        var result = builder.WithPhoneSender(phone);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(KnownEndpointTypes.Phone, builder.Message.Sender!.Type);
        Assert.Equal(phone, builder.Message.Sender.Address);
    }

    [Fact]
    public void WithReceiver_SetsMessageReceiver()
    {
        // Arrange
        var builder = new MessageBuilder();
        var receiver = new Endpoint(KnownEndpointTypes.Email, "receiver@test.com");

        // Act
        var result = builder.WithReceiver(receiver);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(KnownEndpointTypes.Email, builder.Message.Receiver!.Type);
        Assert.Equal("receiver@test.com", builder.Message.Receiver.Address);
    }

    [Fact]
    public void WithEmailReceiver_SetsEmailReceiver()
    {
        // Arrange
        var builder = new MessageBuilder();
        var email = "receiver@test.com";

        // Act
        var result = builder.WithEmailReceiver(email);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(KnownEndpointTypes.Email, builder.Message.Receiver!.Type);
        Assert.Equal(email, builder.Message.Receiver.Address);
    }

    [Fact]
    public void WithPhoneReceiver_SetsPhoneReceiver()
    {
        // Arrange
        var builder = new MessageBuilder();
        var phone = "+1234567890";

        // Act
        var result = builder.WithPhoneReceiver(phone);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(KnownEndpointTypes.Phone, builder.Message.Receiver!.Type);
        Assert.Equal(phone, builder.Message.Receiver.Address);
    }

    [Fact]
    public void WithContent_SetsMessageContent()
    {
        // Arrange
        var builder = new MessageBuilder();
        var content = new TextContent("Test content");

        // Act
        var result = builder.WithContent(content);

        // Assert
        Assert.Same(builder, result);
        Assert.IsType<TextContent>(builder.Message.Content);
        Assert.Equal("Test content", ((TextContent)builder.Message.Content).Text);
    }

    [Fact]
    public void WithContent_NullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MessageBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithContent(null!));
    }

    [Fact]
    public void WithTextContent_SetsTextContent()
    {
        // Arrange
        var builder = new MessageBuilder();
        var text = "Hello, World!";

        // Act
        var result = builder.WithTextContent(text);

        // Assert
        Assert.Same(builder, result);
        Assert.IsType<TextContent>(builder.Message.Content);
        Assert.Equal(text, ((TextContent)builder.Message.Content).Text);
    }

    [Fact]
    public void WithTextContent_WithEncoding_SetsTextContentWithEncoding()
    {
        // Arrange
        var builder = new MessageBuilder();
        var text = "Hello, World!";
        var encoding = "utf-8";

        // Act
        var result = builder.WithTextContent(text, encoding);

        // Assert
        Assert.Same(builder, result);
        Assert.IsType<TextContent>(builder.Message.Content);
        var textContent = (TextContent)builder.Message.Content;
        Assert.Equal(text, textContent.Text);
        Assert.Equal(encoding, textContent.Encoding);
    }

    [Fact]
    public void With_Properties_SetsMessageProperties()
    {
        // Arrange
        var builder = new MessageBuilder();
        var properties = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        // Act
        var result = builder.With(properties);

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(builder.Message.Properties);
        Assert.Equal("value1", builder.Message.Properties["key1"].Value);
        Assert.Equal(123, builder.Message.Properties["key2"].Value);
    }

    [Fact]
    public void With_SingleProperty_SetsMessageProperty()
    {
        // Arrange
        var builder = new MessageBuilder();

        // Act
        var result = builder.With("testKey", "testValue");

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(builder.Message.Properties);
        Assert.Equal("testValue", builder.Message.Properties["testKey"].Value);
    }

    [Fact]
    public void With_MergesExistingProperties()
    {
        // Arrange
        var builder = new MessageBuilder();
        builder.With("existingKey", "existingValue");

        // Act
        var result = builder.With("newKey", "newValue");

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(builder.Message.Properties);
        Assert.Equal("existingValue", builder.Message.Properties["existingKey"].Value);
        Assert.Equal("newValue", builder.Message.Properties["newKey"].Value);
    }

    [Fact]
    public void WithSubject_SetsSubjectProperty()
    {
        // Arrange
        var builder = new MessageBuilder();
        var subject = "Test Subject";

        // Act
        var result = builder.WithSubject(subject);

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(builder.Message.Properties);
        Assert.Equal(subject, builder.Message.Properties[KnownMessageProperties.Subject].Value);
    }

    [Fact]
    public void WithRemoteId_SetsRemoteMessageIdProperty()
    {
        // Arrange
        var builder = new MessageBuilder();
        var remoteId = "remote-message-id";

        // Act
        var result = builder.WithRemoteId(remoteId);

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(builder.Message.Properties);
        Assert.Equal(remoteId, builder.Message.Properties[KnownMessageProperties.RemoteMessageId].Value);
    }

    [Fact]
    public void WithReplyTo_SetsReplyToProperty()
    {
        // Arrange
        var builder = new MessageBuilder();
        var replyToId = "reply-to-message-id";

        // Act
        var result = builder.WithReplyTo(replyToId);

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(builder.Message.Properties);
        Assert.Equal(replyToId, builder.Message.Properties[KnownMessageProperties.ReplyTo].Value);
    }

    [Fact]
    public void FluentInterface_MethodChaining_BuildsCompleteMessage()
    {
        // Arrange & Act
        var message = new MessageBuilder()
            .WithId("test-message-id")
            .WithEmailSender("sender@test.com")
            .WithEmailReceiver("receiver@test.com")
            .WithTextContent("Hello, World!")
            .WithSubject("Test Subject")
            .WithRemoteId("remote-123")
            .WithReplyTo("original-message-id")
            .Message;

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
        var builder = new MessageBuilder()
            .WithId("test-id")
            .WithEmailSender("sender@test.com")
            .WithEmailReceiver("receiver@test.com")
            .WithTextContent("Test content")
            .With("testProp", "testValue");

        // Act & Assert (testing IMessage interface)
        IMessage message = builder;
        Assert.Equal("test-id", message.Id);
        Assert.Equal("sender@test.com", message.Sender!.Address);
        Assert.Equal("receiver@test.com", message.Receiver!.Address);
        Assert.IsAssignableFrom<IMessageContent>(message.Content);
        Assert.NotNull(message.Properties);
        Assert.Equal("testValue", message.Properties["testProp"].Value);
    }
}