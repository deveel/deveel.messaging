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

Register connector health checks with ASP.NET Core health monitoring:

### Basic Registration

```csharp
builder.Services
    .AddHealthChecks()
    .AddCheck<MessagingHealthCheck>("messaging", tags: ["ready"]);
```

### Implementation

```csharp
public class MessagingHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IChannelConnector> _connectors;

    public MessagingHealthCheck(IEnumerable<IChannelConnector> connectors)
    {
        _connectors = connectors;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        var healthy = true;
        var data = new Dictionary<string, object>();

        foreach (var connector in _connectors)
        {
            var health = await connector.GetHealthAsync(ct);
            var connectorName = $"{connector.Schema.ChannelProvider}/{connector.Schema.ChannelType}";
            
            data[$"{connectorName}_healthy"] = health.Value?.IsHealthy;
            data[$"{connectorName}_state"] = health.Value?.State.ToString();
            healthy &= health.Value?.IsHealthy ?? false;
        }

        return healthy
            ? HealthCheckResult.Healthy(data: data)
            : HealthCheckResult.Degraded(data: data);
    }
}
```

### Health Check Endpoint

Configure health check endpoint in `Program.cs`:

```csharp
var app = builder.Build();

app.MapHealthChecks("/health");           // Basic health check
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

## Manual Health Verification

Test connector connectivity before sending messages:

```csharp
public async Task<bool> VerifyConnectorAsync(IChannelConnector connector)
{
    // Test connection to provider
    var testResult = await connector.TestConnectionAsync(CancellationToken.None);
    if (testResult.IsFailure())
        return false;

    // Check health status
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
        // Test actual API connectivity
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
| `Ready` | ✅ Yes | Operational, can send/receive |
| `Initializing` | ❌ No | Still initializing |
| `Uninitialized` | ❌ No | Not yet initialized |
| `Error` | ❌ No | Error state, needs recovery |
| `ShuttingDown` | ❌ No | Graceful shutdown in progress |
| `Shutdown` | ❌ No | Connector is shut down |

## Monitoring and Alerting

### Prometheus Metrics

Export health status as metrics:

```csharp
// In your monitoring service
var connectors = serviceProvider.GetServices<IChannelConnector>();
foreach (var connector in connectors)
{
    var health = await connector.GetHealthAsync(ct);
    var isHealthy = health.Value?.IsHealthy == true ? 1 : 0;
    
    Metrics.RecordGauge("connector_health", isHealthy, 
        new("connector", connector.Schema.ChannelType));
}
```

### Application Insights

Track health with telemetry:

```csharp
telemetryClient.GetMetric("Connector Health")
    .TrackMetric(health.Value?.IsHealthy == true ? 1.0 : 0.0,
        new Dictionary<string, string>
        {
            ["Connector"] = connector.Schema.ChannelType,
            ["Provider"] = connector.Schema.ChannelProvider
        });
```

## Troubleshooting

### Connector Reports Unhealthy

**Check:**
1. **Initialization** - Was `InitializeAsync()` called successfully?
2. **Authentication** - Are credentials valid and not expired?
3. **Network** - Can the connector reach the provider API?
4. **Provider Status** - Is the provider experiencing outages?

**Recovery:**
```csharp
// Attempt to reinitialize
if (!health.IsHealthy)
{
    await connector.InitializeAsync(ct);
    
    // Verify recovery
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

### ✅ DO: Implement Health Checks

Always implement `GetConnectorHealthAsync()` in custom connectors - it's essential for monitoring.

### ✅ DO: Test Provider Connectivity

Don't just check internal state - verify you can actually reach the provider API.

### ✅ DO: Include Diagnostic Information

Provide useful diagnostic data in health check results:

```csharp
health.AdditionalData["LastSuccessfulSend"] = _lastSuccessfulSend;
health.AdditionalData["FailedSendCount"] = _failedSendCount;
```

### ❌ DON'T: Perform Expensive Operations

Health checks should be lightweight and fast:

```csharp
// ❌ Bad - sends actual message as health check
await SendMessageAsync(testMessage, ct);

// ✅ Good - lightweight connectivity test
await _httpClient.GetAsync("/ping", ct);
```

### ❌ DON'T: Cache Health Status

Always perform fresh health checks - cached status may be stale.

## See Also

- [Timeouts](timeouts.md) - Configure operation timeouts
- [Retry Policies](retry-policies.md) - Automatic retry for transient failures
- [Connector Implementation - Advanced Topics](../connectors-implementation/advanced-topics.md) - Health check implementation details
