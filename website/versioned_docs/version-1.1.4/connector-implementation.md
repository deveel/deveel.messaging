---
sidebar_position: 10
---

# Connector Implementation

Not every messaging provider has a ready-made connector in the framework. When you need to integrate with a custom or niche provider — an internal notification gateway, a legacy SMTP server, a proprietary chat system — you build a connector. The framework provides `ChannelConnectorBase`, an abstract base class that implements the `IChannelConnector` contract and handles all cross-cutting concerns so you only write the provider-specific translation layer.

The design follows the Template Method pattern: the base class defines the skeleton of each operation (initialize, send, receive, check status) and calls into your overrides for the provider-specific steps. The base class handles:
- **State management** — tracks the lifecycle and prevents operations when not ready
- **Capability guards** — checks schema capabilities before delegating to your code
- **Message validation** — validates messages against the schema before your send logic runs
- **Authentication** — resolves credentials via the authentication manager
- **Error wrapping** — catches exceptions from your code and wraps them in `OperationResult<T>`
- **Logging scopes** — creates structured scopes for tracing

The result is that your override methods stay focused on one thing: translating between the framework's `IMessage` model and the provider's API.

Build custom connectors by extending `ChannelConnectorBase`. The base class provides state management, capability validation, authentication integration, message validation, logging scopes, and standardized error wrapping. You implement the provider-specific parts.

## Minimum implementation

Four abstract methods must be overridden:

```csharp
using Ratatosk;
using Microsoft.Extensions.Logging;

[ChannelSchema(typeof(MySchemaFactory))]
public class MyConnector : ChannelConnectorBase
{
    private HttpClient _httpClient;

    public MyConnector(
        IChannelSchema schema,
        ConnectionSettings? settings = null,
        ILogger? logger = null,
        IAuthenticationManager? authManager = null)
        : base(schema, settings, logger, authManager) { }

    // ── 1. Initialize ─────────────────────────────────────────
    // Validate settings, create provider client, authenticate.
    // Called by InitializeAsync().
    protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
    {
        var apiKey = ConnectionSettings.GetParameter("ApiKey");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("ApiKey is required");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        SetState(ConnectorState.Ready);
        return ValueTask.CompletedTask;
    }

    // ── 2. Test connection ────────────────────────────────────
    // Lightweight ping to verify the provider is reachable.
    // Called by TestConnectionAsync().
    protected override ValueTask TestConnectorConnectionAsync(CancellationToken ct)
    {
        // Throw on failure — base class wraps the exception
        return ValueTask.CompletedTask;
    }

    // ── 3. Send message ──────────────────────────────────────
    // Translate IMessage to the provider API and send.
    // Called by SendMessageAsync().
    protected override async Task<SendResult> SendMessageCoreAsync(
        IMessage message, CancellationToken ct)
    {
        var payload = new
        {
            to = message.Receiver?.Address,
            from = message.Sender?.Address,
            text = (message.Content as TextContent)?.Text
        };

        var response = await _httpClient.PostAsJsonAsync("/api/send", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(ct);

        return new SendResult
        {
            MessageId = message.Id,
            RemoteMessageId = result!.Id,
            Status = MessageStatus.Sent,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    // ── 4. Get status ────────────────────────────────────────
    // Return the current connector status.
    // Called by GetStatusAsync().
    protected override async Task<StatusInfo> GetConnectorStatusAsync(
        CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health", ct);
            return new StatusInfo(
                response.IsSuccessStatusCode ? "connected" : "degraded",
                null,
                DateTimeOffset.UtcNow);
        }
        catch
        {
            return new StatusInfo("disconnected", "Provider unreachable",
                DateTimeOffset.UtcNow);
        }
    }
}
```

### What the base class does

| Concern | Provided by base class |
|---|---|
| State management | Tracks `Uninitialized` → `Initializing` → `Ready` → ... |
| Capability guards | Throws `NotSupportedException` if capability not set |
| Message validation | Calls `ValidateMessage` before send |
| Error wrapping | Catches exceptions, wraps in `OperationResult<T>` |
| Authentication | Provides `AuthenticateAsync()`, `GetAuthenticationHeader()` |
| Logging scopes | Auto-creates scopes per connector and per message |
| Cancellation | Passes token to all operations |
| Retry support | Override `GetDefaultRetryPolicy()`; configurable via builder or connection settings (see [Retry Policies](connectors-configuration/retry-policies.md)) |
| Result wrapping | Your core methods return raw values; base class wraps them |

