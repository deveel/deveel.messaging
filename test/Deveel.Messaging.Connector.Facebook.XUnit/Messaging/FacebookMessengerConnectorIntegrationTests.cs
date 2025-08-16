//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Moq;

using System.Text.Json;

namespace Deveel.Messaging;

/// <summary>
/// Integration tests for the FacebookMessengerConnector class that test complete workflows
/// including webhook message receiving, error scenarios, and performance characteristics.
/// </summary>
public class FacebookMessengerConnectorIntegrationTests
{
    [Fact]
    public async Task FacebookMessenger_FullLifecycle_WorksEndToEnd()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var pageInfo = new FacebookPageInfo { Id = "test-page-id", Name = "Test Page", Category = "Business" };
        var messageResponse = new FacebookMessageResponse { MessageId = "fb-msg-123", RecipientId = "user-456" };

        mockFacebookService.Setup(x => x.FetchPageAsync("test-page-id", It.IsAny<CancellationToken>()))
                          .ReturnsAsync(pageInfo);
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(messageResponse);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id")
            .SetParameter("WebhookUrl", "https://example.com/webhook")
            .SetParameter("VerifyToken", "verify-token");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);

        // Act & Assert - Full lifecycle

        // 1. Initialize
        var initResult = await connector.InitializeAsync(CancellationToken.None);
        Assert.True(initResult.Successful);
        Assert.Equal(ConnectorState.Ready, connector.State);

        // 2. Test connection
        var connectionResult = await connector.TestConnectionAsync(CancellationToken.None);
        Assert.True(connectionResult.Successful);

        // 3. Get status
        var statusResult = await connector.GetStatusAsync(CancellationToken.None);
        Assert.True(statusResult.Successful);
        Assert.Contains("Facebook Messenger Connector", statusResult.Value!.Description);

        // 4. Get health
        var healthResult = await connector.GetHealthAsync(CancellationToken.None);
        Assert.True(healthResult.Successful);
        Assert.True(healthResult.Value!.IsHealthy);

        // 5. Send message
        var message = new Message
        {
            Id = "test-msg-1",
            Receiver = new Endpoint(EndpointType.UserId, "user-456"),
            Content = new TextContent("Hello from integration test!")
        };

        var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
        Assert.True(sendResult.Successful);
        Assert.Equal("test-msg-1", sendResult.Value!.MessageId);
        Assert.Equal("fb-msg-123", sendResult.Value.RemoteMessageId);

        // 6. Shutdown
        await connector.ShutdownAsync(CancellationToken.None);
        Assert.Equal(ConnectorState.Shutdown, connector.State);
    }

    [Fact]
    public async Task ReceiveMessages_FromFacebookWebhook_ParsesCorrectly()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Create Facebook webhook JSON payload
        var webhookPayload = new
        {
            @object = "page",
            entry = new[]
            {
                new
                {
                    id = "test-page-id",
                    time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    messaging = new[]
                    {
                        new
                        {
                            sender = new { id = "user-123" },
                            recipient = new { id = "test-page-id" },
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            message = new
                            {
                                mid = "fb-msg-456",
                                text = "Hello from user!"
                            }
                        }
                    }
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(webhookPayload);
        var messageSource = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);

        var receivedMessage = result.Value.Messages.First();
        Assert.Equal("fb-msg-456", receivedMessage.Id);
        Assert.Equal(EndpointType.UserId, receivedMessage.Sender!.Type);
        Assert.Equal("user-123", receivedMessage.Sender.Address);
        Assert.Equal(EndpointType.UserId, receivedMessage.Receiver!.Type);
        Assert.Equal("test-page-id", receivedMessage.Receiver.Address);
        Assert.Equal(MessageContentType.PlainText, receivedMessage.Content!.ContentType);
        Assert.Equal("Hello from user!", ((ITextContent)receivedMessage.Content).Text);
    }

    [Fact]
    public async Task ReceiveMessages_WithMediaAttachment_ParsesCorrectly()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Create Facebook webhook JSON payload with media attachment
        var webhookPayload = new
        {
            @object = "page",
            entry = new[]
            {
                new
                {
                    id = "test-page-id",
                    time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    messaging = new[]
                    {
                        new
                        {
                            sender = new { id = "user-789" },
                            recipient = new { id = "test-page-id" },
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            message = new
                            {
                                mid = "fb-msg-media-123",
                                attachments = new[]
                                {
                                    new
                                    {
                                        type = "image",
                                        payload = new
                                        {
                                            url = "https://example.com/image.jpg"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(webhookPayload);
        var messageSource = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);

        var receivedMessage = result.Value.Messages.First();
        Assert.Equal("fb-msg-media-123", receivedMessage.Id);
        Assert.Equal("user-789", receivedMessage.Sender!.Address);
        Assert.Equal(MessageContentType.Media, receivedMessage.Content!.ContentType);
        
        var mediaContent = (IMediaContent)receivedMessage.Content;
        Assert.Equal("https://example.com/image.jpg", mediaContent.FileUrl);
        Assert.Equal(MediaType.Image, mediaContent.MediaType);
    }

    [Fact]
    public async Task ReceiveMessages_InvalidWebhookData_ReturnsError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Create invalid webhook payload (missing required fields)
        var invalidPayload = new { invalid = "data" };
        var jsonPayload = JsonSerializer.Serialize(invalidPayload);
        var messageSource = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.InvalidWebhookData, result.Error!.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessages_NonJsonContentType_ReturnsError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var messageSource = MessageSource.UrlPost("invalid=form&data=true");

        // Act
        var result = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.UnsupportedContentType, result.Error!.ErrorCode);
    }

    [Fact]
    public async Task SendMessage_WithQuickReplies_SendsCorrectRequest()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var messageResponse = new FacebookMessageResponse { MessageId = "fb-msg-quick-123", RecipientId = "user-999" };

        FacebookMessageRequest? capturedRequest = null;
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .Callback<FacebookMessageRequest, CancellationToken>((req, ct) => capturedRequest = req)
                          .ReturnsAsync(messageResponse);

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var quickRepliesJson = JsonSerializer.Serialize(new[]
        {
            new { content_type = "text", title = "Yes", payload = "YES_PAYLOAD" },
            new { content_type = "text", title = "No", payload = "NO_PAYLOAD" }
        });

        var message = new Message
        {
            Id = "test-msg-quick",
            Receiver = new Endpoint(EndpointType.UserId, "user-999"),
            Content = new TextContent("Do you agree?"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "QuickReplies", new MessageProperty("QuickReplies", quickRepliesJson) },
                { "NotificationType", new MessageProperty("NotificationType", "SILENT_PUSH") }
            }
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(capturedRequest);
        Assert.Equal("user-999", capturedRequest.Recipient);
        Assert.Equal("Do you agree?", capturedRequest.Message.Text);
        Assert.Equal("SILENT_PUSH", capturedRequest.NotificationType);
        Assert.NotNull(capturedRequest.Message.QuickReplies);
        Assert.Equal(2, capturedRequest.Message.QuickReplies.Count);
        Assert.Equal("Yes", capturedRequest.Message.QuickReplies[0].Title);
        Assert.Equal("YES_PAYLOAD", capturedRequest.Message.QuickReplies[0].Payload);
    }

    [Fact]
    public async Task ValidateMessage_WithVeryLongText_ReturnsValidationError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        // Create message with text exceeding Facebook's limit (2000 characters)
        var longText = new string('A', 2001);
        var message = new Message
        {
            Id = "test-msg-long",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new TextContent(longText)
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
        {
            if (result != System.ComponentModel.DataAnnotations.ValidationResult.Success)
                validationResults.Add(result);
        }

        // Assert
        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public async Task HealthCheck_WhenConnectionFails_ReturnsUnhealthyStatus()
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
        var healthResult = await connector.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(healthResult.Successful); // Health check itself should succeed
        Assert.NotNull(healthResult.Value);
        Assert.False(healthResult.Value.IsHealthy); // But connector should be unhealthy
        Assert.Single(healthResult.Value.Issues);
        Assert.Contains("Connection test failed", healthResult.Value.Issues.First());
    }

    [Fact]
    public async Task ConcurrentMessageSending_HighThroughput_HandlesCorrectly()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        var messageCounter = 0;

        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(() => new FacebookMessageResponse 
                          { 
                              MessageId = $"fb-msg-{Interlocked.Increment(ref messageCounter)}", 
                              RecipientId = "user-123" 
                          });

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var messageCount = 50;
        var semaphore = new SemaphoreSlim(10); // Limit concurrency to simulate real-world conditions

        // Act - Send many concurrent messages
        var tasks = Enumerable.Range(1, messageCount).Select(async i =>
        {
            await semaphore.WaitAsync();
            try
            {
                var message = new Message
                {
                    Id = $"test-msg-{i}",
                    Receiver = new Endpoint(EndpointType.UserId, "user-123"),
                    Content = new TextContent($"Concurrent message {i}")
                };

                return await connector.SendMessageAsync(message, CancellationToken.None);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Successful));
        Assert.Equal(messageCount, results.Length);

        // Verify all message IDs are unique
        var messageIds = results.Select(r => r.Value!.RemoteMessageId).ToHashSet();
        Assert.Equal(messageCount, messageIds.Count);

        // Verify service was called the expected number of times
        mockFacebookService.Verify(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()), 
                                  Times.Exactly(messageCount));
    }

    [Fact]
    public async Task ErrorHandling_ServiceThrowsException_ReturnsAppropriateError()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new HttpRequestException("Facebook Graph API error: 403 - Forbidden"));

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-access-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync(CancellationToken.None);

        var message = new Message
        {
            Id = "test-msg-error",
            Receiver = new Endpoint(EndpointType.UserId, "user-123"),
            Content = new TextContent("This will fail")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(FacebookErrorCodes.SendMessageFailed, result.Error!.ErrorCode);
        Assert.Contains("Facebook Graph API error", result.Error.ErrorMessage);
    }
}