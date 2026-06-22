# Connector Implementation Overview

When you need to integrate with a messaging provider that doesn't have a built-in connector, you build a custom connector. This guide explains the architecture and what the framework provides.

## What is a Connector?

A connector is a class that implements the `IChannelConnector` interface, translating between the framework's unified `IMessage` model and a provider's specific API.

## When to Build a Custom Connector

Build a custom connector when:
- ✅ You need to integrate with a proprietary or internal messaging system
- ✅ A provider doesn't have an existing connector in the framework
- ✅ You need custom authentication or message translation logic
- ✅ You want to wrap legacy systems with the framework's unified interface

## Architecture: Template Method Pattern

The framework uses the **Template Method pattern**:

```
ChannelConnectorBase (abstract base class)
  ├── Defines operation skeletons (initialize, send, receive, status)
  ├── Handles cross-cutting concerns
  └── Calls your overrides for provider-specific logic

Your Connector (concrete implementation)
  ├── Implements provider-specific initialization
  ├── Translates IMessage to provider API format
  ├── Makes API calls and parses responses
  └── Handles connection testing
```

## What the Base Class Provides

`ChannelConnectorBase` handles all cross-cutting concerns so you focus on provider-specific logic:

### ✅ State Management
Tracks connector lifecycle and prevents operations when not ready:

```csharp
// Base class prevents operations if not in Ready state
if (State != ConnectorState.Ready)
    throw new InvalidOperationException("Connector not ready");
```

### ✅ Capability Validation
Checks schema capabilities before delegating to your code:

```csharp
// Base class validates capability before calling your send method
ValidateCapability(ChannelCapability.SendMessages);
```

### ✅ Message Validation
Validates messages against the schema before your send logic runs:

```csharp
// Base class validates message before calling SendMessageCoreAsync
await foreach (var result in ValidateMessageAsync(message, ct))
{
    if (result != ValidationResult.Success)
        return OperationResult<SendResult>.ValidationFailed(...);
}
```

### ✅ Authentication
Resolves credentials via the authentication manager:

```csharp
// Base class auto-authenticates during InitializeAsync()
// Credential is available via AuthenticationCredential property
var token = AuthenticationCredential?.Value;
```

### ✅ Error Wrapping
Catches exceptions from your code and wraps them in `OperationResult<T>`:

```csharp
try
{
    // Your code throws exception
    throw new HttpRequestException("Network error");
}
catch (Exception ex)
{
    // Base class wraps as OperationResult.Fail()
    return OperationResult<SendResult>.Fail("HTTP_ERROR", ex.Message);
}
```

### ✅ Logging Scopes
Creates structured logging scopes for tracing:

```csharp
using var scope = BeginConnectorLoggerScope();
// Logs include: [Provider/Type v1.0.0]

using var messageScope = BeginMessageLoggerScope(message);
// Logs include: [MessageId: msg-123]
```

## What You Implement

You implement **provider-specific translation logic** in override methods:

### Required Overrides (4 methods)

1. **`InitializeConnectorAsync()`** - Validate settings, create client, authenticate
2. **`TestConnectorConnectionAsync()`** - Lightweight connectivity test
3. **`SendMessageCoreAsync()`** - Translate `IMessage` to provider API and send
4. **`GetConnectorStatusAsync()`** - Return current connector status

### Optional Overrides

- `ReceiveMessagesCoreAsync()` - Receive inbound messages
- `GetMessageStatusCoreAsync()` - Query message delivery status
- `GetConnectorHealthAsync()` - Health check implementation
- `SendBatchCoreAsync()` - Batch message sending
- `GetSendTimeout()`, `GetReceiveTimeout()` - Custom timeout values

## Minimal Example

```csharp
[ChannelSchema(typeof(MySchemaFactory))]
public class MyConnector : ChannelConnectorBase
{
    private HttpClient _httpClient;

    public MyConnector(IChannelSchema schema, ConnectionSettings? settings = null, 
        ILogger? logger = null, IAuthenticationManager? authManager = null)
        : base(schema, settings, logger, authManager) { }

    // 1. Initialize
    protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
    {
        var apiKey = ConnectionSettings.GetParameter<string>("ApiKey");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        SetState(ConnectorState.Ready);
        return ValueTask.CompletedTask;
    }

    // 2. Test connection
    protected override ValueTask TestConnectorConnectionAsync(CancellationToken ct)
    {
        // Throw on failure - base class wraps exception
        return ValueTask.CompletedTask;
    }

    // 3. Send message
    protected override async Task<SendResult> SendMessageCoreAsync(
        IMessage message, CancellationToken ct)
    {
        var payload = new
        {
            to = message.Receiver?.Address,
            text = (message.Content as TextContent)?.Text
        };

        var response = await _httpClient.PostAsJsonAsync("/api/send", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(ct);

        return new SendResult(message.Id, result.Id);
    }

    // 4. Get status
    protected override Task<StatusInfo> GetConnectorStatusAsync(CancellationToken ct)
    {
        return Task.FromResult(new StatusInfo("OK"));
    }
}
```

## Getting Started

1. **Read [Minimum Implementation](minimum-implementation.md)** - Build a working connector
2. **Review [Authentication](authentication.md)** - Configure authentication
3. **Study [Message Validation](message-validation.md)** - Add custom validation
4. **Explore [Advanced Topics](advanced-topics.md)** - Receive, status, batch, testing

## Next Steps

- **Build your first connector**: [Minimum Implementation Guide](minimum-implementation.md)
- **Understand authentication**: [Authentication in Custom Connectors](authentication.md)
- **Add validation**: [Message Validation](message-validation.md)
- **Advanced features**: [Advanced Topics](advanced-topics.md)
