using System.Text.Json;
using Xunit;

namespace Deveel.Messaging;

/// <summary>
/// Edge case tests for SendGridEmailConnector JSON message source handling,
/// covering various error scenarios, malformed data, and SendGrid-specific cases.
/// </summary>
public class SendGridEmailConnectorJsonEdgeCaseTests
{
    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_EmptyJsonObject_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var emptyJson = "{}";
        var source = MessageSource.Json(emptyJson);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_NullStringValues_HandlesGracefully()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // JSON with null string values
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "null_values_123",
            from = "nulltest@example.com",
            to = "inbox@yourdomain.com",
            subject = (string?)null,
            text = (string?)null,
            html = (string?)null
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
        Assert.Equal("null_values_123", message.Id);
        Assert.Equal("", ((ITextContent)message.Content!).Text); // null content should become empty string
        Assert.Equal("", message.Properties!["Subject"].Value); // null subject should become empty string
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_VeryLongContent_HandlesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create very long content to test large payload handling
        var longContent = new string('A', 100000); // 100KB of text
        var longHtml = $"<html><body><p>{longContent}</p></body></html>";
        
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "large_content_123",
            from = "largecontent@example.com",
            to = "inbox@yourdomain.com",
            subject = "Email with very large content",
            text = longContent,
            html = longHtml
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
        Assert.Equal("large_content_123", message.Id);
        Assert.Equal(MessageContentType.Html, message.Content!.ContentType);
        Assert.Equal(longHtml, ((IHtmlContent)message.Content!).Html);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_UnicodeContent_PreservesEncoding()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Unicode content with various character sets
        var unicodeSubject = "???? - Test Email with émojis ?? and special chars ñáéíóú";
        var unicodeContent = "Hello! ??! Bonjour! ?????! ????????????! ?????";
        
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "unicode_content_123",
            from = "unicode@example.com",
            to = "inbox@yourdomain.com",
            subject = unicodeSubject,
            text = unicodeContent,
            sender_name = "José María Ñoño-González"
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
        Assert.Equal(unicodeContent, ((ITextContent)message.Content!).Text);
        Assert.Equal(unicodeSubject, message.Properties!["Subject"].Value);
        Assert.Equal("José María Ñoño-González", message.Properties["sender_name"].Value);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonStatusCallback_MissingMessageId_UsesDefault()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Status callback missing sg_message_id
        var statusJson = new
        {
            @event = "delivered",
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
        Assert.Equal("unknown", result.Value.MessageId); // Should default to "unknown"
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonStatusCallback_InvalidTimestamp_UsesCurrentTime()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Status callback with invalid timestamp
        var statusJson = new
        {
            @event = "delivered",
            sg_message_id = "invalid_timestamp_123",
            email = "test@example.com",
            timestamp = "not_a_number"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("invalid_timestamp_123", result.Value.MessageId);
        // Timestamp should be recent (within last minute)
        Assert.True(result.Value.Timestamp >= DateTime.UtcNow.AddMinutes(-1));
        Assert.True(result.Value.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_MalformedEmailAddresses_HandlesGracefully()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Email with unusual but valid email address formats
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "unusual_emails_123",
            from = "\"Test User\" <test.user+tag@sub.domain.co.uk>",
            to = "inbox+filter@yourdomain.com",
            subject = "Email with unusual address formats",
            text = "Testing unusual email formats"
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
        Assert.Equal("\"Test User\" <test.user+tag@sub.domain.co.uk>", message.Sender?.Address);
        Assert.Equal("inbox+filter@yourdomain.com", message.Receiver?.Address);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_ComplexEnvelopeData_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Complex envelope data with multiple recipients
        var complexEnvelope = "{\"to\":[\"inbox@yourdomain.com\",\"backup@yourdomain.com\"],\"from\":\"sender@example.com\"}";
        
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "complex_envelope_123",
            from = "sender@example.com",
            to = "inbox@yourdomain.com",
            subject = "Email with complex envelope",
            text = "Testing complex envelope data",
            envelope = complexEnvelope,
            dkim = "{\"@yourdomain.com\" : \"pass\"}",
            SPF = "pass"
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
        Assert.Equal("complex_envelope_123", message.Id);
        Assert.True(message.Properties!.ContainsKey("envelope"));
        Assert.Equal(complexEnvelope, message.Properties["envelope"].Value);
        Assert.True(message.Properties.ContainsKey("dkim"));
        Assert.True(message.Properties.ContainsKey("SPF"));
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonStatusCallback_AllAdditionalProperties_PreservesData()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Comprehensive SendGrid event with all possible fields
        var statusJson = new
        {
            @event = "delivered",
            sg_message_id = "comprehensive_event_123",
            email = "recipient@example.com",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            smtp_id = "<comprehensive_test@smtp.sendgrid.net>",
            category = new[] { "newsletter", "monthly" },
            asm_group_id = 42,
            useragent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2)",
            ip = "255.255.255.255",
            url = "http://www.sendgrid.com/",
            unique_arg_campaign = "summer_campaign",
            unique_arg_userid = "12345",
            response = "250 OK id=1234567890",
            attempt = "1",
            newsletter = new { newsletter_user_list_id = "10557865", newsletter_id = "1943530", newsletter_send_id = "2308608" }
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("comprehensive_event_123", result.Value.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
        
        // Verify that all additional properties are preserved
        Assert.True(result.Value.AdditionalData.ContainsKey("Channel"));
        Assert.Equal("Email", result.Value.AdditionalData["Channel"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("Provider"));
        Assert.Equal("SendGrid", result.Value.AdditionalData["Provider"]);
        Assert.True(result.Value.AdditionalData.ContainsKey("email"));
        Assert.True(result.Value.AdditionalData.ContainsKey("smtp_id"));
        Assert.True(result.Value.AdditionalData.ContainsKey("response"));
        Assert.True(result.Value.AdditionalData.ContainsKey("unique_arg_campaign"));
        Assert.Equal("summer_campaign", result.Value.AdditionalData["unique_arg_campaign"]);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_EmptyArrayEvents_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // JSON with empty events array
        var emptyArray = new object[] { };
        var jsonPayload = JsonSerializer.Serialize(emptyArray);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithSendGridJsonStatusCallback_EmptyEventsArray_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // JSON with empty events array
        var emptyArray = new object[] { };
        var jsonPayload = JsonSerializer.Serialize(emptyArray);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.ReceiveStatusFailed, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_MixedEventTypes_FiltersCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Mix of valid and invalid event types
        var webhookJson = new object[]
        {
            new { @event = "inbound", sg_message_id = "valid_1", from = "sender1@example.com", to = "inbox@yourdomain.com", text = "Valid inbound" },
            new { @event = "delivered", sg_message_id = "invalid_1", email = "recipient@example.com" }, // Should be filtered out
            new { @event = "processed", sg_message_id = "valid_2", from = "sender2@example.com", to = "inbox@yourdomain.com", text = "Valid processed" },
            new { @event = "bounce", sg_message_id = "invalid_2", email = "bounced@example.com" } // Should be filtered out
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Messages.Count); // Only 2 valid messages
        
        var messages = result.Value.Messages.ToList();
        Assert.Equal("valid_1", messages[0].Id);
        Assert.Equal("valid_2", messages[1].Id);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_CaseSensitiveFields_HandlesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Test case sensitivity - SendGrid uses lowercase "event" field
        var jsonPayload = """
        {
            "event": "inbound",
            "sg_message_id": "case_sensitive_123",
            "from": "casesensitive@example.com",
            "to": "inbox@yourdomain.com",
            "subject": "Case sensitive test",
            "text": "Testing case sensitivity"
        }
        """;

        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("case_sensitive_123", message.Id);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithSendGridJsonWebhook_AttachmentMetadata_PreservesInProperties()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Email with attachment metadata
        var webhookJson = new
        {
            @event = "inbound",
            sg_message_id = "with_attachments_123",
            from = "attachments@example.com",
            to = "inbox@yourdomain.com",
            subject = "Email with attachments",
            text = "This email has attachments",
            attachments = "2",
            attachment1 = "document.pdf",
            attachment2 = "image.jpg",
            attachment_info = "{\"attachment1\": {\"filename\": \"document.pdf\", \"type\": \"application/pdf\", \"disposition\": \"attachment\"}, \"attachment2\": {\"filename\": \"image.jpg\", \"type\": \"image/jpeg\", \"disposition\": \"attachment\"}}"
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
        Assert.True(message.Properties!.ContainsKey("attachments"));
        Assert.Equal("2", message.Properties["attachments"].Value);
        Assert.True(message.Properties.ContainsKey("attachment1"));
        Assert.True(message.Properties.ContainsKey("attachment2"));
        Assert.True(message.Properties.ContainsKey("attachment_info"));
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
    }
}