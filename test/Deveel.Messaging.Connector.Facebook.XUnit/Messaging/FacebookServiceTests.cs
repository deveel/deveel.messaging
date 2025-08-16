//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using RestSharp;

namespace Deveel.Messaging;

/// <summary>
/// Unit tests for the FacebookService class using RestSharp for Facebook Graph API integration.
/// </summary>
public class FacebookServiceTests
{
    [Fact]
    public void Initialize_WithValidToken_SetsToken()
    {
        // Arrange
        var service = new FacebookService();

        // Act
        service.Initialize("EAATest123456789|ValidPageAccessToken");

        // Assert - No exception should be thrown
        Assert.True(true); // If we get here, initialization succeeded
    }

    [Fact]
    public void Initialize_WithNullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new FacebookService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Initialize(null!));
    }

    [Fact]
    public void Initialize_WithInvalidToken_ThrowsArgumentException()
    {
        // Arrange
        var service = new FacebookService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Initialize("invalid-token"));
        Assert.Throws<ArgumentException>(() => service.Initialize("short"));
        Assert.Throws<ArgumentException>(() => service.Initialize("token with spaces"));
    }

    [Fact]
    public async Task FetchPageAsync_WithoutInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new FacebookService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.FetchPageAsync("test-page-id", CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithoutInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new FacebookService();
        var request = new FacebookMessageRequest
        {
            Recipient = "user-123",
            Message = new FacebookMessage { Text = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.SendMessageAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithInvalidRequest_ThrowsArgumentException()
    {
        // Arrange
        var service = new FacebookService();
        service.Initialize("EAATest123456789|ValidPageAccessToken");

        // Act & Assert - Null request
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMessageAsync(null!, CancellationToken.None));

        // Act & Assert - Empty recipient
        var emptyRecipientRequest = new FacebookMessageRequest
        {
            Recipient = "",
            Message = new FacebookMessage { Text = "Test" }
        };
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SendMessageAsync(emptyRecipientRequest, CancellationToken.None));

        // Act & Assert - Null message
        var nullMessageRequest = new FacebookMessageRequest
        {
            Recipient = "user-123",
            Message = null!
        };
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SendMessageAsync(nullMessageRequest, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithTooLongText_ThrowsArgumentException()
    {
        // Arrange
        var service = new FacebookService();
        service.Initialize("EAATest123456789|ValidPageAccessToken");

        var longTextRequest = new FacebookMessageRequest
        {
            Recipient = "user-123",
            Message = new FacebookMessage { Text = new string('A', 2001) } // Exceeds Facebook's 2000 char limit
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SendMessageAsync(longTextRequest, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithTooManyQuickReplies_ThrowsArgumentException()
    {
        // Arrange
        var service = new FacebookService();
        service.Initialize("EAATest123456789|ValidPageAccessToken");

        var request = new FacebookMessageRequest
        {
            Recipient = "user-123",
            Message = new FacebookMessage 
            { 
                Text = "Choose an option:",
                QuickReplies = Enumerable.Range(1, 14).Select(i => new FacebookQuickReply 
                { 
                    ContentType = "text", 
                    Title = $"Option {i}", 
                    Payload = $"OPTION_{i}" 
                }).ToList()
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SendMessageAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithInvalidMessagingType_ThrowsArgumentException()
    {
        // Arrange
        var service = new FacebookService();
        service.Initialize("EAATest123456789|ValidPageAccessToken");

        var request = new FacebookMessageRequest
        {
            Recipient = "user-123",
            Message = new FacebookMessage { Text = "Test" },
            MessagingType = "INVALID_TYPE"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SendMessageAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithInvalidNotificationType_ThrowsArgumentException()
    {
        // Arrange
        var service = new FacebookService();
        service.Initialize("EAATest123456789|ValidPageAccessToken");

        var request = new FacebookMessageRequest
        {
            Recipient = "user-123",
            Message = new FacebookMessage { Text = "Test" },
            NotificationType = "INVALID_NOTIFICATION"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SendMessageAsync(request, CancellationToken.None));
    }

    [Fact]
    public void Constructor_WithoutRestClient_CreatesDefaultClient()
    {
        // Arrange & Act
        var service = new FacebookService();

        // Assert - Should not throw exception
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithRestClient_UsesProvidedClient()
    {
        // Arrange
        var restClient = new RestClient("https://graph.facebook.com");

        // Act
        var service = new FacebookService(restClient);

        // Assert - Should not throw exception
        Assert.NotNull(service);
    }

    [Fact]
    public void ValidatePageAccessToken_ValidTokens_ReturnsTrue()
    {
        // Use reflection to test the private method
        var method = typeof(FacebookService).GetMethod("IsValidPageAccessToken", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Test valid token formats
        Assert.True((bool)method!.Invoke(null, new object[] { "EAATest123456789|ValidPageAccessToken" })!);
        Assert.True((bool)method.Invoke(null, new object[] { "EAAGTest123456789abcdef" })!);
        Assert.True((bool)method.Invoke(null, new object[] { "someLongTokenWithPipe|123456" })!);
    }

    [Fact]
    public void ValidatePageAccessToken_InvalidTokens_ReturnsFalse()
    {
        // Use reflection to test the private method
        var method = typeof(FacebookService).GetMethod("IsValidPageAccessToken", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Test invalid token formats
        Assert.False((bool)method!.Invoke(null, new object[] { "" })!);
        Assert.False((bool)method.Invoke(null, new object[] { "short" })!);
        Assert.False((bool)method.Invoke(null, new object[] { "token with spaces" })!);
        Assert.False((bool)method.Invoke(null, new object[] { "   " })!);
    }

    [Fact]
    public void BuildFacebookMessagePayload_ValidRequest_CreatesCorrectStructure()
    {
        // Use reflection to test the private method
        var method = typeof(FacebookService).GetMethod("BuildFacebookMessagePayload", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var request = new FacebookMessageRequest
        {
            Recipient = "user-123",
            MessagingType = "RESPONSE",
            Message = new FacebookMessage { Text = "Hello, World!" }
        };

        // Act
        var result = method!.Invoke(null, new object[] { request });

        // Assert
        Assert.NotNull(result);
        var payload = result as Dictionary<string, object>;
        Assert.NotNull(payload);
        Assert.True(payload.ContainsKey("recipient"));
        Assert.True(payload.ContainsKey("messaging_type"));
        Assert.True(payload.ContainsKey("message"));
        Assert.Equal("RESPONSE", payload["messaging_type"]);
    }

    [Fact]
    public void BuildMessageContent_WithTextAndQuickReplies_CreatesCorrectStructure()
    {
        // Use reflection to test the private method
        var method = typeof(FacebookService).GetMethod("BuildMessageContent", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var message = new FacebookMessage
        {
            Text = "Choose an option:",
            QuickReplies = new List<FacebookQuickReply>
            {
                new FacebookQuickReply 
                { 
                    ContentType = "text", 
                    Title = "Yes", 
                    Payload = "YES_PAYLOAD",
                    ImageUrl = "https://example.com/yes.png"
                },
                new FacebookQuickReply 
                { 
                    ContentType = "text", 
                    Title = "No", 
                    Payload = "NO_PAYLOAD"
                }
            }
        };

        // Act
        var result = method!.Invoke(null, new object[] { message });

        // Assert
        Assert.NotNull(result);
        var content = result as Dictionary<string, object>;
        Assert.NotNull(content);
        Assert.True(content.ContainsKey("text"));
        Assert.True(content.ContainsKey("quick_replies"));
        Assert.Equal("Choose an option:", content["text"]);
        
        var quickReplies = content["quick_replies"] as object[];
        Assert.NotNull(quickReplies);
        Assert.Equal(2, quickReplies.Length);
    }

    [Fact]
    public void BuildMessageContent_WithAttachment_CreatesCorrectStructure()
    {
        // Use reflection to test the private method
        var method = typeof(FacebookService).GetMethod("BuildMessageContent", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var message = new FacebookMessage
        {
            Attachment = new FacebookAttachment
            {
                Type = "image",
                Payload = new FacebookPayload 
                { 
                    Url = "https://example.com/image.jpg",
                    IsReusable = true
                }
            }
        };

        // Act
        var result = method!.Invoke(null, new object[] { message });

        // Assert
        Assert.NotNull(result);
        var content = result as Dictionary<string, object>;
        Assert.NotNull(content);
        Assert.True(content.ContainsKey("attachment"));
        Assert.False(content.ContainsKey("text"));
    }
}