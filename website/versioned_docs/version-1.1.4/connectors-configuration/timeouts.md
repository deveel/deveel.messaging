# Connector Timeouts

Timeouts prevent connector operations from blocking indefinitely when providers are slow or unresponsive. The framework provides per-operation timeout configuration with automatic cancellation and error handling.

## Timeout Types

The framework supports three operation-specific timeouts:

| Timeout | Default | Operation |
|---------|---------|-----------|
| `SendTimeout` | 60 seconds | `SendMessageAsync()` |
| `ReceiveTimeout` | 60 seconds | `ReceiveMessagesAsync()` |
| `StatusQueryTimeout` | 30 seconds | `GetMessageStatusAsync()` |

**Note:** Initialize and health check operations do not have configurable timeouts - they use the connector's default cancellation behavior.

## Configuration Methods

### Fluent API (Recommended)

Configure timeouts during connector registration:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithTimeout(t => t
            .WithSendTimeout(TimeSpan.FromSeconds(60))
            .WithReceiveTimeout(TimeSpan.FromSeconds(30))
            .WithStatusQueryTimeout(TimeSpan.FromSeconds(15))));
```

### Connection Strings

```
AccountSid=AC123;AuthToken=secret;Timeout.Send=00:01:00;Timeout.Receive=00:00:30
```

### appsettings.json

```json
{
  "Twilio": {
    "AccountSid": "AC123",
    "AuthToken": "secret",
    "Timeout": {
      "Send": "00:01:00",
      "Receive": "00:00:30",
      "StatusQuery": "00:00:15"
    }
  }
}
```

### Typed Options

```csharp
var options = new TwilioSmsOptions
{
    AccountSid = "AC123",
    AuthToken = "secret",
    SendTimeout = TimeSpan.FromSeconds(60),
    ReceiveTimeout = TimeSpan.FromSeconds(30)
};

builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithOptions(options));
```

## Timeout Behavior

When an operation exceeds its configured timeout:

1. **Cancellation token is triggered** - The underlying HTTP request is cancelled
2. **Operation returns failure** - `OperationResult<T>.Fail()` with timeout error code
3. **No exception thrown** - Timeouts are handled gracefully at the connector level
4. **Error is logged** - Structured log with operation type and timeout duration

### Error Codes

| Error Code | Operation | Description |
|------------|-----------|-------------|
| `SEND_TIMEOUT` | Send | Send operation exceeded configured timeout |
| `RECEIVE_TIMEOUT` | Receive | Receive operation exceeded configured timeout |
| `STATUS_QUERY_TIMEOUT` | Status Query | Status query exceeded configured timeout |

## Integration with Retry Policies

By default, timeout errors are **retriable**. Configure retry behavior:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithTimeout(t => t
            .WithSendTimeout(TimeSpan.FromSeconds(30))
            .WithRetryOnTimeout(true))  // Retry on timeout (default)
        .WithRetryPolicy(r => r
            .WithMaxAttempts(3)
            .WithExponentialBackoff()));
```

To disable retry on timeout:

```csharp
.WithTimeout(t => t
    .WithRetryOnTimeout(false))  // Don't retry timeouts
```

## Connector-Specific Examples

### Twilio SMS

```csharp
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(60))
    .WithReceiveTimeout(TimeSpan.FromSeconds(30)))
```

### SendGrid Email

```csharp
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(90)))  // Email can take longer
```

### Firebase Push

```csharp
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(30)))  // Push is usually fast
```

### Facebook Messenger

```csharp
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(45))
    .WithReceiveTimeout(TimeSpan.FromSeconds(30)))
```

### Telegram Bot

```csharp
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(30))
    .WithReceiveTimeout(TimeSpan.FromSeconds(30)))
```

## Custom Connector Implementation

When building custom connectors, use the base class timeout methods:

```csharp
public class MyConnector : ChannelConnectorBase
{
    protected override async Task<SendResult> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        // Timeout is automatically applied by the base class
        // Use the provided cancellationToken - it's already linked to timeout
        
        var response = await _httpClient.PostAsync(..., cancellationToken);
        
        // If timeout occurs, cancellationToken will be cancelled
        // and base class will handle the timeout error
    }
}
```

### Override Timeout Values

Custom connectors can override timeout values:

```csharp
protected override TimeSpan GetSendTimeout()
{
    // Use base timeout plus extra time for this specific provider
    return base.GetSendTimeout() + TimeSpan.FromSeconds(30);
}

protected override bool ShouldRetryOnTimeout()
{
    // This provider's timeouts should not be retried
    return false;
}
```

## Best Practices

### ✅ DO: Set Appropriate Timeouts

- **Fast operations** (push notifications): 15-30 seconds
- **Standard operations** (SMS, chat): 30-60 seconds
- **Slow operations** (email with attachments): 60-120 seconds

### ✅ DO: Enable Retry on Timeout

Transient network issues often resolve quickly:

```csharp
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(30))
    .WithRetryOnTimeout(true))
.WithRetryPolicy(r => r
    .WithMaxAttempts(3))
```

### ❌ DON'T: Set Timeouts Too Short

Avoid frustrating users with premature timeouts:

```csharp
// ❌ Bad - 5 seconds is too short for most operations
.WithSendTimeout(TimeSpan.FromSeconds(5))

// ✅ Good - 60 seconds allows for network latency
.WithSendTimeout(TimeSpan.FromSeconds(60))
```

### ❌ DON'T: Disable Timeouts

Never use `TimeSpan.MaxValue` or `TimeSpan.Zero` to disable timeouts - this can cause hanging operations:

```csharp
// ❌ Bad - operation could hang indefinitely
.WithSendTimeout(TimeSpan.MaxValue)

// ✅ Good - use reasonable timeout
.WithSendTimeout(TimeSpan.FromMinutes(2))
```

## Troubleshooting

### Frequent Timeouts

**Symptoms:** Operations frequently fail with timeout errors

**Solutions:**
1. **Increase timeout** - Current value may be too aggressive
2. **Check network latency** - Provider may be geographically distant
3. **Review provider status** - Provider may be experiencing issues
4. **Enable retry** - Transient issues resolve with retry

```csharp
// Increase timeout and enable retry
.WithTimeout(t => t
    .WithSendTimeout(TimeSpan.FromSeconds(120))
    .WithRetryOnTimeout(true))
```

### Timeout Not Triggering

**Symptoms:** Operations run longer than configured timeout

**Causes:**
- Connector doesn't use the provided `CancellationToken`
- Custom connector bypasses base class timeout handling

**Solution:** Ensure custom connectors respect the cancellation token:

```csharp
// ✅ Correct - token is passed to async operations
await _httpClient.PostAsync(url, content, cancellationToken);

// ❌ Wrong - token is ignored
await _httpClient.PostAsync(url, content);  // No cancellation!
```

## See Also

- [Retry Policies](retry-policies.md) - Configure automatic retry for transient failures
- [Connection Strings](connection-strings.md) - Timeout configuration in connection strings
- [Health Checks](health-checks.md) - Monitor connector operational status
