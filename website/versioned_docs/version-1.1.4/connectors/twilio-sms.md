---
sidebar_position: 1
---

# Twilio SMS Connector

Send and receive SMS (and optional MMS with media attachments) via Twilio's Programmable SMS API.

## Package

```bash
dotnet add package Ratatosk.Twilio
```

## Configuration Settings

| Parameter | Type | Required | Is Secret | Default | Description |
|-----------|------|----------|-----------|---------|-------------|
| `AccountSid` | `string` | **Yes** | No | — | Twilio account SID (starts with `AC`) |
| `AuthToken` | `string` | **Yes** | **Yes** | — | Twilio authentication token |
| `MessagingServiceSid` | `string` | No | No | — | Messaging Service SID for advanced routing |
| `WebhookUrl` | `string` | No | No | — | URL for inbound SMS and status callbacks |
| `StatusCallback` | `string` | No | No | — | URL for delivery status callbacks |
| `ValidityPeriod` | `int` | No | No | `14400` | Message validity in seconds (4 hours) |
| `MaxPrice` | `decimal` | No | No | — | Maximum price per message |
| `SmartEncoded` | `bool` | No | No | `false` | Optimize for GSM-7 charset |

**Notes:**
- Either `AccountSid` + `AuthToken` OR `MessagingServiceSid` must be provided
- `AuthToken` is marked as sensitive and will be redacted in logs
- `ValidityPeriod` default is 14400 seconds (4 hours) per Twilio defaults

## Schema

| Property | Value |
|---|---|
| Provider | `Twilio` |
| Type | `SMS` |
| Version | `1.0.0` |
| Capabilities | `SendMessages`, `MessageStatusQuery` |
| Content types | `PlainText`, `Media` |
| Endpoints | `PhoneNumber` (send + receive) |
| Authentication | Basic (`AccountSid` + `AuthToken`) |
| Parameters | 2 required, 5 optional |

## Send examples

### Text SMS

```csharp
var settings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC...")
    .SetParameter("AuthToken", "...");

var connector = new TwilioSmsConnector(TwilioChannelSchemas.SimpleSms, settings);
await connector.InitializeAsync(ct);

var message = new MessageBuilder()
    .WithId("sms-1")
    .FromPhone("+15550001111")
    .ToPhone("+15550002222")
    .WithText("Hello from Twilio SMS")
    .Build();

var result = await connector.SendMessageAsync(message, ct);

if (result.IsSuccess)
{
    Console.WriteLine($"Sent! Twilio SID: {result.Data?.RemoteMessageId}");
    Console.WriteLine($"Status: {result.Data?.Status}");
}
```

### SMS with media (MMS)

```csharp
var message = new MessageBuilder()
    .WithId("mms-1")
    .FromPhone("+15550001111")
    .ToPhone("+15550002222")
    .WithContent(new MediaContent(MediaType.Image, "photo.jpg",
        "https://example.com/photo.jpg"))
    .Build();
```

### SMS with custom validity

```csharp
new MessageBuilder()
    .FromPhone("+15550001111")
    .ToPhone("+15550002222")
    .WithText("Time-sensitive message")
    .WithValidityPeriod(300)     // 5 minutes
    .WithMaxPrice(0.01)          // $0.01 max
    .Build();
```

## Configuration Examples

### Connection String

**Basic:**
```
AccountSid=AC123456;AuthToken=your_auth_token
```

**With Webhook:**
```
AccountSid=AC123456;AuthToken=your_auth_token;WebhookUrl=https://myapp.com/twilio
```

**With Timeout Configuration:**
```
AccountSid=AC123456;AuthToken=your_auth_token;Timeout.Send=00:01:00;Timeout.Receive=00:00:30
```

**With Retry Policy:**
```
AccountSid=AC123456;AuthToken=your_auth_token;Retry.MaxAttempts=3;Retry.BackoffType=Exponential;Retry.UseJitter=true
```

