# Channel Registry Guide

The Channel Registry provides a centralized way to register channel schemas and their associated connectors, ensuring that each connector type is bound to a specific master schema and that runtime schemas are validated for compatibility.

## Table of Contents

1. [Overview](#overview)
2. [Key Concepts](#key-concepts)
3. [Registration](#registration)
4. [Runtime Schema Validation](#runtime-schema-validation)
5. [Connector Creation](#connector-creation)
6. [Query and Discovery](#query-and-discovery)
7. [Dependency Injection Integration](#dependency-injection-integration)
8. [Real-World Examples](#real-world-examples)
9. [Best Practices](#best-practices)

## Overview

The Channel Registry addresses the fundamental requirement that **Channel Connectors are bound to a specific master Channel Schema**, while allowing users to provide compatible runtime schemas when creating connector instances.

### Key Benefits

- **Master Schema Binding**: Each connector type is associated with a definitive master schema
- **Runtime Validation**: Ensures runtime schemas are compatible with master schemas
- **Centralized Management**: Single point for managing all available channels
- **Type Safety**: Validates schema compatibility at runtime
- **Discovery**: Query available channels by capabilities, providers, or types

## Key Concepts

### Master Schema
The authoritative schema that defines the full capabilities and requirements of a connector type. This schema serves as the reference for validation.

### Runtime Schema
A schema provided when creating a connector instance. Must be **compatible** with and a **valid restriction** of the master schema.

### Channel Registration
The process of binding a connector type to its master schema in the registry.

### Logical Identity
The combination of `ChannelProvider/ChannelType/Version` that uniquely identifies a schema family.

## Registration

### Basic Registration

```csharp
// Register channel with master schema
services.AddChannelRegistry()
    .RegisterChannel<TwilioSmsConnector>("twilio-sms", twilioMasterSchema)
    .RegisterChannel<SendGridConnector>("sendgrid-email", sendGridMasterSchema);
```

### Registration with Custom Factory

```csharp
services.AddChannelRegistry()
    .RegisterChannel<TwilioSmsConnector>("twilio-sms", twilioMasterSchema, 
        (serviceProvider, schema) => 
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TwilioSmsConnector>>();
            return new TwilioSmsConnector(schema, logger);
        });
```

### Master Schema Example

```csharp
// Create a comprehensive master schema with all possible capabilities
var twilioMasterSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Master Schema")
    .WithCapabilities(
        ChannelCapability.SendMessages |
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery |
        ChannelCapability.BulkMessaging |
        ChannelCapability.HealthCheck)
    .AddParameter(new ChannelParameter("AccountSid", DataType.String)
    {
        IsRequired = true,
        Description = "Twilio Account SID"
    })
    .AddParameter(new ChannelParameter("AuthToken", DataType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Twilio Auth Token"
    })
    .AddParameter(new ChannelParameter("FromNumber", DataType.String)
    {
        IsRequired = true,
        Description = "Sender phone number in E.164 format"
    })
    .AddParameter(new ChannelParameter("WebhookUrl", DataType.String)
    {
        IsRequired = false,
        Description = "Webhook URL for receiving messages and status updates"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .HandlesMessageEndpoint(EndpointType.PhoneNumber)
    .AddAuthenticationType(AuthenticationType.Basic);
```

## Runtime Schema Validation

### Compatible Runtime Schema

A runtime schema is **compatible** if:
1. It has the same logical identity (`ChannelProvider/ChannelType/Version`)
2. It's a valid restriction of the master schema
3. All its capabilities are present in the master schema
4. All its parameters are defined in the master schema
5. All its content types are supported by the master schema

### Creating Compatible Runtime Schemas

```csharp
// Get the master schema
var masterSchema = registry.GetMasterSchema("twilio-sms");

// Create a customer-specific runtime schema (valid restriction)
var customerSchema = new ChannelSchema(masterSchema, "Customer ABC - Send Only")
    .RemoveCapability(ChannelCapability.ReceiveMessages)    // Remove receive capability
    .RemoveCapability(ChannelCapability.BulkMessaging)      // Remove bulk messaging
    .RemoveParameter("WebhookUrl")                          // Remove webhook parameter
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1234567890";                 // Set customer's number
        param.Description = "Customer ABC's assigned phone number";
    });

// Validate the runtime schema
var validationResults = registry.ValidateRuntimeSchema("twilio-sms", customerSchema);
if (validationResults.Any())
{
    // Handle validation errors
    foreach (var error in validationResults)
    {
        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    }
}
```

### Invalid Runtime Schema Examples

```csharp
// ? INVALID: Different logical identity
var incompatibleSchema = new ChannelSchema("DifferentProvider", "SMS", "2.1.0");

// ? INVALID: Adds capability not in master schema
var invalidSchema = new ChannelSchema(masterSchema, "Invalid")
    .WithCapability(ChannelCapability.Templates); // Not in master schema

// ? INVALID: Adds parameter not in master schema
var invalidSchema2 = new ChannelSchema(masterSchema, "Invalid")
    .AddParameter(new ChannelParameter("NewParam", DataType.String)); // Not in master schema
```

## Connector Creation

### Create with Master Schema

```csharp
// Create connector using the full master schema
var masterConnector = await registry.CreateConnectorAsync("twilio-sms");

// Connector has all capabilities defined in master schema
Console.WriteLine(masterConnector.Schema.Capabilities); 
// Output: SendMessages, ReceiveMessages, MessageStatusQuery, BulkMessaging, HealthCheck
```

### Create with Runtime Schema

```csharp
// Create customer-specific restricted schema
var customerSchema = new ChannelSchema(masterSchema, "Customer Restricted")
    .RemoveCapability(ChannelCapability.ReceiveMessages)
    .RemoveCapability(ChannelCapability.BulkMessaging);

// Validate before creating
var validationResults = registry.ValidateRuntimeSchema("twilio-sms", customerSchema);
if (!validationResults.Any())
{
    // Create connector with restricted capabilities
    var customerConnector = await registry.CreateConnectorAsync("twilio-sms", customerSchema);
    
    Console.WriteLine(customerConnector.Schema.Capabilities);
    // Output: SendMessages, MessageStatusQuery, HealthCheck
}
```

## Query and Discovery

### Get Registered Channels

```csharp
// Get all registered channel IDs
var allChannels = registry.GetRegisteredChannels();
Console.WriteLine($"Registered channels: {string.Join(", ", allChannels)}");
```

### Query by Capabilities

```csharp
// Find all channels that support sending messages
var sendCapableChannels = registry.GetChannelDescriptors(d => 
    d.SupportsCapability(ChannelCapability.SendMessages));

// Find channels that support bulk messaging
var bulkChannels = registry.GetChannelDescriptors(d => 
    d.SupportsCapability(ChannelCapability.BulkMessaging));
```

### Query by Channel Type

```csharp
// Find all SMS channels
var smsChannels = registry.GetChannelDescriptors(d => d.ChannelType == "SMS");

// Find all email channels
var emailChannels = registry.GetChannelDescriptors(d => d.ChannelType == "Email");
```

### Query by Provider

```csharp
// Find all Twilio channels
var twilioChannels = registry.GetChannelDescriptors(d => d.ChannelProvider == "Twilio");

// Find all SendGrid channels
var sendGridChannels = registry.GetChannelDescriptors(d => d.ChannelProvider == "SendGrid");
```

### Query by Content Type Support

```csharp
// Find channels that support media content
var mediaChannels = registry.GetChannelDescriptors(d => 
    d.SupportsContentType(MessageContentType.Media));

// Find channels that support HTML content
var htmlChannels = registry.GetChannelDescriptors(d => 
    d.SupportsContentType(MessageContentType.Html));
```

## Dependency Injection Integration

### Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure channel registry
builder.Services.AddChannelRegistry()
    .RegisterChannel<TwilioSmsConnector>("twilio-sms", CreateTwilioMasterSchema())
    .RegisterChannel<SendGridConnector>("sendgrid-email", CreateSendGridMasterSchema())
    .RegisterChannel<FirebasePushConnector>("firebase-push", CreateFirebaseMasterSchema());

var app = builder.Build();
```

### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class MessagingController : ControllerBase
{
    private readonly IChannelRegistry _registry;

    public MessagingController(IChannelRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet("channels")]
    public IActionResult GetAvailableChannels()
    {
        var channels = _registry.GetChannelDescriptors();
        return Ok(channels.Select(c => new
        {
            c.ChannelId,
            c.ChannelProvider,
            c.ChannelType,
            c.Version,
            c.DisplayName,
            c.Capabilities
        }));
    }

    [HttpPost("channels/{channelId}/connectors")]
    public async Task<IActionResult> CreateConnector(string channelId, [FromBody] CreateConnectorRequest request)
    {
        try
        {
            // Get master schema
            var masterSchema = _registry.GetMasterSchema(channelId);
            
            // Create runtime schema if customizations are provided
            IChannelSchema runtimeSchema = masterSchema;
            if (request.Customizations != null)
            {
                runtimeSchema = ApplyCustomizations(masterSchema, request.Customizations);
            }

            // Validate runtime schema
            var validationResults = _registry.ValidateRuntimeSchema(channelId, runtimeSchema);
            if (validationResults.Any())
            {
                return BadRequest(validationResults.Select(v => v.ErrorMessage));
            }

            // Create connector
            var connector = await _registry.CreateConnectorAsync(channelId, runtimeSchema);
            
            return Ok(new { ConnectorId = Guid.NewGuid(), Schema = runtimeSchema });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

## Real-World Examples

### Multi-Tenant SaaS Application

```csharp
public class TenantMessagingService
{
    private readonly IChannelRegistry _registry;
    private readonly ITenantService _tenantService;

    public TenantMessagingService(IChannelRegistry registry, ITenantService tenantService)
    {
        _registry = registry;
        _tenantService = tenantService;
    }

    public async Task<IChannelConnector> GetTenantConnectorAsync(string tenantId, string channelType)
    {
        var tenant = await _tenantService.GetTenantAsync(tenantId);
        var masterSchema = _registry.GetMasterSchema($"master-{channelType}");

        // Create tenant-specific schema based on their subscription level
        var tenantSchema = CreateTenantSpecificSchema(masterSchema, tenant);

        // Validate and create connector
        var validationResults = _registry.ValidateRuntimeSchema($"master-{channelType}", tenantSchema);
        if (validationResults.Any())
        {
            throw new InvalidOperationException($"Invalid tenant configuration: {string.Join(", ", validationResults.Select(v => v.ErrorMessage))}");
        }

        return await _registry.CreateConnectorAsync($"master-{channelType}", tenantSchema);
    }

    private IChannelSchema CreateTenantSpecificSchema(IChannelSchema masterSchema, Tenant tenant)
    {
        var tenantSchema = new ChannelSchema(masterSchema, $"Tenant {tenant.Id} Schema");

        // Apply restrictions based on subscription level
        switch (tenant.SubscriptionLevel)
        {
            case SubscriptionLevel.Basic:
                tenantSchema = tenantSchema
                    .RemoveCapability(ChannelCapability.BulkMessaging)
                    .RemoveCapability(ChannelCapability.Templates);
                break;
            
            case SubscriptionLevel.Premium:
                tenantSchema = tenantSchema
                    .RemoveCapability(ChannelCapability.BulkMessaging); // Bulk only for enterprise
                break;
            
            case SubscriptionLevel.Enterprise:
                // Keep all capabilities
                break;
        }

        // Apply tenant-specific parameter defaults
        foreach (var parameter in tenant.ChannelParameters)
        {
            tenantSchema = tenantSchema.UpdateParameter(parameter.Name, param =>
            {
                param.DefaultValue = parameter.Value;
            });
        }

        return tenantSchema;
    }
}
```

### Department-Specific Configurations

```csharp
public class DepartmentChannelManager
{
    private readonly IChannelRegistry _registry;

    public DepartmentChannelManager(IChannelRegistry registry)
    {
        _registry = registry;
    }

    public async Task<IChannelConnector> GetDepartmentConnectorAsync(Department department, string channelId)
    {
        var masterSchema = _registry.GetMasterSchema(channelId);
        var departmentSchema = department switch
        {
            Department.HR => CreateHRSchema(masterSchema),
            Department.Marketing => CreateMarketingSchema(masterSchema),
            Department.Finance => CreateFinanceSchema(masterSchema),
            _ => masterSchema
        };

        return await _registry.CreateConnectorAsync(channelId, departmentSchema);
    }

    private IChannelSchema CreateHRSchema(IChannelSchema masterSchema)
    {
        return new ChannelSchema(masterSchema, "HR Department Schema")
            .RemoveCapability(ChannelCapability.BulkMessaging)    // HR sends individual messages
            .RemoveCapability(ChannelCapability.MediaAttachments) // Security policy: no attachments
            .RestrictContentTypes(MessageContentType.PlainText); // Text only for security
    }

    private IChannelSchema CreateMarketingSchema(IChannelSchema masterSchema)
    {
        return new ChannelSchema(masterSchema, "Marketing Department Schema")
            .WithCapability(ChannelCapability.BulkMessaging)      // Marketing needs bulk messaging
            .WithCapability(ChannelCapability.Templates);        // Template support for campaigns
    }

    private IChannelSchema CreateFinanceSchema(IChannelSchema masterSchema)
    {
        return new ChannelSchema(masterSchema, "Finance Department Schema")
            .RemoveCapability(ChannelCapability.BulkMessaging)    // Individual notifications only
            .AddMessageProperty(new MessagePropertyConfiguration("ComplianceLevel", DataType.String)
            {
                IsRequired = true,
                Description = "Financial compliance classification"
            });
    }
}
```

## Best Practices

### 1. Design Comprehensive Master Schemas

```csharp
// ? Good - Comprehensive master schema with all possible features
var masterSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(/* All capabilities the connector can support */)
    .AddParameter(/* All parameters the connector accepts */)
    .AddContentType(/* All content types the connector handles */)
    .HandlesMessageEndpoint(/* All endpoint types supported */);

// Then create restrictions as needed
var restrictedSchema = new ChannelSchema(masterSchema, "Restricted")
    .RemoveCapability(ChannelCapability.BulkMessaging);
```

### 2. Use Descriptive Channel IDs

```csharp
// ? Good - Clear, descriptive IDs
.RegisterChannel<TwilioSmsConnector>("twilio-sms-v2", schema)
.RegisterChannel<SendGridConnector>("sendgrid-email-transactional", schema)

// ? Avoid - Generic or confusing IDs
.RegisterChannel<TwilioSmsConnector>("connector1", schema)
.RegisterChannel<SendGridConnector>("email", schema)
```

### 3. Validate Runtime Schemas

```csharp
// ? Good - Always validate before creating connectors
var validationResults = registry.ValidateRuntimeSchema(channelId, runtimeSchema);
if (validationResults.Any())
{
    throw new InvalidOperationException($"Invalid schema: {string.Join(", ", validationResults.Select(v => v.ErrorMessage))}");
}

var connector = await registry.CreateConnectorAsync(channelId, runtimeSchema);
```

### 4. Use Meaningful Display Names for Runtime Schemas

```csharp
// ? Good - Descriptive names that explain the purpose
var customerSchema = new ChannelSchema(masterSchema, "Customer ABC - Outbound Only")
    .RemoveCapability(ChannelCapability.ReceiveMessages);

var deptSchema = new ChannelSchema(masterSchema, "HR Department - Secure Messaging")
    .RemoveCapability(ChannelCapability.MediaAttachments);
```

### 5. Document Schema Customizations

```csharp
// ? Good - Document why parameters were updated
var customSchema = new ChannelSchema(masterSchema, "Customer Specific")
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1234567890"; // Customer's dedicated number
        param.Description = "Customer's assigned phone number for branding";
    })
    .RemoveCapability(ChannelCapability.ReceiveMessages); // Customer requirement: outbound only
```

### 6. Handle Validation Errors Gracefully

```csharp
public async Task<Result<IChannelConnector>> CreateConnectorSafelyAsync(string channelId, IChannelSchema runtimeSchema)
{
    try
    {
        var validationResults = _registry.ValidateRuntimeSchema(channelId, runtimeSchema);
        if (validationResults.Any())
        {
            var errors = validationResults.Select(v => v.ErrorMessage!).ToList();
            return Result<IChannelConnector>.Failure($"Schema validation failed: {string.Join("; ", errors)}");
        }

        var connector = await _registry.CreateConnectorAsync(channelId, runtimeSchema);
        return Result<IChannelConnector>.Success(connector);
    }
    catch (InvalidOperationException ex)
    {
        return Result<IChannelConnector>.Failure(ex.Message);
    }
}
```

### 7. Use Dependency Injection for Connector Dependencies

```csharp
// ? Good - Use factory pattern for complex dependencies
services.AddChannelRegistry()
    .RegisterChannel<TwilioSmsConnector>("twilio-sms", masterSchema, 
        (serviceProvider, schema) => 
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TwilioSmsConnector>>();
            var httpClient = serviceProvider.GetRequiredService<HttpClient>();
            return new TwilioSmsConnector(schema, logger, httpClient);
        });
```

This comprehensive registry model ensures that your channel connectors are properly bound to their master schemas while providing the flexibility to create customized runtime instances that maintain compatibility and type safety.