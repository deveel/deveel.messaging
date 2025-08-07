# ChannelRegistry and Connector Architecture Documentation

## Overview

The `ChannelRegistry` serves as the central hub for managing channel connectors within the Deveel Messaging Framework. It provides comprehensive lifecycle management, schema validation, and resource coordination for all messaging connectors.

## Core Architecture

### ChannelRegistry Implementation

```csharp
public class ChannelRegistry : IChannelRegistry, IDisposable, IAsyncDisposable
{
    // Thread-safe connector and schema management
    private readonly ConcurrentDictionary<Type, ConnectorRegistration> _registrations = new();
    private readonly ConcurrentBag<IChannelConnector> _connectors = new();
    private readonly IServiceProvider _services;
}
```

#### Key Features

1. **Thread-Safe Operations**: All registry operations are thread-safe using concurrent collections
2. **Automatic Schema Discovery**: Uses `[ChannelSchema]` attributes to discover connector schemas
3. **Dependency Injection Integration**: Full support for .NET DI container
4. **Graceful Resource Management**: Async disposal with connector shutdown coordination
5. **Runtime Schema Validation**: Validates derived schemas against master schemas

### Connector Registration Pattern

```csharp
// Register connectors with automatic schema discovery
registry.RegisterConnector<TwilioSmsConnector>();
registry.RegisterConnector<TwilioWhatsAppConnector>();
registry.RegisterConnector<SendGridEmailConnector>();

// Register with custom factory
registry.RegisterConnector<CustomConnector>(schema => 
    new CustomConnector(schema, customDependency));
```

### Schema Discovery and Validation

The registry automatically discovers schemas using reflection:

```csharp
[ChannelSchema(typeof(TwilioSmsSchemaFactory))]
public class TwilioSmsConnector : ChannelConnectorBase
{
    // Implementation
}
```

## Message Validation Architecture

### Current Issue: Validation Duplication

**Problem Identified**: All connectors currently perform duplicate validation in their `SendMessageCoreAsync` methods, which duplicates what `ChannelConnectorBase.ValidateMessage` should already handle.

#### Examples of Duplication:

**Twilio SMS Connector:**
```csharp
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    // ? DUPLICATE VALIDATION - This is already done in base class
    var validationResults = Schema.ValidateMessage(message);
    var validationErrors = validationResults.ToList();
    if (validationErrors.Count > 0)
    {
        // Return validation failed...
    }
    // Rest of implementation...
}
```

**SendGrid Email Connector:**
```csharp
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    // ? DUPLICATE VALIDATION - Extract properties and validate again
    var messageProperties = ExtractMessageProperties(message);
    var validationResults = channelSchema.ValidateMessageProperties(messageProperties);
    var validationErrors = validationResults.ToList();
    if (validationErrors.Count > 0)
    {
        // Return validation failed...
    }
    // Rest of implementation...
}
```

**Twilio WhatsApp Connector:**
```csharp
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    // ? DUPLICATE VALIDATION - Same pattern repeated
    var validationResults = Schema.ValidateMessage(message);
    var validationErrors = validationResults.ToList();
    if (validationErrors.Count > 0)
    {
        // Return validation failed...
    }
    // Rest of implementation...
}
```

### Recommended Solution: Centralized Validation

#### Current Base Class Validation

`ChannelConnectorBase` already provides validation in `SendMessageAsync`:

```csharp
public async Task<ConnectorResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken)
{
    ValidateCapability(ChannelCapability.SendMessages);
    ValidateOperationalState();
    
    // ? Validation is already performed here via ValidateMessageAsync
    var validationResults = ValidateMessageAsync(message, cancellationToken);
    await foreach (var validationResult in validationResults)
    {
        if (validationResult != ValidationResult.Success)
        {
            // Handle validation errors
        }
    }
    
    // Call the concrete implementation
    return await SendMessageCoreAsync(message, cancellationToken);
}
```

#### Proposed Refactoring

**Remove duplicate validation from connector implementations:**

```csharp
// ? CORRECTED - Remove duplicate validation
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    try
    {
        _logger?.LogDebug("Sending message {MessageId}", message.Id);

        // Validation is already performed by base class - proceed with provider-specific logic
        var messageBody = ExtractMessageBody(message);
        var senderNumber = ExtractPhoneNumber(message.Sender);
        // ... rest of implementation without redundant validation
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Failed to send message {MessageId}", message.Id);
        return ConnectorResult<SendResult>.Fail(ErrorCodes.SendMessageFailed, ex.Message);
    }
}
```

## Enhanced Documentation

### Registry Lifecycle Management

#### Connector Creation and Initialization

```csharp
// Create connector with automatic initialization
var connector = await registry.CreateConnectorAsync<TwilioSmsConnector>(cancellationToken);

// Create with custom schema
var customSchema = new ChannelSchema(TwilioChannelSchemas.TwilioSms, "Custom SMS")
    .RemoveCapability(ChannelCapability.ReceiveMessages);

var restrictedConnector = await registry.CreateConnectorAsync<TwilioSmsConnector>(
    customSchema, cancellationToken);
```

