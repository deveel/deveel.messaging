using System.Text.Json;
using Xunit;

namespace Deveel.Messaging;

/// <summary>
/// Comprehensive tests for TwilioSmsConnector JSON message source handling including
/// message receiving, status updates, batch processing, and error scenarios.
/// </summary>
public class TwilioSmsConnectorJsonTests
{
    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_SingleMessage_ParsesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook for single SMS message
        var webhookJson = new
        {
            MessageSid = "SM1234567890abcdef",
            From = "+1234567890",
            To = "+1987654321",
            Body = "Hello from JSON webhook!",
            MessageStatus = "received",
            NumSegments = "1",
            AccountSid = "AC1234567890123456789012345678901234",
            DateCreated = "2023-12-01T10:30:00Z",
            DateUpdated = "2023-12-01T10:30:05Z"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM1234567890abcdef", message.Id);
        Assert.Equal("+1234567890", message.Sender?.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Sender?.Type);
        Assert.Equal("+1987654321", message.Receiver?.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver?.Type);
        Assert.Equal("Hello from JSON webhook!", ((ITextContent)message.Content!).Text);
        Assert.Equal(MessageContentType.PlainText, message.Content!.ContentType);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_BatchMessages_ParsesAll()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook for batch of SMS messages
        var webhookJson = new
        {
            Messages = new[]
            {
                new { MessageSid = "SM1111111111", From = "+1111111111", To = "+1987654321", Body = "First message" },
                new { MessageSid = "SM2222222222", From = "+2222222222", To = "+1987654321", Body = "Second message" },
                new { MessageSid = "SM3333333333", From = "+3333333333", To = "+1987654321", Body = "Third message" }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Messages.Count);
        
        var messages = result.Value.Messages.ToList();
        Assert.Equal("SM1111111111", messages[0].Id);
        Assert.Equal("SM2222222222", messages[1].Id);
        Assert.Equal("SM3333333333", messages[2].Id);
        
        Assert.Equal("+1111111111", messages[0].Sender?.Address);
        Assert.Equal("+2222222222", messages[1].Sender?.Address);
        Assert.Equal("+3333333333", messages[2].Sender?.Address);
        
        Assert.Equal("First message", ((ITextContent)messages[0].Content!).Text);
        Assert.Equal("Second message", ((ITextContent)messages[1].Content!).Text);
        Assert.Equal("Third message", ((ITextContent)messages[2].Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_WhatsAppMessage_ParsesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook for WhatsApp message
        var webhookJson = new
        {
            MessageSid = "SM9876543210abcdef",
            From = "whatsapp:+1234567890",
            To = "whatsapp:+1987654321",
            Body = "Hello from WhatsApp via JSON!",
            MessageStatus = "received",
            ProfileName = "John Doe",
            AccountSid = "AC1234567890123456789012345678901234"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM9876543210abcdef", message.Id);
        Assert.Equal("whatsapp:+1234567890", message.Sender?.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Sender?.Type);
        Assert.Equal("whatsapp:+1987654321", message.Receiver?.Address);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver?.Type);
        Assert.Equal("Hello from WhatsApp via JSON!", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_EmptyBody_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook with empty body (MMS or button response)
        var webhookJson = new
        {
            MessageSid = "SM4444444444",
            From = "+1234567890",
            To = "+1987654321",
            Body = "",
            MessageStatus = "received",
            ButtonText = "Yes",
            ButtonPayload = "confirm_order"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM4444444444", message.Id);
        Assert.Equal("", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_ParsesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON status callback
        var statusJson = new
        {
            MessageSid = "SM1234567890abcdef",
            MessageStatus = "delivered",
            To = "+1987654321",
            From = "+1234567890",
            AccountSid = "AC1234567890123456789012345678901234",
            MessagePrice = "0.0075",
            MessagePriceUnit = "USD",
            Timestamp = "2023-12-01T10:35:00Z"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("SM1234567890abcdef", result.Value.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
        
        // Check additional data
        Assert.True(result.Value.AdditionalData.ContainsKey("MessagePrice"));
        Assert.Equal("0.0075", result.Value.AdditionalData["MessagePrice"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("MessagePriceUnit"));
        Assert.Equal("USD", result.Value.AdditionalData["MessagePriceUnit"]);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_FailedStatus_ParsesErrorInfo()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON status callback for failed message
        var statusJson = new
        {
            MessageSid = "SM1234567890abcdef",
            MessageStatus = "failed",
            To = "+1987654321",
            From = "+1234567890",
            ErrorCode = "30008",
            ErrorMessage = "Unknown destination handset",
            AccountSid = "AC1234567890123456789012345678901234"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("SM1234567890abcdef", result.Value.MessageId);
        Assert.Equal(MessageStatus.DeliveryFailed, result.Value.Status);
        
        // Check error information
        Assert.True(result.Value.AdditionalData.ContainsKey("ErrorCode"));
        Assert.Equal("30008", result.Value.AdditionalData["ErrorCode"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("ErrorMessage"));
        Assert.Equal("Unknown destination handset", result.Value.AdditionalData["ErrorMessage"]);
    }

    [Theory]
    [InlineData("queued", MessageStatus.Queued)]
    [InlineData("accepted", MessageStatus.Queued)]
    [InlineData("sending", MessageStatus.Sent)]
    [InlineData("sent", MessageStatus.Sent)]
    [InlineData("delivered", MessageStatus.Delivered)]
    [InlineData("undelivered", MessageStatus.DeliveryFailed)]
    [InlineData("failed", MessageStatus.DeliveryFailed)]
    [InlineData("received", MessageStatus.Received)]
    [InlineData("unknown_status", MessageStatus.Unknown)]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_AllStatuses_MapsCorrectly(string twilioStatus, MessageStatus expectedStatus)
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var statusJson = new
        {
            MessageSid = "SM1234567890abcdef",
            MessageStatus = twilioStatus,
            To = "+1987654321",
            From = "+1234567890"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedStatus, result.Value.Status);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_MissingMessageSid_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook missing required MessageSid
        var invalidJson = new
        {
            From = "+1234567890",
            To = "+1987654321",
            Body = "Message without SID",
            MessageStatus = "received"
        };

        var jsonPayload = JsonSerializer.Serialize(invalidJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_MissingFromOrTo_ReturnsNull()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook missing From field
        var invalidJson = new
        {
            MessageSid = "SM1234567890",
            To = "+1987654321",
            Body = "Message without From",
            MessageStatus = "received"
        };

        var jsonPayload = JsonSerializer.Serialize(invalidJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Invalid JSON content
        var invalidJson = "{ \"MessageSid\": \"SM123\", \"From\": \"+123456789";
        var source = MessageSource.Json(invalidJson);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.ReceiveMessageFailed, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Invalid JSON content
        var invalidJson = "{ \"MessageSid\": \"SM123\", \"MessageStatus\":";
        var source = MessageSource.Json(invalidJson);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.ReceiveStatusFailed, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_ComplexMessage_ParsesAllFields()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate comprehensive Twilio JSON webhook with all fields
        var complexWebhookJson = new
        {
            MessageSid = "SM1234567890abcdef",
            AccountSid = "AC1234567890123456789012345678901234",
            From = "+1234567890",
            To = "+1987654321",
            Body = "Complex message with all fields",
            MessageStatus = "received",
            NumSegments = "2",
            NumMedia = "1",
            MediaUrl0 = "https://api.twilio.com/2010-04-01/Accounts/AC123/Messages/SM123/Media/ME123",
            MediaContentType0 = "image/jpeg",
            DateCreated = "2023-12-01T10:30:00Z",
            DateUpdated = "2023-12-01T10:30:05Z",
            Price = "0.0075",
            PriceUnit = "USD",
            Direction = "inbound",
            ApiVersion = "2010-04-01",
            Uri = "/2010-04-01/Accounts/AC123/Messages/SM123.json"
        };

        var jsonPayload = JsonSerializer.Serialize(complexWebhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM1234567890abcdef", message.Id);
        Assert.Equal("+1234567890", message.Sender?.Address);
        Assert.Equal("+1987654321", message.Receiver?.Address);
        Assert.Equal("Complex message with all fields", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_UnicodeContent_PreservesEncoding()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate Twilio JSON webhook with Unicode content
        var unicodeMessage = "Hello! ?? Testing émojis and ñ special characters ??";
        var webhookJson = new
        {
            MessageSid = "SM1234567890abcdef",
            From = "+1234567890",
            To = "+1987654321",
            Body = unicodeMessage,
            MessageStatus = "received"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal(unicodeMessage, ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_LargeMessage_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a large message (SMS can be up to 1600 characters)
        var largeBody = new string('A', 1600);
        var webhookJson = new
        {
            MessageSid = "SM1234567890abcdef",
            From = "+1234567890",
            To = "+1987654321",
            Body = largeBody,
            MessageStatus = "received",
            NumSegments = "11" // Large message split into multiple segments
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal(largeBody, ((ITextContent)message.Content!).Text);
        Assert.Equal(1600, ((ITextContent)message.Content!).Text.Length);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_AllAdditionalProperties_PreservesData()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate comprehensive Twilio JSON status callback with all possible fields
        var statusJson = new
        {
            MessageSid = "SM1234567890abcdef",
            MessageStatus = "delivered",
            To = "+1987654321",
            From = "+1234567890",
            AccountSid = "AC1234567890123456789012345678901234",
            MessagePrice = "0.0075",
            MessagePriceUnit = "USD",
            NumSegments = "1",
            Direction = "outbound-api",
            DateCreated = "2023-12-01T10:30:00Z",
            DateUpdated = "2023-12-01T10:35:00Z",
            DateSent = "2023-12-01T10:30:01Z",
            Uri = "/2010-04-01/Accounts/AC123/Messages/SM123.json",
            CustomField = "custom_value",
            Extra = "additional_data"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("SM1234567890abcdef", result.Value.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
        
        // Verify that all additional properties (except MessageSid and MessageStatus) are preserved
        Assert.True(result.Value.AdditionalData.ContainsKey("To"));
        Assert.True(result.Value.AdditionalData.ContainsKey("From"));
        Assert.True(result.Value.AdditionalData.ContainsKey("AccountSid"));
        Assert.True(result.Value.AdditionalData.ContainsKey("MessagePrice"));
        Assert.True(result.Value.AdditionalData.ContainsKey("CustomField"));
        Assert.True(result.Value.AdditionalData.ContainsKey("Extra"));
        
        Assert.Equal("custom_value", result.Value.AdditionalData["CustomField"]);
        Assert.Equal("additional_data", result.Value.AdditionalData["Extra"]);
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
    }
}