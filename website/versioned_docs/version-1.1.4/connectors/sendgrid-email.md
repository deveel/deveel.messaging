---
sidebar_position: 3
---

# SendGrid Email Connector

Transactional and bulk email via the SendGrid v3 API.

## Package

```bash
dotnet add package Ratatosk.Sendgrid
```

## Configuration Settings

| Parameter | Type | Required | Is Secret | Default | Description |
|-----------|------|----------|-----------|---------|-------------|
| `ApiKey` | `string` | **Yes** | **Yes** | — | SendGrid API key (starts with `SG.`) |
| `SandboxMode` | `bool` | No | No | `false` | When `true`, no email is actually sent |
| `WebhookUrl` | `string` | No | No | — | URL for event webhooks |
| `TrackingSettings` | `string` | No | No | — | JSON with click/open tracking settings |
| `DefaultFromName` | `string` | No | No | — | Default sender name for emails |
| `DefaultReplyTo` | `string` | No | No | — | Default reply-to address |

**Notes:**
- `ApiKey` is marked as sensitive and will be redacted in logs
- `SandboxMode=true` is recommended for development/testing environments

## Configuration Examples

### Connection String

**Basic:**
```
ApiKey=SG.xxxxxxxxxxxx
```

**With Sandbox Mode:**
```
ApiKey=SG.xxxxxxxxxxxx;SandboxMode=true
```

**Complete:**
```
ApiKey=SG.xxxxxxxxxxxx;
SandboxMode=false;DefaultFromName=Support;DefaultReplyTo=support@example.com;
Timeout.Send=00:01:00;
Retry.MaxAttempts=3;Retry.BackoffType=Exponential;
Telemetry.EnableTracing=true;Telemetry.EnableMetrics=true
```

### From appsettings.json

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxxx",
    "SandboxMode": false,
    "DefaultFromName": "Support Team",
    "DefaultReplyTo": "support@example.com",
    "Timeout": {
      "Send": "00:01:00"
    },
    "Retry": {
      "MaxAttempts": 3,
      "BackoffType": "Exponential"
    }
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<SendGridEmailConnector>(cfg => cfg
        .WithSettings("SendGrid"));
```

### Typed Options

```csharp
var options = new SendGridEmailOptions
{
    ApiKey = "SG.xxxxxxxxxxxx",
    SandboxMode = false,
    DefaultFromName = "Support Team",
    DefaultReplyTo = "support@example.com",
    SendTimeout = TimeSpan.FromSeconds(60)
};

builder.Services
    .AddMessaging()
    .AddConnector<SendGridEmailConnector>(cfg => cfg
        .WithOptions(options));
```

## Schema

| Property | Value |
|---|---|
| Provider | `SendGrid` |
| Type | `Email` |
| Version | `1.1.0` |
| Capabilities | `SendMessages`, `BulkMessaging`, `Templates`, `HealthCheck` |
| Content types | `PlainText`, `Html`, `Multipart`, `Template` |
| Endpoints | `EmailAddress` (send + receive) |
| Authentication | API Key |

## Send examples

### Plain text email

```csharp
var settings = new ConnectionSettings()
    .SetParameter("ApiKey", "SG...");

var connector = new SendGridEmailConnector(SendGridChannelSchemas.SendGridEmail, settings);
await connector.InitializeAsync(ct);

var email = new MessageBuilder()
    .WithId("email-1")
    .FromEmail("noreply@yourdomain.com")
    .ToEmail("user@example.com")
    .WithText("Welcome to the app!")
    .WithSubject("Welcome!")
    .Build();

var result = await connector.SendMessageAsync(email, ct);
```

### HTML email

```csharp
new MessageBuilder()
    .FromEmail("noreply@yourdomain.com")
    .ToEmail("user@example.com")
    .WithHtml("<h1>Welcome!</h1><p>Thanks for signing up, <b>{{name}}</b>.</p>")
    .WithSubject("Welcome!")
    .Build();
```

### Multipart email (text + HTML)

```csharp
var multipart = new MultipartContent();
multipart.Parts.Add(new TextContent("Welcome to the app! Please verify your email."));
multipart.Parts.Add(new HtmlContent(
    "<h1>Welcome!</h1><p>Please <a href='{{link}}'>verify your email</a>.</p>"));