#### Resource Management with Graceful Shutdown

```csharp
public async Task DisposeAsync()
{
    // Collect all connectors for shutdown
    var connectorsToDispose = _connectors.ToList();
    var shutdownTasks = new List<Task>();

    // Start shutdown for all connectors concurrently
    foreach (var connector in connectorsToDispose)
    {
        shutdownTasks.Add(connector.ShutdownAsync(CancellationToken.None));
    }

    // Wait for all shutdowns with timeout
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await Task.WhenAll(shutdownTasks).WaitAsync(timeoutCts.Token);

    // Dispose connectors
    foreach (var connector in connectorsToDispose)
    {
        if (connector is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (connector is IDisposable disposable)
            disposable.Dispose();
    }
}
```

### Schema Query and Discovery

#### Finding Connectors by Provider/Type

```csharp
// Find connector type by provider and channel type
var smsConnectorType = registry.FindConnector("Twilio", "SMS");
var emailConnectorType = registry.FindConnector("SendGrid", "Email");

// Get schema for a connector type
var schema = registry.GetConnectorSchema<TwilioSmsConnector>();

// Query schemas by capabilities
var receivingCapableSchemas = registry.QuerySchemas(schema => 
    schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
```

#### Schema Validation and Compatibility

```csharp
// Validate a runtime schema against a master schema
var validationResults = registry.ValidateSchema<TwilioSmsConnector>(runtimeSchema);

if (validationResults.Any())
{
    foreach (var error in validationResults)
    {
        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    }
}
```

### Advanced Usage Patterns

#### Multi-Tenant Connector Management

```csharp
public class TenantAwareConnectorRegistry
{
    private readonly IChannelRegistry _baseRegistry;
    private readonly ConcurrentDictionary<string, IChannelConnector> _tenantConnectors = new();

    public async Task<IChannelConnector> GetConnectorForTenant(string tenantId, Type connectorType)
    {
        var key = $"{tenantId}:{connectorType.Name}";
        
        if (_tenantConnectors.TryGetValue(key, out var existingConnector))
            return existingConnector;

        // Create tenant-specific schema
        var baseSchema = _baseRegistry.GetConnectorSchema(connectorType);
        var tenantSchema = new ChannelSchema(baseSchema, $"Tenant {tenantId}")
            .AddParameter(new ChannelParameter("TenantId", DataType.String) { IsRequired = true });

        var connector = await _baseRegistry.CreateConnectorAsync(connectorType, tenantSchema);
        _tenantConnectors.TryAdd(key, connector);
        
        return connector;
    }
}
```

#### Health Monitoring and Status Tracking

```csharp
public class ConnectorHealthMonitor
{
    private readonly IChannelRegistry _registry;
    private readonly Timer _healthCheckTimer;

    public ConnectorHealthMonitor(IChannelRegistry registry)
    {
        _registry = registry;
        _healthCheckTimer = new Timer(CheckConnectorHealth, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    }

    private async void CheckConnectorHealth(object? state)
    {
        var descriptors = _registry.GetConnectorDescriptors();
        
        foreach (var descriptor in descriptors)
        {
            try
            {
                var connector = await _registry.CreateConnectorAsync(descriptor.ConnectorType);
                var health = await connector.GetHealthAsync(CancellationToken.None);
                
                if (!health.Value?.IsHealthy == true)
                {
                    // Log health issues or trigger alerts
                    LogHealthIssue(descriptor.ConnectorType, health.Value?.Issues);
                }
            }
            catch (Exception ex)
            {
                LogHealthCheckError(descriptor.ConnectorType, ex);
            }
        }
    }
}
```

## Best Practices and Recommendations

### 1. Validation Architecture Fixes

**Remove duplicate validation from all connector implementations:**

1. **Twilio SMS Connector**: Remove `Schema.ValidateMessage()` call from `SendMessageCoreAsync`
2. **Twilio WhatsApp Connector**: Remove `Schema.ValidateMessage()` call from `SendMessageCoreAsync`  
3. **SendGrid Email Connector**: Remove `ValidateMessageProperties()` call from `SendMessageCoreAsync`

**Trust base class validation** - `ChannelConnectorBase` already handles this properly.

### 2. Schema Design Patterns

#### Comprehensive Base Schemas

```csharp
// Start with full-featured base schema
public static ChannelSchema BaseEmailSchema => new ChannelSchema("Provider", "Email", "1.0.0")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery |
        ChannelCapability.HandleMessageState |
        ChannelCapability.BulkMessaging |
        ChannelCapability.Templates)
    .AddParameter(/* all possible parameters */)
    .AddContentType(/* all content types */);

// Create derived schemas by restriction
public static ChannelSchema SimpleEmailSchema => new ChannelSchema(BaseEmailSchema, "Simple Email")
    .RemoveCapability(ChannelCapability.ReceiveMessages)
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .RemoveCapability(ChannelCapability.Templates);
```

