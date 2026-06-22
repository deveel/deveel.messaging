---
sidebar_position: 12
---

# Retry Policies

When a connector sends a message, transient failures can happen: rate limits, network timeouts, or temporary provider outages. The retry policy system lets you control how `ChannelConnectorBase` retries failed send operations with configurable backoff, jitter, and optional circuit breaking.

Retry is **opt-in** by default — no retries happen unless you configure a policy.

## How it works

The base class wraps `SendMessageCoreAsync()` with a Polly resilience pipeline when a retry policy is configured. The pipeline uses `Microsoft.Extensions.Resilience` (based on Polly v8) and respects your configured error codes, backoff strategy, and jitter settings.

Only errors whose code appears in `RetryableErrorCodes` trigger a retry. All other errors (including non-`ConnectorException` exceptions) are classified as non-retryable and fail immediately.

## Configuration

### Via the connector builder (recommended)

```csharp
services.AddMessaging()
    .AddConnector<TwilioSmsConnector>("sms", cfg => cfg
        .WithRetryPolicy(options =>
        {
            options.WithMaxAttempts(3)                    // maximum send attempts (incl. initial)
                   .WithExponentialBackoff()              // delay increases exponentially
                   .WithBaseDelay(TimeSpan.FromSeconds(1)) // base delay for backoff calculation
                   .WithJitter()                          // randomize delay to avoid thundering herd
                   .RetryOnErrorCodes("RATE_LIMITED", "SERVICE_UNAVAILABLE");
        }));
```

### Via connection settings

You can also configure retry by setting individual parameters on the connection settings:

```csharp
var settings = new ConnectionSettings()
    .SetParameter("Retry.MaxAttempts", 3)
    .SetParameter("Retry.RetryableErrorCodes", "RATE_LIMITED,SERVICE_UNAVAILABLE")
    .SetParameter("Retry.EnableCircuitBreaker", true)
    .SetParameter("Retry.BackoffType", "Exponential")
    .SetParameter("Retry.BaseDelay", "00:00:02")
    .SetParameter("Retry.UseJitter", true)
    .SetParameter("Retry.CircuitBreaker.FailureRatio", 0.5);

var connector = new TwilioSmsConnector(schema, settings);
```

All settings keys are available as constants in `RetrySettingsKeys`:

| Key | `RetrySettingsKeys` constant | Expected value |
|---|---|---|
| `Retry.MaxAttempts` | `.MaxAttempts` | `int` — total send attempts (including initial) |
| `Retry.BackoffType` | `.BackoffType` | `string` — `"Constant"`, `"Linear"`, or `"Exponential"` |
| `Retry.BaseDelay` | `.BaseDelay` | `string` — `TimeSpan` format (e.g. `"00:00:02"`) |
| `Retry.UseJitter` | `.UseJitter` | `bool` |
| `Retry.RetryableErrorCodes` | `.RetryableErrorCodes` | `string` — comma-separated error codes |
| `Retry.EnableCircuitBreaker` | `.EnableCircuitBreaker` | `bool` |
| `Retry.CircuitBreaker.FailureRatio` | `.CircuitBreakerFailureRatio` | `double` |
| `Retry.CircuitBreaker.SamplingDuration` | `.CircuitBreakerSamplingDuration` | `string` — `TimeSpan` format |
| `Retry.CircuitBreaker.MinimumThroughput` | `.CircuitBreakerMinimumThroughput` | `int` |
| `Retry.CircuitBreaker.BreakDuration` | `.CircuitBreakerBreakDuration` | `string` — `TimeSpan` format |

Settings configured via `WithRetryPolicy` or individual keys in `ConnectionSettings` always take precedence over `GetDefaultRetryPolicy()`.

## RetryPolicyOptions reference