### How wrapping works

Your override returns a raw `SendResult` or `ValueTask`. The base class:

1. Validates connector state (`Ready`)
2. Validates capability (e.g., `SendMessages`)
3. Validates message against schema
4. Calls your override
5. Catches any exception
6. Wraps the result (or error) in `OperationResult<T>`

This means your override can throw on error — you never need to create `OperationResult<T>` instances yourself.

## Retry policy

Connectors can provide a default retry policy by overriding `GetDefaultRetryPolicy()`:

```csharp
protected override RetryPolicyOptions? GetDefaultRetryPolicy()
    => new RetryPolicyOptions
    {
        MaxRetryAttempts = 5,
        RetryableErrorCodes = { "RATE_LIMITED", "NETWORK_ERROR" }
    };
```

The policy configured via `WithRetryPolicy` or individual `RetrySettingsKeys.*` parameters in `ConnectionSettings` takes precedence. See [Retry Policies](connectors-configuration/retry-policies.md) for details.

## Optional overrides

Override only what your provider supports:

```csharp
// Bulk messaging (requires BulkMessaging capability)
protected override async Task<BatchSendResult> SendBatchCoreAsync(
    IMessageBatch batch, CancellationToken ct)
{
    var results = new Dictionary<string, SendResult>();
    foreach (var message in batch.Messages)
    {
        var sendResult = await SendMessageCoreAsync(message, ct);
        results[message.Id] = sendResult;
    }
    return new BatchSendResult
    {
        BatchId = batch.Id,
        MessageResults = results
    };
}

// Inbound message receiving (requires ReceiveMessages capability)
protected override async Task<ReceiveResult> ReceiveMessagesCoreAsync(
    MessageSource source, CancellationToken ct)
{
    var message = ParseInboundMessage(source);
    return new ReceiveResult
    {
        BatchId = Guid.NewGuid().ToString(),
        Messages = [message]
    };
}

// Status callback receiving (requires HandleMessageState capability)
protected override async Task<StatusUpdateResult> ReceiveMessageStatusCoreAsync(
    MessageSource source, CancellationToken ct)
{
    var update = ParseStatusUpdate(source);
    return new StatusUpdateResult
    {
        MessageId = update.MessageId,
        Status = update.Status,
        Timestamp = update.Timestamp
    };
}

// Delivery state query (requires MessageStatusQuery capability)
protected override async Task<StatusUpdatesResult> GetMessageStatusCoreAsync(
    string messageId, CancellationToken ct)
{
    var response = await _httpClient.GetAsync($"/api/status/{messageId}", ct);
    var data = await response.Content.ReadFromJsonAsync<StatusData>(ct);
    return new StatusUpdatesResult
    {
        MessageId = messageId,
        Updates = data!.Entries.Select(e => new StatusUpdateResult
        {
            MessageId = messageId,
            Status = e.Status,
            Timestamp = e.Timestamp
        }).ToList()
    };
}

// Custom health check (requires HealthCheck capability)
protected override async Task<ConnectorHealth> GetConnectorHealthAsync(
    CancellationToken ct)
{
    var status = await GetConnectorStatusAsync(ct);
    return new ConnectorHealth
    {
        State = State,
        IsHealthy = status.Status == "connected",
        LastHealthCheck = DateTime.UtcNow,
        Metrics = new Dictionary<string, object> { ["uptime"] = Environment.TickCount }
    };
}

// Clean shutdown
protected override Task ShutdownConnectorAsync(CancellationToken ct)
{
    _httpClient?.Dispose();
    return Task.CompletedTask;
}
```

## State management

A connector moves through a well-defined lifecycle: it starts uninitialized, initializes (authenticates and sets up resources), becomes ready for operations, and eventually shuts down. Errors can transition it to an error state. The base class enforces this lifecycle automatically — operations that require the `Ready` state return a failure result if called before initialization or after shutdown.

