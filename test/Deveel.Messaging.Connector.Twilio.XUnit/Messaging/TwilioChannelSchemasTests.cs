namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="TwilioChannelSchemas"/> class to verify
/// the predefined Twilio SMS and WhatsApp channel schemas are configured correctly.
/// </summary>
public class TwilioChannelSchemasTests
{
    [Fact]
    public void TwilioSms_HasCorrectBasicProperties()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.Equal(TwilioConnectorConstants.Provider, schema.ChannelProvider);
        Assert.Equal(TwilioConnectorConstants.SmsChannel, schema.ChannelType);
        Assert.Equal("1.0.0", schema.Version);
        Assert.Equal("Twilio SMS Connector", schema.DisplayName);
    }

    [Fact]
    public void TwilioWhatsApp_HasCorrectBasicProperties()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        Assert.Equal(TwilioConnectorConstants.Provider, schema.ChannelProvider);
        Assert.Equal(TwilioConnectorConstants.WhatsAppChannel, schema.ChannelType);
        Assert.Equal("1.0.0", schema.Version);
        Assert.Equal("Twilio WhatsApp Business API Connector", schema.DisplayName);
    }

    [Fact]
    public void TwilioSms_HasCorrectCapabilities()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
    }

    [Fact]
    public void TwilioWhatsApp_HasCorrectCapabilities()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.Templates));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging)); // WhatsApp doesn't support bulk messaging
    }

    [Fact]
    public void TwilioSms_HasRequiredParameters()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        var requiredParams = schema.Parameters.Where(p => p.IsRequired).ToList();
        Assert.Equal(2, requiredParams.Count); // AccountSid and AuthToken only
        
        Assert.Contains(requiredParams, p => p.Name == "AccountSid" && p.DataType == DataType.String);
        Assert.Contains(requiredParams, p => p.Name == "AuthToken" && p.DataType == DataType.String && p.IsSensitive);
    }

    [Fact]
    public void TwilioWhatsApp_HasRequiredParameters()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        var requiredParams = schema.Parameters.Where(p => p.IsRequired).ToList();
        Assert.Equal(2, requiredParams.Count); // AccountSid and AuthToken only
        
        Assert.Contains(requiredParams, p => p.Name == "AccountSid" && p.DataType == DataType.String);
        Assert.Contains(requiredParams, p => p.Name == "AuthToken" && p.DataType == DataType.String && p.IsSensitive);
    }

    [Fact]
    public void TwilioSms_HasOptionalParameters()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        var optionalParams = schema.Parameters.Where(p => !p.IsRequired).ToList();
        Assert.True(optionalParams.Count >= 5);
        
        Assert.Contains(optionalParams, p => p.Name == "WebhookUrl");
        Assert.Contains(optionalParams, p => p.Name == "StatusCallback");
        Assert.Contains(optionalParams, p => p.Name == "ValidityPeriod" && p.DefaultValue?.Equals(14400) == true);
        Assert.Contains(optionalParams, p => p.Name == "MaxPrice");
        Assert.Contains(optionalParams, p => p.Name == "MessagingServiceSid");
    }

    [Fact]
    public void TwilioWhatsApp_HasOptionalParameters()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        var optionalParams = schema.Parameters.Where(p => !p.IsRequired).ToList();
        Assert.True(optionalParams.Count >= 2); // WebhookUrl and StatusCallback
        
        Assert.Contains(optionalParams, p => p.Name == "WebhookUrl");
        Assert.Contains(optionalParams, p => p.Name == "StatusCallback");
        // ContentSid and ContentVariables are now extracted from ITemplateContent, not parameters
    }

    [Fact]
    public void TwilioSms_HasCorrectContentTypes()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.Equal(2, schema.ContentTypes.Count);
        Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
        Assert.Contains(MessageContentType.Media, schema.ContentTypes);
    }

    [Fact]
    public void TwilioWhatsApp_HasCorrectContentTypes()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        Assert.Equal(3, schema.ContentTypes.Count);
        Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
        Assert.Contains(MessageContentType.Media, schema.ContentTypes);
        Assert.Contains(MessageContentType.Template, schema.ContentTypes);
    }

    [Fact]
    public void TwilioSms_HasCorrectEndpoints()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.Equal(2, schema.Endpoints.Count);

        var phoneEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.PhoneNumber);
        Assert.NotNull(phoneEndpoint);
        Assert.True(phoneEndpoint.CanSend);
        Assert.True(phoneEndpoint.CanReceive);

        var urlEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
        Assert.NotNull(urlEndpoint);
        Assert.False(urlEndpoint.CanSend);
        Assert.True(urlEndpoint.CanReceive);
    }

    [Fact]
    public void TwilioWhatsApp_HasCorrectEndpoints()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        Assert.Equal(2, schema.Endpoints.Count);

        var phoneEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.PhoneNumber);
        Assert.NotNull(phoneEndpoint);
        Assert.True(phoneEndpoint.CanSend);
        Assert.True(phoneEndpoint.CanReceive);

        var urlEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
        Assert.NotNull(urlEndpoint);
        Assert.False(urlEndpoint.CanSend);
        Assert.True(urlEndpoint.CanReceive);
    }

    [Fact]
    public void TwilioSms_HasCorrectAuthenticationType()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.Single(schema.AuthenticationTypes);
        Assert.Contains(AuthenticationType.Basic, schema.AuthenticationTypes);
    }

    [Fact]
    public void TwilioWhatsApp_HasCorrectAuthenticationType()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        Assert.Single(schema.AuthenticationTypes);
        Assert.Contains(AuthenticationType.Basic, schema.AuthenticationTypes);
    }

    [Fact]
    public void TwilioSms_HasMessageProperties()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.True(schema.MessageProperties.Count >= 6);
        
        // Note: Body and MediaUrl are no longer message properties - they are extracted from message content
        // Body comes from TextContent.Text when ContentType = PlainText
        // MediaUrl comes from MediaContent.FileUrl when ContentType = Media
        var optionalProps = schema.MessageProperties.Where(p => !p.IsRequired).ToList();
        Assert.Contains(optionalProps, p => p.Name == "ValidityPeriod");
        Assert.Contains(optionalProps, p => p.Name == "MaxPrice");
        Assert.Contains(optionalProps, p => p.Name == "ProvideCallback");
        Assert.Contains(optionalProps, p => p.Name == "AttemptLimits");
        Assert.Contains(optionalProps, p => p.Name == "SmartEncoded");
        Assert.Contains(optionalProps, p => p.Name == "PersistentAction");
    }

    [Fact]
    public void TwilioWhatsApp_HasMessageProperties()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Assert
        Assert.True(schema.MessageProperties.Count >= 2);
        
        // Note: ContentSid and ContentVariables are now extracted from ITemplateContent, not message properties
        // TemplateId maps to ContentSid, Parameters map to ContentVariables JSON
        var optionalProps = schema.MessageProperties.Where(p => !p.IsRequired).ToList();
        Assert.Contains(optionalProps, p => p.Name == "ProvideCallback");
        Assert.Contains(optionalProps, p => p.Name == "PersistentAction");
    }

    [Fact]
    public void SimpleSms_IsCorrectlyDerivedFromTwilioSms()
    {
        // Arrange & Act
        var baseSchema = TwilioChannelSchemas.TwilioSms;
        var simplifiedSchema = TwilioChannelSchemas.SimpleSms;

        // Assert - Core identity should be the same
        Assert.Equal(baseSchema.ChannelProvider, simplifiedSchema.ChannelProvider);
        Assert.Equal(baseSchema.ChannelType, simplifiedSchema.ChannelType);
        Assert.Equal(baseSchema.Version, simplifiedSchema.Version);
        Assert.True(baseSchema.IsCompatibleWith(simplifiedSchema));

        // Display name should be different
        Assert.Equal("Twilio Simple SMS", simplifiedSchema.DisplayName);

        // Capabilities should be restricted
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
        Assert.False(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.False(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));

        // Parameters should be reduced
        Assert.DoesNotContain(simplifiedSchema.Parameters, p => p.Name == "WebhookUrl");
        Assert.DoesNotContain(simplifiedSchema.Parameters, p => p.Name == "StatusCallback");
        Assert.DoesNotContain(simplifiedSchema.Parameters, p => p.Name == "MessagingServiceSid");

        // Content types should be reduced
        Assert.Single(simplifiedSchema.ContentTypes);
        Assert.Contains(MessageContentType.PlainText, simplifiedSchema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.Media, simplifiedSchema.ContentTypes);
    }

    [Fact]
    public void SimpleWhatsApp_IsCorrectlyDerivedFromTwilioWhatsApp()
    {
        // Arrange & Act
        var baseSchema = TwilioChannelSchemas.TwilioWhatsApp;
        var simplifiedSchema = TwilioChannelSchemas.SimpleWhatsApp;

        // Assert - Core identity should be the same
        Assert.Equal(baseSchema.ChannelProvider, simplifiedSchema.ChannelProvider);
        Assert.Equal(baseSchema.ChannelType, simplifiedSchema.ChannelType);
        Assert.Equal(baseSchema.Version, simplifiedSchema.Version);
        Assert.True(baseSchema.IsCompatibleWith(simplifiedSchema));

        // Display name should be different
        Assert.Equal("Twilio Simple WhatsApp", simplifiedSchema.DisplayName);

        // Capabilities should be restricted
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
        Assert.True(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
        Assert.False(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.False(simplifiedSchema.Capabilities.HasFlag(ChannelCapability.Templates));

        // Parameters should be reduced
        Assert.DoesNotContain(simplifiedSchema.Parameters, p => p.Name == "WebhookUrl");
        Assert.DoesNotContain(simplifiedSchema.Parameters, p => p.Name == "StatusCallback");
        // ContentSid and ContentVariables are no longer parameters

        // Content types should be reduced
        Assert.Equal(2, simplifiedSchema.ContentTypes.Count);
        Assert.Contains(MessageContentType.PlainText, simplifiedSchema.ContentTypes);
        Assert.Contains(MessageContentType.Media, simplifiedSchema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.Template, simplifiedSchema.ContentTypes);
    }

    [Fact]
    public void WhatsAppTemplates_IsCorrectlyDerivedFromTwilioWhatsApp()
    {
        // Arrange & Act
        var baseSchema = TwilioChannelSchemas.TwilioWhatsApp;
        var templateSchema = TwilioChannelSchemas.WhatsAppTemplates;

        // Assert - Core identity should be the same
        Assert.Equal(baseSchema.ChannelProvider, templateSchema.ChannelProvider);
        Assert.Equal(baseSchema.ChannelType, templateSchema.ChannelType);
        Assert.Equal(baseSchema.Version, templateSchema.Version);
        Assert.True(baseSchema.IsCompatibleWith(templateSchema));

        // Display name should be different
        Assert.Equal("Twilio WhatsApp Templates", templateSchema.DisplayName);

        // Capabilities should be restricted
        Assert.True(templateSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(templateSchema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.True(templateSchema.Capabilities.HasFlag(ChannelCapability.Templates));
        Assert.True(templateSchema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
        Assert.False(templateSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.False(templateSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));

        // ContentSid is now derived from TemplateContent.TemplateId, not a parameter

        // Content types should be reduced
        Assert.Equal(2, templateSchema.ContentTypes.Count);
        Assert.Contains(MessageContentType.PlainText, templateSchema.ContentTypes);
        Assert.Contains(MessageContentType.Template, templateSchema.ContentTypes);
        Assert.DoesNotContain(MessageContentType.Media, templateSchema.ContentTypes);
    }

    [Fact]
    public void NotificationSms_IsCorrectlyDerivedFromTwilioSms()
    {
        // Arrange & Act
        var baseSchema = TwilioChannelSchemas.TwilioSms;
        var notificationSchema = TwilioChannelSchemas.NotificationSms;

        // Assert - Core identity should be the same
        Assert.Equal(baseSchema.ChannelProvider, notificationSchema.ChannelProvider);
        Assert.Equal(baseSchema.ChannelType, notificationSchema.ChannelType);
        Assert.Equal(baseSchema.Version, notificationSchema.Version);
        Assert.True(baseSchema.IsCompatibleWith(notificationSchema));

        // Display name should be different
        Assert.Equal("Twilio Notification SMS", notificationSchema.DisplayName);

        // Should maintain status query capability but remove receiving
        Assert.True(notificationSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(notificationSchema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        Assert.False(notificationSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

        // Should not have webhook parameters
        Assert.DoesNotContain(notificationSchema.Parameters, p => p.Name == "WebhookUrl");

        // Should not support media
        Assert.DoesNotContain(MessageContentType.Media, notificationSchema.ContentTypes);
    }

    [Fact]
    public void BulkSms_IsCorrectlyDerivedFromTwilioSms()
    {
        // Arrange & Act
        var baseSchema = TwilioChannelSchemas.TwilioSms;
        var bulkSchema = TwilioChannelSchemas.BulkSms;

        // Assert - Core identity should be the same
        Assert.Equal(baseSchema.ChannelProvider, bulkSchema.ChannelProvider);
        Assert.Equal(baseSchema.ChannelType, bulkSchema.ChannelType);
        Assert.Equal(baseSchema.Version, bulkSchema.Version);
        Assert.True(baseSchema.IsCompatibleWith(bulkSchema));

        // Display name should be different
        Assert.Equal("Twilio Bulk SMS", bulkSchema.DisplayName);

        // Should maintain bulk messaging capability but remove receiving
        Assert.True(bulkSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(bulkSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
        Assert.False(bulkSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

        // MessagingServiceSid should be required
        var messagingServiceParam = bulkSchema.Parameters.FirstOrDefault(p => p.Name == "MessagingServiceSid");
        Assert.NotNull(messagingServiceParam);
        Assert.True(messagingServiceParam.IsRequired);

        // Phone number endpoint should be optional in bulk messaging (messaging service handles sender selection)
        var phoneEndpoint = bulkSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.PhoneNumber);
        Assert.NotNull(phoneEndpoint);
        Assert.False(phoneEndpoint.IsRequired);
    }

    [Fact]
    public void AllSchemas_PassValidationAsRestrictionsOfBase()
    {
        // Arrange
        var smsBaseSchema = TwilioChannelSchemas.TwilioSms;
        var whatsAppBaseSchema = TwilioChannelSchemas.TwilioWhatsApp;
        
        var smsDerivedSchemas = new[]
        {
            TwilioChannelSchemas.SimpleSms,
            TwilioChannelSchemas.NotificationSms,
            TwilioChannelSchemas.BulkSms
        };

        var whatsAppDerivedSchemas = new[]
        {
            TwilioChannelSchemas.SimpleWhatsApp,
            TwilioChannelSchemas.WhatsAppTemplates
        };

        // Act & Assert SMS schemas
        foreach (var derivedSchema in smsDerivedSchemas)
        {
            var validationResults = derivedSchema.ValidateAsRestrictionOf(smsBaseSchema);
            Assert.Empty(validationResults);
        }

        // Act & Assert WhatsApp schemas
        foreach (var derivedSchema in whatsAppDerivedSchemas)
        {
            var validationResults = derivedSchema.ValidateAsRestrictionOf(whatsAppBaseSchema);
            Assert.Empty(validationResults);
        }
    }

    [Fact]
    public void AllSchemas_ValidateConnectionSettings()
    {
        // Arrange
        var validSmsSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");

        var validWhatsAppSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");

        var validBulkSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("MessagingServiceSid", "MG1234567890123456789012345678901234");

        var validTemplateSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("ContentSid", "HX1234567890123456789012345678901234");

        var smsSchemas = new[]
        {
            TwilioChannelSchemas.TwilioSms,
            TwilioChannelSchemas.SimpleSms,
            TwilioChannelSchemas.NotificationSms
        };

        var whatsAppSchemas = new[]
        {
            TwilioChannelSchemas.TwilioWhatsApp,
            TwilioChannelSchemas.SimpleWhatsApp
        };

        // Act & Assert SMS schemas
        foreach (var schema in smsSchemas)
        {
            var validationResults = schema.ValidateConnectionSettings(validSmsSettings);
            Assert.Empty(validationResults);
        }

        // Act & Assert WhatsApp schemas
        foreach (var schema in whatsAppSchemas)
        {
            var validationResults = schema.ValidateConnectionSettings(validWhatsAppSettings);
            Assert.Empty(validationResults);
        }

        // Test bulk schema separately with messaging service
        var bulkValidationResults = TwilioChannelSchemas.BulkSms.ValidateConnectionSettings(validBulkSettings);
        Assert.Empty(bulkValidationResults);

        // Test template schema separately - no longer requires ContentSid parameter
        var templateValidationResults = TwilioChannelSchemas.WhatsAppTemplates.ValidateConnectionSettings(validWhatsAppSettings);
        Assert.Empty(templateValidationResults);
    }

    [Fact]
    public void TwilioSms_ValidatesMessageProperties()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;

        var validProps = new Dictionary<string, object?>
        {
            // Note: Body and MediaUrl are no longer message properties - they are extracted from message content
            // Body comes from TextContent.Text when ContentType = PlainText
            // MediaUrl comes from MediaContent.FileUrl when ContentType = Media
            ["ValidityPeriod"] = 3600,
            ["MaxPrice"] = 0.05m,
            ["ProvideCallback"] = true
        };

        var invalidProps = new Dictionary<string, object?>
        {
            ["ValidityPeriod"] = "invalid", // Wrong type
            ["UnknownProperty"] = "value"
        };

        // Act
        var validResults = schema.ValidateMessageProperties(validProps);
        var invalidResults = schema.ValidateMessageProperties(invalidProps).ToList();

        // Assert
        Assert.Empty(validResults);
        Assert.NotEmpty(invalidResults);
        Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Message property 'ValidityPeriod' has an incompatible type"));
        Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Unknown message property 'UnknownProperty'"));
    }

    [Fact]
    public void TwilioWhatsApp_ValidatesMessageProperties()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        var validProps = new Dictionary<string, object?>
        {
            // Note: ContentSid and ContentVariables are now extracted from ITemplateContent, not message properties
            // TemplateId maps to ContentSid, Parameters map to ContentVariables JSON
            ["ProvideCallback"] = true
        };

        var invalidProps = new Dictionary<string, object?>
        {
            ["UnknownProperty"] = "value"
        };

        // Act
        var validResults = schema.ValidateMessageProperties(validProps);
        var invalidResults = schema.ValidateMessageProperties(invalidProps).ToList();

        // Assert
        Assert.Empty(validResults);
        Assert.NotEmpty(invalidResults);
        Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Unknown message property 'UnknownProperty'"));
    }

    [Fact]
    public void TwilioMessagePropertyConfigurations_ValidatesSmsPhoneNumbers()
    {
        // Arrange
        var validProps = new Dictionary<string, object?>
        {
            // Note: Body and MediaUrl are no longer message properties - they are extracted from message content
            // This method now validates other properties specific to Twilio SMS
            ["ValidityPeriod"] = 3600
        };

        var invalidProps = new Dictionary<string, object?>
        {
            // Currently no specific validations are implemented for SMS properties
        };

        // Act
        var validResults = TwilioMessagePropertyConfigurations.ValidateTwilioSmsProperties(validProps);
        var invalidResults = TwilioMessagePropertyConfigurations.ValidateTwilioSmsProperties(invalidProps).ToList();

        // Assert
        Assert.Empty(validResults);
        Assert.Empty(invalidResults); // No specific validations currently implemented
    }

    [Fact]
    public void TwilioMessagePropertyConfigurations_ValidatesWhatsAppPhoneNumbers()
    {
        // Arrange
        var validProps = new Dictionary<string, object?>
        {
            // Note: ContentSid and ContentVariables are now extracted from ITemplateContent, not message properties
            // This method now validates other properties specific to Twilio WhatsApp
            ["ProvideCallback"] = true
        };

        var invalidProps = new Dictionary<string, object?>
        {
            // Currently no specific validations are implemented for WhatsApp properties
        };

        // Act
        var validResults = TwilioMessagePropertyConfigurations.ValidateTwilioWhatsAppProperties(validProps);
        var invalidResults = TwilioMessagePropertyConfigurations.ValidateTwilioWhatsAppProperties(invalidProps).ToList();

        // Assert
        Assert.Empty(validResults);
        Assert.Empty(invalidResults); // No specific validations currently implemented
    }

    [Fact]
    public void TwilioSms_AuthenticationValidation_ValidCredentials_PassesValidation()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC123456789abcdef123456789abcdef12")
            .SetParameter("AuthToken", "your_auth_token_here");

        // Act
        var results = schema.ValidateConnectionSettings(connectionSettings);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void TwilioSms_AuthenticationValidation_MissingAuthToken_FailsValidation()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC123456789abcdef123456789abcdef12");

        // Act
        var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

        // Assert
        Assert.True(results.Count >= 1);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Required parameter 'AuthToken'") || 
                                     r.ErrorMessage!.Contains("Basic authentication requires"));
    }

    [Fact]
    public void TwilioWhatsApp_AuthenticationValidation_ValidCredentials_PassesValidation()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC123456789abcdef123456789abcdef12")
            .SetParameter("AuthToken", "your_auth_token_here");

        // Act
        var results = schema.ValidateConnectionSettings(connectionSettings);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void TwilioWhatsApp_AuthenticationValidation_MissingAccountSid_FailsValidation()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AuthToken", "your_auth_token_here");

        // Act
        var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

        // Assert
        Assert.True(results.Count >= 1);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Required parameter 'AccountSid'") || 
                                     r.ErrorMessage!.Contains("Basic authentication requires"));
    }
}