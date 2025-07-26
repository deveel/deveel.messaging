# ChannelSchema Logical Identity and Compatibility

## Overview

ChannelSchemas are logically identified by the combination of `ChannelProvider`, `ChannelType`, and `Version`. Two schemas with the same logical identity represent the same type of channel, even if they have different configurations. This approach allows the application layer to determine relationships between schemas without direct references.

## Key Principles

### 1. **Logical Identity**
- Schemas are identified by: `ChannelProvider/ChannelType/Version`
- Two schemas with the same logical identity are compatible
- No direct parent-child references exist between schemas

### 2. **Application-Layer Relationships**
- The application determines which schema is the "parent" and which is the "child"
- Schemas can validate compatibility and restrictions independently
- Relationship management happens outside the schema objects

### 3. **Independent Schema Objects**
- Each schema is self-contained
- No direct references to other schemas
- Modifications are always independent

## Schema Creation

### Base Schema
```csharp
var baseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Connector")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint("sms");
```

### Copy-Based Schema Creation
```csharp
// Creates a copy with the same logical identity
var restrictedSchema = new ChannelSchema(baseSchema, "Customer Restricted SMS")
    .RestrictCapabilities(ChannelCapability.SendMessages)  // Remove receive capability
    .RemoveParameter("AuthToken")                          // Remove auth token requirement
    .RestrictContentTypes(MessageContentType.PlainText);   // Only plain text

// Both schemas have the same logical identity:
Assert.Equal("Twilio/SMS/2.1.0", baseSchema.GetLogicalIdentity());
Assert.Equal("Twilio/SMS/2.1.0", restrictedSchema.GetLogicalIdentity());
Assert.True(baseSchema.IsCompatibleWith(restrictedSchema));
```

## Logical Identity Methods

### IsCompatibleWith()
```csharp
public bool IsCompatibleWith(IChannelSchema otherSchema)
```
Returns `true` if both schemas have the same `ChannelProvider`, `ChannelType`, and `Version`.

### GetLogicalIdentity()
```csharp
public string GetLogicalIdentity()
```
Returns the logical identity as a string: `"Provider/Type/Version"`

### ValidateAsRestrictionOf()
```csharp
public IEnumerable<ValidationResult> ValidateAsRestrictionOf(IChannelSchema targetSchema)
```
Validates whether this schema is a valid restriction of the target schema. Returns validation errors if:
- Schemas are not compatible (different logical identity)
- This schema has capabilities not in the target schema
- This schema has parameters not in the target schema
- This schema has content types not in the target schema
- etc.

## Application-Layer Usage Examples

### Schema Registry Pattern
```csharp
public class SchemaRegistry
{
    private readonly Dictionary<string, IChannelSchema> _baseSchemas = new();
    private readonly Dictionary<string, List<IChannelSchema>> _derivedSchemas = new();

    public void RegisterBaseSchema(IChannelSchema schema)
    {
        var identity = schema.GetLogicalIdentity();
        _baseSchemas[identity] = schema;
        _derivedSchemas[identity] = new List<IChannelSchema>();
    }

    public IChannelSchema CreateRestrictedSchema(string baseIdentity, string displayName, 
        Action<ChannelSchema> restrictions)
    {
        if (!_baseSchemas.TryGetValue(baseIdentity, out var baseSchema))
            throw new ArgumentException($"Base schema not found: {baseIdentity}");

        var restrictedSchema = new ChannelSchema(baseSchema, displayName);
        restrictions((ChannelSchema)restrictedSchema);

        // Validate it's a proper restriction
        var validationResults = restrictedSchema.ValidateAsRestrictionOf(baseSchema);
        if (validationResults.Any())
        {
            throw new InvalidOperationException(
                $"Invalid restriction: {string.Join(", ", validationResults.Select(r => r.ErrorMessage))}");
        }

        _derivedSchemas[baseIdentity].Add(restrictedSchema);
        return restrictedSchema;
    }

    public IEnumerable<IChannelSchema> GetCompatibleSchemas(string logicalIdentity)
    {
        if (_derivedSchemas.TryGetValue(logicalIdentity, out var schemas))
        {
            yield return _baseSchemas[logicalIdentity];
            foreach (var schema in schemas)
                yield return schema;
        }
    }
}
```

