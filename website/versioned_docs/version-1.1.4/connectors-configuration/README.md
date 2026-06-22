# Connector Configuration

This section covers configuration options for connectors, including connection settings, retry policies, timeouts, and health checks.

## Configuration Topics

| Topic | Description |
|-------|-------------|
| [Connection Settings](connection-settings.md) | Parameter types, validation, and configuration methods |
| [Connection Strings](connection-strings.md) | Compact semicolon-delimited configuration format |
| [Retry Policies](retry-policies.md) | Automatic retry for transient failures |
| [Timeouts](timeouts.md) | Per-operation timeout configuration |
| [Health Checks](health-checks.md) | Monitoring connector health status |

## Quick Start

Configure connectors during registration:

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithSettings("Twilio")           // Load from appsettings.json
        .WithTimeout(t => t               // Configure timeouts
            .WithSendTimeout(TimeSpan.FromSeconds(60)))
        .WithRetryPolicy(r => r           // Configure retry
            .WithMaxAttempts(3)));
```

## Configuration Sources

Connectors can be configured from multiple sources:

1. **appsettings.json** - Use `WithSettings("SectionName")`
2. **Connection strings** - Use `WithConnectionString("key=value;...")`
3. **Typed options** - Use `WithOptions(options)`
4. **Programmatic** - Use `WithSetting("key", value)`

All sources can be mixed, with later values overriding earlier ones.

## Security

Sensitive parameters (credentials, tokens, keys) are automatically redacted in logs. Mark parameters as sensitive in your schema:

```csharp
new ChannelParameter("AuthToken", DataType.String)
{
    IsRequired = true,
    IsSensitive = true  // Value appears as "***" in logs
}
```

See [Connection Settings](connection-settings.md#sensitive-parameters) for details.

## Next Steps

- **Using existing connectors**: See [Connector Guides](connectors/README.md)
- **Building custom connectors**: See [Connector Implementation](connectors-implementation/overview.md)
