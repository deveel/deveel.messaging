# SendGrid Email Connector Documentation

The SendGrid Email Connector provides comprehensive email messaging capabilities through the SendGrid API, including transactional emails, marketing campaigns, template messaging, and webhook support for email events.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Schema Specifications](#schema-specifications)
4. [Connection Parameters](#connection-parameters)
5. [Message Properties](#message-properties)
6. [Usage Examples](#usage-examples)
7. [Template Messaging](#template-messaging)
8. [Email Attachments](#email-attachments)
9. [Marketing Features](#marketing-features)
10. [Webhook Integration](#webhook-integration)
11. [Error Handling](#error-handling)
12. [Best Practices](#best-practices)

## Overview

The `SendGridEmailConnector` implements email messaging using the SendGrid API. It supports:

- **Transactional Emails**: Automated emails like receipts, confirmations, and notifications
- **Marketing Emails**: Campaigns, newsletters, and promotional content
- **Template Messaging**: Dynamic content using SendGrid templates
- **Email Attachments**: Send files and media via email
- **Webhook Events**: Track opens, clicks, bounces, and other email events
- **Bulk Operations**: Efficient batch email sending

### Capabilities

| Capability | Supported | Description |
|------------|-----------|-------------|
| SendMessages | ? | Send emails to recipients |
| ReceiveMessages | ? | Receive email events via webhooks |
| MessageStatusQuery | ? | Query email delivery status |
| HandleMessageState | ? | Process email event callbacks |
| BulkMessaging | ? | Send multiple emails efficiently |
| Templates | ? | Dynamic email templates |
| MediaAttachments | ? | File and media attachments |
| HealthCheck | ? | Monitor SendGrid service health |

## Installation

```bash
dotnet add package Deveel.Messaging.Connector.Sendgrid
```

## Schema Specifications

### Base Schema: SendGridEmail

```csharp
var schema = SendGridChannelSchemas.SendGridEmail;
// Provider: "SendGrid"
// Type: "Email" 
// Version: "1.0.0"
// Capabilities: SendMessages | ReceiveMessages | MessageStatusQuery | HandleMessageState | BulkMessaging | Templates | MediaAttachments | HealthCheck
```

### Available Schema Variants

| Schema | Description | Use Case |
|--------|-------------|----------|
| `SendGridEmail` | Full-featured email with all capabilities | Complete email system |
| `SimpleEmail` | Basic email without advanced features | Simple notifications |
| `TransactionalEmail` | Automated emails with tracking | Order confirmations, receipts |
| `MarketingEmail` | Campaign emails with tracking | Newsletters, promotions |
| `TemplateEmail` | Template-focused messaging | Dynamic content emails |
| `BulkEmail` | High-volume email campaigns | Mass communications |

### Schema Comparison

```csharp
// Full featured schema
var fullSchema = SendGridChannelSchemas.SendGridEmail;

// Simple email schema  
var simpleSchema = SendGridChannelSchemas.SimpleEmail;
// Removes: ReceiveMessages, HandleMessageState, BulkMessaging, Templates, MediaAttachments
// Removes: WebhookUrl, TrackingSettings parameters

// Transactional email schema
var transactionalSchema = SendGridChannelSchemas.TransactionalEmail;
// Removes: ReceiveMessages, HandleMessageState, BulkMessaging, Templates
// Focuses on single email delivery with tracking

// Marketing email schema
var marketingSchema = SendGridChannelSchemas.MarketingEmail;
// Adds: ListId, CampaignId properties
// Optimized for campaign tracking

// Template email schema
var templateSchema = SendGridChannelSchemas.TemplateEmail;
// Removes: PlainText, Html, Multipart content types
// Focuses on template-based emails only
```

## Connection Parameters

### Required Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `ApiKey` | String | SendGrid API Key (sensitive) | `"SG.xxxx"` |

### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SandboxMode` | Boolean | false | Enable sandbox mode for testing |
| `WebhookUrl` | String | null | URL for receiving email event webhooks |
| `TrackingSettings` | Boolean | true | Enable email tracking (opens, clicks) |
| `DefaultFromName` | String | null | Default sender name |
| `DefaultReplyTo` | String | null | Default reply-to address |

### Configuration Example

```csharp
var connectionSettings = new ConnectionSettings()
    .AddParameter("ApiKey", "SG.your_api_key_here")
    .AddParameter("SandboxMode", false)
    .AddParameter("WebhookUrl", "https://yourapp.com/webhooks/sendgrid")
    .AddParameter("TrackingSettings", true)
    .AddParameter("DefaultFromName", "Your Company")
    .AddParameter("DefaultReplyTo", "support@yourcompany.com");
```

### API Key Setup

1. **Go to SendGrid Console**: https://app.sendgrid.com/
2. **Navigate to**: Settings ? API Keys
3. **Click**: "Create API Key"
4. **Choose**: "Full Access" or "Restricted Access"
5. **Copy the API key** and use it as the `ApiKey` parameter

## Message Properties

### Required Properties

| Property | Type | Required | Description | Max Length |
|----------|------|----------|-------------|------------|
| `Subject` | String | Yes | Email subject line | 998 chars |

### Optional Properties

| Property | Type | Required | Description | Values |
|----------|------|----------|-------------|--------|
| `Priority` | String | No | Email priority | `low`, `normal`, `high` |
| `Categories` | String | No | Comma-separated categories | Max 10 categories |
| `CustomArgs` | String | No | JSON custom arguments | Valid JSON |
| `SendAt` | String | No | Scheduled send time | ISO 8601 or DateTime |
| `BatchId` | String | No | Batch ID for grouping | - |
| `IpPoolName` | String | No | IP pool for sending | - |
| `AsmGroupId` | Integer | No | Unsubscribe group ID | Positive integer |

### Marketing Properties

| Property | Type | Description |
|----------|------|-------------|
| `ListId` | String | Marketing list ID |
| `CampaignId` | String | Campaign tracking ID |
| `MailBatchId` | String | Mail batch identifier |
| `UnsubscribeGroupId` | Integer | Unsubscribe group for compliance |

### Template Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `TemplateId` | String | Yes (for templates) | SendGrid template ID |
| `TemplateData` | String | No | JSON template substitutions |

## Usage Examples

### Basic Email Sending

```csharp
using Deveel.Messaging;

// Create connector
var schema = SendGridChannelSchemas.SendGridEmail;
var connectionSettings = new ConnectionSettings()
    .AddParameter("ApiKey", "SG.your_api_key_here")
    .AddParameter("DefaultFromName", "Your Company");

var connector = new SendGridEmailConnector(schema, connectionSettings);
await connector.InitializeAsync(cancellationToken);

// Create and send email
var email = new MessageBuilder()
    .WithId("email-001")
    .WithEmailSender("noreply@yourcompany.com")
    .WithEmailReceiver("customer@example.com")
    .WithHtmlContent("<h1>Welcome!</h1><p>Thank you for joining our service.</p>")
    .WithProperty("Subject", "Welcome to Our Service")
    .WithProperty("Priority", "normal")
    .Message;

var result = await connector.SendMessageAsync(email, cancellationToken);
if (result.IsSuccess)
{
    Console.WriteLine($"Email sent with ID: {result.Value?.MessageId}");
}
```

### Plain Text Email

```csharp
var textEmail = new MessageBuilder()
    .WithId("text-email-001")
    .WithEmailSender("support@yourcompany.com")
    .WithEmailReceiver("user@example.com")
    .WithTextContent("Hello! This is a plain text email message.")
    .WithProperty("Subject", "Plain Text Message")
    .WithProperty("Categories", "support,notification")
    .Message;

var result = await connector.SendMessageAsync(textEmail, cancellationToken);
```

### Email with Multiple Recipients

```csharp
var multiRecipientEmail = new MessageBuilder()
    .WithId("multi-001")
    .WithEmailSender("newsletter@yourcompany.com")
    .WithEmailReceiver("user1@example.com") // Primary recipient
    .WithHtmlContent("<h2>Monthly Newsletter</h2><p>Here's what's new...</p>")
    .WithProperty("Subject", "Monthly Newsletter - March 2024")
    .WithProperty("Categories", "newsletter,monthly")
    .Message;

// For multiple recipients, send as batch
var recipients = new[] { "user1@example.com", "user2@example.com", "user3@example.com" };
var messages = recipients.Select(email => 
    new MessageBuilder()
        .WithId($"newsletter-{Guid.NewGuid()}")
        .WithEmailSender("newsletter@yourcompany.com")
        .WithEmailReceiver(email)
        .WithHtmlContent("<h2>Monthly Newsletter</h2><p>Here's what's new...</p>")
        .WithProperty("Subject", "Monthly Newsletter - March 2024")
        .Message
).ToList();

var batch = new MessageBatch(messages);
var batchResult = await connector.SendBatchAsync(batch, cancellationToken);
```

### Scheduled Email

```csharp
var scheduledEmail = new MessageBuilder()
    .WithId("scheduled-001")
    .WithEmailSender("reminders@yourcompany.com")
    .WithEmailReceiver("customer@example.com")
    .WithHtmlContent("<p>This is your scheduled reminder!</p>")
    .WithProperty("Subject", "Scheduled Reminder")
    .WithProperty("SendAt", DateTime.UtcNow.AddHours(24).ToString("O")) // Send in 24 hours
    .Message;

var result = await connector.SendMessageAsync(scheduledEmail, cancellationToken);
```

## Template Messaging

SendGrid templates allow you to create dynamic email content with variable substitution.

### Basic Template Email

```csharp
// Use template schema
var templateSchema = SendGridChannelSchemas.TemplateEmail;
var templateConnector = new SendGridEmailConnector(templateSchema, connectionSettings);

var templateEmail = new MessageBuilder()
    .WithId("template-001")
    .WithEmailSender("welcome@yourcompany.com")
    .WithEmailReceiver("newuser@example.com")
    .WithTemplateContent("d-1234567890abcdef", new // SendGrid template ID
    {
        first_name = "John",
        last_name = "Doe",
        company_name = "Acme Corp",
        login_url = "https://yourapp.com/login"
    })
    .WithProperty("Subject", "Welcome {{first_name}}!") // Subject can use template vars
    .Message;

var result = await templateConnector.SendMessageAsync(templateEmail, cancellationToken);
```

### Advanced Template with Custom Data

```csharp
var advancedTemplate = new MessageBuilder()
    .WithId("advanced-template-001")
    .WithEmailSender("orders@yourcompany.com")
    .WithEmailReceiver("customer@example.com")
    .WithTemplateContent("d-order-confirmation", new
    {
        customer = new
        {
            first_name = "Jane",
            last_name = "Smith",
            email = "customer@example.com"
        },
        order = new
        {
            order_id = "ORD-12345",
            total = "$99.99",
            items = new[]
            {
                new { name = "Product A", quantity = 2, price = "$29.99" },
                new { name = "Product B", quantity = 1, price = "$39.99" }
            }
        },
        delivery = new
        {
            estimated_date = "March 15, 2024",
            tracking_number = "1Z999AA1234567890"
        }
    })
    .WithProperty("Subject", "Order Confirmation - {{order.order_id}}")
    .WithProperty("Categories", "order,confirmation")
    .WithProperty("CustomArgs", JsonSerializer.Serialize(new
    {
        order_id = "ORD-12345",
        customer_type = "premium"
    }))
    .Message;

var result = await templateConnector.SendMessageAsync(advancedTemplate, cancellationToken);
```

## Email Attachments

### Single File Attachment

```csharp
var attachmentEmail = new MessageBuilder()
    .WithId("attachment-001")
    .WithEmailSender("documents@yourcompany.com")
    .WithEmailReceiver("client@example.com")
    .WithMultipartContent(content => content
        .AddHtmlPart("<p>Please find the attached document.</p>")
        .AddAttachment("invoice.pdf", pdfBytes, "application/pdf")
    )
    .WithProperty("Subject", "Invoice #12345")
    .Message;

var result = await connector.SendMessageAsync(attachmentEmail, cancellationToken);
```

### Multiple Attachments

```csharp
var multiAttachmentEmail = new MessageBuilder()
    .WithId("multi-attachment-001")
    .WithEmailSender("reports@yourcompany.com")
    .WithEmailReceiver("manager@example.com")
    .WithMultipartContent(content => content
        .AddHtmlPart(@"<h2>Monthly Reports</h2>
                      <p>Please find attached the monthly reports.</p>")
        .AddAttachment("sales-report.pdf", salesReportBytes, "application/pdf")
        .AddAttachment("analytics-chart.png", chartImageBytes, "image/png")
        .AddAttachment("data-export.csv", csvBytes, "text/csv")
    )
    .WithProperty("Subject", "Monthly Reports - March 2024")
    .WithProperty("Categories", "reports,monthly")
    .Message;

var result = await connector.SendMessageAsync(multiAttachmentEmail, cancellationToken);
```

## Marketing Features

### Newsletter Campaign

```csharp
// Use marketing schema
var marketingSchema = SendGridChannelSchemas.MarketingEmail;
var marketingConnector = new SendGridEmailConnector(marketingSchema, connectionSettings);

var newsletter = new MessageBuilder()
    .WithId("newsletter-001")
    .WithEmailSender("newsletter@yourcompany.com")
    .WithEmailReceiver("subscriber@example.com")
    .WithHtmlContent(newsletterHtmlContent)
    .WithProperty("Subject", "Weekly Newsletter - Tech Updates")
    .WithProperty("Categories", "newsletter,weekly,tech")
    .WithProperty("CampaignId", "campaign-2024-03")
    .WithProperty("ListId", "newsletter-subscribers")
    .WithProperty("AsmGroupId", 12345) // Unsubscribe group
    .WithProperty("CustomArgs", JsonSerializer.Serialize(new
    {
        campaign_type = "newsletter",
        audience_segment = "tech_enthusiasts"
    }))
    .Message;

var result = await marketingConnector.SendMessageAsync(newsletter, cancellationToken);
```

### A/B Testing Campaign

```csharp
public async Task SendABTestCampaign(List<string> recipients)
{
    var random = new Random();
    var messages = new List<IMessage>();
    
    foreach (var recipient in recipients)
    {
        var variant = random.Next(2) == 0 ? "A" : "B";
        var subject = variant == "A" 
            ? "?? Special Offer Inside!" 
            : "Don't Miss Out - Limited Time Offer";
        
        var message = new MessageBuilder()
            .WithId($"ab-test-{variant}-{Guid.NewGuid()}")
            .WithEmailSender("marketing@yourcompany.com")
            .WithEmailReceiver(recipient)
            .WithHtmlContent(GetEmailContent(variant))
            .WithProperty("Subject", subject)
            .WithProperty("Categories", $"ab-test,variant-{variant}")
            .WithProperty("CustomArgs", JsonSerializer.Serialize(new
            {
                test_variant = variant,
                test_id = "promo-2024-03"
            }))
            .Message;
            
        messages.Add(message);
    }
    
    var batch = new MessageBatch(messages);
    var result = await marketingConnector.SendBatchAsync(batch, cancellationToken);
    
    Console.WriteLine($"A/B test campaign sent to {recipients.Count} recipients");
}
```

## Webhook Integration

### Email Event Handling

```csharp
[ApiController]
[Route("api/webhooks/sendgrid")]
public class SendGridWebhookController : ControllerBase
{
    private readonly SendGridEmailConnector _connector;

    [HttpPost]
    public async Task<IActionResult> HandleWebhook([FromBody] JsonElement[] events)
    {
        foreach (var eventData in events)
        {
            var messageSource = MessageSource.FromJson(eventData);
            
            // Determine event type
            var eventType = eventData.GetProperty("event").GetString();
            
            switch (eventType)
            {
                case "delivered":
                case "opened":
                case "clicked":
                case "bounced":
                case "dropped":
                    var statusResult = await _connector.ReceiveMessageStatusAsync(messageSource, CancellationToken.None);
                    if (statusResult.IsSuccess)
                    {
                        await ProcessEmailEvent(statusResult.Value, eventType);
                    }
                    break;
                    
                case "processed":
                case "deferred":
                    // Handle processing events
                    break;
                    
                case "unsubscribe":
                case "group_unsubscribe":
                    await HandleUnsubscribe(eventData);
                    break;
                    
                case "spam_report":
                    await HandleSpamReport(eventData);
                    break;
            }
        }
        
        return Ok();
    }

    private async Task ProcessEmailEvent(StatusUpdateResult statusUpdate, string eventType)
    {
        Console.WriteLine($"Email {statusUpdate.MessageId} event: {eventType}");
        
        // Update database with email status
        await UpdateEmailStatus(statusUpdate.MessageId, eventType, statusUpdate.Timestamp);
        
        // Handle specific events
        switch (eventType)
        {
            case "opened":
                await TrackEmailOpen(statusUpdate);
                break;
                
            case "clicked":
                await TrackEmailClick(statusUpdate);
                break;
                
            case "bounced":
                await HandleBounce(statusUpdate);
                break;
                
            case "dropped":
                await HandleDrop(statusUpdate);
                break;
        }
    }

    private async Task HandleUnsubscribe(JsonElement eventData)
    {
        var email = eventData.GetProperty("email").GetString();
        var asmGroupId = eventData.TryGetProperty("asm_group_id", out var groupProp) 
            ? groupProp.GetInt32() 
            : (int?)null;
        
        Console.WriteLine($"Unsubscribe: {email} from group {asmGroupId}");
        
        // Add to unsubscribe list
        await AddToUnsubscribeList(email, asmGroupId);
    }

    private async Task HandleSpamReport(JsonElement eventData)
    {
        var email = eventData.GetProperty("email").GetString();
        
        Console.WriteLine($"Spam report from: {email}");
        
        // Add to suppression list
        await AddToSuppressionList(email, "spam");
    }

    private async Task TrackEmailOpen(StatusUpdateResult statusUpdate)
    {
        // Extract additional data from webhook
        var userAgent = statusUpdate.AdditionalData.GetValueOrDefault("useragent")?.ToString();
        var ipAddress = statusUpdate.AdditionalData.GetValueOrDefault("ip")?.ToString();
        
        await RecordEmailEngagement(statusUpdate.MessageId, "open", userAgent, ipAddress);
    }

    private async Task TrackEmailClick(StatusUpdateResult statusUpdate)
    {
        var url = statusUpdate.AdditionalData.GetValueOrDefault("url")?.ToString();
        
        await RecordEmailEngagement(statusUpdate.MessageId, "click", url: url);
    }
}
```

### Webhook Event Types

| Event | Description | When It Occurs |
|-------|-------------|----------------|
| `processed` | Email processed by SendGrid | Initial processing |
| `delivered` | Email delivered to recipient | Successful delivery |
| `opened` | Email opened by recipient | Email tracking image loaded |
| `clicked` | Link clicked in email | User clicks tracked link |
| `bounced` | Email bounced | Delivery failed |
| `dropped` | Email dropped | Rejected before sending |
| `deferred` | Email deferred | Temporary delivery delay |
| `unsubscribe` | User unsubscribed | Unsubscribe link clicked |
| `spam_report` | Marked as spam | User reported as spam |

## Error Handling

### Common Error Scenarios

| Error | Description | Solution |
|-------|-------------|----------|
| `INVALID_API_KEY` | API key is invalid | Verify API key in SendGrid console |
| `INSUFFICIENT_PERMISSIONS` | API key lacks permissions | Update API key permissions |
| `INVALID_EMAIL` | Email address format invalid | Validate email format |
| `SUPPRESSED_ADDRESS` | Email is on suppression list | Remove from suppression or use different address |
| `TEMPLATE_NOT_FOUND` | Template ID doesn't exist | Verify template ID in SendGrid |

### Error Handling Example

```csharp
public async Task<bool> SendEmailSafely(string to, string subject, string content)
{
    try
    {
        var email = new MessageBuilder()
            .WithId($"safe-{Guid.NewGuid()}")
            .WithEmailSender("noreply@yourcompany.com")
            .WithEmailReceiver(to)
            .WithHtmlContent(content)
            .WithProperty("Subject", subject)
            .Message;

        var result = await connector.SendMessageAsync(email, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            return true;
        }

        // Handle specific errors
        switch (result.ErrorCode)
        {
            case SendGridErrorCodes.InvalidApiKey:
                Console.WriteLine("Invalid SendGrid API key - check configuration");
                break;
                
            case SendGridErrorCodes.InsufficientPermissions:
                Console.WriteLine("API key needs 'Mail Send' permission");
                break;
                
            case SendGridErrorCodes.InvalidEmail:
                Console.WriteLine($"Invalid email address: {to}");
                await RemoveInvalidEmail(to);
                break;
                
            case SendGridErrorCodes.SuppressedAddress:
                Console.WriteLine($"Email address suppressed: {to}");
                await HandleSuppressedEmail(to);
                break;
                
            default:
                Console.WriteLine($"Email send failed: {result.ErrorMessage}");
                break;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception sending email: {ex.Message}");
        return false;
    }
}
```

### Bulk Email Error Handling

```csharp
public async Task HandleBulkEmailErrors(MessageBatch batch)
{
    var result = await connector.SendBatchAsync(batch, CancellationToken.None);
    
    if (result.IsSuccess)
    {
        var suppressedEmails = new List<string>();
        var invalidEmails = new List<string>();
        
        foreach (var (messageId, sendResult) in result.Value.Results)
        {
            if (!sendResult.IsSuccess)
            {
                switch (sendResult.ErrorCode)
                {
                    case SendGridErrorCodes.SuppressedAddress:
                        if (sendResult.AdditionalData.TryGetValue("Email", out var suppressedEmail))
                        {
                            suppressedEmails.Add(suppressedEmail.ToString());
                        }
                        break;
                        
                    case SendGridErrorCodes.InvalidEmail:
                        if (sendResult.AdditionalData.TryGetValue("Email", out var invalidEmail))
                        {
                            invalidEmails.Add(invalidEmail.ToString());
                        }
                        break;
                }
            }
        }
        
        // Clean up problematic emails
        if (suppressedEmails.Any())
        {
            await UpdateSuppressionStatus(suppressedEmails);
            Console.WriteLine($"Updated suppression status for {suppressedEmails.Count} emails");
        }
        
        if (invalidEmails.Any())
        {
            await RemoveInvalidEmails(invalidEmails);
            Console.WriteLine($"Removed {invalidEmails.Count} invalid emails");
        }
    }
}
```

## Best Practices

### 1. Email Authentication

```csharp
// ? Good - Use verified sender identity
var email = new MessageBuilder()
    .WithEmailSender("noreply@yourdomain.com") // Verified domain
    .WithEmailReceiver(recipient)
    .WithProperty("Subject", "Important Update")
    .Message;

// ? Good - Set proper reply-to
var emailWithReplyTo = new MessageBuilder()
    .WithEmailSender("noreply@yourdomain.com")
    .WithEmailReceiver(recipient)
    .WithProperty("Subject", "Customer Support")
    .WithProperty("ReplyTo", "support@yourdomain.com") // Human-monitored address
    .Message;
```

### 2. Subject Line Optimization

```csharp
// ? Good - Clear, descriptive subjects
.WithProperty("Subject", "Order #12345 Confirmation")
.WithProperty("Subject", "Password Reset Request")
.WithProperty("Subject", "Welcome to YourApp - Getting Started")

// ? Avoid - Spam-triggering subjects
.WithProperty("Subject", "URGENT!!! CLICK NOW!!!")
.WithProperty("Subject", "Make $1000 Fast")
.WithProperty("Subject", "RE: (no previous email)")
```

### 3. Content Best Practices

```csharp
// ? Good - Provide both HTML and text versions
var multipartEmail = new MessageBuilder()
    .WithMultipartContent(content => content
        .AddHtmlPart(@"<h1>Welcome!</h1>
                      <p>Thank you for joining our service.</p>
                      <a href='https://yourapp.com/welcome'>Get Started</a>")
        .AddTextPart(@"Welcome!
                      
                      Thank you for joining our service.
                      Get started: https://yourapp.com/welcome")
    )
    .WithProperty("Subject", "Welcome to Our Service")
    .Message;

// ? Good - Use templates for consistent branding
var brandedEmail = new MessageBuilder()
    .WithTemplateContent("branded-template", new
    {
        header_text = "Welcome",
        body_content = "Thank you for joining our service.",
        cta_text = "Get Started",
        cta_url = "https://yourapp.com/welcome"
    })
    .Message;
```

### 4. List Management

```csharp
// ? Good - Implement proper unsubscribe handling
public async Task SendMarketingEmail(string recipient, bool hasOptIn)
{
    if (!hasOptIn)
    {
        Console.WriteLine($"Skipping {recipient} - no marketing opt-in");
        return;
    }
    
    var isUnsubscribed = await IsUnsubscribed(recipient);
    if (isUnsubscribed)
    {
        Console.WriteLine($"Skipping {recipient} - unsubscribed");
        return;
    }
    
    var email = new MessageBuilder()
        .WithEmailReceiver(recipient)
        .WithProperty("AsmGroupId", 12345) // Unsubscribe group
        .Message;
        
    await connector.SendMessageAsync(email, CancellationToken.None);
}

// ? Good - Regular list hygiene
public async Task PerformListHygiene()
{
    // Remove bounced emails
    var bouncedEmails = await GetBouncedEmails();
    await RemoveFromMarketingList(bouncedEmails);
    
    // Remove old unsubscribes
    var oldUnsubscribes = await GetOldUnsubscribes(TimeSpan.FromDays(30));
    await PurgeOldUnsubscribes(oldUnsubscribes);
}
```

### 5. Template Management

```csharp
// ? Good - Validate template data
public async Task SendTemplateEmail(string templateId, string recipient, object templateData)
{
    try
    {
        // Validate template data against schema
        var isValid = await ValidateTemplateData(templateId, templateData);
        if (!isValid)
        {
            throw new ArgumentException("Template data validation failed");
        }
        
        var email = new MessageBuilder()
            .WithTemplateContent(templateId, templateData)
            .WithEmailReceiver(recipient)
            .Message;
            
        await connector.SendMessageAsync(email, CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Template email failed: {ex.Message}");
        
        // Fallback to basic email
        await SendFallbackEmail(recipient);
    }
}

// ? Good - Use version control for templates
public class TemplateManager
{
    private readonly Dictionary<string, string> _templateVersions = new()
    {
        ["welcome"] = "d-welcome-v2",
        ["order-confirmation"] = "d-order-v3",
        ["password-reset"] = "d-password-v1"
    };
    
    public string GetCurrentTemplateId(string templateName)
    {
        return _templateVersions.GetValueOrDefault(templateName) 
            ?? throw new ArgumentException($"Unknown template: {templateName}");
    }
}
```

### 6. Analytics and Tracking

```csharp
// ? Good - Implement comprehensive tracking
public async Task SendTrackedEmail(string recipient, string campaignId)
{
    var email = new MessageBuilder()
        .WithEmailReceiver(recipient)
        .WithProperty("Categories", $"campaign,{campaignId}")
        .WithProperty("CustomArgs", JsonSerializer.Serialize(new
        {
            campaign_id = campaignId,
            sent_at = DateTime.UtcNow.ToString("O"),
            recipient_segment = await GetRecipientSegment(recipient)
        }))
        .Message;
        
    await connector.SendMessageAsync(email, CancellationToken.None);
}

// ? Good - Monitor email metrics
public async Task AnalyzeCampaignPerformance(string campaignId)
{
    var metrics = await GetEmailMetrics(campaignId);
    
    Console.WriteLine($"Campaign {campaignId} Performance:");
    Console.WriteLine($"  Sent: {metrics.Sent}");
    Console.WriteLine($"  Delivered: {metrics.Delivered} ({metrics.DeliveryRate:P})");
    Console.WriteLine($"  Opened: {metrics.Opens} ({metrics.OpenRate:P})");
    Console.WriteLine($"  Clicked: {metrics.Clicks} ({metrics.ClickRate:P})");
    Console.WriteLine($"  Bounced: {metrics.Bounces} ({metrics.BounceRate:P})");
}
```

### 7. Rate Limiting and Scheduling

```csharp
// ? Good - Respect rate limits
public async Task SendBulkEmailsWithRateLimit(List<IMessage> emails)
{
    const int batchSize = 100;
    const int delayMs = 1000; // 1 second between batches
    
    var batches = emails.Chunk(batchSize);
    
    foreach (var batch in batches)
    {
        var messageBatch = new MessageBatch(batch.ToList());
        await connector.SendBatchAsync(messageBatch, CancellationToken.None);
        
        // Respect rate limits
        await Task.Delay(delayMs);
    }
}

// ? Good - Schedule emails for optimal times
public async Task ScheduleOptimalSend(IMessage email, string recipientTimezone)
{
    var optimalTime = CalculateOptimalSendTime(recipientTimezone);
    
    if (optimalTime > DateTime.UtcNow)
    {
        // Schedule for later
        email = new MessageBuilder(email)
            .WithProperty("SendAt", optimalTime.ToString("O"))
            .Message;
    }
    
    await connector.SendMessageAsync(email, CancellationToken.None);
}
```

## Related Documentation

- [SendGrid API Documentation](https://docs.sendgrid.com/api-reference/)
- [Channel Schema Usage Guide](../ChannelSchema-Usage.md)
- [Connector Implementation Guide](../ChannelConnector-Usage.md)
- [Email Deliverability Best Practices](https://docs.sendgrid.com/ui/sending-email/deliverability)