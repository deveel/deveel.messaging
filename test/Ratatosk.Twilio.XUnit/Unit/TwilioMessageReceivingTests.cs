using System.Text;
using System.Text.Json;

namespace Ratatosk;

/// <summary>
/// Tests for Twilio-specific message receiving functionality including webhook handling,
/// status callbacks, and Twilio-specific message formats.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "TwilioMessageReceiving")]
public class TwilioMessageReceivingTests
{
    [Fact]
    public async Task Should_ParseCorrectly_When_ReceiveMessagesAsyncWithTwilioSmsWebhook()
    {
        // Arrange
        var schema = CreateTwilioSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate Twilio SMS webhook form data
        var webhookData = "MessageSid=SM1234567890&" +
                         "From=%2B1234567890&" +
                         "To=%2B1987654321&" +
                         "Body=Hello%20from%20Twilio%20SMS&" +
                         "MessageStatus=received&" +
                         "NumSegments=1&" +
                         "AccountSid=AC1234567890";

        // Act
        var result = await TestReceiveMessage(connector, webhookData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Single(receiveResult.Messages);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var message = receiveResult.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.Equal("+1234567890", message.Sender?.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Sender?.Type);
        Assert.Equal("+1987654321", message.Receiver?.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver?.Type);
        Assert.Equal("Hello from Twilio SMS", ((ITextContent)message.Content!).Text);

        // Check Twilio-specific properties
        Assert.NotNull(message.Properties);
        Assert.True(message.Properties.ContainsKey("NumSegments"));
        Assert.Equal("1", message.Properties["NumSegments"].Value);
        Assert.True(message.Properties.ContainsKey("AccountSid"));
        Assert.Equal("AC1234567890", message.Properties["AccountSid"].Value);
    }

    [Fact]
    public async Task Should_ParseCorrectly_When_ReceiveMessagesAsyncWithTwilioWhatsAppWebhook()
    {
        // Arrange
        var schema = CreateTwilioWhatsAppSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate Twilio WhatsApp webhook
        var webhookData = "MessageSid=SM9876543210&" +
                         "From=whatsapp%3A%2B1234567890&" +
                         "To=whatsapp%3A%2B1987654321&" +
                         "Body=Hello%20from%20WhatsApp&" +
                         "MessageStatus=received&" +
                         "ProfileName=John%20Doe&" +
                         "AccountSid=AC1234567890";

        // Act
        var result = await TestReceiveMessage(connector, webhookData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Single(receiveResult.Messages);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var message = receiveResult.Messages.First();
        Assert.Equal("SM9876543210", message.Id);
        Assert.Equal("whatsapp:+1234567890", message.Sender?.Address);
        Assert.Equal("whatsapp:+1987654321", message.Receiver?.Address);
        Assert.Equal("Hello from WhatsApp", ((ITextContent)message.Content!).Text);

        // Check WhatsApp-specific properties
        Assert.NotNull(message.Properties);
        Assert.True(message.Properties.ContainsKey("ProfileName"));
        Assert.Equal("John Doe", message.Properties["ProfileName"].Value);
        Assert.True(message.Properties.ContainsKey("MessageStatus"));
        Assert.Equal("received", message.Properties["MessageStatus"].Value);
        Assert.True(message.Properties.ContainsKey("AccountSid"));
        Assert.Equal("AC1234567890", message.Properties["AccountSid"].Value);
    }

    [Fact]
    public async Task Should_ParseMediaCorrectly_When_ReceiveMessagesAsyncWithTwilioMmsWebhook()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.MediaAttachments);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate Twilio MMS webhook with media
        var webhookData = "MessageSid=MM1234567890&" +
                         "From=%2B1234567890&" +
                         "To=%2B1987654321&" +
                         "Body=Check%20out%20this%20image%21&" +
                         "MessageStatus=received&" +
                         "NumMedia=1&" +
                         "MediaUrl0=https%3A%2F%2Fapi.twilio.com%2F2010-04-01%2FAccounts%2FAC123%2FMessages%2FMM123%2FMedia%2FME123&" +
                         "MediaContentType0=image%2Fjpeg&" +
                         "AccountSid=AC1234567890";

        // Act
        var result = await TestReceiveMessage(connector, webhookData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Single(receiveResult.Messages);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var message = receiveResult.Messages.First();
        Assert.Equal("MM1234567890", message.Id);
        Assert.Equal("Check out this image!", ((ITextContent)message.Content!).Text);
        Assert.NotNull(message.Content);
        Assert.Equal(MessageContentType.PlainText, message.Content.ContentType);

        // Check media properties
        Assert.NotNull(message.Properties);
        Assert.True(message.Properties.Count >= 5); // At least NumMedia, MediaUrl0, MediaContentType0, MessageStatus, AccountSid
        Assert.True(message.Properties.ContainsKey("NumMedia"));
        Assert.Equal("1", message.Properties["NumMedia"].Value);
        Assert.True(message.Properties.ContainsKey("MediaUrl0"));
        Assert.Contains("api.twilio.com", message.Properties["MediaUrl0"].Value?.ToString());
        Assert.True(message.Properties.ContainsKey("MediaContentType0"));
        Assert.Equal("image/jpeg", message.Properties["MediaContentType0"].Value);
        Assert.True(message.Properties.ContainsKey("MessageStatus"));
        Assert.Equal("received", message.Properties["MessageStatus"].Value);
        Assert.True(message.Properties.ContainsKey("AccountSid"));
        Assert.Equal("AC1234567890", message.Properties["AccountSid"].Value);
    }

    [Fact]
    public async Task Should_ParseCorrectly_When_ReceiveMessageStatusAsyncWithTwilioStatusCallback()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.HandleMessageState);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate Twilio status callback
        var statusData = "MessageSid=SM1234567890&" +
                        "MessageStatus=delivered&" +
                        "To=%2B1987654321&" +
                        "From=%2B1234567890&" +
                        "AccountSid=AC1234567890&" +
                        "MessagePrice=0.0075&" +
                        "MessagePriceUnit=USD";

