---
sidebar_position: 2
---

# Twilio WhatsApp Connector

WhatsApp Business messaging through Twilio's API for WhatsApp.

## Package

```bash
dotnet add package Ratatosk.Twilio
```

## Configuration Settings

| Parameter | Type | Required | Is Secret | Default | Description |
|-----------|------|----------|-----------|---------|-------------|
| `AccountSid` | `string` | **Yes** | No | — | Twilio account SID (starts with `AC`) |
| `AuthToken` | `string` | **Yes** | **Yes** | — | Twilio authentication token |
| `WebhookUrl` | `string` | No | No | — | URL for inbound WhatsApp messages |
| `StatusCallback` | `string` | No | No | — | URL for delivery status callbacks |

**Notes:**
- `AuthToken` is marked as sensitive and will be redacted in logs
- WhatsApp numbers must use `whatsapp:` prefix (e.g., `whatsapp:+14155238886`)

## Configuration Examples

### Connection String

**Basic:**
```
AccountSid=AC123456;AuthToken=your_auth_token
```

**With Timeout:**
```
AccountSid=AC123456;AuthToken=your_auth_token;Timeout.Send=00:01:00
```

**Complete:**
```
AccountSid=AC123456;AuthToken=your_auth_token;
WebhookUrl=https://myapp.com/whatsapp;
Timeout.Send=00:01:00;Timeout.Receive=00:00:30;
Retry.MaxAttempts=3;Retry.BackoffType=Exponential;
Telemetry.EnableTracing=true;Telemetry.EnableMetrics=true
```

### From appsettings.json

```json
{
  "TwilioWhatsApp": {
    "AccountSid": "AC123456",
    "AuthToken": "your_auth_token",
    "Timeout": {
      "Send": "00:01:00",
      "Receive": "00:00:30"
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
    .AddConnector<TwilioWhatsAppConnector>(cfg => cfg
        .WithSettings("TwilioWhatsApp"));
```

## Number format

Phone numbers must use the `whatsapp:` prefix with E.164 format:

```csharp
new MessageBuilder()
    // Sender — must be a Twilio WhatsApp-enabled number
    .FromPhone("whatsapp:+14155238886")   // Twilio's default WhatsApp number
    // Recipient — must have opted in to receive messages from your business
    .ToPhone("whatsapp:+15550002222")
    .WithText("Hello")
    .Build();
```

## Schema

| Property | Value |
|---|---|
| Provider | `Twilio` |
| Type | `WhatsApp` |
| Version | `1.0.0` |
| Capabilities | `SendMessages`, `MessageStatusQuery`, `Templates` |
| Content types | `PlainText`, `Media`, `Template` |
| Endpoints | `PhoneNumber` (send + receive) |
| Authentication | Basic (`AccountSid` + `AuthToken`) |

## Send examples

### Text message

```csharp
var settings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC...")
    .SetParameter("AuthToken", "...");

var connector = new TwilioWhatsAppConnector(TwilioChannelSchemas.TwilioWhatsApp, settings);
await connector.InitializeAsync(ct);

var message = new MessageBuilder()
    .WithId("wa-1")
    .FromPhone("whatsapp:+14155238886")
    .ToPhone("whatsapp:+15550002222")
    .WithText("Hello from WhatsApp!")
    .Build();

var result = await connector.SendMessageAsync(message, ct);
```

### Template message (for business-initiated conversations)

WhatsApp requires approved templates for the first message to a user:

```csharp
var message = new MessageBuilder()
    .WithId("wa-template-1")
    .ToPhone("whatsapp:+15550002222")
    .WithContent(new TemplateContent("order_confirmed", new Dictionary<string, object?>
    {
        ["order_id"] = "ORD-123",
        ["delivery_date"] = "2026-05-15"
    }))
    .Build();
```

### Media message with caption

```csharp
new MessageBuilder()
    .FromPhone("whatsapp:+14155238886")
    .ToPhone("whatsapp:+15550002222")
    .WithContent(new MediaContent(MediaType.Image, "product.jpg",
        "https://example.com/product.jpg"))
    .WithProperty("caption", "Check out our new product!")
    .Build();
```

## Message properties

| Property | Type | Description |
|---|---|---|
| `caption` | `string` | Caption for media messages |
| `ValidityPeriod` | `int` | Message validity in seconds |

## Webhook handling

Similar to Twilio SMS — inbound messages arrive as form POST with `MessageSource.UrlPost()`:

```csharp
[HttpPost("/webhooks/twilio/whatsapp")]
public async Task<IActionResult> WhatsAppWebhook(CancellationToken ct)
{
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadAsStringAsync(ct);
    var source = MessageSource.UrlPost(body);

    var result = await _connector.ReceiveMessagesAsync(source, ct);
    return TwiML.Empty;
}
```

Validate `X-Twilio-Signature` header before processing.

## Error codes

Twilio-specific error codes are defined in `TwilioErrorCodes` with domain `"Twilio"`.

| Code | Description |
|---|---|
| `INVALID_CONNECTION_SETTINGS` | Connection settings validation failed |
| `MISSING_FROM_NUMBER` | Sender WhatsApp number is required |
| `INVALID_SENDER` | Sender number is not in valid `whatsapp:+E164` format |
| `INVALID_MESSAGE` | Message properties failed schema validation |
| `MISSING_CONTENT_SID` | WhatsApp template Content SID is missing |
| `INVALID_WHATSAPP_NUMBER` | WhatsApp number does not follow required format |
| `SEND_WHATSAPP_MESSAGE_FAILED` | WhatsApp message send failed via Twilio API |
| `WHATSAPP_STATUS_QUERY_FAILED` | WhatsApp message status query failed |
| `STATUS_QUERY_FAILED` | Message status query failed |
| `STATUS_ERROR` | Connector status retrieval failed |
| `RECEIVE_MESSAGE_FAILED` | Incoming message webhook processing failed |
| `RECEIVE_STATUS_FAILED` | Status callback webhook processing failed |

Standard `MessagingErrorCodes` are also used — see the [error codes reference](../connectors-implementation/result-types.md#error-code-tables).

### Original provider codes

Twilio API errors (`ApiException`) are mapped to framework error codes in `TwilioService.MapTwilioErrorCode()`:

| Twilio code | Mapped framework code |
|---|---|
| `21211` | `INVALID_RECIPIENT` |
| `21610` | `INVALID_RECIPIENT` |
| `21614` | `INVALID_SENDER` |
| `21408` | `INVALID_SENDER` |
| `20001` | `INVALID_MESSAGE` |
| Other | `SEND_MESSAGE_FAILED` |

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `INVALID_RECIPIENT` | Number without `whatsapp:` prefix | Use `whatsapp:+E164` format |
| Template rejected | Template not approved | Verify template status in Twilio Console |
| `RATE_LIMITED` | Too many messages | WhatsApp has per-conversation limits |
| No delivery | User hasn't opted in | User must send first message or accept invite |
| Media not sent | Unsupported format | Check WhatsApp media requirements (max 64MB, specific formats) |

## TwilioChannelSchemas

```csharp
// WhatsApp messaging schema
TwilioChannelSchemas.TwilioWhatsApp
```
