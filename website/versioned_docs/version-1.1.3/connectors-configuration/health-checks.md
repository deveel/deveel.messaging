# Connector Health Checks

Health checks provide visibility into connector operational status, enabling proactive monitoring and automated recovery.

## Built-in Health Check Integration

Connectors implement `GetHealthAsync()` which returns connector health status:

```csharp
var health = await connector.GetHealthAsync(ct);

if (health.IsSuccess())
{
    var healthInfo = health.Value;
    Console.WriteLine($"Healthy: {healthInfo.IsHealthy}");
    Console.WriteLine($"State: {healthInfo.State}");
}
```

### Health Status Values

| Status | Description |
|--------|-------------|
| `Healthy` | Connector is operational and can send/receive messages |
| `Degraded` | Connector is functional but experiencing issues |
| `Unhealthy` | Connector cannot perform operations |

## ASP.NET Core Health Check Integration

The `Ratatosk.Extensions.HealthChecks` package provides a `ConnectorHealthCheck` that automatically discovers all registered connectors and probes them through the standard ASP.NET Core health check pipeline.

### Installation

Add the `Ratatosk.Extensions.HealthChecks` NuGet package to your project.

### Registration

Register the health check after all connectors have been configured:

```csharp
services.AddMessaging()
    .AddTwilioSms(...)
    .AddSendGridEmail(...);

services.AddHealthChecks()
    .AddRatatoskHealthChecks();
```

This registers a single health check named `"ratatosk"` with the `"messaging"` tag. At check time it discovers all unnamed and named connectors via DI, probes each through `GetHealthAsync()`, and reports the worst status across all connectors.

### Health Check Endpoint

Configure the health check endpoint in `Program.cs`:

```csharp
var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("messaging")
});

app.Run();
```

### Per-Connector Detail

The health check result includes per-connector detail in the `Data` dictionary, keyed by connector type name (with the `Connector` suffix removed) or the named connector's registration name:

> **Note:** By default, ASP.NET Core's `MapHealthChecks` middleware does not serialize `HealthCheckResult.Data` in the HTTP response. To expose per-connector details over HTTP, provide a custom `ResponseWriter`:
>
> ```csharp
> app.MapHealthChecks("/health/detail", new HealthCheckOptions
> {
>     ResponseWriter = async (context, report) =>
>     {
>         var json = JsonSerializer.Serialize(new
>         {
>             status = report.Status.ToString(),
>             results = report.Entries.ToDictionary(
>                 e => e.Key,
>                 e => e.Value.Data)
>         });
>         context.Response.ContentType = "application/json";
>         await context.Response.WriteAsync(json);
>     }
> });
> ```

The per-connector data has this shape:

```json
{
  "status": "Degraded",
  "results": {
    "TwilioSms": {
      "status": "Healthy",
      "state": "Ready",
      "isHealthy": true,
      "issues": [],
      "uptime": "01:23:45",
      "lastHealthCheck": "2026-06-21T10:00:00Z"
    },
    "SendGridEmail": {
      "status": "Degraded",
      "state": "Ready",
      "isHealthy": true,
      "issues": ["High latency detected"],
      "uptime": "01:23:45",
      "lastHealthCheck": "2026-06-21T10:00:00Z"
    }
  }
}
```

### Health Status Mapping

| `ConnectorHealth` | `HealthStatus` | Overall |
|---|---|---|
| `IsHealthy == true`, no `Issues` | `Healthy` | Worst across all connectors |
| `IsHealthy == true`, has `Issues` | `Degraded` | |
| `IsHealthy == false` | `Unhealthy` | |
| Exception or failed `OperationResult` | `Unhealthy` | |

## Manual Health Verification

Test connector connectivity before sending messages:

