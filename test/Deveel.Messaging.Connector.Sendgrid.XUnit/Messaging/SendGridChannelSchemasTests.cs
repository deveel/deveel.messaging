using Microsoft.Extensions.Logging;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="SendGridChannelSchemas"/> class to verify
/// the schema configurations and their derivations.
/// </summary>
public class SendGridChannelSchemasTests
{
    [Fact]
    public void SendGridEmail_HasCorrectBasicProperties()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Assert
        Assert.Equal(SendGridConnectorConstants.Provider, schema.ChannelProvider);
        Assert.Equal(SendGridConnectorConstants.EmailChannel, schema.ChannelType);
        Assert.Equal("1.0.0", schema.Version);
        Assert.Equal("SendGrid Email Connector", schema.DisplayName);
    }

    [Fact]
    public void SendGridEmail_HasRequiredCapabilities()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.Templates));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
    }

    [Fact]
    public void SendGridEmail_HasRequiredParameters()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Assert
        var apiKeyParam = schema.Parameters.FirstOrDefault(p => p.Name == "ApiKey");
        Assert.NotNull(apiKeyParam);
        Assert.True(apiKeyParam.IsRequired);
        Assert.True(apiKeyParam.IsSensitive);
        Assert.Equal(DataType.String, apiKeyParam.DataType);

        var sandboxParam = schema.Parameters.FirstOrDefault(p => p.Name == "SandboxMode");
        Assert.NotNull(sandboxParam);
        Assert.False(sandboxParam.IsRequired);
        Assert.Equal(DataType.Boolean, sandboxParam.DataType);
        Assert.Equal(false, sandboxParam.DefaultValue);
    }

    [Fact]
    public void SendGridEmail_HasCorrectContentTypes()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Assert
        Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
        Assert.Contains(MessageContentType.Html, schema.ContentTypes);
        Assert.Contains(MessageContentType.Template, schema.ContentTypes);
        Assert.Contains(MessageContentType.Multipart, schema.ContentTypes);
    }

    [Fact]
    public void SendGridEmail_HandlesEmailEndpoints()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Assert
        var emailEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.EmailAddress);
        Assert.NotNull(emailEndpoint);
        Assert.True(emailEndpoint.CanSend);
        Assert.True(emailEndpoint.CanReceive); // Email addresses can be both senders and receivers
        Assert.True(emailEndpoint.IsRequired);

        var urlEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
        Assert.NotNull(urlEndpoint);
        Assert.False(urlEndpoint.CanSend);
        Assert.True(urlEndpoint.CanReceive);
    }

    [Fact]
    public void SendGridEmail_HasRequiredMessageProperties()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SendGridEmail;

        // Assert
        var subjectProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Subject");
        Assert.NotNull(subjectProperty);
        Assert.True(subjectProperty.IsRequired);
        Assert.Equal(DataType.String, subjectProperty.DataType);

        var priorityProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Priority");
        Assert.NotNull(priorityProperty);
        Assert.False(priorityProperty.IsRequired);

        var categoriesProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Categories");
        Assert.NotNull(categoriesProperty);
        Assert.False(categoriesProperty.IsRequired);
    }

    [Fact]
    public void SimpleEmail_IsCorrectlyDerived()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.SimpleEmail;

        // Assert
        Assert.Equal("SendGrid Simple Email", schema.DisplayName);
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.Templates));
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
        
        // Should still have basic capabilities
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));

        // Should not have template content type
        Assert.DoesNotContain(MessageContentType.Template, schema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.Multipart, schema.ContentTypes);
        
        // Should still have basic content types
        Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
        Assert.Contains(MessageContentType.Html, schema.ContentTypes);
    }

    [Fact]
    public void TransactionalEmail_IsCorrectlyDerived()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.TransactionalEmail;

        // Assert
        Assert.Equal("SendGrid Transactional Email", schema.DisplayName);
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.Templates));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));

        // Should not have scheduling properties
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "SendAt");
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "BatchId");
    }

    [Fact]
    public void MarketingEmail_HasMarketingProperties()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.MarketingEmail;

        // Assert
        Assert.Equal("SendGrid Marketing Email", schema.DisplayName);
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.Templates));

        // Should have marketing-specific properties
        var listIdProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "ListId");
        Assert.NotNull(listIdProperty);
        Assert.False(listIdProperty.IsRequired);

        var campaignIdProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "CampaignId");
        Assert.NotNull(campaignIdProperty);
        Assert.False(campaignIdProperty.IsRequired);
    }

    [Fact]
    public void TemplateEmail_IsTemplateOptimized()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.TemplateEmail;

        // Assert
        Assert.Equal("SendGrid Template Email", schema.DisplayName);
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.Templates));
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));

        // Should only have template content type
        Assert.Contains(MessageContentType.Template, schema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.PlainText, schema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.Html, schema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.Multipart, schema.ContentTypes);

        // Should have template-specific properties
        var templateIdProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "TemplateId");
        Assert.NotNull(templateIdProperty);
        Assert.True(templateIdProperty.IsRequired);

        var templateDataProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "TemplateData");
        Assert.NotNull(templateDataProperty);
        Assert.False(templateDataProperty.IsRequired);
    }

    [Fact]
    public void BulkEmail_HasBulkCapabilities()
    {
        // Arrange & Act
        var schema = SendGridChannelSchemas.BulkEmail;

        // Assert
        Assert.Equal("SendGrid Bulk Email", schema.DisplayName);
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));

        // Should have bulk-specific properties
        var mailBatchIdProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "MailBatchId");
        Assert.NotNull(mailBatchIdProperty);
        Assert.False(mailBatchIdProperty.IsRequired);

        var unsubscribeGroupIdProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "UnsubscribeGroupId");
        Assert.NotNull(unsubscribeGroupIdProperty);
        Assert.False(unsubscribeGroupIdProperty.IsRequired);
    }

    [Fact]
    public void AllSchemas_SupportEmailAddressEndpoint()
    {
        // Arrange
        var schemas = new[]
        {
            SendGridChannelSchemas.SendGridEmail,
            SendGridChannelSchemas.SimpleEmail,
            SendGridChannelSchemas.TransactionalEmail,
            SendGridChannelSchemas.MarketingEmail,
            SendGridChannelSchemas.TemplateEmail,
            SendGridChannelSchemas.BulkEmail
        };

        // Act & Assert
        foreach (var schema in schemas)
        {
            var emailEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.EmailAddress);
            Assert.NotNull(emailEndpoint);
            Assert.True(emailEndpoint.CanSend);
        }
    }

    [Fact]
    public void AllSchemas_RequireApiKey()
    {
        // Arrange
        var schemas = new[]
        {
            SendGridChannelSchemas.SendGridEmail,
            SendGridChannelSchemas.SimpleEmail,
            SendGridChannelSchemas.TransactionalEmail,
            SendGridChannelSchemas.MarketingEmail,
            SendGridChannelSchemas.TemplateEmail,
            SendGridChannelSchemas.BulkEmail
        };

        // Act & Assert
        foreach (var schema in schemas)
        {
            var apiKeyParam = schema.Parameters.FirstOrDefault(p => p.Name == "ApiKey");
            Assert.NotNull(apiKeyParam);
            Assert.True(apiKeyParam.IsRequired);
            Assert.True(apiKeyParam.IsSensitive);
        }
    }

    [Fact]
    public void AllSchemas_SupportBasicSendingCapability()
    {
        // Arrange
        var schemas = new[]
        {
            SendGridChannelSchemas.SendGridEmail,
            SendGridChannelSchemas.SimpleEmail,
            SendGridChannelSchemas.TransactionalEmail,
            SendGridChannelSchemas.MarketingEmail,
            SendGridChannelSchemas.TemplateEmail,
            SendGridChannelSchemas.BulkEmail
        };

        // Act & Assert
        foreach (var schema in schemas)
        {
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        }
    }
}