#### Environment-Specific Schemas

```csharp
public static class EnvironmentSchemas
{
    public static ChannelSchema ForDevelopment(ChannelSchema baseSchema) =>
        new ChannelSchema(baseSchema, "Development")
            .SetParameter("SandboxMode", true)
            .AddParameter(new ChannelParameter("DebugLogging", DataType.Boolean) { DefaultValue = true });

    public static ChannelSchema ForProduction(ChannelSchema baseSchema) =>
        new ChannelSchema(baseSchema, "Production")
            .RemoveParameter("SandboxMode")
            .AddParameter(new ChannelParameter("RateLimitPerMinute", DataType.Integer) { DefaultValue = 1000 });
}
```

### 3. Error Handling and Resilience

#### Connector Initialization with Retry

```csharp
public async Task<IChannelConnector> CreateConnectorWithRetry<T>(
    IChannelSchema? schema = null, 
    int maxRetries = 3,
    TimeSpan delay = default) where T : class, IChannelConnector
{
    delay = delay == default ? TimeSpan.FromSeconds(1) : delay;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await registry.CreateConnectorAsync<T>(schema);
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            _logger?.LogWarning(ex, "Connector creation attempt {Attempt} failed, retrying in {Delay}", 
                attempt, delay);
            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5); // Exponential backoff
        }
    }
    
    throw new InvalidOperationException($"Failed to create connector after {maxRetries} attempts");
}
```

### 4. Performance Optimization

#### Connector Pooling

```csharp
public class ConnectorPool<T> where T : class, IChannelConnector
{
    private readonly ConcurrentQueue<T> _availableConnectors = new();
    private readonly IChannelRegistry _registry;
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxPoolSize;

    public async Task<T> RentAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        if (_availableConnectors.TryDequeue(out var connector))
        {
            return connector;
        }

        // Create new connector if pool is empty
        return await _registry.CreateConnectorAsync<T>(cancellationToken);
    }

    public void Return(T connector)
    {
        if (_availableConnectors.Count < _maxPoolSize)
        {
            _availableConnectors.Enqueue(connector);
        }
        
        _semaphore.Release();
    }
}
```

## Migration Guide for Validation Fixes

### Step 1: Identify Duplicate Validation

Search for these patterns in connector implementations:
- `Schema.ValidateMessage(message)`
- `ValidateMessageProperties(messageProperties)`
- Manual property extraction followed by validation

### Step 2: Remove Redundant Code

**Before (Twilio SMS):**
```csharp
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    // ? Remove this entire validation block
    var validationResults = Schema.ValidateMessage(message);
    var validationErrors = validationResults.ToList();
    if (validationErrors.Count > 0)
    {
        _logger?.LogError("Message properties validation failed: {Errors}", 
            string.Join(", ", validationErrors.Select(e => e.ErrorMessage)));
        return ConnectorResult<SendResult>.ValidationFailed(TwilioErrorCodes.InvalidMessage, 
            "Message properties validation failed", validationErrors);
    }
    
    // ? Keep the actual implementation logic
    var senderNumber = ExtractPhoneNumber(message.Sender);
    // ... rest of implementation
}
```

**After (Twilio SMS):**
```csharp
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    // ? Start directly with implementation - validation is handled by base class
    var senderNumber = ExtractPhoneNumber(message.Sender);
    // ... rest of implementation
}
```

### Step 3: Trust Base Class Validation

The `ChannelConnectorBase.SendMessageAsync` method already performs comprehensive validation:
- Schema-based message validation
- Capability validation
- Operational state validation

### Step 4: Add Provider-Specific Validation Only When Needed

If you need provider-specific validation that can't be expressed in the schema:

```csharp
protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
{
    // ? Only add provider-specific validation that can't be handled by schema
    if (IsSpecialProviderRequirement(message) && !ValidateSpecialRequirement(message))
    {
        return ConnectorResult<SendResult>.Fail(ProviderErrorCodes.SpecialRequirementFailed, 
            "Provider-specific requirement not met");
    }
    
    // Rest of implementation...
}
```

## Conclusion

The `ChannelRegistry` provides a robust foundation for connector management with automatic schema discovery, validation, and resource management. The key improvement needed is removing duplicate validation from connector implementations and trusting the centralized validation in `ChannelConnectorBase`.

This architectural approach ensures:
- **Consistency**: All connectors follow the same validation patterns
- **Maintainability**: Validation logic is centralized and not duplicated
- **Performance**: Validation occurs only once per message
- **Reliability**: Comprehensive error handling and graceful resource management
- **Flexibility**: Support for custom schemas and tenant-specific configurations

The framework now provides production-ready messaging capabilities with full bidirectional support across all major providers (Twilio SMS/WhatsApp and SendGrid Email).