```csharp
public async Task<bool> VerifyConnectorAsync(IChannelConnector connector)
{
    var testResult = await connector.TestConnectionAsync(CancellationToken.None);
    if (testResult.IsFailure())
        return false;

    var health = await connector.GetHealthAsync(CancellationToken.None);
    return health.IsSuccess() && health.Value?.IsHealthy == true;
}
```

## Health Check Implementation for Custom Connectors

When building custom connectors, implement health checks:

```csharp
public class MyConnector : ChannelConnectorBase
{
    protected override Task<ConnectorHealth> GetConnectorHealthAsync(CancellationToken ct)
    {
        var health = new ConnectorHealth
        {
            State = State,
            IsHealthy = State == ConnectorState.Ready,
            LastHealthCheck = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - _initializedAt
        };

        if (!health.IsHealthy)
        {
            health.Issues.Add($"Connector is in {State} state");
        }

        return Task.FromResult(health);
    }
}
```

### Advanced Health Checks

Perform actual provider connectivity tests:

```csharp
protected override async Task<ConnectorHealth> GetConnectorHealthAsync(CancellationToken ct)
{
    var health = new ConnectorHealth
    {
        State = State,
        LastHealthCheck = DateTime.UtcNow
    };

    try
    {
        var response = await _httpClient.GetAsync("/health", ct);
        response.EnsureSuccessStatusCode();
        
        health.IsHealthy = true;
        health.Uptime = DateTime.UtcNow - _initializedAt;
    }
    catch (Exception ex)
    {
        health.IsHealthy = false;
        health.Issues.Add($"Provider health check failed: {ex.Message}");
    }

    return health;
}
```

## Connector State Transitions

Connector state affects health status:

| State | IsHealthy | Description |
|-------|-----------|-------------|
| `Ready` | Yes | Operational, can send/receive |
| `Initializing` | No | Still initializing |
| `Uninitialized` | No | Not yet initialized |
| `Error` | No | Error state, needs recovery |
| `ShuttingDown` | No | Graceful shutdown in progress |
| `Shutdown` | No | Connector is shut down |

## Troubleshooting

### Connector Reports Unhealthy

**Check:**
1. **Initialization** - Was `InitializeAsync()` called successfully?
2. **Authentication** - Are credentials valid and not expired?
3. **Network** - Can the connector reach the provider API?
4. **Provider Status** - Is the provider experiencing outages?

**Recovery:**
```csharp
if (!health.IsHealthy)
{
    await connector.InitializeAsync(ct);
    health = await connector.GetHealthAsync(ct);
}
```

### Health Check Endpoint Returns 503

**Causes:**
- One or more connectors are unhealthy
- Health check timeout is too short
- Provider API is unavailable

**Solutions:**
1. **Check individual connector health** - Identify which connector is failing
2. **Increase health check timeout** - Allow more time for provider response
3. **Implement degraded mode** - Return `HealthCheckResult.Degraded` instead of `Unhealthy`

## Best Practices

### DO: Implement Health Checks

Always implement `GetConnectorHealthAsync()` in custom connectors - it's essential for monitoring.

### DO: Test Provider Connectivity

Don't just check internal state - verify you can actually reach the provider API.

### DO: Include Diagnostic Information

Provide useful diagnostic data in health check results:

```csharp
health.Metrics["LastSuccessfulSend"] = _lastSuccessfulSend;
health.Metrics["FailedSendCount"] = _failedSendCount;
```

### DON'T: Perform Expensive Operations

Health checks should be lightweight and fast:

```csharp
// Bad - sends actual message as health check
await SendMessageAsync(testMessage, ct);

// Good - lightweight connectivity test
await _httpClient.GetAsync("/ping", ct);
```

### DON'T: Cache Health Status

Always perform fresh health checks - cached status may be stale.

## See Also

- [Timeouts](timeouts.md) - Configure operation timeouts
- [Retry Policies](retry-policies.md) - Automatic retry for transient failures
- [Connector Implementation - Advanced Topics](../connectors-implementation/advanced-topics.md) - Health check implementation details