### Usage Example
```csharp
var registry = new SchemaRegistry();

// Register base Twilio SMS schema
var twilioBase = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String) { IsRequired = false });

registry.RegisterBaseSchema(twilioBase);

// Create customer-specific restrictions
var customerSchema = registry.CreateRestrictedSchema("Twilio/SMS/2.1.0", "Customer A SMS", schema =>
{
    schema.RestrictCapabilities(ChannelCapability.SendMessages)  // Send-only
          .RemoveParameter("WebhookUrl");                        // No webhooks
});

// Application can now manage relationships
var baseIdentity = "Twilio/SMS/2.1.0";
var allCompatibleSchemas = registry.GetCompatibleSchemas(baseIdentity).ToList();
// Returns: [twilioBase, customerSchema]

// Validate customer settings against their restricted schema
var customerSettings = new ConnectionSettings();
customerSettings.SetParameter("AccountSid", "AC123");
customerSettings.SetParameter("AuthToken", "token123");

var validationResults = customerSchema.ValidateConnectionSettings(customerSettings);
Assert.Empty(validationResults); // Valid according to restricted schema
```

## Restriction Methods

All restriction methods from the previous implementation remain available:

### Capability Management
- `RestrictCapabilities(ChannelCapability)` - Keep only specified capabilities
- `RemoveCapability(ChannelCapability)` - Remove specific capability
- `WithCapability(ChannelCapability)` - Add capability

### Parameter Management
- `RemoveParameter(string)` - Remove parameter
- `UpdateParameter(string, Action<ChannelParameter>)` - Modify parameter

### Content Type Management
- `RestrictContentTypes(params MessageContentType[])` - Keep only specified types
- `RemoveContentType(MessageContentType)` - Remove specific type

### Endpoint Management
- `RemoveEndpoint(string)` - Remove endpoint
- `UpdateEndpoint(string, Action<ChannelEndpointConfiguration>)` - Modify endpoint

### Message Property Management
- `RemoveMessageProperty(string)` - Remove message property
- `UpdateMessageProperty(string, Action<MessagePropertyConfiguration>)` - Modify property

## Benefits

### 1. **Decoupled Architecture**
- No direct dependencies between schemas
- Application layer controls relationships
- Easier testing and maintenance

### 2. **Flexible Relationship Management**
- One base schema can have multiple derived versions
- Derived schemas can be compared against any compatible schema
- Support for complex hierarchies through application logic

### 3. **Validation Consistency**
- Clear validation of restrictions
- Logical identity ensures compatibility
- Independent evolution of configurations

### 4. **Schema Evolution**
- Base schemas can evolve independently
- Derived schemas remain stable
- Application layer manages migration strategies

## Real-World Scenarios

### Multi-Tenant SaaS
```csharp
// Base schema defined by the SaaS platform
var baseEmailSchema = new ChannelSchema("Platform", "Email", "1.0.0");

// Tenant-specific restrictions
var tenantASchema = new ChannelSchema(baseEmailSchema, "Tenant A Email")
    .RestrictCapabilities(ChannelCapability.SendMessages)  // Send-only for Tenant A
    .RemoveParameter("BulkOptions");                        // No bulk for Tenant A

var tenantBSchema = new ChannelSchema(baseEmailSchema, "Tenant B Email")
    .AddParameter(new ChannelParameter("TenantId", ParameterType.String) { IsRequired = true });

// Platform validates tenant configurations
var tenantAValidation = tenantASchema.ValidateAsRestrictionOf(baseEmailSchema);
var tenantBValidation = tenantBSchema.ValidateAsRestrictionOf(baseEmailSchema);

// Tenant B is not a valid restriction (it adds parameters)
Assert.Empty(tenantAValidation);
Assert.NotEmpty(tenantBValidation);
```

### API Gateway Pattern
```csharp
// Gateway manages multiple schema versions
public class MessageGateway
{
    public async Task<bool> ValidateMessage(string schemaIdentity, ConnectionSettings settings, 
        Dictionary<string, object?> messageProperties)
    {
        var schema = await GetSchemaByIdentity(schemaIdentity);
        
        var connectionValidation = schema.ValidateConnectionSettings(settings);
        var messageValidation = schema.ValidateMessageProperties(messageProperties);
        
        return !connectionValidation.Any() && !messageValidation.Any();
    }

    public async Task<IEnumerable<IChannelSchema>> GetCompatibleSchemas(string baseIdentity)
    {
        // Returns all schemas with the same logical identity
        return await _schemaRepository.GetByLogicalIdentity(baseIdentity);
    }
}
```

This approach provides maximum flexibility while maintaining clear logical relationships between schemas, allowing applications to determine their own relationship management strategies.