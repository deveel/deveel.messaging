using System.Text.Json;
using Xunit;

namespace Deveel.Messaging;

/// <summary>
/// Comprehensive tests for SendGridEmailConnector JSON message source handling including
/// email receiving, status updates, and error scenarios.
/// </summary>
public class SendGridEmailConnectorJsonTests
{
    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridInboundJsonWebhook_SingleEmail_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate SendGrid inbound parse webhook JSON
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0",
            from = "test@example.com",
            to = "inbox@yourdomain.com",
            subject = "Hello from SendGrid JSON webhook!",
            text = "This is the plain text version of the email.",
            html = "<p>This is the <strong>HTML</strong> version of the email.</p>",
            envelope = "{\"to\":[\"inbox@yourdomain.com\"],\"from\":\"test@example.com\"}",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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
        Assert.Equal("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0", message.Id);
        Assert.Equal("test@example.com", message.Sender?.Address);
        Assert.Equal(EndpointType.EmailAddress, message.Sender?.Type);
        Assert.Equal("inbox@yourdomain.com", message.Receiver?.Address);
        Assert.Equal(EndpointType.EmailAddress, message.Receiver?.Type);
        Assert.Equal(MessageContentType.Html, message.Content!.ContentType);
        Assert.Equal("<p>This is the <strong>HTML</strong> version of the email.</p>", ((IHtmlContent)message.Content!).Html);
        
