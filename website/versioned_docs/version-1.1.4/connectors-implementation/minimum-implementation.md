# Minimum Connector Implementation

This guide shows you how to build a working connector with the minimum required code. By the end, you'll have a connector that can send messages through a custom provider.

## Four Required Methods

Every connector must override these four abstract methods from `ChannelConnectorBase`:

1. **`InitializeConnectorAsync()`** - Initialize the connector
2. **`TestConnectorConnectionAsync()`** - Test connectivity
3. **`SendMessageCoreAsync()`** - Send a message
4. **`GetConnectorStatusAsync()`** - Get connector status

## Complete Example

Here's a complete minimal connector for a hypothetical REST API:

```csharp
using Ratatosk;
using Microsoft.Extensions.Logging;

[ChannelSchema(typeof(MySchemaFactory))]
public class MyRestConnector : ChannelConnectorBase
{
    private HttpClient _httpClient;
    private readonly string _baseUrl;

    public MyRestConnector(
        IChannelSchema schema,
        ConnectionSettings? settings = null,
        ILogger? logger = null,
        IAuthenticationManager? authManager = null)
        : base(schema, settings, logger, authManager)
    {
        _baseUrl = ConnectionSettings.GetParameter<string>("BaseUrl") 
            ?? "https://api.example.com";
    }

    // ── 1. Initialize ─────────────────────────────────────────
    // Validate settings, create HTTP client, authenticate
    protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
    {
        // Validate required settings
        var apiKey = ConnectionSettings.GetParameter<string>("ApiKey");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("ApiKey is required");

        // Create HTTP client
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };

        // Set authentication header
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Mark connector as ready
        SetState(ConnectorState.Ready);

        Logger.LogConnectorInitialized();
        return ValueTask.CompletedTask;
    }

    // ── 2. Test Connection ───────────────────────────────────
    // Lightweight ping to verify provider is reachable
    protected override async ValueTask TestConnectorConnectionAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/ping", ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new ConnectorException(
                "CONNECTION_FAILED", 
                $"Cannot connect to provider: {ex.Message}", 
                ex);
        }
    }

    // ── 3. Send Message ──────────────────────────────────────
    // Translate IMessage to provider API and send
    protected override async Task<SendResult> SendMessageCoreAsync(
        IMessage message, CancellationToken ct)
    {
        // Build provider-specific payload
        var payload = new
        {
            to = message.Receiver?.Address,
            from = message.Sender?.Address,
            content = ExtractContent(message.Content),
            priority = message.Priority
        };

        // Send to provider API
        var response = await _httpClient.PostAsJsonAsync("/messages/send", payload, ct);
        
        // Handle errors
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new ConnectorException(
                "SEND_FAILED",
                $"Provider returned error: {error}");
        }

        // Parse response
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(ct);

        // Return standardized result
        return new SendResult(message.Id, result.MessageId)
        {
            Status = MessageStatus.Sent,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    // ── 4. Get Status ────────────────────────────────────────
    // Return current connector status
    protected override Task<StatusInfo> GetConnectorStatusAsync(CancellationToken ct)
    {
        var status = new StatusInfo(
            State == ConnectorState.Ready ? "Ready" : "Not Ready",
            description: $"Connector state: {State}",
            timestamp: DateTimeOffset.UtcNow);

        status.AdditionalData["BaseUrl"] = _baseUrl;
        status.AdditionalData["State"] = State.ToString();

        return Task.FromResult(status);
    }

    // ── Helper Methods ───────────────────────────────────────
    
    private string ExtractContent(IMessageContent content)
    {
        return content switch
        {
            TextContent text => text.Text,
            HtmlContent html => html.Html,
            _ => content?.ToString() ?? string.Empty
        };
    }

    // ── Response Model ───────────────────────────────────────
    
    private class ApiResponse
    {
        public string MessageId { get; set; }
        public string Status { get; set; }
    }
}

// ── Schema Factory ───────────────────────────────────────────

public class MySchemaFactory : IChannelSchemaFactory
{
    public IChannelSchema Create()
    {
        return new ChannelSchemaBuilder("MyProvider", "REST", "1.0.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .AddAuthenticationScheme(AuthenticationScheme.Bearer)
            .AddParameter("BaseUrl", DataType.String, p => 
            {
                p.IsRequired = true;
                p.Description = "Base URL of the REST API";
            })
            .AddParameter("ApiKey", DataType.String, p => 
            {
                p.IsRequired = true;
                p.IsSensitive = true;
                p.Description = "API key for authentication";
            })
            .HandlesMessageEndpoint(EndpointType.Id, e => 
            {
                e.CanSend = true;
                e.CanReceive = false;
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Html)
            .Build();
    }
}
```

## Method Details

### 1. InitializeConnectorAsync

**Purpose:** Prepare the connector for use

**What to do:**
- ✅ Validate required connection settings
- ✅ Create provider client (HTTP client, SDK client, etc.)
- ✅ Authenticate if needed (or let base class handle it)
- ✅ Call `SetState(ConnectorState.Ready)` when ready

**What NOT to do:**
- ❌ Don't call base class implementation
- ❌ Don't make external API calls (use `TestConnectionAsync` for that)
- ❌ Don't swallow exceptions - let them propagate

