# Firebase Cloud Messaging (FCM) Connector Documentation

The Firebase Cloud Messaging Connector provides comprehensive push notification capabilities for mobile and web applications through Firebase Cloud Messaging, including device targeting, topic messaging, batch operations, and rich notification features.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Schema Specifications](#schema-specifications)
4. [Connection Parameters](#connection-parameters)
5. [Message Properties](#message-properties)
6. [Usage Examples](#usage-examples)
7. [Device Token Messaging](#device-token-messaging)
8. [Topic Messaging](#topic-messaging)
9. [Batch Operations](#batch-operations)
10. [Platform-Specific Features](#platform-specific-features)
11. [Error Handling](#error-handling)
12. [Best Practices](#best-practices)

## Overview

The `FirebasePushConnector` implements push notifications using Firebase Cloud Messaging (FCM). It supports:

- **Device Token Messaging**: Send to specific device tokens
- **Topic Messaging**: Broadcast to topic subscribers
- **Batch Operations**: Efficient multicast delivery to thousands of devices
- **Rich Notifications**: Images, actions, custom data, and platform-specific features
- **Platform Support**: Android, iOS (APNS), and Web Push
- **Health Monitoring**: Connection testing and service health checks

### Capabilities

| Capability | Supported | Description |
|------------|-----------|-------------|
| SendMessages | ? | Send push notifications to devices and topics |
| BulkMessaging | ? | Efficient batch and multicast operations |
| HealthCheck | ? | Monitor Firebase service health |
| ReceiveMessages | ? | Push notifications are send-only |
| MessageStatusQuery | ? | FCM doesn't provide real-time status queries |
| Templates | ? | Rich notification templates with custom data |

## Installation

```bash
dotnet add package Deveel.Messaging.Connector.Firebase
```

## Schema Specifications

### Base Schema: FirebasePush

```csharp
var schema = FirebaseChannelSchemas.FirebasePush;
// Provider: "Firebase"
// Type: "Push" 
// Version: "1.0.0"
// Capabilities: SendMessages | BulkMessaging | HealthCheck
```

### Available Schema Variants

| Schema | Description | Use Case |
|--------|-------------|----------|
| `FirebasePush` | Full-featured FCM with all capabilities | Complete push notification system |
| `SimplePush` | Basic notifications without advanced features | Simple app notifications |
| `BulkPush` | Optimized for high-volume campaigns | Marketing campaigns |
| `RichPush` | Interactive notifications with media | Engaging user experiences |

### Schema Comparison

```csharp
// Full featured schema
var fullSchema = FirebaseChannelSchemas.FirebasePush;

// Simple notifications schema  
var simpleSchema = FirebaseChannelSchemas.SimplePush;
// Removes: BulkMessaging capability
// Removes: Advanced notification properties

// Bulk messaging schema
var bulkSchema = FirebaseChannelSchemas.BulkPush;
// Adds: ConditionExpression, BatchId properties
// Optimized for high-volume delivery

// Rich interactive schema
var richSchema = FirebaseChannelSchemas.RichPush;
// Adds: Actions, Category, Subtitle properties
// Removes: BulkMessaging capability
```

## Connection Parameters

### Required Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `ProjectId` | String | Firebase project ID from Console | `"my-firebase-project"` |
| `ServiceAccountKey` | String | Service account JSON key (sensitive) | `"{\"type\":\"service_account\",...}"` |

### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `DryRun` | Boolean | false | Enable dry run mode for testing |

### Configuration Example

```csharp
var connectionSettings = new ConnectionSettings()
    .AddParameter("ProjectId", "my-firebase-project")
    .AddParameter("ServiceAccountKey", serviceAccountJson)
    .AddParameter("DryRun", false);
```

### Service Account Setup

1. **Go to Firebase Console**: https://console.firebase.google.com/
2. **Select your project**
3. **Navigate to**: Project Settings ? Service Accounts
4. **Click**: "Generate new private key"
5. **Download the JSON file** and use its contents as `ServiceAccountKey`

```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "key-id",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "firebase-adminsdk-xxx@your-project.iam.gserviceaccount.com",
  "client_id": "client-id",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token"
}
```

## Message Properties

### Basic Notification Properties

| Property | Type | Required | Description | Max Length |
|----------|------|----------|-------------|------------|
| `Title` | String | No | Notification title | 1024 chars |
| `ImageUrl` | String | No | Image URL (HTTP/HTTPS) | - |
| `Sound` | String | No | Notification sound | - |
| `ClickAction` | String | No | Action when notification is clicked | - |

### Android-Specific Properties

| Property | Type | Required | Description | Values |
|----------|------|----------|-------------|--------|
| `Color` | String | No | Notification color | `#rrggbb` or `#aarrggbb` |
| `Tag` | String | No | Notification tag for grouping | - |
| `Priority` | String | No | Message priority | `normal`, `high` |
| `TimeToLive` | Integer | No | TTL in seconds | 0-2,419,200 (4 weeks) |
| `CollapseKey` | String | No | Collapse key for grouping | - |
| `RestrictedPackageName` | String | No | Restrict to specific Android app | - |

### iOS-Specific Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Badge` | Integer | No | Badge count |
| `MutableContent` | Boolean | No | Enable notification service extensions |
| `ContentAvailable` | Boolean | No | Enable background app refresh |
| `ThreadId` | String | No | Thread ID for grouping |

### Advanced Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `CustomData` | String | No | JSON string with custom data |
| `Actions` | String | No | JSON array of notification actions |
| `Category` | String | No | iOS notification category |
| `Subtitle` | String | No | iOS notification subtitle |

## Usage Examples

### Basic Device Token Notification

```csharp
using Deveel.Messaging;

// Create connector
var schema = FirebaseChannelSchemas.FirebasePush;
var connectionSettings = new ConnectionSettings()
    .AddParameter("ProjectId", "my-firebase-project")
    .AddParameter("ServiceAccountKey", serviceAccountJson);

var connector = new FirebasePushConnector(schema, connectionSettings);
await connector.InitializeAsync(cancellationToken);

// Create and send notification
var notification = new MessageBuilder()
    .WithId("push-001")
    .WithDeviceReceiver("device-token-123") // FCM device token
    .WithTextContent("You have a new message!")
    .WithProperty("Title", "New Message")
    .WithProperty("ImageUrl", "https://example.com/notification-icon.png")
    .WithProperty("ClickAction", "OPEN_MESSAGE")
    .Message;

var result = await connector.SendMessageAsync(notification, cancellationToken);
if (result.IsSuccess)
{
    Console.WriteLine($"Push notification sent with ID: {result.Value?.MessageId}");
}
```

### Topic Notification

```csharp
var topicNotification = new MessageBuilder()
    .WithId("topic-001")
    .WithTopicReceiver("news-updates") // FCM topic
    .WithTextContent("Breaking news: Major announcement!")
    .WithProperty("Title", "Breaking News")
    .WithProperty("Priority", "high")
    .WithProperty("TimeToLive", 3600) // 1 hour
    .Message;

var result = await connector.SendMessageAsync(topicNotification, cancellationToken);
```

### Rich Notification with Image and Actions

```csharp
var richNotification = new MessageBuilder()
    .WithId("rich-001")
    .WithDeviceReceiver("device-token-456")
    .WithTextContent("Your order #12345 has been shipped and is on its way!")
    .WithProperty("Title", "Order Shipped")
    .WithProperty("ImageUrl", "https://example.com/shipping-truck.jpg")
    .WithProperty("Actions", JsonSerializer.Serialize(new[]
    {
        new { action = "track", title = "Track Package" },
        new { action = "contact", title = "Contact Support" }
    }))
    .WithProperty("CustomData", JsonSerializer.Serialize(new
    {
        orderId = "12345",
        trackingNumber = "1Z999AA1234567890",
        expectedDelivery = "2024-03-15"
    }))
    .Message;

var result = await connector.SendMessageAsync(richNotification, cancellationToken);
```

### Platform-Specific Notification

```csharp
var platformSpecific = new MessageBuilder()
    .WithId("platform-001")
    .WithDeviceReceiver("device-token-789")
    .WithTextContent("You have 3 unread messages")
    .WithProperty("Title", "Unread Messages")
    // Android-specific
    .WithProperty("Color", "#FF0000")
    .WithProperty("Sound", "notification_sound")
    .WithProperty("Tag", "messages")
    // iOS-specific
    .WithProperty("Badge", 3)
    .WithProperty("ThreadId", "messages")
    .WithProperty("MutableContent", true)
    .Message;

var result = await connector.SendMessageAsync(platformSpecific, cancellationToken);
```

## Device Token Messaging

### Single Device Notification

```csharp
public async Task SendToDevice(string deviceToken, string title, string message)
{
    var notification = new MessageBuilder()
        .WithId($"device-{Guid.NewGuid()}")
        .WithDeviceReceiver(deviceToken)
        .WithTextContent(message)
        .WithProperty("Title", title)
        .Message;

    var result = await connector.SendMessageAsync(notification, CancellationToken.None);
    
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Failed to send to device: {result.ErrorMessage}");
    }
}
```

### Device Group Messaging

```csharp
public async Task SendToDeviceGroup(List<string> deviceTokens, string title, string message)
{
    var messages = deviceTokens.Select(token => 
        new MessageBuilder()
            .WithId($"group-{Guid.NewGuid()}")
            .WithDeviceReceiver(token)
            .WithTextContent(message)
            .WithProperty("Title", title)
            .Message
    ).ToList();

    var batch = new MessageBatch(messages);
    var result = await connector.SendBatchAsync(batch, CancellationToken.None);
    
    Console.WriteLine($"Sent {result.Value.Results.Count} notifications to device group");
}
```

## Topic Messaging

### Basic Topic Notification

```csharp
public async Task SendToTopic(string topic, string title, string message)
{
    var notification = new MessageBuilder()
        .WithId($"topic-{Guid.NewGuid()}")
        .WithTopicReceiver(topic)
        .WithTextContent(message)
        .WithProperty("Title", title)
        .Message;

    var result = await connector.SendMessageAsync(notification, CancellationToken.None);
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"Sent notification to topic '{topic}'");
    }
}
```

### Conditional Topic Messaging

```csharp
// Send to users subscribed to both 'sports' and 'news' topics
var conditionalMessage = new MessageBuilder()
    .WithId("conditional-001")
    .WithTopicReceiver("'sports' in topics && 'news' in topics")
    .WithTextContent("Sports news update!")
    .WithProperty("Title", "Sports News")
    .WithProperty("ConditionExpression", "'sports' in topics && 'news' in topics")
    .Message;

var result = await connector.SendMessageAsync(conditionalMessage, CancellationToken.None);
```

## Batch Operations

### Multicast to Multiple Devices

```csharp
public async Task SendMulticast(List<string> deviceTokens, string title, string message)
{
    // Create messages for all devices
    var messages = deviceTokens.Select(token => 
        new MessageBuilder()
            .WithId($"multicast-{Guid.NewGuid()}")
            .WithDeviceReceiver(token)
            .WithTextContent(message)
            .WithProperty("Title", title)
            .Message
    ).ToList();

    // Send as batch - Firebase will optimize as multicast
    var batch = new MessageBatch(messages);
    var result = await connector.SendBatchAsync(batch, CancellationToken.None);
    
    if (result.IsSuccess)
    {
        var successCount = result.Value.Results.Values.Count(r => r.IsSuccess);
        var totalCount = result.Value.Results.Count;
        
        Console.WriteLine($"Multicast result: {successCount}/{totalCount} successful");
        
        // Handle individual failures
        foreach (var (messageId, sendResult) in result.Value.Results)
        {
            if (!sendResult.IsSuccess)
            {
                Console.WriteLine($"Failed to send {messageId}: {sendResult.ErrorMessage}");
            }
        }
    }
}
```

### High-Volume Campaign

```csharp
public async Task SendCampaign(List<string> deviceTokens, string campaignTitle, string campaignMessage)
{
    const int batchSize = 500; // Firebase multicast limit
    var batches = deviceTokens.Chunk(batchSize);
    
    foreach (var batch in batches)
    {
        var messages = batch.Select(token => 
            new MessageBuilder()
                .WithId($"campaign-{Guid.NewGuid()}")
                .WithDeviceReceiver(token)
                .WithTextContent(campaignMessage)
                .WithProperty("Title", campaignTitle)
                .WithProperty("Priority", "normal") // Don't overwhelm users
                .WithProperty("CollapseKey", "campaign") // Group similar messages
                .Message
        ).ToList();

        var messageBatch = new MessageBatch(messages);
        var result = await connector.SendBatchAsync(messageBatch, CancellationToken.None);
        
        // Add delay between batches to respect rate limits
        await Task.Delay(100);
    }
}
```

## Platform-Specific Features

### Android Notifications

```csharp
public async Task SendAndroidNotification(string deviceToken)
{
    var androidNotification = new MessageBuilder()
        .WithId("android-001")
        .WithDeviceReceiver(deviceToken)
        .WithTextContent("Android-specific notification")
        .WithProperty("Title", "Android App")
        .WithProperty("Color", "#FF5722") // Material Design color
        .WithProperty("Sound", "notification_sound")
        .WithProperty("Priority", "high")
        .WithProperty("Tag", "android_notifications")
        .WithProperty("ClickAction", "OPEN_ACTIVITY")
        .WithProperty("CollapseKey", "android_updates")
        .Message;

    var result = await connector.SendMessageAsync(androidNotification, CancellationToken.None);
}
```

### iOS (APNS) Notifications

```csharp
public async Task SendIOSNotification(string deviceToken)
{
    var iosNotification = new MessageBuilder()
        .WithId("ios-001")
        .WithDeviceReceiver(deviceToken)
        .WithTextContent("You have new messages")
        .WithProperty("Title", "Messages")
        .WithProperty("Subtitle", "From your friends")
        .WithProperty("Sound", "default")
        .WithProperty("Badge", 5)
        .WithProperty("MutableContent", true) // Enable notification extensions
        .WithProperty("ContentAvailable", true) // Enable background refresh
        .WithProperty("ThreadId", "messages") // Group notifications
        .WithProperty("Category", "MESSAGE_CATEGORY") // For action buttons
        .Message;

    var result = await connector.SendMessageAsync(iosNotification, CancellationToken.None);
}
```

### Web Push Notifications

```csharp
public async Task SendWebPushNotification(string deviceToken)
{
    var webNotification = new MessageBuilder()
        .WithId("web-001")
        .WithDeviceReceiver(deviceToken)
        .WithTextContent("You have a new notification in your browser")
        .WithProperty("Title", "Web App Notification")
        .WithProperty("ImageUrl", "https://example.com/web-icon.png")
        .WithProperty("ClickAction", "https://example.com/app")
        .WithProperty("CustomData", JsonSerializer.Serialize(new
        {
            url = "https://example.com/details",
            action = "open_app"
        }))
        .Message;

    var result = await connector.SendMessageAsync(webNotification, CancellationToken.None);
}
```

## Error Handling

### Common Error Scenarios

| Error | Description | Solution |
|-------|-------------|----------|
| `INVALID_TOKEN` | Device token is invalid | Remove token from database |
| `UNREGISTERED` | App uninstalled from device | Remove token from database |
| `SENDER_ID_MISMATCH` | Token belongs to different project | Verify project configuration |
| `MESSAGE_TOO_BIG` | Payload exceeds 4KB limit | Reduce message size |
| `AUTHENTICATION_ERROR` | Invalid service account key | Verify service account configuration |

### Error Handling Example

```csharp
public async Task<bool> SendNotificationSafely(string deviceToken, string title, string message)
{
    try
    {
        var notification = new MessageBuilder()
            .WithId($"safe-{Guid.NewGuid()}")
            .WithDeviceReceiver(deviceToken)
            .WithTextContent(message)
            .WithProperty("Title", title)
            .Message;

        var result = await connector.SendMessageAsync(notification, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            return true;
        }

        // Handle specific errors
        switch (result.ErrorMessage?.ToUpperInvariant())
        {
            case var error when error?.Contains("INVALID_TOKEN") == true:
            case var error when error?.Contains("UNREGISTERED") == true:
                // Remove invalid token from database
                await RemoveDeviceToken(deviceToken);
                Console.WriteLine($"Removed invalid device token: {deviceToken}");
                break;
                
            case var error when error?.Contains("MESSAGE_TOO_BIG") == true:
                // Try sending with shorter message
                await SendNotificationSafely(deviceToken, title, TruncateMessage(message));
                break;
                
            default:
                Console.WriteLine($"Send failed: {result.ErrorMessage}");
                break;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception sending notification: {ex.Message}");
        return false;
    }
}

private string TruncateMessage(string message, int maxLength = 100)
{
    if (message.Length <= maxLength) return message;
    return message.Substring(0, maxLength - 3) + "...";
}
```

### Batch Error Handling

```csharp
public async Task HandleBatchErrors(MessageBatch batch)
{
    var result = await connector.SendBatchAsync(batch, CancellationToken.None);
    
    if (result.IsSuccess)
    {
        var invalidTokens = new List<string>();
        var retriableErrors = new List<string>();
        
        foreach (var (messageId, sendResult) in result.Value.Results)
        {
            if (!sendResult.IsSuccess)
            {
                var errorMessage = sendResult.ErrorMessage?.ToUpperInvariant() ?? "";
                
                if (errorMessage.Contains("INVALID_TOKEN") || errorMessage.Contains("UNREGISTERED"))
                {
                    // Extract device token from additional data
                    if (sendResult.AdditionalData.TryGetValue("Token", out var token))
                    {
                        invalidTokens.Add(token.ToString());
                    }
                }
                else if (errorMessage.Contains("INTERNAL") || errorMessage.Contains("UNAVAILABLE"))
                {
                    retriableErrors.Add(messageId);
                }
            }
        }
        
        // Clean up invalid tokens
        if (invalidTokens.Any())
        {
            await RemoveDeviceTokens(invalidTokens);
            Console.WriteLine($"Removed {invalidTokens.Count} invalid tokens");
        }
        
        // Retry transient errors
        if (retriableErrors.Any())
        {
            Console.WriteLine($"Will retry {retriableErrors.Count} failed messages");
            // Implement retry logic
        }
    }
}
```

## Best Practices

### 1. Token Management

```csharp
// ? Good - Maintain clean token database
public async Task CleanupInvalidTokens()
{
    // Regularly clean up invalid tokens reported by FCM
    var invalidTokens = await GetInvalidTokensFromLogs();
    await RemoveDeviceTokens(invalidTokens);
}

// ? Good - Handle token refresh
public async Task HandleTokenRefresh(string oldToken, string newToken)
{
    await UpdateDeviceToken(oldToken, newToken);
    Console.WriteLine($"Updated device token: {oldToken} -> {newToken}");
}
```

### 2. Message Optimization

```csharp
// ? Good - Optimize message size
public IMessage CreateOptimizedNotification(string deviceToken, string title, string message)
{
    // Keep total payload under 4KB
    var truncatedTitle = title.Length > 100 ? title.Substring(0, 97) + "..." : title;
    var truncatedMessage = message.Length > 200 ? message.Substring(0, 197) + "..." : message;
    
    return new MessageBuilder()
        .WithDeviceReceiver(deviceToken)
        .WithTextContent(truncatedMessage)
        .WithProperty("Title", truncatedTitle)
        .Message;
}

// ? Good - Use data-only messages for large payloads
public IMessage CreateDataOnlyNotification(string deviceToken, object data)
{
    return new MessageBuilder()
        .WithDeviceReceiver(deviceToken)
        .WithTextContent("") // No notification, app handles data
        .WithProperty("CustomData", JsonSerializer.Serialize(data))
        .Message;
}
```

### 3. Topic Strategy

```csharp
// ? Good - Hierarchical topic structure
public async Task SubscribeToTopics(string deviceToken, UserPreferences preferences)
{
    var topics = new List<string>();
    
    // Geographic topics
    topics.Add($"region_{preferences.Region}");
    topics.Add($"city_{preferences.City}");
    
    // Interest topics
    if (preferences.Interests.Contains("sports"))
        topics.Add("sports");
    if (preferences.Interests.Contains("news"))
        topics.Add("news");
    
    // User type topics
    topics.Add($"user_type_{preferences.UserType}");
    
    // Subscribe to relevant topics
    foreach (var topic in topics)
    {
        await SubscribeToTopic(deviceToken, topic);
    }
}

// ? Good - Use conditional messaging for complex targeting
public async Task SendTargetedCampaign(string campaignMessage)
{
    var condition = "'sports' in topics && ('premium' in topics || 'vip' in topics)";
    
    var message = new MessageBuilder()
        .WithTopicReceiver(condition)
        .WithTextContent(campaignMessage)
        .WithProperty("Title", "Exclusive Sports Update")
        .WithProperty("ConditionExpression", condition)
        .Message;
        
    await connector.SendMessageAsync(message, CancellationToken.None);
}
```

### 4. Batch Optimization

```csharp
// ? Good - Optimize batch sizes
public async Task SendOptimizedBatch(List<string> deviceTokens, string title, string message)
{
    const int optimalBatchSize = 500; // Firebase multicast limit
    
    var batches = deviceTokens
        .Chunk(optimalBatchSize)
        .Select(batch => new MessageBatch(
            batch.Select(token => 
                new MessageBuilder()
                    .WithDeviceReceiver(token)
                    .WithTextContent(message)
                    .WithProperty("Title", title)
                    .Message
            ).ToList()
        ));
    
    foreach (var batch in batches)
    {
        await connector.SendBatchAsync(batch, CancellationToken.None);
        await Task.Delay(50); // Rate limiting
    }
}
```

### 5. Testing and Monitoring

```csharp
// ? Good - Use dry run for testing
public async Task TestNotificationCampaign(List<string> testTokens, string title, string message)
{
    // Use dry run mode
    var testSettings = new ConnectionSettings()
        .AddParameter("ProjectId", "my-firebase-project")
        .AddParameter("ServiceAccountKey", serviceAccountJson)
        .AddParameter("DryRun", true); // Enable dry run
    
    var testConnector = new FirebasePushConnector(schema, testSettings);
    await testConnector.InitializeAsync(CancellationToken.None);
    
    // Test the campaign
    var testMessage = new MessageBuilder()
        .WithDeviceReceiver(testTokens.First())
        .WithTextContent(message)
        .WithProperty("Title", title)
        .Message;
    
    var result = await testConnector.SendMessageAsync(testMessage, CancellationToken.None);
    
    if (result.IsSuccess)
    {
        Console.WriteLine("? Campaign test successful - ready for production");
    }
    else
    {
        Console.WriteLine($"? Campaign test failed: {result.ErrorMessage}");
    }
}

// ? Good - Monitor notification delivery
public async Task MonitorNotificationMetrics()
{
    var health = await connector.GetConnectorHealthAsync(CancellationToken.None);
    
    if (health.IsSuccess && health.Value.IsHealthy)
    {
        Console.WriteLine("? Firebase connector healthy");
    }
    else
    {
        Console.WriteLine("? Firebase connector issues detected");
        foreach (var issue in health.Value.Issues)
        {
            Console.WriteLine($"  - {issue}");
        }
    }
}
```

### 6. Security Best Practices

```csharp
// ? Good - Secure service account key handling
public static ConnectionSettings CreateSecureConnection()
{
    // Load from secure configuration, not hardcoded
    var serviceAccountJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_KEY");
    var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
    
    if (string.IsNullOrEmpty(serviceAccountJson) || string.IsNullOrEmpty(projectId))
    {
        throw new InvalidOperationException("Firebase credentials not configured");
    }
    
    return new ConnectionSettings()
        .AddParameter("ProjectId", projectId)
        .AddParameter("ServiceAccountKey", serviceAccountJson);
}

// ? Good - Validate user permissions before sending
public async Task<bool> CanSendToUser(string userId, string notificationType)
{
    var userPreferences = await GetUserNotificationPreferences(userId);
    
    // Check if user has opted out of this notification type
    if (userPreferences.OptedOut.Contains(notificationType))
    {
        return false;
    }
    
    // Check if within quiet hours
    if (IsInQuietHours(userPreferences.QuietHours))
    {
        return false;
    }
    
    return true;
}
```

## Related Documentation

- [Firebase Console Setup Guide](https://console.firebase.google.com/)
- [FCM HTTP v1 API Documentation](https://firebase.google.com/docs/cloud-messaging/http-server-ref)
- [Channel Schema Usage Guide](../ChannelSchema-Usage.md)
- [Connector Implementation Guide](../ChannelConnector-Usage.md)