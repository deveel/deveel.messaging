# Twilio WhatsApp Business Connector Documentation

The Twilio WhatsApp Business Connector provides comprehensive WhatsApp Business messaging capabilities through the Twilio API, including interactive messaging, media attachments, template messaging, and webhook support.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Schema Specifications](#schema-specifications)
4. [Connection Parameters](#connection-parameters)
5. [Message Properties](#message-properties)
6. [Usage Examples](#usage-examples)
7. [Template Messaging](#template-messaging)
8. [Interactive Messages](#interactive-messages)
9. [Webhook Integration](#webhook-integration)
10. [Error Handling](#error-handling)
11. [Best Practices](#best-practices)

## Overview

The `TwilioWhatsAppConnector` implements WhatsApp Business messaging using the Twilio WhatsApp Business API. It supports:

- **Send WhatsApp Messages**: Text, media, and template messages
- **Receive WhatsApp Messages**: Webhook-based message receiving with business fields
- **Interactive Elements**: Buttons, lists, quick replies, and menu interactions
- **Template Messaging**: WhatsApp Business approved message templates
- **Media Support**: Images, documents, audio, video, and location sharing
- **Status Tracking**: Real-time delivery status including read receipts

### Capabilities

| Capability | Supported | Description |
|------------|-----------|-------------|
| SendMessages | ? | Send WhatsApp messages to phone numbers |
| ReceiveMessages | ? | Receive WhatsApp messages via webhooks |
| MessageStatusQuery | ? | Query delivery status |
| HandleMessageState | ? | Process status callbacks |
| Templates | ? | WhatsApp Business approved templates |
| MediaAttachments | ? | Images, documents, audio, video |
| HealthCheck | ? | Monitor connector health |

## Installation

```bash
dotnet add package Deveel.Messaging.Connector.Twilio
```

## Schema Specifications

### Base Schema: TwilioWhatsApp

```csharp
var schema = TwilioChannelSchemas.TwilioWhatsApp;
// Provider: "Twilio"
// Type: "WhatsApp" 
// Version: "1.0.0"
// Capabilities: SendMessages | ReceiveMessages | MessageStatusQuery | HandleMessageState | Templates | MediaAttachments | HealthCheck
```

### Available Schema Variants

| Schema | Description | Use Case |
|--------|-------------|----------|
| `TwilioWhatsApp` | Full-featured WhatsApp with all capabilities | Complete WhatsApp Business integration |
| `SimpleWhatsApp` | Basic send-only WhatsApp | Simple notifications |
| `WhatsAppTemplates` | Template-focused messaging | Business notifications |

### Schema Comparison

```csharp
// Full featured schema
var fullSchema = TwilioChannelSchemas.TwilioWhatsApp;

// Simple send-only schema  
var simpleSchema = TwilioChannelSchemas.SimpleWhatsApp;
// Removes: ReceiveMessages, HandleMessageState, Templates capabilities
// Removes: WebhookUrl, StatusCallback parameters
// Removes: Template content type

// Template-focused schema
var templateSchema = TwilioChannelSchemas.WhatsAppTemplates;
// Removes: ReceiveMessages, HandleMessageState, MediaAttachments capabilities
// Removes: Media content type
// Focuses on template messaging only
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
| `WebhookUrl` | String | null | URL for receiving WhatsApp message and status webhooks |
| `StatusCallback` | String | null | URL for delivery status callbacks |

### Configuration Example

```csharp
var connectionSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "AC1234567890abcdef1234567890abcdef")
    .AddParameter("AuthToken", "your_auth_token_here")
    .AddParameter("WebhookUrl", "https://yourapp.com/webhooks/twilio/whatsapp")
    .AddParameter("StatusCallback", "https://yourapp.com/webhooks/twilio/whatsapp/status");
```

## Message Properties

### Basic Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `ProvideCallback` | Boolean | No | Enable delivery status callbacks |
| `PersistentAction` | String | No | RCS-specific action for WhatsApp |

### WhatsApp-Specific Fields (Received in Webhooks)

| Field | Description |
|-------|-------------|
| `ProfileName` | WhatsApp user display name |
| `ButtonText` | Interactive button text |
| `ButtonPayload` | Button action payload |
| `ListId` | List selection identifier |
| `ListTitle` | List selection title |
| `BusinessDisplayName` | Business account display name |
| `ForwardedCount` | Message forwarding count |

## Usage Examples

### Basic WhatsApp Text Message

```csharp
using Deveel.Messaging;

// Create connector with full schema
var schema = TwilioChannelSchemas.TwilioWhatsApp;
var connectionSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "your_account_sid")
    .AddParameter("AuthToken", "your_auth_token");

var connector = new TwilioWhatsAppConnector(schema, connectionSettings);
await connector.InitializeAsync(cancellationToken);

// Create and send message
var message = new MessageBuilder()
    .WithId("whatsapp-001")
    .WithPhoneSender("whatsapp:+1234567890") // Your WhatsApp Business number
    .WithPhoneReceiver("whatsapp:+0987654321") // Recipient number
    .WithTextContent("Hello from WhatsApp Business!")
    .Message;

var result = await connector.SendMessageAsync(message, cancellationToken);
if (result.IsSuccess)
{
    Console.WriteLine($"WhatsApp message sent with ID: {result.Value?.MessageId}");
}
```

### WhatsApp Media Message

```csharp
var mediaMessage = new MessageBuilder()
    .WithId("whatsapp-media")
    .WithPhoneSender("whatsapp:+1234567890")
    .WithPhoneReceiver("whatsapp:+0987654321")
    .WithMediaContent("https://example.com/product-image.jpg", "image/jpeg", "Product Image")
    .WithTextContent("Check out our latest product!")
    .Message;

var result = await connector.SendMessageAsync(mediaMessage, cancellationToken);
```

### WhatsApp Document Message

```csharp
var documentMessage = new MessageBuilder()
    .WithId("whatsapp-document")
    .WithPhoneSender("whatsapp:+1234567890")
    .WithPhoneReceiver("whatsapp:+0987654321")
    .WithMediaContent("https://example.com/invoice.pdf", "application/pdf", "Invoice #12345")
    .WithTextContent("Your invoice is ready for download.")
    .Message;

var result = await connector.SendMessageAsync(documentMessage, cancellationToken);
```

## Template Messaging

WhatsApp Business requires pre-approved templates for certain types of messages.

### Template Message Example

```csharp
// Template message using approved WhatsApp Business template
var templateMessage = new MessageBuilder()
    .WithId("whatsapp-template")
    .WithPhoneSender("whatsapp:+1234567890")
    .WithPhoneReceiver("whatsapp:+0987654321")
    .WithTemplateContent("welcome_template", new
    {
        customer_name = "John Doe",
        company_name = "Acme Corp",
        appointment_date = "March 15, 2024"
    })
    .Message;

var result = await connector.SendMessageAsync(templateMessage, cancellationToken);
```

### Template Content Structure

```csharp
// Template with parameters
var templateParams = new Dictionary<string, object>
{
    ["1"] = "John", // First parameter
    ["2"] = "March 15, 2024", // Second parameter
    ["3"] = "10:00 AM" // Third parameter
};

var templateMessage = new MessageBuilder()
    .WithTemplateContent("appointment_reminder", templateParams)
    .Message;
```

### Template Schema Configuration

```csharp
// Use template-focused schema
var templateSchema = TwilioChannelSchemas.WhatsAppTemplates;
var templateConnector = new TwilioWhatsAppConnector(templateSchema, connectionSettings);

// This schema optimizes for template messaging
// Removes media capabilities to focus on business notifications
```

## Interactive Messages

WhatsApp Business supports interactive elements like buttons and lists.

### Button Response Handling

When users interact with buttons, the webhook receives the button data:

```json
{
  "MessageSid": "SM1234567890",
  "From": "whatsapp:+1234567890",
  "To": "whatsapp:+1987654321",
  "Body": "",
  "ButtonText": "Confirm Booking",
  "ButtonPayload": "booking_confirmed",
  "MessageStatus": "received"
}
```

### List Selection Handling

When users make list selections:

```json
{
  "MessageSid": "SM1234567890",
  "From": "whatsapp:+1234567890", 
  "To": "whatsapp:+1987654321",
  "Body": "",
  "ListId": "option_1",
  "ListTitle": "Product Information",
  "MessageStatus": "received"
}
```

## Webhook Integration

### Receiving WhatsApp Messages

```csharp
[ApiController]
[Route("api/webhooks/twilio")]
public class TwilioWhatsAppWebhookController : ControllerBase
{
    private readonly TwilioWhatsAppConnector _connector;

    [HttpPost("whatsapp")]
    public async Task<IActionResult> ReceiveWhatsApp([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            foreach (var message in result.Value.Messages)
            {
                await ProcessIncomingWhatsApp(message, formData);
            }
        }
        
        return Ok();
    }

    [HttpPost("whatsapp/json")]
    public async Task<IActionResult> ReceiveWhatsAppJson([FromBody] JsonElement jsonData)
    {
        var messageSource = MessageSource.FromJson(jsonData);
        var result = await _connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            foreach (var message in result.Value.Messages)
            {
                await ProcessIncomingWhatsApp(message);
            }
        }
        
        return Ok();
    }

    [HttpPost("whatsapp/status")]
    public async Task<IActionResult> ReceiveWhatsAppStatus([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _connector.ReceiveMessageStatusAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            await ProcessWhatsAppStatusUpdate(result.Value);
        }
        
        return Ok();
    }

    private async Task ProcessIncomingWhatsApp(IMessage message, Dictionary<string, string>? webhookData = null)
    {
        Console.WriteLine($"Received WhatsApp from {message.Sender?.Address}: {message.Content?.Value}");
        
        // Check for interactive elements
        if (webhookData != null)
        {
            if (webhookData.TryGetValue("ButtonText", out var buttonText) && 
                webhookData.TryGetValue("ButtonPayload", out var buttonPayload))
            {
                await HandleButtonResponse(message, buttonText, buttonPayload);
                return;
            }
            
            if (webhookData.TryGetValue("ListId", out var listId) && 
                webhookData.TryGetValue("ListTitle", out var listTitle))
            {
                await HandleListSelection(message, listId, listTitle);
                return;
            }
            
            // Check for business profile information
            if (webhookData.TryGetValue("ProfileName", out var profileName))
            {
                Console.WriteLine($"Customer profile name: {profileName}");
            }
        }
        
        // Process regular text message
        await HandleTextMessage(message);
    }

    private async Task HandleButtonResponse(IMessage message, string buttonText, string buttonPayload)
    {
        Console.WriteLine($"Button pressed: {buttonText} (payload: {buttonPayload})");
        
        var response = buttonPayload switch
        {
            "booking_confirmed" => "Great! Your booking has been confirmed. You'll receive a confirmation shortly.",
            "booking_cancelled" => "Your booking has been cancelled. No charges will be applied.",
            "more_info" => "Here's more information about our services...",
            _ => "Thank you for your response!"
        };
        
        await SendWhatsAppReply(message, response);
    }

    private async Task HandleListSelection(IMessage message, string listId, string listTitle)
    {
        Console.WriteLine($"List selection: {listTitle} (ID: {listId})");
        
        var response = listId switch
        {
            "product_info" => "Here's detailed information about our products...",
            "support_contact" => "Our support team is available 24/7. How can we help?",
            "store_location" => "Our stores are located at...",
            _ => "Thank you for your selection!"
        };
        
        await SendWhatsAppReply(message, response);
    }

    private async Task HandleTextMessage(IMessage message)
    {
        var text = message.Content?.Value?.ToLowerInvariant() ?? "";
        
        var response = text switch
        {
            var t when t.Contains("help") => "How can we assist you today? Type 'menu' to see available options.",
            var t when t.Contains("menu") => "Our services include: 1) Product Info 2) Support 3) Store Locations",
            var t when t.Contains("hours") => "We're open Monday-Friday 9AM-6PM, Saturday 10AM-4PM",
            _ => "Thank you for your message. Our team will get back to you soon!"
        };
        
        await SendWhatsAppReply(message, response);
    }

    private async Task SendWhatsAppReply(IMessage originalMessage, string responseText)
    {
        var reply = new MessageBuilder()
            .WithId($"reply-{Guid.NewGuid()}")
            .WithPhoneSender(originalMessage.Receiver?.Address) // Swap sender/receiver
            .WithPhoneReceiver(originalMessage.Sender?.Address)
            .WithTextContent(responseText)
            .Message;
            
        await _connector.SendMessageAsync(reply, CancellationToken.None);
    }

    private async Task ProcessWhatsAppStatusUpdate(StatusUpdateResult statusUpdate)
    {
        Console.WriteLine($"WhatsApp message {statusUpdate.MessageId} status: {statusUpdate.Status}");
        
        // WhatsApp-specific status handling
        if (statusUpdate.Status == MessageStatus.Delivered && 
            statusUpdate.AdditionalData.ContainsKey("Channel") &&
            statusUpdate.AdditionalData["Channel"].ToString() == "WhatsApp")
        {
            // WhatsApp read receipt (mapped to Delivered status)
            if (statusUpdate.AdditionalData.ContainsKey("ProfileName"))
            {
                var profileName = statusUpdate.AdditionalData["ProfileName"];
                Console.WriteLine($"Message read by {profileName}");
            }
        }
        
        await UpdateMessageStatus(statusUpdate.MessageId, statusUpdate.Status);
    }
}
```

### Webhook Configuration

1. **WhatsApp Webhook URL**: `https://yourapp.com/api/webhooks/twilio/whatsapp`
2. **Status Callback URL**: `https://yourapp.com/api/webhooks/twilio/whatsapp/status`
3. **HTTP Method**: POST
4. **Content Type**: application/x-www-form-urlencoded or application/json

### Webhook Payload Examples

**Incoming WhatsApp Text Message:**
```
MessageSid=SM1234567890abcdef
From=whatsapp%3A%2B15551234567
To=whatsapp%3A%2B15559876543
Body=Hello%20from%20customer
ProfileName=John%20Doe
AccountSid=AC1234567890abcdef
```

**Button Response:**
```
MessageSid=SM1234567890abcdef
From=whatsapp%3A%2B15551234567
To=whatsapp%3A%2B15559876543
Body=
ButtonText=Confirm%20Booking
ButtonPayload=booking_confirmed
ProfileName=John%20Doe
```

**JSON Webhook Format:**
```json
{
  "MessageSid": "SM1234567890abcdef",
  "From": "whatsapp:+15551234567",
  "To": "whatsapp:+15559876543",
  "Body": "Hello from customer",
  "ProfileName": "John Doe",
  "AccountSid": "AC1234567890abcdef"
}
```

## Error Handling

### Common Error Codes

| Error Code | Description | Solution |
|------------|-------------|----------|
| `MISSING_CREDENTIALS` | AccountSid or AuthToken missing | Verify connection parameters |
| `INVALID_RECIPIENT` | Invalid WhatsApp number format | Use whatsapp:+1234567890 format |
| `TEMPLATE_NOT_APPROVED` | Template not approved by WhatsApp | Use approved templates only |
| `MEDIA_SIZE_EXCEEDED` | Media file too large | Reduce file size or use different format |

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
            Console.WriteLine($"Invalid WhatsApp number format: {message.Receiver?.Address}");
            Console.WriteLine("Use format: whatsapp:+1234567890");
            break;
            
        case "TEMPLATE_NOT_APPROVED":
            Console.WriteLine("WhatsApp template not approved. Use approved templates only.");
            break;
            
        case "MEDIA_SIZE_EXCEEDED":
            Console.WriteLine("Media file too large. Maximum sizes: Image 5MB, Document 100MB");
            break;
            
        default:
            Console.WriteLine($"WhatsApp send failed: {result.ErrorMessage}");
            break;
    }
}
```

## Best Practices

### 1. Phone Number Formatting

```csharp
// ? Good - Use WhatsApp prefix format
.WithPhoneSender("whatsapp:+1234567890")
.WithPhoneReceiver("whatsapp:+441234567890")

// ? Avoid - Missing WhatsApp prefix
.WithPhoneSender("+1234567890")
.WithPhoneReceiver("1234567890")
```

### 2. Template Message Guidelines

```csharp
// ? Good - Use approved templates for business notifications
var templateMessage = new MessageBuilder()
    .WithTemplateContent("appointment_reminder", new { 
        customer_name = "John",
        appointment_date = "March 15",
        appointment_time = "2:00 PM"
    })
    .Message;

// ? Good - Follow WhatsApp Business Policy
// - Use templates for notifications outside 24-hour window
// - Keep promotional content to minimum
// - Provide clear opt-out instructions
```

### 3. Interactive Message Design

```csharp
// Design messages that anticipate button/list responses
var interactiveMessage = new MessageBuilder()
    .WithTextContent(@"Thanks for your interest! What would you like to know?

?? Product Information
?? Pricing Details  
?? Schedule Demo
?? Contact Support

Please select an option from the menu.")
    .Message;

// Handle responses appropriately in webhook
```

### 4. Media Message Optimization

```csharp
// ? Good - Optimize media for WhatsApp
var optimizedMedia = new MessageBuilder()
    .WithMediaContent(
        "https://yourcdn.com/optimized-image.jpg", // Compressed for mobile
        "image/jpeg", 
        "Product Showcase"
    )
    .WithTextContent("Check out our featured product!") // Always include caption
    .Message;

// WhatsApp media limits:
// - Images: 5MB max, JPEG/PNG recommended
// - Documents: 100MB max
// - Videos: 16MB max, MP4 recommended
// - Audio: 16MB max, MP3/AAC recommended
```

### 5. Business Profile Integration

```csharp
// Process business-specific webhook fields
private async Task ProcessWhatsAppBusiness(Dictionary<string, string> webhookData)
{
    // Extract business information
    var profileName = webhookData.GetValueOrDefault("ProfileName");
    var businessName = webhookData.GetValueOrDefault("BusinessDisplayName");
    var isVerified = webhookData.GetValueOrDefault("BusinessVerified") == "true";
    
    if (!string.IsNullOrEmpty(businessName) && isVerified)
    {
        // Handle verified business account differently
        await ProcessVerifiedBusinessMessage(webhookData);
    }
    
    // Store customer profile information
    await UpdateCustomerProfile(profileName, businessName, isVerified);
}
```

### 6. Status Monitoring for WhatsApp

```csharp
// ? Good - Monitor WhatsApp-specific statuses
public async Task MonitorWhatsAppDelivery(string messageId)
{
    var maxWaitTime = TimeSpan.FromHours(1); // WhatsApp has longer delivery windows
    var startTime = DateTime.UtcNow;
    
    while (DateTime.UtcNow - startTime < maxWaitTime)
    {
        var statusResult = await connector.GetMessageStatusAsync(messageId, CancellationToken.None);
        
        if (statusResult.IsSuccess)
        {
            var status = statusResult.Value.StatusUpdates.FirstOrDefault()?.Status;
            
            switch (status)
            {
                case MessageStatus.Delivered:
                    // In WhatsApp, this often means "read"
                    Console.WriteLine("Message delivered/read by recipient");
                    return;
                    
                case MessageStatus.DeliveryFailed:
                    Console.WriteLine("Message delivery failed");
                    return;
                    
                case MessageStatus.Sent:
                    Console.WriteLine("Message sent to WhatsApp");
                    break; // Continue monitoring
            }
        }
        
        await Task.Delay(TimeSpan.FromSeconds(30)); // Check every 30 seconds
    }
}
```

### 7. Compliance and Privacy

```csharp
// ? Good - Implement opt-out handling
private async Task HandleOptOut(IMessage message)
{
    var text = message.Content?.Value?.ToLowerInvariant() ?? "";
    
    if (text.Contains("stop") || text.Contains("unsubscribe") || text.Contains("opt out"))
    {
        // Add to opt-out list
        await AddToOptOutList(message.Sender?.Address);
        
        // Send confirmation
        var optOutResponse = new MessageBuilder()
            .WithPhoneSender(message.Receiver?.Address)
            .WithPhoneReceiver(message.Sender?.Address)
            .WithTextContent("You have been unsubscribed. Reply START to re-subscribe.")
            .Message;
            
        await connector.SendMessageAsync(optOutResponse, CancellationToken.None);
    }
}

// ? Good - Check opt-out status before sending
private async Task<bool> CanSendToNumber(string phoneNumber)
{
    var isOptedOut = await IsNumberOptedOut(phoneNumber);
    if (isOptedOut)
    {
        Console.WriteLine($"Cannot send to {phoneNumber}: User has opted out");
        return false;
    }
    
    return true;
}
```

## Related Documentation

- [Twilio SMS Connector](twilio-sms-connector.md)
- [Template Messaging Guide](../template-messaging.md)
- [Webhook Integration Guide](../webhook-integration.md)
- [WhatsApp Business API Documentation](https://developers.facebook.com/docs/whatsapp)