The base class tracks the connector's lifecycle state:

```csharp
// Transition states
SetState(ConnectorState.Ready);
SetState(ConnectorState.Error);
SetState(ConnectorState.Shutdown);

// Read current state (IChannelConnector.State)
if (connector.State == ConnectorState.Ready)
    // safe to send
```

States and transitions:

```
Uninitialized ──InitializeAsync()──▶ Initializing ──▶ Ready
                                        │                │            ──▶ Disconnected
                                        ▼                ▼
                                      Error          ShuttingDown
                                        │                │
                                        ▼                ▼
                                      Error            Shutdown
```

The `Disconnected` state represents a temporary loss of connectivity (transient, may recover). `Error` indicates an unrecoverable failure. `Shutdown` is terminal.

The base class prevents operations when not in `Ready` state. Calling `SendMessageAsync` on a connector that hasn't been initialized returns a failure result with `INVALID_STATE` error code.

## Authentication integration

The base class handles authentication automatically. During `InitializeAsync()`, before `InitializeConnectorAsync()` is called, the base class iterates through the schema's `AuthenticationConfigurations`, selects the first one that satisfies the provided `ConnectionSettings` (via `IsSatisfiedBy`), and calls `IAuthenticationManager.AuthenticateAsync()` to obtain a credential. The credential is then available in `InitializeConnectorAsync()` through the `AuthenticationCredential` property:

```csharp
protected override async ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    // AuthenticationCredential is already populated by the base class.
    // Access the value directly:
    var token = AuthenticationCredential?.Value;

    // Use helpers for common auth patterns:
    var authHeader = GetAuthenticationHeader();
    // Returns "Bearer <token>" or "Basic <base64>" depending on credential type

    var apiKey = GetApiKey();
    // Returns raw API key if credential is an ApiKey type

    // Store for later use
    _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
}
```

### When auto-authentication fails

If no auth configuration matches the connection settings, the base class logs a warning but does not prevent initialization. Your connector can handle this case:

```csharp
protected override async ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    if (AuthenticationCredential == null)
    {
        // Try manual authentication
        var result = await AuthenticateAsync(ct);
        if (!result.IsSuccess())
            throw new InvalidOperationException(
                "Unable to authenticate with the provided settings");
    }

    var token = AuthenticationCredential!.Value;
    // ...
}
```

### Schema auth configuration

Your connector's schema must declare what authentication it supports:

```csharp
// Via convenience method alias
.AddAuthenticationScheme(AuthenticationScheme.Bearer)

// Via explicit configuration (recommended)
.AddAuthenticationConfiguration(
    new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
        .WithField("ApiKey", DataType.String, f =>
        {
            f.AuthenticationRole = "principal";
            f.IsSensitive = true;
        }))
```

When using `AddAuthenticationConfiguration()`, fields with `AuthenticationRole = "principal"` are automatically registered as optional schema parameters.

See [Authentication](authentication.md) for the full authentication model.

## Error handling

Connector code inevitably deals with provider errors: HTTP 401, rate limiting, timeouts, malformed responses. The traditional approach is to catch every exception and convert it to a result type, which clutters the connector logic with error-handling boilerplate. `ChannelConnectorBase` takes a different approach: your core methods throw exceptions for error conditions, and the base class catches them and converts them into `OperationResult<T>.Fail()` automatically. This keeps the send/receive logic focused on the happy path.

Throw exceptions from your core methods — the base class catches them and converts them to `OperationResult<T>.Fail()`:

```csharp
protected override async Task<SendResult> SendMessageCoreAsync(
    IMessage message, CancellationToken ct)
{
    var response = await _httpClient.PostAsync(url, content, ct);

    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new ProviderException(
            "PROVIDER_ERROR",
            $"Provider returned {response.StatusCode}: {body}");
    }

    var result = await response.Content.ReadFromJsonAsync<ApiResult>(ct);
    return new SendResult { ... };
}
```

### Error conventions