        // Check subject property
        Assert.NotNull(message.Properties);
        Assert.True(message.Properties.ContainsKey("Subject"));
        Assert.Equal("Hello from SendGrid JSON webhook!", message.Properties["Subject"].Value);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridProcessedEvent_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate SendGrid processed event (email successfully processed)
        var webhookJson = new
        {
            @event = "processed",
            sg_message_id = "filter0001.16648.5515E0B88.0",
            from = "sender@example.com",
            to = "recipient@example.com",
            subject = "Your order confirmation",
            text = "Thank you for your order!",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            category = "order_confirmation",
            unique_arg_campaign = "summer_sale"
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
        Assert.Equal("filter0001.16648.5515E0B88.0", message.Id);
        Assert.Equal("sender@example.com", message.Sender?.Address);
        Assert.Equal("recipient@example.com", message.Receiver?.Address);
        Assert.Equal(MessageContentType.PlainText, message.Content!.ContentType);
        Assert.Equal("Thank you for your order!", ((ITextContent)message.Content!).Text);
        
        // Check additional properties
        Assert.True(message.Properties!.ContainsKey("category"));
        Assert.Equal("order_confirmation", message.Properties["category"].Value);
        Assert.True(message.Properties.ContainsKey("unique_arg_campaign"));
        Assert.Equal("summer_sale", message.Properties["unique_arg_campaign"].Value);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_BatchEmails_ParsesAll()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate SendGrid JSON webhook with multiple events
        var webhookJson = new[]
        {
            new { @event = "inbound", sg_message_id = "email_001", from = "sender1@example.com", to = "inbox@yourdomain.com", subject = "First Email", text = "First email content" },
            new { @event = "inbound", sg_message_id = "email_002", from = "sender2@example.com", to = "inbox@yourdomain.com", subject = "Second Email", text = "Second email content" },
            new { @event = "processed", sg_message_id = "email_003", from = "sender3@example.com", to = "inbox@yourdomain.com", subject = "Third Email", text = "Third email content" }
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
        Assert.Equal("email_001", messages[0].Id);
        Assert.Equal("email_002", messages[1].Id);
        Assert.Equal("email_003", messages[2].Id);
        
        Assert.Equal("sender1@example.com", messages[0].Sender?.Address);
        Assert.Equal("sender2@example.com", messages[1].Sender?.Address);
        Assert.Equal("sender3@example.com", messages[2].Sender?.Address);
        
        Assert.Equal("First email content", ((ITextContent)messages[0].Content!).Text);
        Assert.Equal("Second email content", ((ITextContent)messages[1].Content!).Text);
        Assert.Equal("Third email content", ((ITextContent)messages[2].Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonStatusCallback_DeliveredStatus_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate SendGrid JSON event webhook for delivered status
        var statusJson = new
        {
            @event = "delivered",
            sg_message_id = "14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0",
            email = "recipient@example.com",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            smtp_id = "<14c5d75ce93.dfd.64b469@ismtpd-555>",
            category = "transactional",
            asm_group_id = 1
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0", result.Value.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
        
        // Check SendGrid-specific additional data
        Assert.True(result.Value.AdditionalData.ContainsKey("Channel"));
        Assert.Equal("Email", result.Value.AdditionalData["Channel"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("Provider"));
        Assert.Equal("SendGrid", result.Value.AdditionalData["Provider"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("email"));
        Assert.Equal("recipient@example.com", result.Value.AdditionalData["email"]);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridBounceEvent_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate SendGrid bounce event
        var statusJson = new
        {
            @event = "bounce",
            sg_message_id = "bounce_message_123",
            email = "invalid@nonexistentdomain.com",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            reason = "550 5.1.1 User unknown",
            status = "5.1.1",
            type = "bounce"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("bounce_message_123", result.Value.MessageId);
        Assert.Equal(MessageStatus.DeliveryFailed, result.Value.Status);
        
        // Check bounce-specific data
        Assert.True(result.Value.AdditionalData.ContainsKey("reason"));
        Assert.Equal("550 5.1.1 User unknown", result.Value.AdditionalData["reason"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("status"));
        Assert.Equal("5.1.1", result.Value.AdditionalData["status"]);
    }

    [Theory]
    [InlineData("processed", MessageStatus.Queued)]
    [InlineData("deferred", MessageStatus.Queued)]
    [InlineData("delivered", MessageStatus.Delivered)]
    [InlineData("open", MessageStatus.Delivered)]
    [InlineData("click", MessageStatus.Delivered)]
    [InlineData("bounce", MessageStatus.DeliveryFailed)]
    [InlineData("dropped", MessageStatus.DeliveryFailed)]
    [InlineData("spamreport", MessageStatus.DeliveryFailed)]
    [InlineData("unsubscribe", MessageStatus.Delivered)]
    [InlineData("group_unsubscribe", MessageStatus.Delivered)]
    [InlineData("group_resubscribe", MessageStatus.Delivered)]
    [InlineData("inbound", MessageStatus.Received)]
    [InlineData("unknown_event", MessageStatus.Unknown)]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonStatusCallback_AllEvents_MapsCorrectly(string eventType, MessageStatus expectedStatus)
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var statusJson = new
        {
            @event = eventType,
            sg_message_id = "test_message_123",
            email = "test@example.com",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedStatus, result.Value.Status);
        Assert.Equal("Email", result.Value.AdditionalData["Channel"]);
        Assert.Equal("SendGrid", result.Value.AdditionalData["Provider"]);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_EmailWithoutHtml_UsesPlainText()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate email with only plain text content
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "plaintext_email_123",
            from = "plaintext@example.com",
            to = "inbox@yourdomain.com",
            subject = "Plain text only email",
            text = "This email contains only plain text content.",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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
        Assert.Equal(MessageContentType.PlainText, message.Content!.ContentType);
        Assert.Equal("This email contains only plain text content.", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_EmailWithoutContent_UsesEmptyText()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate email without text or HTML content
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "empty_content_123",
            from = "nocontent@example.com",
            to = "inbox@yourdomain.com",
            subject = "Email without body content",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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
        Assert.Equal(MessageContentType.PlainText, message.Content!.ContentType);
        Assert.Equal("", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_NonInboundEvent_FiltersOut()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate non-inbound/non-processed events (should be filtered out)
        var webhookJson = new[]
        {
            new { @event = "delivered", sg_message_id = "delivered_123", email = "test@example.com" },
            new { @event = "bounce", sg_message_id = "bounce_123", email = "invalid@example.com" },
            new { @event = "open", sg_message_id = "open_123", email = "opener@example.com" }
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful); // Should fail because no valid messages found
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonArray_ProcessesFirstEvent()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate array of status events
        var statusJson = new[]
        {
            new { @event = "delivered", sg_message_id = "first_message", email = "first@example.com", timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new { @event = "open", sg_message_id = "second_message", email = "second@example.com", timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("first_message", result.Value.MessageId); // Should process first event
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_MissingFromField_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate webhook missing required from field
        var invalidJson = new
        {
            @event = "inbound",
            sg_message_id = "missing_from_123",
            to = "inbox@yourdomain.com",
            subject = "Email without from field",
            text = "This should fail"
        };

        var jsonPayload = JsonSerializer.Serialize(invalidJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Invalid JSON content
        var invalidJson = "{ \"event\": \"inbound\", \"sg_message_id\": \"test123\", \"from\": \"test@exam";
        var source = MessageSource.Json(invalidJson);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.ReceiveMessageFailed, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Invalid JSON content
        var invalidJson = "{ \"event\": \"delivered\", \"sg_message_id\":";
        var source = MessageSource.Json(invalidJson);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.ReceiveStatusFailed, result.Error.ErrorCode);
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
    }
}