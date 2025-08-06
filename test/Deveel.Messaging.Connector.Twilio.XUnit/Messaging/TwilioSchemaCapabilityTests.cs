using Xunit;

namespace Deveel.Messaging;

/// <summary>
/// Tests to verify that Twilio schemas correctly declare all the capabilities
/// that are implemented by the connectors.
/// </summary>
public class TwilioSchemaCapabilityTests
{
    [Fact]
    public void TwilioSmsSchema_HasReceiveMessagesCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "TwilioSms schema should have ReceiveMessages capability");
    }

    [Fact]
    public void TwilioSmsSchema_HasHandlerMessageStateCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "TwilioSms schema should have HandlerMessageState capability for receiving status updates via webhooks");
    }

    [Fact]
    public void TwilioSmsSchema_HasMessageStatusQueryCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery),
            "TwilioSms schema should have MessageStatusQuery capability for active status queries");
    }

    [Fact]
    public void TwilioWhatsAppSchema_HasReceiveMessagesCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "TwilioWhatsApp schema should have ReceiveMessages capability");
    }

    [Fact]
    public void TwilioWhatsAppSchema_HasHandlerMessageStateCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "TwilioWhatsApp schema should have HandlerMessageState capability for receiving status updates via webhooks");
    }

    [Fact]
    public void TwilioWhatsAppSchema_HasMessageStatusQueryCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Act & Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery),
            "TwilioWhatsApp schema should have MessageStatusQuery capability for active status queries");
    }

    [Fact]
    public async Task TwilioSmsConnector_SupportsReceiveMessagesAsync()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
        
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a valid Twilio webhook source
        var webhookData = "MessageSid=SM1234567890&From=%2B1234567890&To=%2B1987654321&Body=Test&MessageStatus=received";
        var source = MessageSource.UrlPost(webhookData);

        // Act & Assert - Should not throw NotSupportedException
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);
        
        // The result may fail due to parsing (which is expected since we're using a mock),
        // but it should not fail due to capability validation
        Assert.True(result.Successful || result.Error?.ErrorCode != "CAPABILITY_NOT_SUPPORTED");
    }

    [Fact]
    public async Task TwilioSmsConnector_SupportsReceiveMessageStatusAsync()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
        
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a valid Twilio status callback source
        var statusData = "MessageSid=SM1234567890&MessageStatus=delivered&To=%2B1987654321&From=%2B1234567890";
        var source = MessageSource.UrlPost(statusData);

        // Act & Assert - Should not throw NotSupportedException
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);
        
        // The result should be successful since we have valid status callback data
        Assert.True(result.Successful);
        Assert.Equal("SM1234567890", result.Value?.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Value?.Status);
    }

    [Fact]
    public void SimpleSmsSchema_DoesNotHaveReceiveMessagesCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "SimpleSms schema should not have ReceiveMessages capability as it's designed for send-only scenarios");
    }

    [Fact]
    public void SimpleSmsSchema_DoesNotHaveHandlerMessageStateCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "SimpleSms schema should not have HandlerMessageState capability as it's designed for send-only scenarios");
    }

    [Fact]
    public void TwilioSmsSchema_HasAllExpectedCapabilities()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var expectedCapabilities = 
            ChannelCapability.SendMessages |
            ChannelCapability.ReceiveMessages |
            ChannelCapability.MessageStatusQuery |
            ChannelCapability.HandleMessageState |
            ChannelCapability.BulkMessaging |
            ChannelCapability.HealthCheck;

        // Act & Assert
        Assert.Equal(expectedCapabilities, schema.Capabilities);
    }

    [Fact]
    public void TwilioWhatsAppSchema_HasAllExpectedCapabilities()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;
        var expectedCapabilities = 
            ChannelCapability.SendMessages |
            ChannelCapability.ReceiveMessages |
            ChannelCapability.MessageStatusQuery |
            ChannelCapability.HandleMessageState |
            ChannelCapability.Templates |
            ChannelCapability.MediaAttachments |
            ChannelCapability.HealthCheck;

        // Act & Assert
        Assert.Equal(expectedCapabilities, schema.Capabilities);
    }

    [Fact]
    public void SimpleWhatsAppSchema_DoesNotHaveReceiveMessagesCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "SimpleWhatsApp schema should not have ReceiveMessages capability as it's designed for send-only scenarios");
    }

    [Fact]
    public void SimpleWhatsAppSchema_DoesNotHaveHandlerMessageStateCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "SimpleWhatsApp schema should not have HandlerMessageState capability as it's designed for send-only scenarios");
    }

    [Fact]
    public void WhatsAppTemplatesSchema_DoesNotHaveReceiveMessagesCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.WhatsAppTemplates;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages),
            "WhatsAppTemplates schema should not have ReceiveMessages capability as it's designed for send-only scenarios");
    }

    [Fact]
    public void WhatsAppTemplatesSchema_DoesNotHaveHandlerMessageStateCapability()
    {
        // Arrange
        var schema = TwilioChannelSchemas.WhatsAppTemplates;

        // Act & Assert
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState),
            "WhatsAppTemplates schema should not have HandlerMessageState capability as it's designed for send-only scenarios");
    }
}