| Property | Default | Description |
|---|---|---|
| `MaxRetryAttempts` | `3` | Maximum number of send attempts (inclusive of the initial attempt). Set to `1` to disable retries. |
| `BaseDelay` | `1s` | Base delay between retries, adjusted by the backoff type |
| `BackoffType` | `Exponential` | Backoff strategy: `Constant`, `Linear`, or `Exponential` |
| `UseJitter` | `true` | Randomizes the delay to prevent thundering herd |
| `RetryableErrorCodes` | empty | Error codes that trigger a retry. Empty = nothing is retried. |
| `EnableCircuitBreaker` | `false` | Enables circuit breaker pattern |
| `CircuitBreakerFailureRatio` | `0.5` | Ratio of failures required to open the circuit |
| `CircuitBreakerSamplingDuration` | `30s` | Time window for evaluating the failure ratio |
| `CircuitBreakerMinimumThroughput` | `10` | Minimum requests in the sampling window before the breaker evaluates |
| `CircuitBreakerBreakDuration` | `30s` | How long the circuit stays open before allowing a trial request |

### Retry backoff types

- **Constant**: `BaseDelay` between every retry
- **Linear**: `BaseDelay * attempt` between retries
- **Exponential**: `BaseDelay * 2^attempt` between retries (default)

With jitter enabled, each delay is randomized within `[0, computedDelay)` to spread out concurrent retries.

### Circuit breaker

When enabled, the circuit breaker monitors the failure ratio within a sampling window. If the ratio exceeds `CircuitBreakerFailureRatio`, the circuit opens and all subsequent requests fail immediately for `CircuitBreakerBreakDuration`. After that duration, a trial request is allowed — if it succeeds, the circuit closes; if it fails, the circuit opens again.

Circuit breaker errors use the `CircuitBreakerOpen` error code.

## Attempt counting

Each send result records the number of attempts made in its `AdditionalData` dictionary under the key `"RetryAttempts"`:

```csharp
var result = await connector.SendMessageAsync(message, ct);

if (result.IsSuccess())
{
    int attempts = result.Value.GetRetryAttempts();
    // 1 = first attempt succeeded
    // 3 = first two attempts failed, third succeeded
    Console.WriteLine($"Sent after {attempts} attempt(s)");
}
```

The `GetRetryAttempts()` extension method is available on `SendResult`, `StatusUpdateResult`, and `StatusInfo`. It returns `1` when no retry information is present (message was sent on the first attempt or retry metadata is unavailable).

## Error codes

| Code | Description |
|---|---|
| `RETRY_ATTEMPTS_EXHAUSTED` | All retry attempts were exhausted and the operation failed |
| `CIRCUIT_BREAKER_OPEN` | The circuit breaker is open; requests are blocked until it recovers |

## Logging

When retry is active, the connector logs at each stage:

- `LogRetryAttempt` — a retry attempt is starting
- `LogRetrySucceeded` — a retry attempt succeeded
- `LogRetryExhausted` — all retry attempts exhausted
- `RetryAttemptsExhausted` — the operation failed after exhausting retries
- `CircuitBreakerOpen` — the circuit breaker blocked a request

## Testing retry behavior

When testing connectors that use retry policies, subclass `ChannelConnectorBase` and override `SendMessageCoreAsync` to simulate transient failures:

```csharp
public class TestableConnector : MyConnector
{
    public int CallCount;

    protected override async Task<SendResult> SendMessageCoreAsync(
        IMessage message, CancellationToken ct)
    {
        CallCount++;
        if (CallCount < 3)
            throw new ConnectorException("RATE_LIMITED", "Test", "Transient");

        return new SendResult(message.Id, "remote-id")
        {
            Status = MessageStatus.Delivered
        };
    }
}
```

Configure a policy with the matching error codes and verify that the connector retries until success (or exhaustion):

```csharp
var settings = new ConnectionSettings()
    .SetParameter(RetrySettingsKeys.MaxAttempts, 5)
    .SetParameter(RetrySettingsKeys.RetryableErrorCodes, "RATE_LIMITED");

var connector = new TestableConnector(schema, settings);
await connector.InitializeAsync(ct);

var result = await connector.SendMessageAsync(message, ct);

Assert.True(result.IsSuccess());
Assert.Equal(3, connector.CallCount); // 2 failures + 1 success
Assert.Equal(3, result.Value.GetRetryAttempts());
```