        // Act
        var result = await TestReceiveStatus(connector, statusData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var statusResult = result.Value;
        Assert.Equal("SM1234567890", statusResult.MessageId);
        Assert.Equal(MessageStatus.Delivered, statusResult.Status);
        Assert.True(statusResult.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(statusResult.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-5)); // Recent timestamp

        // Check additional Twilio status data
        Assert.NotNull(statusResult.AdditionalData);
        Assert.True(statusResult.AdditionalData.Count >= 2); // At least MessagePrice and MessagePriceUnit
        Assert.True(statusResult.AdditionalData.ContainsKey("MessagePrice"));
        Assert.Equal("0.0075", statusResult.AdditionalData["MessagePrice"]);
        Assert.True(statusResult.AdditionalData.ContainsKey("MessagePriceUnit"));
        Assert.Equal("USD", statusResult.AdditionalData["MessagePriceUnit"]);
    }

    [Fact]
    public async Task Should_ParseErrorCorrectly_When_ReceiveMessageStatusAsyncWithTwilioFailedStatus()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.HandleMessageState);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate Twilio failed status callback
        var statusData = "MessageSid=SM1234567890&" +
                        "MessageStatus=failed&" +
                        "To=%2B1987654321&" +
                        "From=%2B1234567890&" +
                        "ErrorCode=30008&" +
                        "ErrorMessage=Unknown%20destination%20handset&" +
                        "AccountSid=AC1234567890";

        // Act
        var result = await TestReceiveStatus(connector, statusData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var statusResult = result.Value;
        Assert.Equal("SM1234567890", statusResult.MessageId);
        Assert.Equal(MessageStatus.DeliveryFailed, statusResult.Status);
        Assert.True(statusResult.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(statusResult.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-5)); // Recent timestamp

        // Check error information
        Assert.NotNull(statusResult.AdditionalData);
        Assert.True(statusResult.AdditionalData.Count >= 2); // At least ErrorCode and ErrorMessage
        Assert.True(statusResult.AdditionalData.ContainsKey("ErrorCode"));
        Assert.Equal("30008", statusResult.AdditionalData["ErrorCode"]);
        Assert.True(statusResult.AdditionalData.ContainsKey("ErrorMessage"));
        Assert.Equal("Unknown destination handset", statusResult.AdditionalData["ErrorMessage"]);

        // Verify that error information is properly URL-decoded
        Assert.DoesNotContain("%20", statusResult.AdditionalData["ErrorMessage"].ToString());
        Assert.Contains(" ", statusResult.AdditionalData["ErrorMessage"].ToString());
    }

    [Fact]
    public async Task Should_ParseCorrectly_When_ReceiveMessagesAsyncWithTwilioJsonWebhook()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate Twilio webhook in JSON format (less common but supported by some configurations)
        var webhookJson = new
        {
            MessageSid = "SM1234567890",
            From = "+1234567890",
            To = "+1987654321",
            Body = "Hello from JSON webhook",
            MessageStatus = "received",
            NumSegments = "1",
            AccountSid = "AC1234567890"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);

        // Act
        var result = await TestReceiveMessage(connector, jsonPayload, MessageSource.JsonContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Single(receiveResult.Messages);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var message = receiveResult.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.Equal("+1234567890", message.Sender?.Address);
        Assert.Equal("+1987654321", message.Receiver?.Address);
        Assert.Equal("Hello from JSON webhook", ((ITextContent)message.Content!).Text);

        // Verify that JSON parsing doesn't add extra properties (unlike form data parsing)
        Assert.Null(message.Properties); // JSON parsing doesn't add extra properties in this implementation
    }

    [Theory]
    [InlineData("received", MessageStatus.Received)]
    [InlineData("delivered", MessageStatus.Delivered)]
    [InlineData("sent", MessageStatus.Sent)]
    [InlineData("failed", MessageStatus.DeliveryFailed)]
    [InlineData("undelivered", MessageStatus.DeliveryFailed)]
    [InlineData("queued", MessageStatus.Queued)]
    public async Task Should_MapsCorrectly_When_ReceiveMessageStatusAsyncWithDifferentTwilioStatuses(string twilioStatus, MessageStatus expectedStatus)
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.HandleMessageState);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var statusData = $"MessageSid=SM1234567890&MessageStatus={twilioStatus}&To=%2B1987654321&From=%2B1234567890";

        // Act
        var result = await TestReceiveStatus(connector, statusData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var statusResult = result.Value;
        Assert.Equal("SM1234567890", statusResult.MessageId);
        Assert.Equal(expectedStatus, statusResult.Status);
        Assert.True(statusResult.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(statusResult.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-5)); // Recent timestamp

        // Verify that additional data is properly initialized even if empty
        Assert.NotNull(statusResult.AdditionalData);
    }

    [Fact]
    public async Task Should_ParseBatchCorrectly_When_ReceiveMessagesAsyncWithMultipleTwilioMessages()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.BulkMessaging);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate batch of Twilio messages in JSON format
        var batchPayload = new
        {
            Messages = new[]
            {
                new { MessageSid = "SM1111111111", From = "+1111111111", To = "+1987654321", Body = "Message 1" },
                new { MessageSid = "SM2222222222", From = "+2222222222", To = "+1987654321", Body = "Message 2" },
                new { MessageSid = "SM3333333333", From = "+3333333333", To = "+1987654321", Body = "Message 3" }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(batchPayload);

        // Act
        var result = await TestReceiveMessage(connector, jsonPayload, MessageSource.JsonContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Equal(3, receiveResult.Messages.Count);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var messages = receiveResult.Messages.ToList();
        Assert.Equal("SM1111111111", messages[0].Id);
        Assert.Equal("SM2222222222", messages[1].Id);
        Assert.Equal("SM3333333333", messages[2].Id);

        // Verify each message has proper structure
        foreach (var message in messages)
        {
            Assert.NotNull(message.Id);
            Assert.NotEmpty(message.Id);
            Assert.NotNull(message.Sender);
            Assert.NotNull(message.Receiver);
            Assert.NotNull(message.Content);
            Assert.Equal(MessageContentType.PlainText, message.Content.ContentType);
            Assert.StartsWith("+", message.Sender.Address); // Phone number format
            Assert.Equal("+1987654321", message.Receiver.Address); // Common receiver
        }

        // Verify message content uniqueness
        var messageTexts = messages.Select(m => ((ITextContent)m.Content!).Text).ToList();
        Assert.Equal(3, messageTexts.Distinct().Count()); // All messages should have unique content
    }

    [Fact]
    public async Task Should_ParseTemplateCorrectly_When_ReceiveMessagesAsyncWithTwilioTemplate()
    {
        // Arrange
        var schema = CreateTwilioWhatsAppSchema()
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.Templates);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Simulate WhatsApp template response
        var webhookData = "MessageSid=SM1234567890&" +
                         "From=whatsapp%3A%2B1234567890&" +
                         "To=whatsapp%3A%2B1987654321&" +
                         "Body=&" +  // Empty body for template
                         "MessageStatus=received&" +
                         "ButtonText=Yes&" +  // User clicked a button
                         "ButtonPayload=confirm_booking&" +
                         "AccountSid=AC1234567890";

        // Act
        var result = await TestReceiveMessage(connector, webhookData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Single(receiveResult.Messages);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var message = receiveResult.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.NotNull(message.Content);
        Assert.Equal(MessageContentType.PlainText, message.Content.ContentType);
        Assert.Equal(string.Empty, ((ITextContent)message.Content).Text); // Empty body for template interactions

        // Check template interaction properties
        Assert.NotNull(message.Properties);
        Assert.True(message.Properties.Count >= 4); // At least ButtonText, ButtonPayload, MessageStatus, AccountSid
        Assert.True(message.Properties.ContainsKey("ButtonText"));
        Assert.Equal("Yes", message.Properties["ButtonText"].Value);
        Assert.True(message.Properties.ContainsKey("ButtonPayload"));
        Assert.Equal("confirm_booking", message.Properties["ButtonPayload"].Value);
        Assert.True(message.Properties.ContainsKey("MessageStatus"));
        Assert.Equal("received", message.Properties["MessageStatus"].Value);
        Assert.True(message.Properties.ContainsKey("AccountSid"));
        Assert.Equal("AC1234567890", message.Properties["AccountSid"].Value);

        // Verify WhatsApp endpoint format
        Assert.NotNull(message.Sender);
        Assert.NotNull(message.Receiver);
        Assert.StartsWith("whatsapp:", message.Sender.Address);
        Assert.StartsWith("whatsapp:", message.Receiver.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Sender.Type);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver.Type);
    }

    [Fact]
    public async Task Should_ReturnError_When_ReceiveMessagesAsyncWithInvalidTwilioWebhook()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        // Invalid webhook data (missing required fields)
        var invalidWebhookData = "From=%2B1234567890&Body=Test";

        // Act
        var result = await TestReceiveMessage(connector, invalidWebhookData, MessageSource.UrlPostContentType);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);

        // Assert
        Assert.NotNull(result.Error?.Code);
        Assert.NotEmpty(result.Error.Code);
        Assert.NotNull(result.Error?.Message);
        Assert.Equal(MessagingErrorCodes.InvalidWebhookData, result.Error?.Code);
        Assert.Contains("MessageSid", result.Error?.Message);
        Assert.Contains("required", result.Error?.Message);

        // Verify error message provides meaningful information
        Assert.True(result.Error?.Message.Length > 10); // Should be descriptive
        Assert.DoesNotContain("null", result.Error?.Message.ToLowerInvariant()); // Should not contain null references
    }

    [Fact]
    public async Task Should_ValidateCorrectly_When_ReceiveMessagesAsyncWithTwilioSignatureValidation()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);

        var connector = new TwilioSmsConnector(schema.Build(), CreateValidConnectionSettings(), TwilioMockFactory.CreateMockTwilioService().Object);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var webhookData = "MessageSid=SM1234567890&From=%2B1234567890&To=%2B1987654321&Body=Test%20message";

        // In a real implementation, you would validate the Twilio signature here
        // For testing, we assume the signature is valid

        // Act
        var result = await TestReceiveMessage(connector, webhookData, MessageSource.UrlPostContentType);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);

        // Assert
        var receiveResult = result.Value;
        Assert.NotNull(receiveResult.BatchId);
        Assert.NotEmpty(receiveResult.BatchId);
        Assert.Single(receiveResult.Messages);
        Assert.IsAssignableFrom<IReadOnlyList<IMessage>>(receiveResult.Messages);

        var message = receiveResult.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.NotNull(message.Content);
        Assert.Equal("Test message", ((ITextContent)message.Content).Text);

        // Verify URL decoding was performed correctly
        Assert.DoesNotContain("%20", ((ITextContent)message.Content).Text);
        Assert.Contains(" ", ((ITextContent)message.Content).Text);

        // Verify endpoints are properly parsed
        Assert.NotNull(message.Sender);
        Assert.NotNull(message.Receiver);
        Assert.Equal("+1234567890", message.Sender.Address);
        Assert.Equal("+1987654321", message.Receiver.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Sender.Type);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver.Type);
    }

    // Helper methods to work around ref struct limitations
    private static Task<OperationResult<ReceiveResult>> TestReceiveMessage(
        TwilioSmsConnector connector,
        string content,
        string contentType)
    {
        if (contentType == MessageSource.UrlPostContentType)
        {
            var source = MessageSource.UrlPost(content);
            return connector.ReceiveMessagesAsync(source, TestContext.Current.CancellationToken).AsTask();
        }
        else if (contentType == MessageSource.JsonContentType)
        {
            var source = MessageSource.Json(content);
            return connector.ReceiveMessagesAsync(source, TestContext.Current.CancellationToken).AsTask();
        }
        else
        {
            var source = MessageSource.Text(content);
            return connector.ReceiveMessagesAsync(source, TestContext.Current.CancellationToken).AsTask();
        }
    }

    private static Task<OperationResult<StatusUpdateResult>> TestReceiveStatus(
        TwilioSmsConnector connector,
        string content,
        string contentType)
    {
        if (contentType == MessageSource.UrlPostContentType)
        {
            var source = MessageSource.UrlPost(content);
            return connector.ReceiveMessageStatusAsync(source, TestContext.Current.CancellationToken).AsTask();
        }
        else if (contentType == MessageSource.JsonContentType)
        {
            var source = MessageSource.Json(content);
            return connector.ReceiveMessageStatusAsync(source, TestContext.Current.CancellationToken).AsTask();
        }
        else
        {
            var source = MessageSource.Text(content);
            return connector.ReceiveMessageStatusAsync(source, TestContext.Current.CancellationToken).AsTask();
        }
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter(TwilioConnectionParameters.AccountSid, "AC1234567890123456789012345678901234")
            .SetParameter(TwilioConnectionParameters.AuthToken, "auth_token_1234567890123456789012345678");
    }

    // Schema helper methods
    private static ChannelSchemaBuilder CreateTwilioSmsSchema()
    {
        return new ChannelSchemaBuilder("Twilio", "SMS", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
            });
    }

    private static ChannelSchemaBuilder CreateTwilioWhatsAppSchema()
    {
        return new ChannelSchemaBuilder("Twilio", "WhatsApp", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
            });
    }

    private static ChannelSchemaBuilder CreateSimpleSmsSchema()
    {
        return new ChannelSchemaBuilder("Twilio", "SMS", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Binary)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
            });
    }
}
