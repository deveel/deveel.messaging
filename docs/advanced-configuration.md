# Advanced Configuration Guide

This guide covers advanced configuration patterns, best practices, and optimization techniques for the Deveel Messaging Framework.

## Table of Contents

1. [Multi-Environment Configuration](#multi-environment-configuration)
2. [Dynamic Schema Configuration](#dynamic-schema-configuration)
3. [Custom Parameter Types](#custom-parameter-types)
4. [Advanced Message Properties](#advanced-message-properties)
5. [Connector Pools and Load Balancing](#connector-pools-and-load-balancing)
6. [Security Configuration](#security-configuration)
7. [Performance Optimization](#performance-optimization)
8. [Monitoring and Diagnostics](#monitoring-and-diagnostics)

## Multi-Environment Configuration

### Environment-Specific Schemas

```csharp
public class EnvironmentAwareSchemaFactory
{
    private readonly IConfiguration _configuration;
    
    public EnvironmentAwareSchemaFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IChannelSchema CreateEmailSchema(string environment)
    {
        var baseSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
            .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.HealthCheck)
            .AddContentType(MessageContentType.Html)
            .AddContentType(MessageContentType.PlainText)
            .AllowsMessageEndpoint(EndpointType.EmailAddress);

        return environment.ToLower() switch
        {
            "development" => ConfigureForDevelopment(baseSchema),
            "staging" => ConfigureForStaging(baseSchema),
            "production" => ConfigureForProduction(baseSchema),
            _ => throw new ArgumentException($"Unknown environment: {environment}")
        };
    }

    private IChannelSchema ConfigureForDevelopment(ChannelSchema baseSchema)
    {
        return baseSchema
            .AddParameter(new ChannelParameter("Host", ParameterType.String)
            {
                DefaultValue = "localhost",
                Description = "SMTP server for development"
            })
            .AddParameter(new ChannelParameter("Port", ParameterType.Integer)
            {
                DefaultValue = 1025, // MailHog default port
                Description = "SMTP port for development"
            })
            .AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean)
            {
                DefaultValue = false,
                Description = "SSL not required for development"
            });
    }

    private IChannelSchema ConfigureForProduction(ChannelSchema baseSchema)
    {
        return baseSchema
            .AddParameter(new ChannelParameter("Host", ParameterType.String)
            {
                IsRequired = true,
                Description = "Production SMTP server hostname"
            })
            .AddParameter(new ChannelParameter("Port", ParameterType.Integer)
            {
                DefaultValue = 587,
                Description = "Production SMTP port"
            })
            .AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean)
            {
                DefaultValue = true,
                Description = "SSL required for production"
            })
            .AddParameter(new ChannelParameter("ConnectionTimeout", ParameterType.Integer)
            {
                DefaultValue = 30000,
                Description = "Connection timeout in milliseconds"
            })
            .WithCapabilities(baseSchema.Capabilities | ChannelCapability.MessageStatusQuery);
    }
}
```

### Configuration Binding

```csharp
public class MessagingConfiguration
{
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();
    public SecurityConfiguration Security { get; set; } = new();
    public PerformanceConfiguration Performance { get; set; } = new();
}

public class ProviderConfiguration
{
    public string ProviderName { get; set; } = "";
    public string ChannelType { get; set; } = "";
    public string Version { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> Capabilities { get; set; } = new();
    public List<string> EndpointTypes { get; set; } = new();
    public List<string> ContentTypes { get; set; } = new();
}

// Usage with dependency injection
services.Configure<MessagingConfiguration>(configuration.GetSection("Messaging"));

// Factory that creates schemas from configuration
public class ConfigurationBasedSchemaFactory
{
    private readonly MessagingConfiguration _config;

    public ConfigurationBasedSchemaFactory(IOptions<MessagingConfiguration> options)
    {
        _config = options.Value;
    }

    public IChannelSchema CreateSchema(string providerKey)
    {
        if (!_config.Providers.TryGetValue(providerKey, out var providerConfig))
            throw new ArgumentException($"Provider '{providerKey}' not found in configuration");

        var schema = new ChannelSchema(
            providerConfig.ProviderName,
            providerConfig.ChannelType,
            providerConfig.Version);

        // Configure capabilities
        var capabilities = providerConfig.Capabilities
            .Select(c => Enum.Parse<ChannelCapability>(c))
            .Aggregate(ChannelCapability.None, (acc, cap) => acc | cap);
        schema = schema.WithCapabilities(capabilities);

        // Configure endpoint types
        foreach (var endpointType in providerConfig.EndpointTypes)
        {
            schema = schema.AllowsMessageEndpoint(Enum.Parse<EndpointType>(endpointType));
        }

        // Configure content types
        foreach (var contentType in providerConfig.ContentTypes)
        {
            schema = schema.AddContentType(Enum.Parse<MessageContentType>(contentType));
        }

        return schema;
    }
}
```

## Dynamic Schema Configuration

### Runtime Schema Modification

```csharp
public class DynamicSchemaManager
{
    private readonly ConcurrentDictionary<string, IChannelSchema> _schemas = new();

    public IChannelSchema GetOrCreateSchema(string key, Func<IChannelSchema> factory)
    {
        return _schemas.GetOrAdd(key, _ => factory());
    }

    public void UpdateSchema(string key, Func<IChannelSchema, IChannelSchema> updater)
    {
        _schemas.AddOrUpdate(key, 
            key => throw new KeyNotFoundException($"Schema '{key}' not found"),
            (key, existing) => updater(existing));
    }

    public IChannelSchema CreateCustomerSpecificSchema(string customerId, IChannelSchema baseSchema)
    {
        // Customer-specific customizations
        var customerConfig = GetCustomerConfiguration(customerId);
        
        var customSchema = new ChannelSchema(baseSchema, $"Customer-{customerId}")
            .WithDisplayName($"{baseSchema.DisplayName} (Customer {customerId})");

        // Apply customer-specific restrictions
        if (customerConfig.RestrictToPlainText)
        {
            customSchema = customSchema.RestrictContentTypes(MessageContentType.PlainText);
        }

        // Add customer-specific message properties
        foreach (var property in customerConfig.CustomProperties)
        {
            customSchema = customSchema.AddMessageProperty(
                new MessagePropertyConfiguration(property.Name, property.Type)
                {
                    IsRequired = property.IsRequired,
                    Description = property.Description
                });
        }

        return customSchema;
    }

    private CustomerConfiguration GetCustomerConfiguration(string customerId)
    {
        // Retrieve customer-specific configuration from database or external service
        return new CustomerConfiguration
        {
            RestrictToPlainText = false,
            CustomProperties = new[]
            {
                new CustomProperty { Name = "CustomerRef", Type = ParameterType.String, IsRequired = true },
                new CustomProperty { Name = "Priority", Type = ParameterType.Integer, IsRequired = false }
            }
        };
    }
}

public class CustomerConfiguration
{
    public bool RestrictToPlainText { get; set; }
    public CustomProperty[] CustomProperties { get; set; } = Array.Empty<CustomProperty>();
}

public class CustomProperty
{
    public string Name { get; set; } = "";
    public ParameterType Type { get; set; }
    public bool IsRequired { get; set; }
    public string Description { get; set; } = "";
}
```

## Custom Parameter Types

### Extended Parameter Types

```csharp
public enum ExtendedParameterType
{
    // Standard types
    String = ParameterType.String,
    Integer = ParameterType.Integer,
    Boolean = ParameterType.Boolean,
    
    // Custom types
    Url = 100,
    Email = 101,
    PhoneNumber = 102,
    Json = 103,
    Base64 = 104,
    Duration = 105,
    CronExpression = 106
}

public class ExtendedChannelParameter : ChannelParameter
{
    public ExtendedParameterType ExtendedType { get; }
    public string ValidationPattern { get; set; } = "";
    public string[] AllowedValues { get; set; } = Array.Empty<string>();
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }

    public ExtendedChannelParameter(string name, ExtendedParameterType type) 
        : base(name, (ParameterType)(int)type)
    {
        ExtendedType = type;
    }

    public override bool ValidateValue(object? value)
    {
        if (!base.ValidateValue(value))
            return false;

        if (value == null)
            return !IsRequired;

        return ExtendedType switch
        {
            ExtendedParameterType.Url => ValidateUrl(value.ToString()),
            ExtendedParameterType.Email => ValidateEmail(value.ToString()),
            ExtendedParameterType.PhoneNumber => ValidatePhoneNumber(value.ToString()),
            ExtendedParameterType.Json => ValidateJson(value.ToString()),
            ExtendedParameterType.Base64 => ValidateBase64(value.ToString()),
            ExtendedParameterType.Duration => ValidateDuration(value.ToString()),
            ExtendedParameterType.CronExpression => ValidateCronExpression(value.ToString()),
            _ => true
        };
    }

    private bool ValidateUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private bool ValidateEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidatePhoneNumber(string? value)
    {
        // Implement phone number validation
        return !string.IsNullOrWhiteSpace(value) && 
               value.All(c => char.IsDigit(c) || c == '+' || c == '-' || c == ' ' || c == '(' || c == ')');
    }

    private bool ValidateJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateDuration(string? value)
    {
        return TimeSpan.TryParse(value, out _);
    }

    private bool ValidateCronExpression(string? value)
    {
        // Implement cron expression validation
        // This is a simplified check - use a proper cron library in production
        return !string.IsNullOrWhiteSpace(value) && value.Split(' ').Length >= 5;
    }
}
```

## Advanced Message Properties

### Message Property Builders

```csharp
public class MessagePropertyBuilder
{
    private readonly Dictionary<string, object> _properties = new();

    public MessagePropertyBuilder AddEmail(string name, string value)
    {
        ValidateEmail(value);
        _properties[name] = value;
        return this;
    }

    public MessagePropertyBuilder AddUrl(string name, string value)
    {
        ValidateUrl(value);
        _properties[name] = value;
        return this;
    }

    public MessagePropertyBuilder AddDuration(string name, TimeSpan value)
    {
        _properties[name] = value.ToString();
        return this;
    }

    public MessagePropertyBuilder AddJson<T>(string name, T value)
    {
        _properties[name] = JsonSerializer.Serialize(value);
        return this;
    }

    public MessagePropertyBuilder AddConditional(string name, object value, Func<bool> condition)
    {
        if (condition())
        {
            _properties[name] = value;
        }
        return this;
    }

    public Dictionary<string, object> Build() => new(_properties);

    private static void ValidateEmail(string email)
    {
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            throw new ArgumentException($"Invalid email address: {email}");
    }

    private static void ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException($"Invalid URL: {url}");
    }
}

// Usage
var properties = new MessagePropertyBuilder()
    .AddEmail("ReplyTo", "support@company.com")
    .AddUrl("TrackingUrl", "https://tracking.company.com/msg123")
    .AddDuration("Expires", TimeSpan.FromHours(24))
    .AddJson("Metadata", new { source = "api", version = "v2" })
    .AddConditional("Priority", "High", () => isUrgent)
    .Build();

var message = new MessageBuilder()
    .WithId("msg-001")
    .WithEmailSender("sender@company.com")
    .WithEmailReceiver("recipient@example.com")
    .WithTextContent("Your message content")
    .WithProperties(properties)
    .Message;
```

## Connector Pools and Load Balancing

### Connector Pool Implementation

```csharp
public interface IConnectorPool : IDisposable
{
    Task<T?> ExecuteAsync<T>(Func<IChannelConnector, Task<T>> operation, CancellationToken cancellationToken = default);
    Task<IChannelConnector?> AcquireConnectorAsync(CancellationToken cancellationToken = default);
    Task ReleaseConnectorAsync(IChannelConnector connector);
    Task<int> GetAvailableCountAsync();
}

public class ChannelConnectorPool : IConnectorPool
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<IChannelConnector> _connectors = new();
    private readonly IChannelSchema _schema;
    private readonly Func<IChannelSchema, IChannelConnector> _connectorFactory;
    private readonly int _maxSize;
    private readonly TimeSpan _acquireTimeout;
    private int _currentSize;

    public ChannelConnectorPool(
        IChannelSchema schema,
        Func<IChannelSchema, IChannelConnector> connectorFactory,
        int maxSize = 10,
        TimeSpan? acquireTimeout = null)
    {
        _schema = schema;
        _connectorFactory = connectorFactory;
        _maxSize = maxSize;
        _acquireTimeout = acquireTimeout ?? TimeSpan.FromSeconds(30);
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    public async Task<T?> ExecuteAsync<T>(Func<IChannelConnector, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var connector = await AcquireConnectorAsync(cancellationToken);
        if (connector == null)
            return default;

        try
        {
            return await operation(connector);
        }
        finally
        {
            await ReleaseConnectorAsync(connector);
        }
    }

    public async Task<IChannelConnector?> AcquireConnectorAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_acquireTimeout, cancellationToken))
            return null;

        if (_connectors.TryDequeue(out var connector))
        {
            if (connector.State == ConnectorState.Connected)
                return connector;

            // Connector is not connected, dispose and create new one
            if (connector is IDisposable disposable)
                disposable.Dispose();
        }

        // Create new connector
        connector = _connectorFactory(_schema);
        try
        {
            await connector.InitializeAsync(cancellationToken);
            Interlocked.Increment(ref _currentSize);
            return connector;
        }
        catch
        {
            if (connector is IDisposable disposable)
                disposable.Dispose();
            _semaphore.Release();
            throw;
        }
    }

    public async Task ReleaseConnectorAsync(IChannelConnector connector)
    {
        if (connector.State == ConnectorState.Connected)
        {
            _connectors.Enqueue(connector);
        }
        else
        {
            if (connector is IDisposable disposable)
                disposable.Dispose();
            Interlocked.Decrement(ref _currentSize);
        }

        _semaphore.Release();
        await Task.CompletedTask;
    }

    public async Task<int> GetAvailableCountAsync()
    {
        await Task.CompletedTask;
        return _semaphore.CurrentCount;
    }

    public void Dispose()
    {
        while (_connectors.TryDequeue(out var connector))
        {
            if (connector is IDisposable disposable)
                disposable.Dispose();
        }
        _semaphore.Dispose();
    }
}
```

### Load Balancing Connector

```csharp
public class LoadBalancingConnector : IChannelConnector
{
    private readonly List<IConnectorPool> _pools;
    private readonly ILoadBalancingStrategy _strategy;
    private int _currentIndex;

    public LoadBalancingConnector(
        IEnumerable<IConnectorPool> pools,
        ILoadBalancingStrategy? strategy = null)
    {
        _pools = pools.ToList();
        _strategy = strategy ?? new RoundRobinStrategy();
    }

    public async Task<ConnectorResult<MessageResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        var pool = _strategy.SelectPool(_pools, message);
        
        return await pool.ExecuteAsync(async connector =>
        {
            return await connector.SendMessageAsync(message, cancellationToken);
        }, cancellationToken) ?? ConnectorResult<MessageResult>.Failure("No available connector");
    }

    // Implement other IChannelConnector methods...
}

public interface ILoadBalancingStrategy
{
    IConnectorPool SelectPool(IList<IConnectorPool> pools, IMessage? message = null);
}

public class RoundRobinStrategy : ILoadBalancingStrategy
{
    private int _index;

    public IConnectorPool SelectPool(IList<IConnectorPool> pools, IMessage? message = null)
    {
        var index = Interlocked.Increment(ref _index) % pools.Count;
        return pools[index];
    }
}

public class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    public IConnectorPool SelectPool(IList<IConnectorPool> pools, IMessage? message = null)
    {
        return pools
            .OrderByDescending(p => p.GetAvailableCountAsync().Result)
            .First();
    }
}
```

## Security Configuration

### Secure Parameter Handling

```csharp
public class SecureParameterManager
{
    private readonly IDataProtector _protector;

    public SecureParameterManager(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("DevleMessaging.Parameters");
    }

    public string ProtectParameter(string value)
    {
        return _protector.Protect(value);
    }

    public string UnprotectParameter(string protectedValue)
    {
        return _protector.Unprotect(protectedValue);
    }

    public Dictionary<string, object> SecureConfiguration(
        Dictionary<string, object> configuration,
        IChannelSchema schema)
    {
        var secureConfig = new Dictionary<string, object>(configuration);

        foreach (var parameter in schema.Parameters.Where(p => p.IsSensitive))
        {
            if (secureConfig.TryGetValue(parameter.Name, out var value) && value != null)
            {
                secureConfig[parameter.Name] = ProtectParameter(value.ToString() ?? "");
            }
        }

        return secureConfig;
    }

    public Dictionary<string, object> UnsecureConfiguration(
        Dictionary<string, object> secureConfiguration,
        IChannelSchema schema)
    {
        var config = new Dictionary<string, object>(secureConfiguration);

        foreach (var parameter in schema.Parameters.Where(p => p.IsSensitive))
        {
            if (config.TryGetValue(parameter.Name, out var value) && value != null)
            {
                config[parameter.Name] = UnprotectParameter(value.ToString() ?? "");
            }
        }

        return config;
    }
}
```

### Audit Logging

```csharp
public class AuditingConnectorDecorator : IChannelConnector
{
    private readonly IChannelConnector _innerConnector;
    private readonly IAuditLogger _auditLogger;

    public AuditingConnectorDecorator(IChannelConnector innerConnector, IAuditLogger auditLogger)
    {
        _innerConnector = innerConnector;
        _auditLogger = auditLogger;
    }

    public async Task<ConnectorResult<MessageResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        var auditEntry = new AuditEntry
        {
            Operation = "SendMessage",
            MessageId = message.Id,
            Sender = message.Sender?.Value,
            Receiver = message.Receiver?.Value,
            Timestamp = DateTimeOffset.UtcNow,
            ConnectorType = _innerConnector.GetType().Name
        };

        try
        {
            var result = await _innerConnector.SendMessageAsync(message, cancellationToken);
            
            auditEntry.Success = result.IsSuccess;
            auditEntry.ResultMessageId = result.Value?.MessageId;
            auditEntry.ErrorMessage = result.ErrorMessage;

            return result;
        }
        catch (Exception ex)
        {
            auditEntry.Success = false;
            auditEntry.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            await _auditLogger.LogAsync(auditEntry);
        }
    }

    // Implement other IChannelConnector methods with similar audit logging...
}

public class AuditEntry
{
    public string Operation { get; set; } = "";
    public string? MessageId { get; set; }
    public string? Sender { get; set; }
    public string? Receiver { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string ConnectorType { get; set; } = "";
    public bool Success { get; set; }
    public string? ResultMessageId { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry);
}
```

## Performance Optimization

### Caching Strategies

```csharp
public class CachingSchemaFactory : IChannelSchemaFactory
{
    private readonly IChannelSchemaFactory _innerFactory;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration;

    public CachingSchemaFactory(
        IChannelSchemaFactory innerFactory,
        IMemoryCache cache,
        TimeSpan? cacheExpiration = null)
    {
        _innerFactory = innerFactory;
        _cache = cache;
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(30);
    }

    public async Task<IChannelSchema> CreateSchemaAsync(string schemaKey)
    {
        var cacheKey = $"schema:{schemaKey}";
        
        if (_cache.TryGetValue(cacheKey, out IChannelSchema? cachedSchema) && cachedSchema != null)
        {
            return cachedSchema;
        }

        var schema = await _innerFactory.CreateSchemaAsync(schemaKey);
        
        _cache.Set(cacheKey, schema, _cacheExpiration);
        
        return schema;
    }
}
```

### Batch Processing

```csharp
public class BatchingConnectorDecorator : IChannelConnector
{
    private readonly IChannelConnector _innerConnector;
    private readonly BatchingOptions _options;
    private readonly SemaphoreSlim _batchSemaphore;
    private readonly List<BatchItem> _batch = new();
    private readonly Timer _flushTimer;

    public BatchingConnectorDecorator(IChannelConnector innerConnector, BatchingOptions? options = null)
    {
        _innerConnector = innerConnector;
        _options = options ?? new BatchingOptions();
        _batchSemaphore = new SemaphoreSlim(1, 1);
        _flushTimer = new Timer(FlushBatch, null, _options.FlushInterval, _options.FlushInterval);
    }

    public async Task<ConnectorResult<MessageResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableBatching || !_innerConnector.Schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging))
        {
            return await _innerConnector.SendMessageAsync(message, cancellationToken);
        }

        var tcs = new TaskCompletionSource<ConnectorResult<MessageResult>>();
        var batchItem = new BatchItem(message, tcs);

        await _batchSemaphore.WaitAsync(cancellationToken);
        try
        {
            _batch.Add(batchItem);
            
            if (_batch.Count >= _options.BatchSize)
            {
                await FlushBatchInternal();
            }
        }
        finally
        {
            _batchSemaphore.Release();
        }

        return await tcs.Task;
    }

    private async void FlushBatch(object? state)
    {
        await _batchSemaphore.WaitAsync();
        try
        {
            await FlushBatchInternal();
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    private async Task FlushBatchInternal()
    {
        if (_batch.Count == 0)
            return;

        var items = _batch.ToList();
        _batch.Clear();

        try
        {
            var messages = items.Select(i => i.Message).ToList();
            var results = await _innerConnector.SendMessagesAsync(messages, CancellationToken.None);
            
            if (results.IsSuccess && results.Value != null)
            {
                var resultsList = results.Value.ToList();
                for (int i = 0; i < items.Count && i < resultsList.Count; i++)
                {
                    items[i].TaskCompletionSource.SetResult(
                        ConnectorResult<MessageResult>.Success(resultsList[i]));
                }
            }
            else
            {
                foreach (var item in items)
                {
                    item.TaskCompletionSource.SetResult(
                        ConnectorResult<MessageResult>.Failure(results.ErrorMessage ?? "Batch send failed"));
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var item in items)
            {
                item.TaskCompletionSource.SetException(ex);
            }
        }
    }

    private record BatchItem(IMessage Message, TaskCompletionSource<ConnectorResult<MessageResult>> TaskCompletionSource);
}

public class BatchingOptions
{
    public bool EnableBatching { get; set; } = true;
    public int BatchSize { get; set; } = 10;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);
}
```

## Monitoring and Diagnostics

### Health Checks

```csharp
public class MessagingHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IChannelConnector> _connectors;

    public MessagingHealthCheck(IEnumerable<IChannelConnector> connectors)
    {
        _connectors = connectors;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<(string Name, bool Healthy, string? Error)>();

        foreach (var connector in _connectors)
        {
            try
            {
                if (connector.Schema.Capabilities.HasFlag(ChannelCapability.HealthCheck))
                {
                    var testResult = await connector.TestConnectionAsync(cancellationToken);
                    results.Add((
                        $"{connector.Schema.ChannelProvider}-{connector.Schema.ChannelType}",
                        testResult.IsSuccess,
                        testResult.ErrorMessage
                    ));
                }
                else
                {
                    results.Add((
                        $"{connector.Schema.ChannelProvider}-{connector.Schema.ChannelType}",
                        connector.State == ConnectorState.Connected,
                        connector.State != ConnectorState.Connected ? $"State: {connector.State}" : null
                    ));
                }
            }
            catch (Exception ex)
            {
                results.Add((
                    $"{connector.Schema.ChannelProvider}-{connector.Schema.ChannelType}",
                    false,
                    ex.Message
                ));
            }
        }

        var failedConnectors = results.Where(r => !r.Healthy).ToList();
        
        if (failedConnectors.Any())
        {
            var data = failedConnectors.ToDictionary(
                f => f.Name,
                f => (object)(f.Error ?? "Unknown error")
            );
            
            return HealthCheckResult.Degraded(
                $"Some connectors are unhealthy: {string.Join(", ", failedConnectors.Select(f => f.Name))}",
                data: data
            );
        }

        return HealthCheckResult.Healthy("All messaging connectors are healthy");
    }
}

// Registration
services.AddHealthChecks()
    .AddCheck<MessagingHealthCheck>("messaging");
```

### Metrics Collection

```csharp
public class MetricsCollectingConnectorDecorator : IChannelConnector
{
    private readonly IChannelConnector _innerConnector;
    private readonly IMetricsCollector _metricsCollector;

    public MetricsCollectingConnectorDecorator(
        IChannelConnector innerConnector,
        IMetricsCollector metricsCollector)
    {
        _innerConnector = innerConnector;
        _metricsCollector = metricsCollector;
    }

    public async Task<ConnectorResult<MessageResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var tags = new Dictionary<string, string>
        {
            ["connector"] = _innerConnector.GetType().Name,
            ["provider"] = _innerConnector.Schema.ChannelProvider,
            ["channel_type"] = _innerConnector.Schema.ChannelType
        };

        try
        {
            _metricsCollector.IncrementCounter("messaging.send.attempts", tags);
            
            var result = await _innerConnector.SendMessageAsync(message, cancellationToken);
            
            stopwatch.Stop();
            _metricsCollector.RecordDuration("messaging.send.duration", stopwatch.Elapsed, tags);
            
            if (result.IsSuccess)
            {
                _metricsCollector.IncrementCounter("messaging.send.success", tags);
            }
            else
            {
                tags["error"] = result.ErrorCode ?? "unknown";
                _metricsCollector.IncrementCounter("messaging.send.failure", tags);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            tags["error"] = ex.GetType().Name;
            _metricsCollector.IncrementCounter("messaging.send.error", tags);
            _metricsCollector.RecordDuration("messaging.send.duration", stopwatch.Elapsed, tags);
            throw;
        }
    }

    // Implement other methods with similar metrics collection...
}

public interface IMetricsCollector
{
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);
    void RecordDuration(string name, TimeSpan duration, Dictionary<string, string>? tags = null);
    void RecordValue(string name, double value, Dictionary<string, string>? tags = null);
}
```

This advanced configuration guide covers sophisticated patterns for production deployments, including multi-environment configuration, security considerations, performance optimization, and comprehensive monitoring.