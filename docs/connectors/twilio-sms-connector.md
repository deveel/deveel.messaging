# Twilio SMS Connector Documentation

The Twilio SMS Connector provides comprehensive SMS messaging capabilities through the Twilio API, including two-way messaging, delivery status tracking, and webhook support.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Schema Specifications](#schema-specifications)
4. [Connection Parameters](#connection-parameters)
5. [Message Properties](#message-properties)
6. [Usage Examples](#usage-examples)
7. [Webhook Integration](#webhook-integration)
8. [Error Handling](#error-handling)
9. [Best Practices](#best-practices)

## Overview

The `TwilioSmsConnector` implements SMS messaging using the Twilio Programmable Messaging API. It supports:

- **Send SMS Messages**: Text and media messages
- **Receive SMS Messages**: Webhook-based message receiving
- **Status Tracking**: Real-time delivery status updates
- **Bulk Messaging**: Efficient batch SMS operations
- **Health Monitoring**: Connection testing and diagnostics

### Capabilities

| Capability | Supported | Description |
|------------|-----------|-------------|
| SendMessages | ? | Send SMS messages to phone numbers |
| ReceiveMessages | ? | Receive SMS via webhooks |
| MessageStatusQuery | ? | Query delivery status |
| HandleMessageState | ? | Process status callbacks |
| BulkMessaging | ? | Send multiple messages efficiently |
| HealthCheck | ? | Monitor connector health |

## Installation

```bash
dotnet add package Deveel.Messaging.Connector.Twilio
```

## Schema Specifications

### Base Schema: TwilioSms

```csharp
var schema = TwilioChannelSchemas.TwilioSms;
// Provider: "Twilio"
// Type: "SMS" 
// Version: "1.0.0"
// Capabilities: SendMessages | ReceiveMessages | MessageStatusQuery | HandleMessageState | BulkMessaging | HealthCheck
```

### Available Schema Variants

| Schema | Description | Use Case |
|--------|-------------|----------|
| `TwilioSms` | Full-featured SMS with all capabilities | Complete SMS integration |
| `SimpleSms` | Basic send-only SMS | Simple notifications |
| `NotificationSms` | Send with status tracking | Automated alerts |
| `BulkSms` | Optimized for high-volume campaigns | Marketing campaigns |

### Schema Comparison

```csharp
// Full featured schema
var fullSchema = TwilioChannelSchemas.TwilioSms;

// Simple send-only schema  
var simpleSchema = TwilioChannelSchemas.SimpleSms;
// Removes: ReceiveMessages, HandleMessageState, BulkMessaging capabilities
// Removes: WebhookUrl, StatusCallback, MessagingServiceSid parameters

// Notification schema with status tracking
var notificationSchema = TwilioChannelSchemas.NotificationSms;
// Removes: ReceiveMessages, HandleMessageState capabilities
// Keeps: MessageStatusQuery for delivery confirmation

// Bulk messaging schema
var bulkSchema = TwilioChannelSchemas.BulkSms;
// Requires: MessagingServiceSid parameter
// Optimized for high-volume sending
```

## Connection Parameters

### Required Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `AccountSid` | String | Twilio Account SID from Console Dashboard | `"AC1234567890abcdef1234567890abcdef"` |
| `AuthToken` | String | Twilio Auth Token (sensitive) | `"your_auth_token_here"` |

### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `WebhookUrl` | String | null | URL for receiving message and status webhooks |
| `StatusCallback` | String | null | URL for delivery status callbacks |
| `ValidityPeriod` | Integer | 14400 | Message validity in seconds (4 hours) |
| `MaxPrice` | Number | null | Maximum price willing to pay for message |
| `MessagingServiceSid` | String | null | Messaging Service SID (required for bulk) |

### Configuration Example

```csharp
var connectionSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "AC1234567890abcdef1234567890abcdef")
    .AddParameter("AuthToken", "your_auth_token_here")
    .AddParameter("WebhookUrl", "https://yourapp.com/webhooks/twilio/sms")
    .AddParameter("StatusCallback", "https://yourapp.com/webhooks/twilio/status")
    .AddParameter("ValidityPeriod", 3600); // 1 hour
```

## Message Properties

### Basic Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `ValidityPeriod` | Integer | No | Override default message validity period |
| `MaxPrice` | Number | No | Override default maximum price |
| `ProvideCallback` | Boolean | No | Enable delivery status callbacks |
| `AttemptLimits` | Integer | No | Maximum delivery attempts |
| `SmartEncoded` | Boolean | No | Enable automatic encoding optimization |
| `PersistentAction` | String | No | RCS-specific action |

### Property Usage Examples

```csharp
var message = new MessageBuilder()
    .WithId("sms-001")
    .WithPhoneSender("+1234567890")
    .WithPhoneReceiver("+0987654321")
    .WithTextContent("Your verification code is: 123456")
    .WithProperty("ValidityPeriod", 900) // 15 minutes
    .WithProperty("MaxPrice", 0.05) // 5 cents maximum
    .WithProperty("ProvideCallback", true) // Enable status callbacks
    .WithProperty("SmartEncoded", true) // Optimize encoding
    .Message;
```

## Usage Examples

### Basic SMS Sending

```csharp
using Deveel.Messaging;

// Create connector with simple schema
var schema = TwilioChannelSchemas.SimpleSms;
var connectionSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "your_account_sid")
    .AddParameter("AuthToken", "your_auth_token");

var connector = new TwilioSmsConnector(schema, connectionSettings);
await connector.InitializeAsync(cancellationToken);

// Create and send message
var message = new MessageBuilder()
    .WithId("welcome-sms")
    .WithPhoneSender("+1234567890") // Your Twilio number
    .WithPhoneReceiver("+0987654321") // Recipient number
    .WithTextContent("Welcome to our service!")
    .Message;

var result = await connector.SendMessageAsync(message, cancellationToken);
if (result.IsSuccess)
{
    Console.WriteLine($"Message sent with ID: {result.Value?.MessageId}");
}
```

### SMS with Media Attachment

```csharp
var mediaMessage = new MessageBuilder()
    .WithId("media-sms")
    .WithPhoneSender("+1234567890")
    .WithPhoneReceiver("+0987654321")
    .WithMediaContent("https://example.com/image.jpg", "image/jpeg", "Welcome Image")
    .WithTextContent("Check out this image!")
    .Message;

var result = await connector.SendMessageAsync(mediaMessage, cancellationToken);
```

### Bulk SMS Campaign

```csharp
// Use bulk schema with messaging service
var bulkSchema = TwilioChannelSchemas.BulkSms;
var bulkSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "your_account_sid")
    .AddParameter("AuthToken", "your_auth_token")
    .AddParameter("MessagingServiceSid", "your_messaging_service_sid");

var bulkConnector = new TwilioSmsConnector(bulkSchema, bulkSettings);
await bulkConnector.InitializeAsync(cancellationToken);

// Create batch of messages
var messages = new List<IMessage>();
var phoneNumbers = new[] { "+1111111111", "+2222222222", "+3333333333" };

foreach (var phone in phoneNumbers)
{
    var message = new MessageBuilder()
        .WithId($"bulk-{Guid.NewGuid()}")
        .WithPhoneReceiver(phone)
        .WithTextContent("Important announcement: Our service will be under maintenance tonight.")
        .Message;
    
    messages.Add(message);
}

// Send as batch
var batch = new MessageBatch(messages);
var batchResult = await bulkConnector.SendBatchAsync(batch, cancellationToken);

Console.WriteLine($"Sent {batchResult.Value.Results.Count} messages");
```

### Message Status Tracking

```csharp
// Send message with status tracking enabled
var trackedMessage = new MessageBuilder()
    .WithId("tracked-sms")
    .WithPhoneSender("+1234567890")
    .WithPhoneReceiver("+0987654321")
    .WithTextContent("Your order has been shipped!")
    .WithProperty("ProvideCallback", true)
    .Message;

var sendResult = await connector.SendMessageAsync(trackedMessage, cancellationToken);

if (sendResult.IsSuccess)
{
    // Query status later
    var statusResult = await connector.GetMessageStatusAsync(sendResult.Value.MessageId, cancellationToken);
    
    if (statusResult.IsSuccess)
    {
        foreach (var status in statusResult.Value.StatusUpdates)
        {
            Console.WriteLine($"Message {status.MessageId}: {status.Status} at {status.Timestamp}");
        }
    }
}
```

## Webhook Integration

### Receiving SMS Messages

```csharp
[ApiController]
[Route("api/webhooks/twilio")]
public class TwilioWebhookController : ControllerBase
{
    private readonly TwilioSmsConnector _connector;

    [HttpPost("sms")]
    public async Task<IActionResult> ReceiveSms([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            foreach (var message in result.Value.Messages)
            {
                // Process incoming SMS
                await ProcessIncomingSms(message);
            }
        }
        
        return Ok();
    }

    [HttpPost("status")]
    public async Task<IActionResult> ReceiveStatus([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _connector.ReceiveMessageStatusAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            // Process status update
            await ProcessStatusUpdate(result.Value);
        }
        
        return Ok();
    }

    private async Task ProcessIncomingSms(IMessage message)
    {
        Console.WriteLine($"Received SMS from {message.Sender?.Address}: {message.Content?.Value}");
        
        // Auto-reply example
        var reply = new MessageBuilder()
            .WithId($"reply-{Guid.NewGuid()}")
            .WithPhoneSender(message.Receiver?.Address) // Swap sender/receiver
            .WithPhoneReceiver(message.Sender?.Address)
            .WithTextContent("Thank you for your message. We'll get back to you soon!")
            .Message;
            
        await _connector.SendMessageAsync(reply, CancellationToken.None);
    }

    private async Task ProcessStatusUpdate(StatusUpdateResult statusUpdate)
    {
        Console.WriteLine($"Message {statusUpdate.MessageId} status: {statusUpdate.Status}");
        
        // Update database, send notifications, etc.
        await UpdateMessageStatus(statusUpdate.MessageId, statusUpdate.Status);
    }
}
```

### Webhook Configuration in Twilio Console

1. **SMS Webhook URL**: `https://yourapp.com/api/webhooks/twilio/sms`
2. **Status Callback URL**: `https://yourapp.com/api/webhooks/twilio/status`
3. **HTTP Method**: POST
4. **Content Type**: application/x-www-form-urlencoded

### Webhook Payload Examples

**Incoming SMS Webhook:**
```
MessageSid=SM1234567890abcdef
From=%2B15551234567
To=%2B15559876543
Body=Hello+from+customer
AccountSid=AC1234567890abcdef
```

**Status Callback Webhook:**
```
MessageSid=SM1234567890abcdef
MessageStatus=delivered
To=%2B15559876543
From=%2B15551234567
AccountSid=AC1234567890abcdef
```

## Error Handling

### Common Error Codes

| Error Code | Description | Solution |
|------------|-------------|----------|
| `MISSING_CREDENTIALS` | AccountSid or AuthToken missing | Verify connection parameters |
| `INVALID_RECIPIENT` | Invalid phone number format | Use E.164 format (+1234567890) |
| `SEND_MESSAGE_FAILED` | Message sending failed | Check Twilio logs and account status |
| `STATUS_QUERY_FAILED` | Cannot retrieve message status | Verify MessageSid exists |

### Error Handling Example

```csharp
var result = await connector.SendMessageAsync(message, cancellationToken);

if (!result.IsSuccess)
{
    switch (result.ErrorCode)
    {
        case TwilioErrorCodes.MissingCredentials:
            Console.WriteLine("Please check your Twilio credentials");
            break;
            
        case TwilioErrorCodes.InvalidRecipient:
            Console.WriteLine($"Invalid phone number format: {message.Receiver?.Address}");
            break;
            
        case TwilioErrorCodes.SendMessageFailed:
            Console.WriteLine($"Send failed: {result.ErrorMessage}");
            // Log to monitoring system, retry logic, etc.
            break;
            
        default:
            Console.WriteLine($"Unexpected error: {result.ErrorMessage}");
            break;
    }
}
```

### Retry Logic

```csharp
public async Task<ConnectorResult<SendResult>> SendWithRetryAsync(IMessage message, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        var result = await connector.SendMessageAsync(message, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            return result;
        }
        
        // Only retry on transient errors
        if (IsRetriableError(result.ErrorCode) && attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
            await Task.Delay(delay);
            continue;
        }
        
        return result; // Return the last failed result
    }
    
    return ConnectorResult<SendResult>.Fail("MAX_RETRIES_EXCEEDED", "Maximum retry attempts reached");
}

private bool IsRetriableError(string? errorCode)
{
    return errorCode switch
    {
        "NETWORK_ERROR" => true,
        "RATE_LIMITED" => true,
        "TEMPORARY_FAILURE" => true,
        _ => false
    };
}
```

## Best Practices

### 1. Phone Number Formatting

```csharp
// ? Good - Use E.164 format
.WithPhoneSender("+1234567890")
.WithPhoneReceiver("+441234567890")

// ? Avoid - Local formats
.WithPhoneSender("(123) 456-7890")
.WithPhoneReceiver("123-456-7890")
```

### 2. Message Content Optimization

```csharp
// ? Good - Concise, clear messaging
var message = new MessageBuilder()
    .WithTextContent("Your verification code: 123456. Expires in 10 minutes.")
    .WithProperty("ValidityPeriod", 600) // 10 minutes
    .Message;

// ? Good - Smart encoding for international characters
var internationalMessage = new MessageBuilder()
    .WithTextContent("Bonjour! Votre code: 123456 !")
    .WithProperty("SmartEncoded", true) // Let Twilio optimize encoding
    .Message;
```

### 3. Bulk Messaging Optimization

```csharp
// ? Good - Use Messaging Service for bulk
var bulkSettings = new ConnectionSettings()
    .AddParameter("MessagingServiceSid", "your_messaging_service_sid"); // Better delivery rates

// ? Good - Batch processing
var batch = new MessageBatch(messages);
await connector.SendBatchAsync(batch, cancellationToken); // More efficient
```

### 4. Webhook Security

```csharp
[HttpPost("sms")]
public async Task<IActionResult> ReceiveSms([FromForm] Dictionary<string, string> formData)
{
    // ? Good - Validate webhook signature
    if (!ValidateTwilioSignature(Request, formData))
    {
        return Unauthorized();
    }
    
    // Process webhook...
}

private bool ValidateTwilioSignature(HttpRequest request, Dictionary<string, string> formData)
{
    // Implement Twilio signature validation
    // See: https://www.twilio.com/docs/usage/webhooks/webhooks-security
    return true; // Placeholder
}
```

### 5. Status Monitoring

```csharp
// ? Good - Enable comprehensive status tracking
var monitoredMessage = new MessageBuilder()
    .WithTextContent("Important notification")
    .WithProperty("ProvideCallback", true) // Enable status callbacks
    .WithProperty("ValidityPeriod", 3600) // 1 hour timeout
    .Message;

// ? Good - Implement status monitoring
public async Task MonitorMessageStatus(string messageId)
{
    var maxWaitTime = TimeSpan.FromMinutes(30);
    var startTime = DateTime.UtcNow;
    
    while (DateTime.UtcNow - startTime < maxWaitTime)
    {
        var statusResult = await connector.GetMessageStatusAsync(messageId, CancellationToken.None);
        
        if (statusResult.IsSuccess)
        {
            var status = statusResult.Value.StatusUpdates.FirstOrDefault()?.Status;
            
            if (status == MessageStatus.Delivered || status == MessageStatus.DeliveryFailed)
            {
                break; // Final status reached
            }
        }
        
        await Task.Delay(TimeSpan.FromSeconds(10)); // Check every 10 seconds
    }
}
```

### 6. Resource Management

```csharp
// ? Good - Proper connector lifecycle management
public class SmsService : IDisposable
{
    private readonly TwilioSmsConnector _connector;
    
    public SmsService()
    {
        _connector = new TwilioSmsConnector(schema, connectionSettings);
    }
    
    public async Task InitializeAsync()
    {
        await _connector.InitializeAsync(CancellationToken.None);
    }
    
    public void Dispose()
    {
        _connector?.Dispose();
    }
}
```

## Related Documentation

- [Twilio WhatsApp Connector](twilio-whatsapp-connector.md)
- [Channel Schema Usage Guide](../ChannelSchema-Usage.md)
- [Connector Implementation Guide](../ChannelConnector-Usage.md)
- [Error Handling Best Practices](../error-handling.md)