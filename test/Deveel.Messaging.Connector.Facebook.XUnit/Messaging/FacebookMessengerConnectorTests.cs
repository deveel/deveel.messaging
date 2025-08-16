//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Unit tests for the FacebookMessengerConnector class.
/// </summary>
public class FacebookMessengerConnectorTests
{
    [Fact]
    public async Task InitializeAsync_WithValidSettings_ShouldSucceed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(ConnectorState.Ready, connector.State);
        mockFacebookService.Verify(x => x.Initialize("test-access-token"), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingPageAccessToken_ShouldFail()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.MissingCredentials, result.Error?.ErrorCode);
        Assert.Equal(ConnectorState.Error, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingPageId_ShouldFail()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.MissingPageId, result.Error?.ErrorCode);
        Assert.Equal(ConnectorState.Error, connector.State);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var pageInfo = new FacebookPageInfo { Id = "test-page-id", Name = "Test Page" };
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ReturnsAsync(pageInfo);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        mockFacebookService.Verify(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ReturnsAsync((FacebookPageInfo?)null);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "invalid-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.ConnectionFailed, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidTextMessage_ShouldSucceed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var messageResponse = new FacebookMessageResponse
        {
            MessageId = "fb-message-123",
            RecipientId = "user-123"
        };
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(messageResponse);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "test-message-1",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new TextContent("Hello, Facebook Messenger!")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("test-message-1", result.Value.MessageId);
        Assert.Equal("fb-message-123", result.Value.RemoteMessageId);
        Assert.Equal(MessageStatus.Sent, result.Value.Status);

        mockFacebookService.Verify(x => x.SendMessageAsync(
            It.Is<FacebookMessageRequest>(req => 
                req.Recipient == "user-123" && 
                req.Message.Text == "Hello, Facebook Messenger!"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithInvalidRecipient_ShouldFail()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "test-message-1",
            Receiver = new Endpoint(EndpointType.EmailAddress, "test@example.com"), // Wrong endpoint type
            Content = new TextContent("Hello, Facebook Messenger!")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(ConnectorErrorCodes.MessageValidationFailed, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_WithMediaMessage_ShouldSucceed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var messageResponse = new FacebookMessageResponse
        {
            MessageId = "fb-message-124",
            RecipientId = "user-123"
        };
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(messageResponse);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "test-message-2",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new MediaContent(MediaType.Image, "image.jpg", "https://example.com/image.jpg")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("test-message-2", result.Value.MessageId);
        Assert.Equal("fb-message-124", result.Value.RemoteMessageId);

        mockFacebookService.Verify(x => x.SendMessageAsync(
            It.Is<FacebookMessageRequest>(req => 
                req.Recipient == "user-123" && 
                req.Message.Attachment != null &&
                req.Message.Attachment.Type == "image" &&
                req.Message.Attachment.Payload.Url == "https://example.com/image.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithMessageProperties_ShouldApplyProperties()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var messageResponse = new FacebookMessageResponse
        {
            MessageId = "fb-message-125",
            RecipientId = "user-123"
        };
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(messageResponse);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "test-message-3",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new TextContent("Hello with properties!"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "MessagingType", new MessageProperty("MessagingType", "UPDATE") },
                { "NotificationType", new MessageProperty("NotificationType", "SILENT_PUSH") },
                { "Tag", new MessageProperty("Tag", "CONFIRMED_EVENT_UPDATE") }
            }
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);

        mockFacebookService.Verify(x => x.SendMessageAsync(
            It.Is<FacebookMessageRequest>(req => 
                req.MessagingType == "UPDATE" &&
                req.NotificationType == "SILENT_PUSH" &&
                req.Tag == "CONFIRMED_EVENT_UPDATE"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnConnectorStatus()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetStatusAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Contains("Facebook Messenger Connector", result.Value.Description);
        Assert.Equal("test-page-id", result.Value.AdditionalData["PageId"]);
        Assert.Equal("Ready", result.Value.AdditionalData["State"]);
    }

    [Fact]
    public async Task GetHealthAsync_WhenHealthy_ShouldReturnHealthyStatus()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var pageInfo = new FacebookPageInfo { Id = "test-page-id", Name = "Test Page" };
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ReturnsAsync(pageInfo);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsHealthy);
        Assert.Equal(ConnectorState.Ready, result.Value.State);
        Assert.Empty(result.Value.Issues);
    }

    [Fact]
    public async Task GetHealthAsync_WhenUnhealthy_ShouldReturnUnhealthyStatus()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ReturnsAsync((FacebookPageInfo?)null);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "invalid-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsHealthy);
        Assert.Single(result.Value.Issues);
        Assert.Contains("Connection test failed", result.Value.Issues.First());
    }

    [Fact]
    public async Task ValidateMessageAsync_WithValidMessage_ShouldReturnSuccess()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "test-message-1",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new TextContent("Valid message")
        };

        // Act
        var validationResults = new List<ValidationResult>();
        await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
        {
            if (result != ValidationResult.Success)
                validationResults.Add(result);
        }

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public async Task ValidateMessageAsync_WithInvalidMessage_ShouldReturnValidationErrors()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "", // Invalid - empty ID
            Receiver = new Endpoint(EndpointType.EmailAddress, "test@example.com"), // Invalid - wrong endpoint type
            Content = new TextContent("Message content")
        };

        // Act
        var validationResults = new List<ValidationResult>();
        await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
        {
            if (result != ValidationResult.Success)
                validationResults.Add(result);
        }

        // Assert
        Assert.NotEmpty(validationResults);
    }
}