using Xunit;

namespace Deveel.Messaging;

/// <summary>
/// Tests for SendGridEmailConnector webhook and status callback capabilities to verify
/// that the connector properly supports receiving emails and status updates via JSON and form data.
/// </summary>
public class SendGridEmailConnectorWebhookTests
{
    [Fact]
    public async Task SendGridEmailConnector_SupportsReceiveMessagesAsync()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a valid SendGrid inbound parse webhook source
        var webhookData = "from=sender%40example.com&to=inbox%40yourdomain.com&subject=Test%20Email&text=Hello%20from%20SendGrid&envelope=%7B%22to%22%3A%5B%22inbox%40yourdomain.com%22%5D%2C%22from%22%3A%22sender%40example.com%22%7D";
        var source = MessageSource.UrlPost(webhookData);

        // Act & Assert - Should not throw NotSupportedException
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);
        
        // The result should be successful since we have valid webhook data
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.NotEmpty(message.Id);
        Assert.Equal("sender@example.com", message.Sender?.Address);
        Assert.Equal("inbox@yourdomain.com", message.Receiver?.Address);
        Assert.Equal("Hello from SendGrid", ((ITextContent)message.Content!).Text);
        Assert.Equal("Test Email", message.Properties!["Subject"].Value);
    }

    [Fact]
    public async Task SendGridEmailConnector_SupportsReceiveMessageStatusAsync()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a valid SendGrid event webhook source
        var statusData = "event=delivered&sg_message_id=14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0&email=recipient%40example.com&timestamp=1672531200";
        var source = MessageSource.UrlPost(statusData);

        // Act & Assert - Should not throw NotSupportedException
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);
        
        // The result should be successful since we have valid status callback data
        Assert.True(result.Successful);
        Assert.Equal("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0", result.Value?.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Value?.Status);
        
        // Check SendGrid-specific fields
        Assert.True(result.Value?.AdditionalData.ContainsKey("Channel"));
        Assert.Equal("Email", result.Value?.AdditionalData["Channel"]);
        Assert.True(result.Value?.AdditionalData.ContainsKey("Provider"));
        Assert.Equal("SendGrid", result.Value?.AdditionalData["Provider"]);
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessagesAsync_WithJsonSource_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a SendGrid inbound JSON webhook payload
        var webhookJson = """
        {
            "event": "inbound",
            "sg_message_id": "14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0",
            "from": "sender@example.com",
            "to": "inbox@yourdomain.com",
            "subject": "Hello from SendGrid JSON!",
            "text": "This is the plain text version.",
            "html": "<p>This is the <strong>HTML</strong> version.</p>"
        }
        """;

        var source = MessageSource.Json(webhookJson);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0", message.Id);
        Assert.Equal("sender@example.com", message.Sender?.Address);
        Assert.Equal("inbox@yourdomain.com", message.Receiver?.Address);
        Assert.Equal(MessageContentType.Html, message.Content!.ContentType);
        Assert.Equal("<p>This is the <strong>HTML</strong> version.</p>", ((IHtmlContent)message.Content!).Html);
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessageStatusAsync_WithJsonSource_ParsesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a SendGrid event JSON webhook payload
        var statusJson = """
        {
            "event": "bounce",
            "sg_message_id": "bounce_message_123",
            "email": "invalid@nonexistentdomain.com",
            "timestamp": 1672531200,
            "reason": "550 5.1.1 User unknown",
            "status": "5.1.1",
            "type": "bounce"
        }
        """;

        var source = MessageSource.Json(statusJson);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal("bounce_message_123", result.Value?.MessageId);
        Assert.Equal(MessageStatus.DeliveryFailed, result.Value?.Status); // bounce maps to DeliveryFailed
        Assert.Equal("Email", result.Value?.AdditionalData["Channel"]);
        Assert.Equal("SendGrid", result.Value?.AdditionalData["Provider"]);
        Assert.Equal("550 5.1.1 User unknown", result.Value?.AdditionalData["reason"]);
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessagesAsync_WithHtmlAndTextContent_PrefersHtml()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create webhook with both HTML and text content
        var webhookData = "from=sender%40example.com&to=inbox%40yourdomain.com&subject=Mixed%20Content&text=Plain%20text%20content&html=%3Cp%3EHTML%20content%3C%2Fp%3E";
        var source = MessageSource.UrlPost(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal(MessageContentType.Html, message.Content!.ContentType);
        Assert.Equal("<p>HTML content</p>", ((IHtmlContent)message.Content!).Html);
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessagesAsync_WithOnlyTextContent_UsesText()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create webhook with only text content
        var webhookData = "from=sender%40example.com&to=inbox%40yourdomain.com&subject=Text%20Only&text=Only%20plain%20text%20content";
        var source = MessageSource.UrlPost(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal(MessageContentType.PlainText, message.Content!.ContentType);
        Assert.Equal("Only plain text content", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessagesAsync_WithInvalidSource_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create an unsupported source type (XML)
        var xmlData = "<email>Not supported</email>";
        var source = MessageSource.Xml(xmlData);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.UnsupportedContentType, result.Error.ErrorCode);
        Assert.Contains("Only JSON and form data are supported", result.Error.ErrorMessage);
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessageStatusAsync_WithInvalidSource_ReturnsError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create an unsupported source type (XML)
        var xmlData = "<status>Not supported</status>";
        var source = MessageSource.Xml(xmlData);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(SendGridErrorCodes.UnsupportedContentType, result.Error.ErrorCode);
        Assert.Contains("Only JSON and form data are supported", result.Error.ErrorMessage);
    }

    [Fact]
    public void SendGridEmailSchema_HasReceiveMessagesCapability()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "SendGridEmail schema should have ReceiveMessages capability for webhook support");
    }

    [Fact]
    public void SendGridEmailSchema_HasHandleMessageStateCapability()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "SendGridEmail schema should have HandleMessageState capability for status callbacks");
    }

    [Fact]
    public void SimpleEmailSchema_DoesNotHaveReceiveCapabilities()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SimpleEmail;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "SimpleEmail schema should not have ReceiveMessages capability as it's designed for send-only scenarios");
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "SimpleEmail schema should not have HandleMessageState capability as it's designed for send-only scenarios");
    }

    [Fact]
    public async Task SimpleEmailConnector_ReceiveMessages_ThrowsNotSupportedException()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SimpleEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = "from=sender%40example.com&to=inbox%40yourdomain.com&subject=Test&text=Test";
        var source = MessageSource.UrlPost(webhookData);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            connector.ReceiveMessagesAsync(source, CancellationToken.None));
    }

    [Fact]
    public async Task SimpleEmailConnector_ReceiveMessageStatus_ThrowsNotSupportedException()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SimpleEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var statusData = "event=delivered&sg_message_id=123&email=test%40example.com";
        var source = MessageSource.UrlPost(statusData);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            connector.ReceiveMessageStatusAsync(source, CancellationToken.None));
    }

    [Fact]
    public async Task SendGridEmailConnector_ReceiveMessagesAsync_WithAttachmentInfo_PreservesMetadata()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create webhook with attachment information
        var webhookData = "from=sender%40example.com&to=inbox%40yourdomain.com&subject=With%20Attachments&text=Email%20with%20files&attachments=2&attachment1=document.pdf&attachment2=image.jpg";
        var source = MessageSource.UrlPost(webhookData);

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
        Assert.Equal("document.pdf", message.Properties["attachment1"].Value);
        Assert.True(message.Properties.ContainsKey("attachment2"));
        Assert.Equal("image.jpg", message.Properties["attachment2"].Value);
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
    public async Task SendGridEmailConnector_ReceiveMessageStatusAsync_AllEvents_MapsCorrectly(string eventType, MessageStatus expectedStatus)
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("ApiKey", "SG.test_api_key_1234567890abcdef")
            .SetParameter("SandboxMode", false);
        
        var connector = new SendGridEmailConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var statusData = $"event={eventType}&sg_message_id=test123&email=test%40example.com&timestamp=1672531200";
        var source = MessageSource.UrlPost(statusData);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedStatus, result.Value.Status);
        Assert.Equal("Email", result.Value.AdditionalData["Channel"]);
        Assert.Equal("SendGrid", result.Value.AdditionalData["Provider"]);
    }
}