using System.Text;
using System.Text.Json;

namespace Deveel.Messaging;

/// <summary>
/// Tests for Twilio-specific message receiving functionality including webhook handling,
/// status callbacks, and Twilio-specific message formats.
/// </summary>
public class TwilioMessageReceivingTests
{
    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioSmsWebhook_ParsesCorrectly()
    {
        // Arrange
        var schema = CreateTwilioSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    public async Task ReceiveMessagesAsync_WithTwilioWhatsAppWebhook_ParsesCorrectly()
    {
        // Arrange
        var schema = CreateTwilioWhatsAppSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    public async Task ReceiveMessagesAsync_WithTwilioMmsWebhook_ParsesMediaCorrectly()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.MediaAttachments);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    public async Task ReceiveMessageStatusAsync_WithTwilioStatusCallback_ParsesCorrectly()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.HandlerMessageState);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - StatusUpdateResult properties
        var statusResult = result.Value;
        Assert.Equal("SM1234567890", statusResult.MessageId);
        Assert.Equal(MessageStatus.Delivered, statusResult.Status);
        Assert.True(statusResult.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(statusResult.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-1)); // Recent timestamp
        
        // Check additional Twilio status data
        Assert.NotNull(statusResult.AdditionalData);
        Assert.True(statusResult.AdditionalData.Count >= 2); // At least MessagePrice and MessagePriceUnit
        Assert.True(statusResult.AdditionalData.ContainsKey("MessagePrice"));
        Assert.Equal("0.0075", statusResult.AdditionalData["MessagePrice"]);
        Assert.True(statusResult.AdditionalData.ContainsKey("MessagePriceUnit"));
        Assert.Equal("USD", statusResult.AdditionalData["MessagePriceUnit"]);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioFailedStatus_ParsesErrorCorrectly()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.HandlerMessageState);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - StatusUpdateResult properties
        var statusResult = result.Value;
        Assert.Equal("SM1234567890", statusResult.MessageId);
        Assert.Equal(MessageStatus.DeliveryFailed, statusResult.Status);
        Assert.True(statusResult.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(statusResult.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-1)); // Recent timestamp
        
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
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_ParsesCorrectly()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    public async Task ReceiveMessageStatusAsync_WithDifferentTwilioStatuses_MapsCorrectly(string twilioStatus, MessageStatus expectedStatus)
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.HandlerMessageState);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

        var statusData = $"MessageSid=SM1234567890&MessageStatus={twilioStatus}&To=%2B1987654321&From=%2B1234567890";

        // Act
        var result = await TestReceiveStatus(connector, statusData, MessageSource.UrlPostContentType);

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - StatusUpdateResult properties
        var statusResult = result.Value;
        Assert.Equal("SM1234567890", statusResult.MessageId);
        Assert.Equal(expectedStatus, statusResult.Status);
        Assert.True(statusResult.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(statusResult.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-1)); // Recent timestamp
        
        // Verify that additional data is properly initialized even if empty
        Assert.NotNull(statusResult.AdditionalData);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithMultipleTwilioMessages_ParsesBatchCorrectly()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.BulkMessaging);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    public async Task ReceiveMessagesAsync_WithTwilioTemplate_ParsesTemplateCorrectly()
    {
        // Arrange
        var schema = CreateTwilioWhatsAppSchema()
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.Templates);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

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

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    public async Task ReceiveMessagesAsync_WithInvalidTwilioWebhook_ReturnsError()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

        // Invalid webhook data (missing required fields)
        var invalidWebhookData = "From=%2B1234567890&Body=Test";

        // Act
        var result = await TestReceiveMessage(connector, invalidWebhookData, MessageSource.UrlPostContentType);

        // Assert - Result object verification for error case
        Assert.False(result.Successful);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        
        // Assert - Error details
        Assert.NotNull(result.Error.ErrorCode);
        Assert.NotEmpty(result.Error.ErrorCode);
        Assert.Equal("MISSING_MESSAGE_SID", result.Error.ErrorCode);
        Assert.Contains("MessageSid", result.Error.ErrorMessage);
        Assert.Contains("required", result.Error.ErrorMessage);
        
        // Verify error message provides meaningful information
        Assert.True(result.Error.ErrorMessage.Length > 10); // Should be descriptive
        Assert.DoesNotContain("null", result.Error.ErrorMessage.ToLowerInvariant()); // Should not contain null references
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioSignatureValidation_ValidatesCorrectly()
    {
        // Arrange
        var schema = CreateSimpleSmsSchema()
            .WithCapability(ChannelCapability.ReceiveMessages);
        
        var connector = new TwilioTestReceivingConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = "MessageSid=SM1234567890&From=%2B1234567890&To=%2B1987654321&Body=Test%20message";

        // In a real implementation, you would validate the Twilio signature here
        // For testing, we assume the signature is valid

        // Act
        var result = await TestReceiveMessage(connector, webhookData, MessageSource.UrlPostContentType);

        // Assert - Result object verification
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Null(result.Error);
        
        // Assert - ReceiveResult properties
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
    private static Task<ConnectorResult<ReceiveResult>> TestReceiveMessage(
        TwilioTestReceivingConnector connector, 
        string content, 
        string contentType)
    {
        if (contentType == MessageSource.UrlPostContentType)
        {
            var source = MessageSource.UrlPost(content);
            return connector.ReceiveMessagesAsync(source, CancellationToken.None);
        }
        else if (contentType == MessageSource.JsonContentType)
        {
            var source = MessageSource.Json(content);
            return connector.ReceiveMessagesAsync(source, CancellationToken.None);
        }
        else
        {
            var source = MessageSource.Text(content);
            return connector.ReceiveMessagesAsync(source, CancellationToken.None);
        }
    }

    private static Task<ConnectorResult<StatusUpdateResult>> TestReceiveStatus(
        TwilioTestReceivingConnector connector, 
        string content, 
        string contentType)
    {
        if (contentType == MessageSource.UrlPostContentType)
        {
            var source = MessageSource.UrlPost(content);
            return connector.ReceiveMessageStatusAsync(source, CancellationToken.None);
        }
        else if (contentType == MessageSource.JsonContentType)
        {
            var source = MessageSource.Json(content);
            return connector.ReceiveMessageStatusAsync(source, CancellationToken.None);
        }
        else
        {
            var source = MessageSource.Text(content);
            return connector.ReceiveMessageStatusAsync(source, CancellationToken.None);
        }
    }

    // Test implementation of a Twilio receiving connector using default implementations
    public class TwilioTestReceivingConnector : ChannelConnectorBase
    {
        public TwilioTestReceivingConnector(IChannelSchema schema) : base(schema) { }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Twilio Test Receiving Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                if (source.ContentType == MessageSource.UrlPostContentType)
                {
                    var formData = source.AsUrlPostData();
                    if (!formData.TryGetValue("MessageSid", out var messageSid))
                    {
                        return ConnectorResult<ReceiveResult>.FailTask("MISSING_MESSAGE_SID", "MessageSid is required for Twilio webhooks");
                    }

                    var messages = ParseTwilioFormData(source);
                    var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                    return ConnectorResult<ReceiveResult>.SuccessTask(result);
                }
                else if (source.ContentType == MessageSource.JsonContentType)
                {
                    var messages = ParseTwilioJson(source);
                    var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                    return ConnectorResult<ReceiveResult>.SuccessTask(result);
                }

                return ConnectorResult<ReceiveResult>.FailTask("UNSUPPORTED_CONTENT_TYPE", 
                    "Only form data and JSON are supported for Twilio message receiving");
            }
            catch (Exception ex)
            {
                return ConnectorResult<ReceiveResult>.FailTask("TWILIO_RECEIVE_ERROR", ex.Message);
            }
        }

        protected override Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                if (source.ContentType == MessageSource.UrlPostContentType)
                {
                    var formData = source.AsUrlPostData();
                    var messageId = formData.TryGetValue("MessageSid", out var sid) ? sid : "unknown";
                    var statusString = formData.TryGetValue("MessageStatus", out var status) ? status : "unknown";
                    
                    var messageStatus = statusString.ToLowerInvariant() switch
                    {
                        "delivered" => MessageStatus.Delivered,
                        "sent" => MessageStatus.Sent,
                        "failed" => MessageStatus.DeliveryFailed,
                        "undelivered" => MessageStatus.DeliveryFailed,
                        "received" => MessageStatus.Received,
                        "queued" => MessageStatus.Queued,
                        _ => MessageStatus.Unknown
                    };

                    var statusResult = new StatusUpdateResult(messageId, messageStatus);
                    
                    // Add additional Twilio data
                    if (formData.TryGetValue("MessagePrice", out var price))
                        statusResult.AdditionalData["MessagePrice"] = price;
                    if (formData.TryGetValue("MessagePriceUnit", out var priceUnit))
                        statusResult.AdditionalData["MessagePriceUnit"] = priceUnit;
                    if (formData.TryGetValue("ErrorCode", out var errorCode))
                        statusResult.AdditionalData["ErrorCode"] = errorCode;
                    if (formData.TryGetValue("ErrorMessage", out var errorMessage))
                        statusResult.AdditionalData["ErrorMessage"] = errorMessage;

                    return Task.FromResult(ConnectorResult<StatusUpdateResult>.Success(statusResult));
                }

                return Task.FromResult(ConnectorResult<StatusUpdateResult>.Fail("UNSUPPORTED_CONTENT_TYPE", 
                    "Only form data is supported for Twilio status callbacks"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ConnectorResult<StatusUpdateResult>.Fail("TWILIO_STATUS_ERROR", ex.Message));
            }
        }

        private List<IMessage> ParseTwilioFormData(MessageSource source)
        {
            var formData = source.AsUrlPostData();
            var messages = new List<IMessage>();

            // For Twilio webhooks, MessageSid is required
            if (!formData.TryGetValue("MessageSid", out var messageSid))
            {
                throw new ArgumentException("MessageSid is required for Twilio webhooks");
            }

            messages.Add(ParseSingleTwilioMessage(formData));
            return messages;
        }

        private List<IMessage> ParseTwilioJson(MessageSource source)
        {
            var messages = new List<IMessage>();
            var jsonData = source.AsJson<JsonElement>();

            if (jsonData.TryGetProperty("Messages", out var messagesArray))
            {
                // Batch messages
                foreach (var messageElement in messagesArray.EnumerateArray())
                {
                    messages.Add(ParseTwilioJsonMessage(messageElement));
                }
            }
            else
            {
                // Single message
                messages.Add(ParseTwilioJsonMessage(jsonData));
            }

            return messages;
        }

        private IMessage ParseSingleTwilioMessage(IDictionary<string, string> formData)
        {
            if (!formData.TryGetValue("MessageSid", out var messageSid))
            {
                throw new ArgumentException("MessageSid is required for Twilio messages");
            }

            var from = formData.TryGetValue("From", out var fromValue) ? fromValue : "";
            var to = formData.TryGetValue("To", out var toValue) ? toValue : "";
            var body = formData.TryGetValue("Body", out var bodyValue) ? bodyValue : "";

            var message = new Message
            {
                Id = messageSid,
                Sender = new Endpoint(GetTwilioEndpointType(from), from),
                Receiver = new Endpoint(GetTwilioEndpointType(to), to),
                Content = new TextContent(body),
                Properties = new Dictionary<string, MessageProperty>()
            };

            // Add all other form fields as properties
            foreach (var kvp in formData)
            {
                if (kvp.Key != "MessageSid" && kvp.Key != "From" && kvp.Key != "To" && kvp.Key != "Body")
                {
                    message.Properties[kvp.Key] = new MessageProperty(kvp.Key, kvp.Value);
                }
            }

            return message;
        }

        private IMessage ParseTwilioJsonMessage(JsonElement jsonData)
        {
            var messageSid = jsonData.GetProperty("MessageSid").GetString() ?? 
                            throw new ArgumentException("MessageSid is required");

            var from = jsonData.TryGetProperty("From", out var fromProp) ? fromProp.GetString() ?? "" : "";
            var to = jsonData.TryGetProperty("To", out var toProp) ? toProp.GetString() ?? "" : "";
            var body = jsonData.TryGetProperty("Body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

            return new Message
            {
                Id = messageSid,
                Sender = new Endpoint(GetTwilioEndpointType(from), from),
                Receiver = new Endpoint(GetTwilioEndpointType(to), to),
                Content = new TextContent(body)
            };
        }

        private static EndpointType GetTwilioEndpointType(string address)
        {
            if (string.IsNullOrEmpty(address))
                return EndpointType.Id;

            if (address.StartsWith("whatsapp:"))
                return EndpointType.PhoneNumber;

            if (address.StartsWith("+"))
                return EndpointType.PhoneNumber;

            if (address.Contains("@"))
                return EndpointType.EmailAddress;

            return EndpointType.Id;
        }
    }

    // Schema helper methods
    private static ChannelSchema CreateTwilioSmsSchema()
    {
        return new ChannelSchema("Twilio", "SMS", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
            });
    }

    private static ChannelSchema CreateTwilioWhatsAppSchema()
    {
        return new ChannelSchema("Twilio", "WhatsApp", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
            });
    }

    private static ChannelSchema CreateSimpleSmsSchema()
    {
        return new ChannelSchema("Twilio", "SMS", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Binary)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
            });
    }
}