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

    // New tests to increase branch coverage and handle edge cases

    #region Initialization Edge Cases

    [Fact]
    public async Task InitializeAsync_FacebookServiceThrowsArgumentException_ShouldFailWithCredentialsError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.Initialize(It.IsAny<string>()))
                          .Throws(new ArgumentException("Invalid access token format"));

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "invalid-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.MissingCredentials, result.Error?.ErrorCode);
        Assert.Contains("Facebook authentication error", result.Error?.ErrorMessage);
        Assert.Equal(ConnectorState.Error, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_FacebookServiceThrowsUnexpectedException_ShouldFailWithInitializationError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.Initialize(It.IsAny<string>()))
                          .Throws(new InvalidOperationException("Unexpected service error"));

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(ConnectorErrorCodes.InitializationError, result.Error?.ErrorCode);
        Assert.Equal(ConnectorState.Error, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_PageAccessTokenIsEmpty_ShouldFail()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "")
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
    public async Task InitializeAsync_PageIdIsEmpty_ShouldFail()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.MissingPageId, result.Error?.ErrorCode);
        Assert.Equal(ConnectorState.Error, connector.State);
    }

    #endregion

    #region Connection Test Edge Cases

    [Fact]
    public async Task TestConnectionAsync_FacebookServiceThrowsGraphApiException_ShouldFailWithSpecificError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var pageInfo = new FacebookPageInfo { Id = "test-page-id", Name = "Test Page" };
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new InvalidOperationException("Facebook Graph API error: Access token expired"));

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "expired-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.ConnectionTestFailed, result.Error?.ErrorCode);
        Assert.Contains("Facebook Graph API error", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task TestConnectionAsync_FacebookServiceThrowsGenericException_ShouldFailWithTestError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new HttpRequestException("Network error"));

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.ConnectionTestFailed, result.Error?.ErrorCode);
        Assert.Contains("Network error", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task TestConnectionAsync_FacebookServiceReturnsNull_ShouldFailWithConnectionFailed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ReturnsAsync((FacebookPageInfo?)null);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.ConnectionFailed, result.Error?.ErrorCode);
        Assert.Contains("Unable to retrieve page information", result.Error?.ErrorMessage);
    }

    #endregion

    #region Message Sending Edge Cases

    [Fact]
    public async Task SendMessageAsync_ReceiverIsNull_ShouldFailWithInvalidRecipient()
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
            Receiver = null, // Null receiver
            Content = new TextContent("Hello, Facebook Messenger!")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidRecipient, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_ReceiverAddressIsEmpty_ShouldFailWithInvalidRecipient()
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
            Receiver = new Endpoint(EndpointType.UserId, ""), // Empty address
            Content = new TextContent("Hello, Facebook Messenger!")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidRecipient, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_ReceiverAddressIsWhitespace_ShouldFailWithInvalidRecipient()
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
            Receiver = new Endpoint(EndpointType.UserId, "   "), // Whitespace address
            Content = new TextContent("Hello, Facebook Messenger!")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidRecipient, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_FacebookServiceThrowsArgumentException_ShouldFailWithValidationError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new ArgumentException("Message text too long"));

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
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.SendMessageFailed, result.Error?.ErrorCode);
        Assert.Contains("Facebook validation error", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task SendMessageAsync_FacebookServiceThrowsGraphApiException_ShouldFailWithApiError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new InvalidOperationException("Facebook Graph API error: Rate limit exceeded"));

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
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.SendMessageFailed, result.Error?.ErrorCode);
        Assert.Contains("Facebook Graph API error", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task SendMessageAsync_FacebookServiceThrowsUnexpectedException_ShouldFailWithGenericError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new HttpRequestException("Network timeout"));

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
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.SendMessageFailed, result.Error?.ErrorCode);
        Assert.Contains("Network timeout", result.Error?.ErrorMessage);
    }

    #endregion

    #region Status Operations Edge Cases

    [Fact]
    public async Task GetStatusAsync_ThrowsException_ShouldFailWithStatusError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        // Create a connector that will throw when accessing status
        var connector = new TestableStatusExceptionConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetStatusAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(ConnectorErrorCodes.GetStatusError, result.Error?.ErrorCode); // Fixed: Base class catches exceptions and converts to standard error code
    }

    // Helper class to test status exception handling
    private class TestableStatusExceptionConnector : FacebookMessengerConnector
    {
        public TestableStatusExceptionConnector(ConnectionSettings connectionSettings, IFacebookService facebookService)
            : base(connectionSettings, facebookService) { }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Status operation failed");
        }
    }

    #endregion

    #region Health Check Edge Cases

    [Fact]
    public async Task GetHealthAsync_TestConnectionThrowsException_ShouldReturnUnhealthyWithIssue()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new InvalidOperationException("Facebook Graph API error: Service unavailable"));

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
        Assert.False(result.Value.IsHealthy);
        Assert.Single(result.Value.Issues);
        Assert.Contains("Connection test failed", result.Value.Issues.First()); // Fixed: Connection test failure message
    }

    [Fact]
    public async Task GetHealthAsync_ConnectorInErrorState_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageId", "test-page-id"); // Missing PageAccessToken

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None); // This will fail and set state to Error

        // Act
        var result = await connector.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsHealthy);
        Assert.Single(result.Value.Issues);
        Assert.Contains("Connector is in Error state", result.Value.Issues.First());
    }

    #endregion

    #region Message Receiving Edge Cases

    [Fact]
    public async Task ReceiveMessagesAsync_NonJsonContentType_ShouldFailWithUnsupportedContentType()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var messageSource = MessageSource.Xml("<xml>test</xml>");

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.UnsupportedContentType, result.Error?.ErrorCode);
        Assert.Contains("Only JSON content type is supported", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_EmptyWebhookData_ShouldFailWithInvalidWebhookData()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var messageSource = MessageSource.Json("{}");

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidWebhookData, result.Error?.ErrorCode);
        Assert.Contains("No valid messages found", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_InvalidJsonData_ShouldFailWithReceiveMessageFailed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var messageSource = MessageSource.Json("invalid json");

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.ReceiveMessageFailed, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_ValidWebhookWithAttachment_ShouldSucceed()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = @"{
            ""object"": ""page"",
            ""entry"": [{
                ""messaging"": [{
                    ""sender"": { ""id"": ""sender-123"" },
                    ""recipient"": { ""id"": ""page-123"" },
                    ""message"": {
                        ""mid"": ""message-id-123"",
                        ""timestamp"": 1458692752478,
                        ""attachments"": [{
                            ""type"": ""image"",
                            ""payload"": {
                                ""url"": ""https://example.com/image.jpg""
                            }
                        }]
                    }
                }]
            }]
        }";

        var messageSource = MessageSource.Json(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.IsType<MediaContent>(message.Content);
        Assert.Equal("sender-123", message.Sender?.Address);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WebhookWithoutTextOrAttachment_ShouldNotReturnMessage()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = @"{
            ""object"": ""page"",
            ""entry"": [{
                ""messaging"": [{
                    ""sender"": { ""id"": ""sender-123"" },
                    ""recipient"": { ""id"": ""page-123"" },
                    ""message"": {
                        ""mid"": ""message-id-123"",
                        ""timestamp"": 1458692752478
                    }
                }]
            }]
        }";

        var messageSource = MessageSource.Json(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidWebhookData, result.Error?.ErrorCode);
    }

    #endregion

    #region Message Validation Edge Cases

    [Fact]
    public async Task ValidateMessageAsync_MessageWithLongText_ShouldReturnValidationError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var longText = new string('A', 2001); // Exceeds Facebook's 2000 character limit
        var message = new Message
        {
            Id = "test-message-1",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new TextContent(longText)
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
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("2000 character limit"));
    }

    [Fact]
    public async Task ValidateMessageAsync_ValidMessage_ShouldReturnSuccess()
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
        var hasValidationErrors = false;
        await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
        {
            if (result != ValidationResult.Success)
            {
                hasValidationErrors = true;
                break;
            }
        }

        // Assert
        Assert.False(hasValidationErrors);
    }

    #endregion

    #region Quick Replies Edge Cases

    [Fact]
    public async Task SendMessageAsync_WithInvalidQuickRepliesJson_ShouldStillSendMessage()
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
            Content = new TextContent("Hello with invalid quick replies"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "QuickReplies", new MessageProperty("QuickReplies", "invalid json {") }
            }
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        // Message should still be sent even if quick replies parsing fails
    }

    #endregion

    #region Media Content Edge Cases

    [Fact]
    public async Task SendMessageAsync_WithUnsupportedMediaType_ShouldUseFileType()
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
            Content = new MediaContent(MediaType.File, "document.pdf", "https://example.com/document.pdf")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        mockFacebookService.Verify(x => x.SendMessageAsync(
            It.Is<FacebookMessageRequest>(req => 
                req.Message.Attachment != null &&
                req.Message.Attachment.Type == "file"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Webhook Parsing Edge Cases

    [Fact]
    public async Task ReceiveMessagesAsync_WebhookWithMissingSenderInfo_ShouldNotReturnMessage()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = @"{
            ""object"": ""page"",
            ""entry"": [{
                ""messaging"": [{
                    ""recipient"": { ""id"": ""page-123"" },
                    ""message"": {
                        ""mid"": ""message-id-123"",
                        ""text"": ""Hello""
                    }
                }]
            }]
        }";

        var messageSource = MessageSource.Json(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidWebhookData, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WebhookWithEmptySenderId_ShouldNotReturnMessage()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = @"{
            ""object"": ""page"",
            ""entry"": [{
                ""messaging"": [{
                    ""sender"": { ""id"": """" },
                    ""recipient"": { ""id"": ""page-123"" },
                    ""message"": {
                        ""mid"": ""message-id-123"",
                        ""text"": ""Hello""
                    }
                }]
            }]
        }";

        var messageSource = MessageSource.Json(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidWebhookData, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WebhookWithPostbackEvent_ShouldNotReturnMessage()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = @"{
            ""object"": ""page"",
            ""entry"": [{
                ""messaging"": [{
                    ""sender"": { ""id"": ""sender-123"" },
                    ""recipient"": { ""id"": ""page-123"" },
                    ""postback"": {
                        ""payload"": ""DEVELOPER_DEFINED_PAYLOAD""
                    }
                }]
            }]
        }";

        var messageSource = MessageSource.Json(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidWebhookData, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WebhookWithMissingMessageId_ShouldGenerateId()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var webhookData = @"{
            ""object"": ""page"",
            ""entry"": [{
                ""messaging"": [{
                    ""sender"": { ""id"": ""sender-123"" },
                    ""recipient"": { ""id"": ""page-123"" },
                    ""message"": {
                        ""text"": ""Hello without message ID""
                    }
                }]
            }]
        }";

        var messageSource = MessageSource.Json(webhookData);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.NotEmpty(message.Id); // Should have generated an ID
        Assert.True(Guid.TryParse(message.Id, out _)); // Should be a valid GUID
    }

    #endregion
}