new MessageBuilder()
    .FromEmail("noreply@yourdomain.com")
    .ToEmail("user@example.com")
    .WithContent(multipart)
    .WithSubject("Verify your email")
    .Build();
```

### Email with attachment

```csharp
new MessageBuilder()
    .FromEmail("noreply@yourdomain.com")
    .ToEmail("user@example.com")
    .WithHtml("<p>Please find the attached report.</p>", html =>
    {
        html.Attachments.Add(new MessageAttachment(
            "report", "report.pdf", "application/pdf", base64PdfContent));
    })
    .WithSubject("Monthly Report")
    .Build();
```

### Template email (SendGrid dynamic templates)

```csharp
new MessageBuilder()
    .FromEmail("noreply@yourdomain.com")
    .ToEmail("user@example.com")
    .WithContent(new TemplateContent("d-abc123def456", new Dictionary<string, object?>
    {
        ["name"] = "Alice",
        ["verification_link"] = "https://example.com/verify?token=xyz"
    }))
    .WithSubject("Welcome!")
    .Build();
```

### Batch send

```csharp
var batch = new MessageBatch();
batch.Messages.Add(email1);
batch.Messages.Add(email2);
batch.Messages.Add(email3);

var result = await connector.SendBatchAsync(batch, ct);
Console.WriteLine($"Batch sent: {result.Data?.BatchId}");

if (result.Data != null)
{
    foreach (var (msgId, sendResult) in result.Data.MessageResults)
        Console.WriteLine($"  {msgId}: {sendResult.Status} ({sendResult.RemoteMessageId})");
}
```

### Sandbox mode (testing)

```csharp
new ConnectionSettings()
    .SetParameter("ApiKey", "SG...")
    .SetParameter("SandboxMode", true);
```

## Message properties

| Property | Type | Description |
|---|---|---|
| `Subject` | `string` | Email subject line (required) |
| `TrackingSettings` | `string` | JSON string for click/open tracking configuration |
| `ReplyTo` | `string` | Reply-to email address |

## Webhook handling

SendGrid posts event data (delivered, opened, clicked, bounced, spam reports) as JSON POST to your webhook URL:

```csharp
[HttpPost("/webhooks/sendgrid")]
public async Task<IActionResult> SendGridWebhook(CancellationToken ct)
{
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync(ct);
    var source = MessageSource.Json(body);

    var result = await _connector.ReceiveMessageStatusAsync(source, ct);
    // Process status updates...

    return Ok();
}
```

Validate `X-Twilio-Email-Event-Webhook-Signature` header when using signed webhooks.

## Error codes

SendGrid-specific error codes are defined in `SendGridErrorCodes` with domain `"SendGrid"`.

| Code | Description |
|---|---|
| `INVALID_EMAIL_ADDRESS` | Email address format is invalid |
| `MISSING_EMAIL_CONTENT` | Email content (subject, body) is missing |

Standard `MessagingErrorCodes` are also used — see the [error codes reference](../connectors-implementation/result-types.md#error-code-tables).

### Original provider codes

SendGrid errors are mapped from HTTP status codes in the connector. There is no custom error code mapping from SendGrid API error codes. HTTP `429 Too Many Requests` maps to `RATE_LIMIT_EXCEEDED`; all other non-success status codes map to `SEND_MESSAGE_FAILED`.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `INVALID_CREDENTIALS` | API key is invalid | Generate a new API key in SendGrid UI |
| `PROVIDER_VALIDATION_FAILED` | Invalid sender email | Verify sender domain (SPF/DKIM setup) |
| Delivery not happening | Sender not verified | Verify sender email or domain in SendGrid |
| Emails going to spam | Poor sender reputation | Check SendGrid reputation reports |
| Template not rendering | Template ID wrong | Verify template ID in SendGrid UI |
| Event webhooks not arriving | Webhook URL not configured | Subscribe to events in SendGrid Mail Settings |

## SendGridChannelSchemas

```csharp
// Full email schema (send, bulk, templates)
SendGridChannelSchemas.SendGridEmail
```
