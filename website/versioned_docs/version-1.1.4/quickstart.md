---
sidebar_position: 4
---

# Quickstart

Every interaction with the framework follows the same lifecycle regardless of which channel provider you use:

1. **Register** — add the `Ratatosk` package and configure connectors in the DI container
2. **Build** — construct an `IMessage` using `MessageBuilder`
3. **Send/Receive** — call `IMessagingClient.SendAsync()` or `IMessagingClient.ReceiveAsync()`
4. **Handle** — check `.IsSuccess()`, read the result value, or inspect `.Error`

This guide walks through the `IMessagingClient` approach — the recommended pattern for all applications. Connectors are infrastructure: you register them in DI and never instantiate them directly.

## 1. Create a project

```bash
dotnet new console -o MessagingDemo
cd MessagingDemo
dotnet add package Ratatosk
dotnet add package Ratatosk.Twilio
```

## 2. Send an SMS

Register the connector in DI, inject `IMessagingClient`, build a message, and send it:

```csharp
// Program.cs
using Ratatosk;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>("sms", cfg => cfg
        .WithSettings("Twilio"))
    .AddClient();

builder.Services.AddSingleton<NotificationService>();

var app = builder.Build();
app.Run();

// NotificationService.cs
public class NotificationService(IMessagingClient messagingClient)
{
    public async Task<string?> SendSmsAsync(string to, string text)
    {
        var message = new MessageBuilder()
            .WithId(Guid.NewGuid().ToString("n"))
            .FromPhone("+15550001111")
            .ToPhone(to)
            .WithText(text)
            .Build();

        var result = await messagingClient.SendAsync("sms", message);

        if (result.IsSuccess)
            return result.Value?.RemoteMessageId;

        throw new InvalidOperationException(
            $"SMS failed: {result.Error?.Message}");
    }
}
```

The client handles connector resolution, lazy initialization, caching, and disposal. You never interact with `IChannelConnector` directly.

## 3. Same code, different channel

Add another connector and send through it using the same client — only the channel name and endpoint types change:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>("sms", cfg => cfg
        .WithSettings("Twilio"))
    .AddConnector<SendGridEmailConnector>("email", cfg => cfg
        .WithSettings("SendGrid"))
    .AddClient();

// In your service:
var smsResult = await client.SendAsync("sms", smsMessage);
var emailResult = await client.SendAsync("email", emailMessage);
```

The `MessageBuilder`, the `IMessagingClient` methods, and the `OperationResult<T>` pattern are identical across channels.

## 4. Receive inbound messages (webhook)

Channels like Twilio, Telegram, and Facebook push inbound messages via webhooks. The client's `ReceiveAsync` normalises the payload into `IMessage` objects:

```csharp
[HttpPost("/webhooks/twilio")]
public async Task<IActionResult> TwilioWebhook(CancellationToken ct)
{
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync(ct);
    var source = MessageSource.UrlPost(body);

    var result = await _client.ReceiveAsync("sms", source, ct);

    if (result.IsSuccess)
    {
        foreach (var message in result.Value?.Messages ?? [])
            Console.WriteLine($"Received: {message.Id} from {message.Sender}");
        return Ok();
    }

    return BadRequest(result.Error?.ErrorMessage);
}
```

Status callbacks (delivery receipts, read receipts) are handled through `ReceiveMessageStatusAsync` on the client, with the same `MessageSource` pattern.

## 5. Advanced resolution strategies

The `IMessagingClient` supports three resolution strategies that can be mixed in the same application.

### Resolution by name (named connectors)

Register multiple channels with distinct names and send through each by name:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>("sms-primary", cfg => cfg
        .WithSettings("Twilio:Primary"))
    .AddConnector<TwilioSmsConnector>("sms-fallback", cfg => cfg
        .WithSettings("Twilio:Fallback"))
    .AddConnector<SendGridEmailConnector>("email", cfg => cfg
        .WithSettings("SendGrid"))
    .AddClient();

// Usage
var result = await client.SendAsync("sms-primary", message);
if (result.IsFailure)
    result = await client.SendAsync("sms-fallback", message);
```

### Resolution by type (anonymous connectors)

When you register a single unnamed connector, resolve it through the generic overload:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg.WithSettings("Twilio"))
    .AddClient();

var result = await client.SendAsync<TwilioSmsConnector>(message);
```

### Runtime resolution

For applications where connection settings are loaded at runtime (from a database, API, or external configuration), register the connector type at startup without providing settings, and supply the settings at the call site:

```csharp
// Program.cs — register the type, no settings
builder.Services
    .AddMessaging()
    .AddConnectorType<FacebookMessengerConnector>("facebook")
    .AddClient();

// In a request handler — load settings at runtime
var runtimeSettings = new ConnectionSettings()
    .SetParameter("PageAccessToken", accessToken)
    .SetParameter("PageId", pageId);

var result = await client.SendAsync("facebook", runtimeSettings, message);
```

The same pattern works with type parameters for anonymous runtime resolution:

```csharp
builder.Services
    .AddMessaging()
    .AddConnectorType<FacebookMessengerConnector>()
    .AddClient();

await client.SendAsync<FacebookMessengerConnector>(runtimeSettings, message);
```

### Auto-initialization

The client automatically initializes the connector on first use. To disable this (for scenarios where initialization is handled externally), configure `MessagingClientOptions`:

```csharp
builder.AddClient(o => o.AutoInitialize = false);
```

## Next steps

- [Message model](messaging-model.md) — all content types, advanced builder methods, message properties
- [Sender management](senders/sender-management.md) - sender identity lifecycle, storage, and governance
- [Sender resolution](senders/sender-resolution.md) - send-time lookup, fallback, and cache behavior
- [Channel schema](channel-schema.md) — defining connector contracts and validation rules
- [Connector guides](connectors/README.md) — per-provider connection parameters and features
