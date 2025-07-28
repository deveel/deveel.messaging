//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="TwilioChannelSchemas"/> class to verify
/// the predefined Twilio SMS channel schemas are configured correctly.
/// </summary>
public class TwilioChannelSchemasTests
{
    [Fact]
    public void TwilioSms_HasCorrectBasicProperties()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        Assert.Equal("Twilio", schema.ChannelProvider);
        Assert.Equal("SMS", schema.ChannelType);
        Assert.Equal("1.0.0", schema.Version);
        Assert.Equal("Twilio SMS Connector", schema.DisplayName);
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
    public void TwilioSms_HasRequiredParameters()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

        // Assert
        var requiredParams = schema.Parameters.Where(p => p.IsRequired).ToList();
        Assert.Equal(2, requiredParams.Count); // Changed from 3 to 2 since FromNumber is now optional
        
        Assert.Contains(requiredParams, p => p.Name == "AccountSid" && p.DataType == ParameterType.String);
        Assert.Contains(requiredParams, p => p.Name == "AuthToken" && p.DataType == ParameterType.String && p.IsSensitive);
        // FromNumber is now optional in the base schema
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
    public void TwilioSms_HasCorrectAuthenticationType()
    {
        // Arrange & Act
        var schema = TwilioChannelSchemas.TwilioSms;

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
        Assert.True(schema.MessageProperties.Count >= 8);
        
        var requiredProps = schema.MessageProperties.Where(p => p.IsRequired).ToList();
        Assert.Single(requiredProps);
        Assert.Contains(requiredProps, p => p.Name == "To" && p.DataType == ParameterType.String);

        var optionalProps = schema.MessageProperties.Where(p => !p.IsRequired).ToList();
        Assert.Contains(optionalProps, p => p.Name == "Body");
        Assert.Contains(optionalProps, p => p.Name == "MediaUrl");
        Assert.Contains(optionalProps, p => p.Name == "ValidityPeriod");
        Assert.Contains(optionalProps, p => p.Name == "MaxPrice");
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

        // FromNumber should be removed (messaging service handles sender selection)
        Assert.DoesNotContain(bulkSchema.Parameters, p => p.Name == "FromNumber");
    }

    [Fact]
    public void AllSchemas_PassValidationAsRestrictionsOfBase()
    {
        // Arrange
        var baseSchema = TwilioChannelSchemas.TwilioSms;
        var derivedSchemas = new[]
        {
            TwilioChannelSchemas.SimpleSms,
            TwilioChannelSchemas.NotificationSms,
            TwilioChannelSchemas.BulkSms
        };

        // Act & Assert
        foreach (var derivedSchema in derivedSchemas)
        {
            var validationResults = derivedSchema.ValidateAsRestrictionOf(baseSchema);
            Assert.Empty(validationResults);
        }
    }

    [Fact]
    public void AllSchemas_ValidateConnectionSettings()
    {
        // Arrange
        var validSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("FromNumber", "+1234567890");

        var validBulkSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("MessagingServiceSid", "MG1234567890123456789012345678901234");

        var schemas = new[]
        {
            TwilioChannelSchemas.TwilioSms,
            TwilioChannelSchemas.SimpleSms,
            TwilioChannelSchemas.NotificationSms
        };

        // Act & Assert
        foreach (var schema in schemas)
        {
            var validationResults = schema.ValidateConnectionSettings(validSettings);
            Assert.Empty(validationResults);
        }

        // Test bulk schema separately with messaging service
        var bulkValidationResults = TwilioChannelSchemas.BulkSms.ValidateConnectionSettings(validBulkSettings);
        Assert.Empty(bulkValidationResults);
    }

    [Fact]
    public void TwilioSms_ValidatesMessageProperties()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;

        var validProps = new Dictionary<string, object?>
        {
            ["To"] = "+1234567890",
            ["Body"] = "Test message",
            ["ValidityPeriod"] = 3600,
            ["MaxPrice"] = 0.05m,
            ["ProvideCallback"] = true
        };

        var invalidProps = new Dictionary<string, object?>
        {
            ["Body"] = "Test message",
            // Missing required "To" property
            ["ValidityPeriod"] = "invalid", // Wrong type
            ["UnknownProperty"] = "value"
        };

        // Act
        var validResults = schema.ValidateMessageProperties(validProps);
        var invalidResults = schema.ValidateMessageProperties(invalidProps).ToList();

        // Assert
        Assert.Empty(validResults);
        Assert.NotEmpty(invalidResults);
        Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Required message property 'To' is missing"));
        Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Message property 'ValidityPeriod' has an incompatible type"));
        Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Unknown message property 'UnknownProperty'"));
    }
}