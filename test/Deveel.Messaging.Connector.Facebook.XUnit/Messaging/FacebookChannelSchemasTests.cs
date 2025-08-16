//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging;

/// <summary>
/// Unit tests for Facebook channel schemas.
/// </summary>
public class FacebookChannelSchemasTests
{
    [Fact]
    public void FacebookMessenger_Schema_HasCorrectBasicProperties()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        Assert.Equal(FacebookConnectorConstants.Provider, schema.ChannelProvider);
        Assert.Equal(FacebookConnectorConstants.MessengerChannel, schema.ChannelType);
        Assert.Equal("1.0.0", schema.Version);
        Assert.Equal("Facebook Messenger Connector", schema.DisplayName);
    }

    [Fact]
    public void FacebookMessenger_Schema_HasCorrectCapabilities()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var expectedCapabilities = ChannelCapability.SendMessages | 
                                  ChannelCapability.ReceiveMessages |
                                  ChannelCapability.MediaAttachments |
                                  ChannelCapability.HealthCheck;

        Assert.Equal(expectedCapabilities, schema.Capabilities);
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
    }

    [Fact]
    public void FacebookMessenger_Schema_HasRequiredParameters()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var parameters = schema.Parameters.ToList();
        
        var pageAccessTokenParam = parameters.FirstOrDefault(p => p.Name == "PageAccessToken");
        Assert.NotNull(pageAccessTokenParam);
        Assert.True(pageAccessTokenParam.IsRequired);
        Assert.True(pageAccessTokenParam.IsSensitive);
        Assert.Equal(DataType.String, pageAccessTokenParam.DataType);

        var pageIdParam = parameters.FirstOrDefault(p => p.Name == "PageId");
        Assert.NotNull(pageIdParam);
        Assert.True(pageIdParam.IsRequired);
        Assert.Equal(DataType.String, pageIdParam.DataType);
    }

    [Fact]
    public void FacebookMessenger_Schema_HasOptionalParameters()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var parameters = schema.Parameters.ToList();
        
        var webhookUrlParam = parameters.FirstOrDefault(p => p.Name == "WebhookUrl");
        Assert.NotNull(webhookUrlParam);
        Assert.False(webhookUrlParam.IsRequired);

        var verifyTokenParam = parameters.FirstOrDefault(p => p.Name == "VerifyToken");
        Assert.NotNull(verifyTokenParam);
        Assert.False(verifyTokenParam.IsRequired);
        Assert.True(verifyTokenParam.IsSensitive);
    }

    [Fact]
    public void FacebookMessenger_Schema_HasCorrectContentTypes()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var contentTypes = schema.ContentTypes.ToList();
        Assert.Contains(MessageContentType.PlainText, contentTypes);
        Assert.Contains(MessageContentType.Media, contentTypes);
        Assert.Equal(2, contentTypes.Count);
    }

    [Fact]
    public void FacebookMessenger_Schema_HasCorrectEndpoints()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var endpoints = schema.Endpoints.ToList();
        
        var userIdEndpoint = endpoints.FirstOrDefault(e => e.Type == EndpointType.UserId);
        Assert.NotNull(userIdEndpoint);
        Assert.True(userIdEndpoint.CanSend);
        Assert.True(userIdEndpoint.CanReceive);
        Assert.True(userIdEndpoint.IsRequired);

        var urlEndpoint = endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
        Assert.NotNull(urlEndpoint);
        Assert.False(urlEndpoint.CanSend);
        Assert.True(urlEndpoint.CanReceive);
    }

    [Fact]
    public void FacebookMessenger_Schema_HasCorrectMessageProperties()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var messageProperties = schema.MessageProperties.ToList();
        
        var quickRepliesProperty = messageProperties.FirstOrDefault(p => p.Name == "QuickReplies");
        Assert.NotNull(quickRepliesProperty);
        Assert.False(quickRepliesProperty.IsRequired);
        Assert.Equal(DataType.String, quickRepliesProperty.DataType);

        var notificationTypeProperty = messageProperties.FirstOrDefault(p => p.Name == "NotificationType");
        Assert.NotNull(notificationTypeProperty);
        Assert.False(notificationTypeProperty.IsRequired);

        var messagingTypeProperty = messageProperties.FirstOrDefault(p => p.Name == "MessagingType");
        Assert.NotNull(messagingTypeProperty);
        Assert.False(messagingTypeProperty.IsRequired);

        var tagProperty = messageProperties.FirstOrDefault(p => p.Name == "Tag");
        Assert.NotNull(tagProperty);
        Assert.False(tagProperty.IsRequired);
    }

    [Fact]
    public void FacebookMessenger_Schema_HasCorrectAuthenticationTypes()
    {
        // Act
        var schema = FacebookChannelSchemas.FacebookMessenger;

        // Assert
        var authTypes = schema.AuthenticationTypes.ToList();
        Assert.Contains(AuthenticationType.Token, authTypes);
        Assert.Single(authTypes);
    }

    [Fact]
    public void SimpleMessenger_Schema_IsCorrectDerivation()
    {
        // Act
        var schema = FacebookChannelSchemas.SimpleMessenger;

        // Assert
        Assert.Equal("Facebook Simple Messenger", schema.DisplayName);
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        
        var parameters = schema.Parameters.ToList();
        Assert.DoesNotContain(parameters, p => p.Name == "WebhookUrl");
        Assert.DoesNotContain(parameters, p => p.Name == "VerifyToken");
        
        var contentTypes = schema.ContentTypes.ToList();
        Assert.DoesNotContain(contentTypes, ct => ct == MessageContentType.Media);
        
        var messageProperties = schema.MessageProperties.ToList();
        Assert.DoesNotContain(messageProperties, p => p.Name == "QuickReplies");
        Assert.DoesNotContain(messageProperties, p => p.Name == "Tag");
    }

    [Fact]
    public void NotificationMessenger_Schema_IsCorrectDerivation()
    {
        // Act
        var schema = FacebookChannelSchemas.NotificationMessenger;

        // Assert
        Assert.Equal("Facebook Notification Messenger", schema.DisplayName);
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
        
        var parameters = schema.Parameters.ToList();
        Assert.DoesNotContain(parameters, p => p.Name == "WebhookUrl");
        Assert.DoesNotContain(parameters, p => p.Name == "VerifyToken");
        
        var messageProperties = schema.MessageProperties.ToList();
        Assert.DoesNotContain(messageProperties, p => p.Name == "QuickReplies");
    }

    [Fact]
    public void MediaMessenger_Schema_IsCorrectDerivation()
    {
        // Act
        var schema = FacebookChannelSchemas.MediaMessenger;

        // Assert
        Assert.Equal("Facebook Media Messenger", schema.DisplayName);
        Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
        
        var messageProperties = schema.MessageProperties.ToList();
        
        var attachmentProperty = messageProperties.FirstOrDefault(p => p.Name == "Attachment");
        Assert.NotNull(attachmentProperty);
        Assert.False(attachmentProperty.IsRequired);
        Assert.Equal(DataType.String, attachmentProperty.DataType);

        var templateProperty = messageProperties.FirstOrDefault(p => p.Name == "Template");
        Assert.NotNull(templateProperty);
        Assert.False(templateProperty.IsRequired);
        Assert.Equal(DataType.String, templateProperty.DataType);
    }

    [Fact]
    public void AllSchemas_HaveConsistentProviderAndVersion()
    {
        // Act
        var schemas = new[]
        {
            FacebookChannelSchemas.FacebookMessenger,
            FacebookChannelSchemas.SimpleMessenger,
            FacebookChannelSchemas.NotificationMessenger,
            FacebookChannelSchemas.MediaMessenger
        };

        // Assert
        foreach (var schema in schemas)
        {
            Assert.Equal(FacebookConnectorConstants.Provider, schema.ChannelProvider);
            Assert.Equal("1.0.0", schema.Version);
        }
    }

    [Fact]
    public void AllSchemas_CanBeValidated()
    {
        // Act & Assert
        var schemas = new[]
        {
            FacebookChannelSchemas.FacebookMessenger,
            FacebookChannelSchemas.SimpleMessenger,
            FacebookChannelSchemas.NotificationMessenger,
            FacebookChannelSchemas.MediaMessenger
        };

        foreach (var schema in schemas)
        {
            // Basic validation - should not throw
            Assert.NotNull(schema.ChannelProvider);
            Assert.NotNull(schema.ChannelType);
            Assert.NotNull(schema.Version);
            Assert.NotEmpty(schema.Parameters);
            Assert.NotEmpty(schema.ContentTypes);
            Assert.NotEmpty(schema.Endpoints);
        }
    }
}