**Complete Configuration:**
```
AccountSid=AC123456;AuthToken=your_auth_token;
WebhookUrl=https://myapp.com/twilio;
Timeout.Send=00:01:00;Timeout.Receive=00:00:30;Timeout.StatusQuery=00:00:15;Timeout.RetryOnTimeout=true;
Retry.MaxAttempts=3;Retry.BaseDelay=00:00:01;Retry.BackoffType=Exponential;
Telemetry.EnableTracing=true;Telemetry.EnableMetrics=true
```

### From appsettings.json

```json
{
  "Twilio": {
    "AccountSid": "AC123456",
    "AuthToken": "your_auth_token",
    "WebhookUrl": "https://myapp.com/twilio",
    "Timeout": {
      "Send": "00:01:00",
      "Receive": "00:00:30",
      "StatusQuery": "00:00:15",
      "RetryOnTimeout": true
    },
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelay": "00:00:01",
      "BackoffType": "Exponential",
      "UseJitter": true,
      "RetryableErrorCodes": "RATE_LIMITED,SERVICE_UNAVAILABLE"
    },
    "Telemetry": {
      "EnableTracing": true,
      "EnableMetrics": true
    }
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithSettings("Twilio"));
```

### Typed Options

```csharp
var options = new TwilioSmsOptions
{
    AccountSid = "AC123456",
    AuthToken = "your_auth_token",
    WebhookUrl = "https://myapp.com/twilio",
    SendTimeout = TimeSpan.FromSeconds(60),
    ReceiveTimeout = TimeSpan.FromSeconds(30)
};

builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithOptions(options));
```

## Message properties

| Property | Type | Description |
|---|---|---|
| `ValidityPeriod` | `int` | Time in seconds the message is valid |
| `MaxPrice` | `decimal` | Maximum price to pay for the message |
| `ProvideCallback` | `bool` | Request a delivery callback |
| `SmartEncoded` | `bool` | Optimize for GSM-7 charset |

## Webhook handling

Twilio sends inbound messages and status callbacks as form POST requests. The `MessageSource.UrlPost()` factory handles parsing:

```csharp
[HttpPost("/webhooks/twilio/sms")]
public async Task<IActionResult> TwilioWebhook(CancellationToken ct)
{
    // Read the raw form body into a MessageSource
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();
    var source = MessageSource.UrlPost(body);

    // Process the status update
    var statusResult = await _connector.ReceiveMessageStatusAsync(source, ct);

    // Or process an inbound message
    var messageResult = await _connector.ReceiveMessagesAsync(source, ct);

    return TwiML.Empty;  // Return TwiML response
}
```

**Important**: validate `X-Twilio-Signature` before processing. See [Twilio security docs](https://www.twilio.com/docs/security).

## Error codes

Twilio-specific error codes are defined in `TwilioErrorCodes` with domain `"Twilio"`.

| Code | Description |
|---|---|
| `INVALID_CONNECTION_SETTINGS` | Connection settings validation failed |
| `MISSING_FROM_NUMBER` | Sender phone number is required when MessagingServiceSid is not configured |
| `INVALID_SENDER` | Sender phone number is not in valid format |
| `INVALID_MESSAGE` | Message properties failed schema validation |
| `MISSING_CONTENT_SID` | WhatsApp template Content SID is missing |
| `INVALID_WHATSAPP_NUMBER` | WhatsApp number does not follow `whatsapp:+E164` format |
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
| `INVALID_CREDENTIALS` | Wrong AccountSid/AuthToken | Check credentials in Twilio Console |
| `INVALID_RECIPIENT` | Phone number not in E.164 | Use `+` prefix and country code |
| `RATE_LIMITED` | Too many requests per second | Add delay between sends |
| `MESSAGE_TOO_LARGE` | SMS > 1600 characters | Split into multiple messages |
| Delivery not received | Recipient phone number issues | Check Twilio Console logs |
| Status callback not received | Webhook URL not reachable | Check callback URL and network |

## TwilioChannelSchemas

The connector ships with a predefined schema:

```csharp
// Simple SMS schema (send only)
TwilioChannelSchemas.SimpleSms
```
