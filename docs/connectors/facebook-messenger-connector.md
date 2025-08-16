# Facebook Messenger Connector

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com/) [![Package](https://img.shields.io/badge/Package-Deveel.Messaging.Connector.Facebook-blue)](https://www.nuget.org/packages/Deveel.Messaging.Connector.Facebook/)

## Overview

The **Facebook Messenger Connector** provides comprehensive integration with the Facebook Messenger Platform API, enabling bidirectional messaging through Facebook Pages. This connector supports text messages, media attachments, quick replies, and webhook integration for receiving messages.

### Key Features

- ? **Send Messages** - Text and media messages to Facebook users
- ? **Receive Messages** - Process incoming messages via webhooks
- ? **Media Support** - Images, videos, audio, and file attachments
- ? **Quick Replies** - Interactive quick reply buttons (up to 13)
- ? **Message Properties** - Messaging type, notification type, and tags
- ? **Health Monitoring** - Built-in connection testing and health checks
- ? **Facebook Graph API** - Full compliance with Facebook Graph API v21.0
- ? **RestSharp Integration** - Modern HTTP client with proper error handling

### Facebook Requirements

- **Facebook Page** - A Facebook Page for your business
- **Facebook App** - A Facebook App connected to your Page
- **Page Access Token** - Long-lived Page Access Token from Facebook
- **Webhook Setup** - For receiving incoming messages (optional)

## ?? Installation

### Package Installation
```bash
dotnet add package Deveel.Messaging.Connector.Facebook
```

### NuGet Package Manager
```powershell
Install-Package Deveel.Messaging.Connector.Facebook
```

### PackageReference
```xml
<PackageReference Include="Deveel.Messaging.Connector.Facebook" Version="1.0.0" />
```

## ?? Configuration

### Basic Setup

The Facebook Messenger connector requires minimal configuration to get started:

```csharp
using Deveel.Messaging;

// Basic configuration
var connectionSettings = new ConnectionSettings()
    .SetParameter("PageAccessToken", "your-page-access-token")
    .SetParameter("PageId", "your-facebook-page-id");

var connector = new FacebookMessengerConnector(connectionSettings);
await connector.InitializeAsync();
```

### Complete Configuration

```csharp
var connectionSettings = new ConnectionSettings()
    .SetParameter("PageAccessToken", "EAAxxxxx|your-long-lived-page-access-token")
    .SetParameter("PageId", "123456789012345")  // Your Facebook Page ID
    .SetParameter("WebhookUrl", "https://your-domain.com/webhook/facebook")  // Optional
    .SetParameter("VerifyToken", "your-webhook-verify-token");  // Optional

var connector = new FacebookMessengerConnector(connectionSettings);
```

### Configuration Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `PageAccessToken` | String | ? Yes | Facebook Page Access Token from your Facebook App |
| `PageId` | String | ? Yes | Facebook Page ID where messages will be sent from |
| `WebhookUrl` | String | ? No | URL to receive webhook notifications for incoming messages |
| `VerifyToken` | String | ? No | Token for Facebook webhook verification |

### Facebook App Setup

1. **Create Facebook App**
   - Go to [Facebook Developers](https://developers.facebook.com/)
   - Create a new App of type "Business"
   - Add "Messenger" product to your app

2. **Get Page Access Token**
   - In Messenger settings, generate a Page Access Token
   - Select your Facebook Page
   - Copy the generated token (starts with "EAA")

3. **Configure Webhooks** (Optional)
   - Set webhook URL: `https://your-domain.com/webhook/facebook`
   - Set verify token: Your custom verification string
   - Subscribe to `messages`, `messaging_postbacks` events

## ?? Usage Examples

### Basic Text Message

```csharp
using Deveel.Messaging;

// Initialize connector
var connectionSettings = new ConnectionSettings()
    .SetParameter("PageAccessToken", "your-page-access-token")
    .SetParameter("PageId", "your-page-id");

var connector = new FacebookMessengerConnector(connectionSettings);
await connector.InitializeAsync();

// Send text message
var message = new Message
{
    Id = Guid.NewGuid().ToString(),
    Receiver = new Endpoint(EndpointType.UserId, "facebook-user-psid"), // Facebook User PSID
    Content = new TextContent("Hello from Facebook Messenger!")
};

var result = await connector.SendMessageAsync(message);

if (result.Successful)
{
    Console.WriteLine($"Message sent! Facebook Message ID: {result.Value.RemoteMessageId}");
}
```

### Media Message (Image)

```csharp
var message = new Message
{
    Id = Guid.NewGuid().ToString(),
    Receiver = new Endpoint(EndpointType.UserId, "facebook-user-psid"),
    Content = new MediaContent(
        MediaType.Image, 
        "product.jpg", 
        "https://your-cdn.com/images/product.jpg"
    )
};

var result = await connector.SendMessageAsync(message);
```

### Message with Quick Replies

```csharp
// Quick replies as JSON
var quickRepliesJson = JsonSerializer.Serialize(new[]
{
    new { content_type = "text", title = "Yes", payload = "USER_SAID_YES" },
    new { content_type = "text", title = "No", payload = "USER_SAID_NO" },
    new { content_type = "text", title = "Maybe", payload = "USER_SAID_MAYBE" }
});

var message = new Message
{
    Id = Guid.NewGuid().ToString(),
    Receiver = new Endpoint(EndpointType.UserId, "facebook-user-psid"),
    Content = new TextContent("Do you like our service?"),
    Properties = new Dictionary<string, MessageProperty>
    {
        { "QuickReplies", new MessageProperty("QuickReplies", quickRepliesJson) }
    }
};

var result = await connector.SendMessageAsync(message);
```

### Message with Properties

```csharp
var message = new Message
{
    Id = Guid.NewGuid().ToString(),
    Receiver = new Endpoint(EndpointType.UserId, "facebook-user-psid"),
    Content = new TextContent("Order confirmed! Your package will arrive tomorrow."),
    Properties = new Dictionary<string, MessageProperty>
    {
        { "MessagingType", new MessageProperty("MessagingType", "UPDATE") },
        { "NotificationType", new MessageProperty("NotificationType", "REGULAR") },
        { "Tag", new MessageProperty("Tag", "CONFIRMED_EVENT_UPDATE") }
    }
};

var result = await connector.SendMessageAsync(message);
```

## ?? Receiving Messages (Webhooks)

### Webhook Setup

Configure your webhook endpoint to receive Facebook messages:

```csharp
[ApiController]
[Route("webhook/facebook")]
public class FacebookWebhookController : ControllerBase
{
    private readonly FacebookMessengerConnector _connector;

    public FacebookWebhookController(FacebookMessengerConnector connector)
    {
        _connector = connector;
    }

    // Webhook verification (GET)
    [HttpGet]
    public IActionResult VerifyWebhook([FromQuery] string hub_mode, 
                                      [FromQuery] string hub_verify_token, 
                                      [FromQuery] string hub_challenge)
    {
        if (hub_mode == "subscribe" && hub_verify_token == "your-verify-token")
        {
            return Ok(hub_challenge);
        }
        return BadRequest();
    }

    // Receive messages (POST)
    [HttpPost]
    public async Task<IActionResult> ReceiveMessage()
    {
        var json = await Request.GetRawBodyStringAsync();
        var messageSource = MessageSource.Json(json);
        
        var result = await _connector.ReceiveMessagesAsync(messageSource);
        
        if (result.Successful)
        {
            foreach (var message in result.Value.Messages)
            {
                await ProcessIncomingMessage(message);
            }
        }
        
        return Ok();
    }

    private async Task ProcessIncomingMessage(IMessage message)
    {
        // Process the incoming Facebook message
        Console.WriteLine($"Received from {message.Sender?.Address}: {message.Content}");
        
        // You can reply to the message
        if (message.Content is ITextContent textContent)
        {
            var reply = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Receiver = message.Sender!,
                Content = new TextContent($"Echo: {textContent.Text}")
            };
            
            await _connector.SendMessageAsync(reply);
        }
    }
}
```

### Processing Incoming Messages

```csharp
// Example of processing different message types
private async Task ProcessIncomingMessage(IMessage message)
{
    switch (message.Content)
    {
        case ITextContent textContent:
            await HandleTextMessage(message, textContent.Text);
            break;
            
        case IMediaContent mediaContent:
            await HandleMediaMessage(message, mediaContent);
            break;
            
        default:
            Console.WriteLine($"Unknown message type from {message.Sender?.Address}");
            break;
    }
}

private async Task HandleTextMessage(IMessage message, string text)
{
    Console.WriteLine($"Text message from {message.Sender?.Address}: {text}");
    
    // Handle specific commands
    switch (text.ToLower())
    {
        case "hello":
        case "hi":
            await SendWelcomeMessage(message.Sender!);
            break;
            
        case "help":
            await SendHelpMessage(message.Sender!);
            break;
            
        default:
            await SendDefaultResponse(message.Sender!);
            break;
    }
}

private async Task SendWelcomeMessage(IEndpoint sender)
{
    var welcomeMessage = new Message
    {
        Id = Guid.NewGuid().ToString(),
        Receiver = sender,
        Content = new TextContent("Welcome! How can I help you today?"),
        Properties = new Dictionary<string, MessageProperty>
        {
            { "QuickReplies", new MessageProperty("QuickReplies", JsonSerializer.Serialize(new[]
            {
                new { content_type = "text", title = "Order Status", payload = "CHECK_ORDER" },
                new { content_type = "text", title = "Support", payload = "GET_SUPPORT" },
                new { content_type = "text", title = "Catalog", payload = "VIEW_CATALOG" }
            }))}
        }
    };
    
    await _connector.SendMessageAsync(welcomeMessage);
}
```

## ?? Message Properties

Facebook Messenger supports several message properties for enhanced functionality:

### Messaging Types

| Type | Description | Use Case |
|------|-------------|----------|
| `RESPONSE` | Default - Response to user message | Most messages |
| `UPDATE` | Update about ongoing conversation | Order updates, status changes |
| `MESSAGE_TAG` | Tagged message outside 24h window | Requires specific tags |
| `NON_PROMOTIONAL_SUBSCRIPTION` | Subscription messages | Newsletter, alerts |

### Notification Types

| Type | Description |
|------|-------------|
| `REGULAR` | Default - Normal notification sound |
| `SILENT_PUSH` | Silent notification |
| `NO_PUSH` | No notification |

### Message Tags

For MESSAGE_TAG messaging type, you can use specific tags:

```csharp
// Examples of Facebook message tags
"CONFIRMED_EVENT_UPDATE"      // Event confirmations
"POST_PURCHASE_UPDATE"        // Purchase updates  
"ACCOUNT_UPDATE"              // Account changes
"PAYMENT_UPDATE"              // Payment notifications
```

## ?? Testing

### Unit Testing

```csharp
using Moq;
using Xunit;

public class FacebookMessengerConnectorTests
{
    [Fact]
    public async Task SendMessage_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var mockFacebookService = new Mock<IFacebookService>();
        mockFacebookService.Setup(x => x.SendMessageAsync(It.IsAny<FacebookMessageRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new FacebookMessageResponse 
                          { 
                              MessageId = "test-message-id", 
                              RecipientId = "test-user-id" 
                          });

        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings, mockFacebookService.Object);
        await connector.InitializeAsync();

        var message = new Message
        {
            Id = "test-id",
            Receiver = new Endpoint(EndpointType.UserId, "test-user-id"),
            Content = new TextContent("Test message")
        };

        // Act
        var result = await connector.SendMessageAsync(message);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal("test-message-id", result.Value.RemoteMessageId);
    }

    [Fact]
    public async Task SendMessage_InvalidRecipient_ReturnsError()
    {
        // Arrange
        var connectionSettings = new ConnectionSettings()
            .SetParameter("PageAccessToken", "test-token")
            .SetParameter("PageId", "test-page-id");

        var connector = new FacebookMessengerConnector(connectionSettings);
        await connector.InitializeAsync();

        var message = new Message
        {
            Id = "test-id",
            Receiver = new Endpoint(EndpointType.EmailAddress, "test@example.com"), // Wrong type
            Content = new TextContent("Test message")
        };

        // Act
        var result = await connector.SendMessageAsync(message);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(ConnectorErrorCodes.MessageValidationFailed, result.Error?.ErrorCode);
    }
}
```

### Integration Testing

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task SendMessage_RealFacebook_DeliversMessage()
{
    // Only run with real credentials
    var pageAccessToken = Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ACCESS_TOKEN");
    var pageId = Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ID");
    var testUserId = Environment.GetEnvironmentVariable("FACEBOOK_TEST_USER_ID");
    
    Skip.If(string.IsNullOrEmpty(pageAccessToken), "No Facebook credentials provided");

    var connectionSettings = new ConnectionSettings()
        .SetParameter("PageAccessToken", pageAccessToken)
        .SetParameter("PageId", pageId);

    var connector = new FacebookMessengerConnector(connectionSettings);
    await connector.InitializeAsync();

    var message = new Message
    {
        Id = Guid.NewGuid().ToString(),
        Receiver = new Endpoint(EndpointType.UserId, testUserId),
        Content = new TextContent($"Integration test message - {DateTime.UtcNow}")
    };

    var result = await connector.SendMessageAsync(message);

    Assert.True(result.Successful);
    Assert.NotEmpty(result.Value.RemoteMessageId);
}
```

## ?? Health Monitoring

### Connection Testing

```csharp
// Test connection to Facebook
var connectionResult = await connector.TestConnectionAsync();

if (connectionResult.Successful)
{
    Console.WriteLine("Facebook connection is healthy");
}
else
{
    Console.WriteLine($"Facebook connection failed: {connectionResult.Error?.ErrorMessage}");
}
```

### Health Checks

```csharp
// Get detailed health information
var healthResult = await connector.GetHealthAsync();

if (healthResult.Successful && healthResult.Value.IsHealthy)
{
    Console.WriteLine("Connector is healthy");
    Console.WriteLine($"Page ID: {healthResult.Value.Metrics["PageId"]}");
    Console.WriteLine($"API Version: {healthResult.Value.Metrics["ApiVersion"]}");
    Console.WriteLine($"Last API Call: {healthResult.Value.Metrics["LastSuccessfulApiCall"]}");
}
else
{
    Console.WriteLine("Connector is unhealthy");
    foreach (var issue in healthResult.Value.Issues)
    {
        Console.WriteLine($"Issue: {issue}");
    }
}
```

### Status Information

```csharp
var statusResult = await connector.GetStatusAsync();

if (statusResult.Successful)
{
    var status = statusResult.Value;
    Console.WriteLine($"Status: {status.Status}");
    Console.WriteLine($"Description: {status.Description}");
    Console.WriteLine($"Page ID: {status.AdditionalData["PageId"]}");
    Console.WriteLine($"State: {status.AdditionalData["State"]}");
    Console.WriteLine($"Uptime: {status.AdditionalData["Uptime"]}");
}
```

## ? Performance

### Concurrent Messages

The connector supports concurrent message sending:

```csharp
var messages = new List<Message>();
for (int i = 0; i < 10; i++)
{
    messages.Add(new Message
    {
        Id = Guid.NewGuid().ToString(),
        Receiver = new Endpoint(EndpointType.UserId, $"user-{i}"),
        Content = new TextContent($"Message {i}")
    });
}

// Send messages concurrently
var tasks = messages.Select(msg => connector.SendMessageAsync(msg));
var results = await Task.WhenAll(tasks);

// Check results
var successCount = results.Count(r => r.Successful);
Console.WriteLine($"Successfully sent {successCount}/{messages.Count} messages");
```

### Rate Limiting

Facebook has rate limits. The connector handles this gracefully:

```csharp
// Facebook rate limits (approximate):
// - 50 requests per second per Page
// - 1000 messages per day for development
// - Higher limits for approved apps

// The connector will throw appropriate exceptions for rate limit errors
try
{
    var result = await connector.SendMessageAsync(message);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Facebook Graph API error"))
{
    // Handle rate limiting or other Facebook API errors
    Console.WriteLine($"Facebook API error: {ex.Message}");
}
```

## ?? Security

### Token Security

```csharp
// Store tokens securely
var pageAccessToken = Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ACCESS_TOKEN");
var pageId = Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ID");

// Or use Azure Key Vault, AWS Secrets Manager, etc.
var connectionSettings = new ConnectionSettings()
    .SetParameter("PageAccessToken", pageAccessToken)
    .SetParameter("PageId", pageId);
```

### Webhook Security

```csharp
// Verify webhook authenticity
[HttpPost]
public async Task<IActionResult> ReceiveMessage()
{
    // Verify Facebook signature
    var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
    var body = await Request.GetRawBodyStringAsync();
    
    if (!VerifyFacebookSignature(signature, body, "your-app-secret"))
    {
        return Unauthorized();
    }
    
    // Process webhook...
}

private bool VerifyFacebookSignature(string signature, string body, string appSecret)
{
    if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
        return false;
        
    var expectedSignature = signature.Substring(7);
    var computedSignature = ComputeHmacSha256(body, appSecret);
    
    return string.Equals(expectedSignature, computedSignature, StringComparison.OrdinalIgnoreCase);
}
```

## ?? Advanced Configuration

### Custom Schema

```csharp
// Create custom schema with specific capabilities
var customSchema = new ChannelSchema("Facebook", "Messenger", "1.0.0")
    .WithDisplayName("Custom Facebook Messenger")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    .AddParameter(new ChannelParameter("PageAccessToken", DataType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Facebook Page Access Token"
    })
    .AddParameter(new ChannelParameter("PageId", DataType.String)
    {
        IsRequired = true,
        Description = "Facebook Page ID"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .HandlesMessageEndpoint(EndpointType.UserId, e =>
    {
        e.CanSend = true;
        e.CanReceive = true;
        e.IsRequired = true;
    });

var connector = new FacebookMessengerConnector(customSchema, connectionSettings);
```

### Dependency Injection

```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register connector
    services.AddSingleton<ConnectionSettings>(provider =>
    {
        var configuration = provider.GetService<IConfiguration>();
        return new ConnectionSettings()
            .SetParameter("PageAccessToken", configuration["Facebook:PageAccessToken"])
            .SetParameter("PageId", configuration["Facebook:PageId"])
            .SetParameter("WebhookUrl", configuration["Facebook:WebhookUrl"])
            .SetParameter("VerifyToken", configuration["Facebook:VerifyToken"]);
    });

    services.AddSingleton<FacebookMessengerConnector>();
    
    // Initialize on startup
    services.AddHostedService<FacebookConnectorInitializer>();
}

public class FacebookConnectorInitializer : IHostedService
{
    private readonly FacebookMessengerConnector _connector;

    public FacebookConnectorInitializer(FacebookMessengerConnector connector)
    {
        _connector = connector;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connector.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _connector.ShutdownAsync(cancellationToken);
    }
}
```

## ?? Error Handling

### Common Error Codes

| Error Code | Description | Solution |
|------------|-------------|----------|
| `MISSING_CREDENTIALS` | Page Access Token not provided | Check token configuration |
| `MISSING_PAGE_ID` | Page ID not provided | Verify Page ID setting |
| `INVALID_RECIPIENT` | Invalid Facebook User ID (PSID) | Use correct PSID format |
| `SEND_MESSAGE_FAILED` | Message sending failed | Check Facebook API response |
| `CONNECTION_FAILED` | Cannot connect to Facebook | Verify token and internet connection |
| `UNSUPPORTED_CONTENT_TYPE` | Invalid webhook content type | Use JSON content type |
| `INVALID_WEBHOOK_DATA` | Invalid webhook payload | Check Facebook webhook format |

### Error Handling Patterns

```csharp
try
{
    var result = await connector.SendMessageAsync(message);
    
    if (!result.Successful)
    {
        switch (result.Error?.ErrorCode)
        {
            case FacebookErrorCodes.InvalidRecipient:
                // Handle invalid recipient
                Console.WriteLine("Invalid Facebook User ID (PSID)");
                break;
                
            case FacebookErrorCodes.SendMessageFailed:
                // Handle send failure
                Console.WriteLine($"Send failed: {result.Error.ErrorMessage}");
                break;
                
            default:
                Console.WriteLine($"Unknown error: {result.Error?.ErrorMessage}");
                break;
        }
    }
}
catch (ArgumentException ex) when (ex.Message.Contains("Facebook validation"))
{
    // Handle validation errors
    Console.WriteLine($"Validation error: {ex.Message}");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Facebook Graph API"))
{
    // Handle Facebook API errors
    Console.WriteLine($"Facebook API error: {ex.Message}");
}
```

### Retry Logic

```csharp
public async Task<ConnectorResult<SendResult>> SendMessageWithRetry(Message message, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var result = await connector.SendMessageAsync(message);
            
            if (result.Successful)
                return result;
                
            // Check if error is retryable
            if (IsRetryableError(result.Error))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                await Task.Delay(delay);
                continue;
            }
            
            return result; // Non-retryable error
        }
        catch (Exception ex) when (IsRetryableException(ex))
        {
            if (attempt == maxRetries)
                throw;
                
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            await Task.Delay(delay);
        }
    }
    
    throw new InvalidOperationException($"Failed to send message after {maxRetries} attempts");
}

private bool IsRetryableError(ConnectorError? error)
{
    return error?.ErrorCode == FacebookErrorCodes.ConnectionFailed ||
           error?.ErrorMessage?.Contains("rate limit") == true;
}

private bool IsRetryableException(Exception ex)
{
    return ex is HttpRequestException ||
           ex is TaskCanceledException ||
           (ex is InvalidOperationException && ex.Message.Contains("Facebook Graph API error: HTTP 5"));
}
```

## ?? API Reference

### FacebookMessengerConnector

#### Constructor
```csharp
public FacebookMessengerConnector(ConnectionSettings connectionSettings, 
                                 IFacebookService? facebookService = null, 
                                 ILogger<FacebookMessengerConnector>? logger = null)

public FacebookMessengerConnector(IChannelSchema schema, 
                                 ConnectionSettings connectionSettings, 
                                 IFacebookService? facebookService = null, 
                                 ILogger<FacebookMessengerConnector>? logger = null)
```

#### Methods
```csharp
Task<ConnectorResult<bool>> InitializeAsync(CancellationToken cancellationToken = default)
Task<ConnectorResult<bool>> TestConnectionAsync(CancellationToken cancellationToken = default)
Task<ConnectorResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
Task<ConnectorResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource source, CancellationToken cancellationToken = default)
Task<ConnectorResult<StatusInfo>> GetStatusAsync(CancellationToken cancellationToken = default)
Task<ConnectorResult<ConnectorHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage message, CancellationToken cancellationToken = default)
```

### Facebook Constants

```csharp
public static class FacebookConnectorConstants
{
    public const string Provider = "facebook";
    public const string MessengerChannel = "messenger";
    public const string GraphApiVersion = "v21.0";
    public const string GraphApiBaseUrl = "https://graph.facebook.com";
}
```

### Facebook Error Codes

```csharp
public static class FacebookErrorCodes
{
    public const string MissingCredentials = "MISSING_CREDENTIALS";
    public const string MissingPageId = "MISSING_PAGE_ID";
    public const string InvalidRecipient = "INVALID_RECIPIENT";
    public const string SendMessageFailed = "SEND_MESSAGE_FAILED";
    public const string ConnectionFailed = "CONNECTION_FAILED";
    public const string ConnectionTestFailed = "CONNECTION_TEST_FAILED";
    public const string UnsupportedContentType = "UNSUPPORTED_CONTENT_TYPE";
    public const string InvalidWebhookData = "INVALID_WEBHOOK_DATA";
    public const string ReceiveMessageFailed = "RECEIVE_MESSAGE_FAILED";
    public const string StatusError = "STATUS_ERROR";
}
```

## ?? Resources

### Facebook Documentation
- **[Messenger Platform API](https://developers.facebook.com/docs/messenger-platform)** - Official Facebook Messenger API documentation
- **[Graph API Reference](https://developers.facebook.com/docs/graph-api)** - Facebook Graph API documentation
- **[Webhooks](https://developers.facebook.com/docs/messenger-platform/webhooks)** - Setting up Facebook webhooks
- **[Page Access Tokens](https://developers.facebook.com/docs/messenger-platform/getting-started/app-setup)** - Getting Page Access Tokens

### Facebook Developer Tools
- **[Facebook Developers](https://developers.facebook.com/)** - Developer console and app management
- **[Graph API Explorer](https://developers.facebook.com/tools/explorer/)** - Test Graph API calls
- **[Webhook Tester](https://developers.facebook.com/tools/webhooks/)** - Test webhook integration

### Framework Documentation
- **[Framework Overview](../README.md)** - Main framework documentation
- **[Getting Started](../getting-started.md)** - Quick start guide
- **[Channel Schemas](../ChannelSchema-Usage.md)** - Schema configuration
- **[Connector Implementation](../ChannelConnector-Usage.md)** - Custom connector guide

---

*The Facebook Messenger Connector provides robust, production-ready integration with Facebook's Messenger Platform API using modern RestSharp HTTP client and comprehensive error handling.*