Use `SCREAMING_SNAKE_CASE` error codes for consistency:

- `INVALID_CREDENTIALS` — authentication failed
- `RATE_LIMITED` — provider rate limit hit
- `NETWORK_ERROR` — connection timed out or refused
- `PROVIDER_VALIDATION_FAILED` — provider rejected the message format
- `MESSAGE_TOO_LARGE` — content exceeds provider limits

## Logging

The base class creates structured logging scopes:

```csharp
// Auto-generated scopes:
// "Connector: {Schema.ChannelProvider}/{Schema.ChannelType}"
// "Message: {message.Id}"

// Log in your connector:
Logger.LogInformation("Sending message to {Receiver}", message.Receiver?.Address);
// Output: [Connector: MyCo/MyChannel] [Message: msg-1] Sending message to +1555...
```

## DI registration

```csharp
// Simple registration — schema auto-discovered from [ChannelSchema] attribute
builder.Services
    .AddMessaging()
    .AddConnector<MyConnector>();

// Named registration
builder.Services
    .AddMessaging()
    .AddConnector<MyConnector>("primary")
    .AddConnector<MyConnector>("secondary", cfg => cfg
        .WithSettings("MyConnector:Secondary"));

// Registration with configuration
builder.Services
    .AddMessaging()
    .AddConnector<MyConnector>(cfg => cfg
        .WithConnectionString("ApiKey=...;Endpoint=https://...")
        .WithSchema(myCustomSchema)
        .WithFactory<MyCustomFactory>());
```

## The factory pattern

Connectors can also be created via `IChannelConnectorFactory<TConnector>`:

```csharp
public class MyFactory : IChannelConnectorFactory<MyConnector>
{
    public MyConnector Create(ConnectionSettings settings, IChannelSchema? schema)
    {
        // Custom construction logic
        return new MyConnector(schema ?? DefaultSchema, settings);
    }
}

// Register the factory
builder.Services.AddMessaging()
    .AddConnector<MyConnector>(cfg => cfg
        .WithFactory<MyFactory>());
```

The default factory uses `ActivatorUtilities.CreateInstance` — it resolves constructor parameters from DI if possible, falling back to the provided values.

## Full example: complete connector

```csharp
[ChannelSchema(typeof(SmsApiSchemaFactory))]
public class SmsApiConnector : ChannelConnectorBase
{
    private HttpClient _http;

    public SmsApiConnector(
        IChannelSchema schema,
        ConnectionSettings? settings = null,
        ILogger? logger = null,
        IAuthenticationManager? authManager = null)
        : base(schema, settings, logger, authManager) { }

    protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
    {
        _http = new HttpClient();
        _http.BaseAddress = new Uri(ConnectionSettings.GetParameter("Endpoint")
            ?? "https://api.sms-provider.com");

        var auth = AuthenticateAsync(ct).GetAwaiter().GetResult();
        if (auth.IsSuccess)
            _http.DefaultRequestHeaders.Add("Authorization", GetAuthenticationHeader());

        SetState(ConnectorState.Ready);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask TestConnectorConnectionAsync(CancellationToken ct)
    {
        // Ping health endpoint
        return ValueTask.CompletedTask;
    }

    protected override async Task<SendResult> SendMessageCoreAsync(
        IMessage message, CancellationToken ct)
    {
        var payload = new
        {
            to = message.Receiver?.Address,
            from = message.Sender?.Address,
            text = (message.Content as TextContent)?.Text
        };

        var response = await _http.PostAsJsonAsync("/messages", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SmsApiResponse>(ct);
        return new SendResult
        {
            MessageId = message.Id,
            RemoteMessageId = result!.MessageId,
            Status = MessageStatus.Sent,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    protected override async Task<StatusInfo> GetConnectorStatusAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            return new StatusInfo(
                response.IsSuccessStatusCode ? "connected" : "degraded",
                null, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            return new StatusInfo("disconnected", ex.Message, DateTimeOffset.UtcNow);
        }
    }

    protected override Task ShutdownConnectorAsync(CancellationToken ct)
    {
        _http?.Dispose();
        return Task.CompletedTask;
    }
}
```