**Example:**
```csharp
protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    // Validate settings
    var apiKey = ConnectionSettings.GetParameter<string>("ApiKey");
    if (string.IsNullOrEmpty(apiKey))
        throw new InvalidOperationException("ApiKey is required");

    // Create client
    _client = new ProviderClient(apiKey);
    
    // Mark as ready
    SetState(ConnectorState.Ready);
    
    return ValueTask.CompletedTask;
}
```

### 2. TestConnectorConnectionAsync

**Purpose:** Verify connectivity to provider

**What to do:**
- ✅ Make a lightweight API call (ping, health check)
- ✅ Throw `ConnectorException` on failure
- ✅ Return `ValueTask.CompletedTask` on success

**What NOT to do:**
- ❌ Don't send actual messages
- ❌ Don't return failure results - throw exceptions
- ❌ Don't perform expensive operations

**Example:**
```csharp
protected override async ValueTask TestConnectorConnectionAsync(CancellationToken ct)
{
    try
    {
        var response = await _httpClient.GetAsync("/health", ct);
        response.EnsureSuccessStatusCode();
    }
    catch (HttpRequestException ex)
    {
        throw new ConnectorException(
            "CONNECTION_FAILED",
            $"Cannot connect: {ex.Message}",
            ex);
    }
}
```

### 3. SendMessageCoreAsync

**Purpose:** Send a message through the provider

**What to do:**
- ✅ Translate `IMessage` to provider format
- ✅ Call provider API
- ✅ Handle errors by throwing `ConnectorException`
- ✅ Return `SendResult` with message ID

**What NOT to do:**
- ❌ Don't validate the message (base class does this)
- ❌ Don't catch and swallow exceptions
- ❌ Don't return null

**Example:**
```csharp
protected override async Task<SendResult> SendMessageCoreAsync(
    IMessage message, CancellationToken ct)
{
    // Translate to provider format
    var payload = BuildProviderPayload(message);
    
    // Send to API
    var response = await _httpClient.PostAsJsonAsync("/send", payload, ct);
    
    // Handle errors
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync(ct);
        throw new ConnectorException("SEND_FAILED", error);
    }
    
    // Parse and return result
    var result = await response.Content.ReadFromJsonAsync<ApiResponse>(ct);
    return new SendResult(message.Id, result.Id);
}
```

### 4. GetConnectorStatusAsync

**Purpose:** Return current connector status

**What to do:**
- ✅ Return meaningful status information
- ✅ Include current state
- ✅ Add diagnostic data in `AdditionalData`

**Example:**
```csharp
protected override Task<StatusInfo> GetConnectorStatusAsync(CancellationToken ct)
{
    var status = new StatusInfo(
        State == ConnectorState.Ready ? "Ready" : "Not Ready",
        description: $"State: {State}",
        timestamp: DateTimeOffset.UtcNow);

    status.AdditionalData["State"] = State.ToString();
    status.AdditionalData["InitializedAt"] = _initializedAt;

    return Task.FromResult(status);
}
```

## State Management

Use `SetState()` to track connector lifecycle:

```csharp
protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    // Validate and setup...
    
    SetState(ConnectorState.Ready);  // ← Mark as ready
    return ValueTask.CompletedTask;
}
```

**Available states:**
- `Uninitialized` - Initial state
- `Initializing` - Currently initializing
- `Ready` - Ready for operations
- `Error` - Error occurred
- `ShuttingDown` - Shutdown in progress
- `Shutdown` - Shut down

## Error Handling

Throw `ConnectorException` for provider-specific errors:

```csharp
if (!response.IsSuccessStatusCode)
{
    throw new ConnectorException(
        "SEND_FAILED",                    // Error code
        "Provider returned 500",          // Human-readable message
        innerException);                  // Optional inner exception
```

The base class automatically wraps exceptions in `OperationResult<T>.Fail()`.

## Connection Settings

Access configuration via `ConnectionSettings`:

```csharp
var apiKey = ConnectionSettings.GetParameter<string>("ApiKey");
var baseUrl = ConnectionSettings.GetParameter<string>("BaseUrl");
var maxRetries = ConnectionSettings.GetParameter<int>("MaxRetries");
```

## Schema Declaration

Use the `[ChannelSchema]` attribute to declare your schema factory:

```csharp
[ChannelSchema(typeof(MySchemaFactory))]
public class MyConnector : ChannelConnectorBase
{
    // ...
}
```

## Registration

Register your connector in DI:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<MyRestConnector>(cfg => cfg
        .WithSetting("BaseUrl", "https://api.example.com")
        .WithSetting("ApiKey", "key-123"));
```

## Testing

Test your connector without hitting the provider:

```csharp
[Fact]
public async Task SendMessageCoreAsync_ReturnsSendResult()
{
    var schema = CreateTestSchema();
    var settings = new ConnectionSettings()
        .SetParameter("ApiKey", "test-key");
    
    var connector = new MyRestConnector(schema, settings);
    await connector.InitializeAsync(CancellationToken.None);
    
    var message = new MessageBuilder()
        .WithId("test-1")
        .To(Endpoint.Id("recipient"))
        .WithText("Hello")
        .Build();
    
    var result = await connector.SendMessageAsync(message, CancellationToken.None);
    
    Assert.True(result.IsSuccess());
    Assert.NotNull(result.Value);
}
```

## Next Steps

- **Add authentication**: [Authentication Guide](authentication.md)
- **Custom validation**: [Message Validation](message-validation.md)
- **Advanced features**: [Advanced Topics](advanced-topics.md)
