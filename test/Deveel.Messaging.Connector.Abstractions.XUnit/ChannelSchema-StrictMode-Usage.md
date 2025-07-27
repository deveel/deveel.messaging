# ChannelSchema Strict Mode Usage Examples

This document demonstrates how to use the strict mode functionality in `ChannelSchema` to control validation behavior for unknown parameters and message properties.

## Overview

The `ChannelSchema` supports two validation modes:

- **Strict Mode** (default): Rejects unknown parameters and message properties that are not defined in the schema
- **Flexible Mode**: Allows unknown parameters and message properties to pass validation

This enables scenarios where:
- **Strict mode** ensures only predefined parameters/properties are used (good for production/security)
- **Flexible mode** allows custom extensions and backwards compatibility (good for development/migration)

## Basic Usage

### Creating Schemas with Different Modes

```csharp
// Default constructor creates strict schema
var strictSchema = new ChannelSchema("Provider", "Type", "1.0.0");
Console.WriteLine($"Is Strict: {strictSchema.IsStrict}"); // True

// Create flexible schema using fluent methods
var flexibleSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithFlexibleMode();
Console.WriteLine($"Is Strict: {flexibleSchema.IsStrict}"); // False
```

### Fluent Configuration

```csharp
// Start with default (strict) and switch to flexible
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithDisplayName("Configurable Schema")
    .WithFlexibleMode()  // Switch to flexible mode
    .AddParameter(new ChannelParameter("KnownParam", ParameterType.String));

// Switch back to strict mode
schema.WithStrictMode();

// Or use the boolean parameter
schema.WithStrictMode(false); // Flexible
schema.WithStrictMode(true);  // Strict
```

## Connection Settings Validation

### Strict Mode - Rejects Unknown Parameters

```csharp
// Create strict schema with defined parameters (default behavior)
var strictSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 });

// Connection settings with unknown parameter
var connectionSettings = new ConnectionSettings()
    .SetParameter("Host", "smtp.example.com")
    .SetParameter("Port", 587)
    .SetParameter("CustomTimeout", 30000);  // Unknown parameter

// Validate - will fail due to unknown parameter
var results = strictSchema.ValidateConnectionSettings(connectionSettings);

foreach (var error in results)
{
    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    // Output: "Unknown parameter 'CustomTimeout' is not supported by this schema."
}
```

### Flexible Mode - Allows Unknown Parameters

```csharp
// Create flexible schema with same parameters
var flexibleSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithFlexibleMode()
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 });

// Same connection settings with unknown parameter
var connectionSettings = new ConnectionSettings()
    .SetParameter("Host", "smtp.example.com")
    .SetParameter("Port", 587)
    .SetParameter("CustomTimeout", 30000);  // Unknown parameter - now allowed

// Validate - will pass
var results = flexibleSchema.ValidateConnectionSettings(connectionSettings);

Console.WriteLine($"Validation Results: {results.Count()}"); // 0 - no errors
```

## Message Properties Validation

### Strict Mode - Rejects Unknown Properties

```csharp
// Create strict schema with defined message properties (default behavior)
var strictEmailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer));

// Message properties with unknown property
var messageProperties = new Dictionary<string, object?>
{
    { "Subject", "Important Update" },
    { "Priority", 3 },
    { "CustomTrackingId", "TRACK-123" }  // Unknown property
};

// Validate - will fail due to unknown property
var results = strictEmailSchema.ValidateMessageProperties(messageProperties);

foreach (var error in results)
{
    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    // Output: "Unknown message property 'CustomTrackingId' is not supported by this schema."
}
```

### Flexible Mode - Allows Unknown Properties

```csharp
// Create flexible schema with same message properties
var flexibleEmailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithFlexibleMode()
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer));

// Same message properties with unknown property
var messageProperties = new Dictionary<string, object?>
{
    { "Subject", "Important Update" },
    { "Priority", 3 },
    { "CustomTrackingId", "TRACK-123" }  // Unknown property - now allowed
};

// Validate - will pass
var results = flexibleEmailSchema.ValidateMessageProperties(messageProperties);

Console.WriteLine($"Validation Results: {results.Count()}"); // 0 - no errors
```

## What Strict Mode Does NOT Affect

Both strict and flexible modes still enforce:

- **Required parameters/properties**: Missing required items still cause validation errors
- **Type validation**: Incorrect types still cause validation errors
- **Constraint validation**: Allowed values and other constraints are still enforced

```csharp
var flexibleSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithFlexibleMode()
    .AddParameter(new ChannelParameter("RequiredParam", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("IntParam", ParameterType.Integer));

// This will still fail validation in flexible mode
var connectionSettings = new ConnectionSettings()
    .SetParameter("IntParam", "not a number")  // Wrong type
    .SetParameter("CustomParam", "allowed");   // Unknown but allowed in flexible mode
    // Missing RequiredParam

var results = flexibleSchema.ValidateConnectionSettings(connectionSettings);
// Will have 2 errors: missing required parameter and wrong type
// Will NOT have error for unknown parameter
```

## Real-World Scenarios

### Production Environment (Strict Mode)

```csharp
// Production schema - strict validation for security and consistency (default)
var productionEmailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithDisplayName("Production Email Connector")
    .AddParameter(new ChannelParameter("Host", ParameterType.String) 
    { 
        IsRequired = true,
        Description = "SMTP server hostname"
    })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) 
    { 
        IsRequired = true,
        DefaultValue = 587,
        Description = "SMTP server port"
    })
    .AddParameter(new ChannelParameter("Username", ParameterType.String) 
    { 
        IsRequired = true,
        Description = "SMTP authentication username"
    })
    .AddParameter(new ChannelParameter("Password", ParameterType.String) 
    { 
        IsRequired = true,
        IsSensitive = true,
        Description = "SMTP authentication password"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer));

// Production usage - only predefined parameters allowed
var productionSettings = new ConnectionSettings()
    .SetParameter("Host", "smtp.company.com")
    .SetParameter("Port", 587)
    .SetParameter("Username", "service@company.com")
    .SetParameter("Password", "secure-password");

var productionMessage = new Dictionary<string, object?>
{
    { "Subject", "System Notification" },
    { "Priority", 2 }
};

// These will pass validation
var settingsResults = productionEmailSchema.ValidateConnectionSettings(productionSettings);
var messageResults = productionEmailSchema.ValidateMessageProperties(productionMessage);
```

### Development Environment (Flexible Mode)

```csharp
// Development schema - flexible for testing and debugging
var developmentEmailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithFlexibleMode()
    .WithDisplayName("Development Email Connector")
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true });

// Development usage - allows custom parameters for debugging
var developmentSettings = new ConnectionSettings()
    .SetParameter("Host", "localhost")
    .SetParameter("Port", 1025)
    .SetParameter("DebugMode", true)           // Custom parameter for debugging
    .SetParameter("LogLevel", "verbose")       // Custom parameter for logging
    .SetParameter("TestRecipient", "dev@test.com"); // Custom parameter for testing

var developmentMessage = new Dictionary<string, object?>
{
    { "Subject", "Test Message" },
    { "TestId", "TEST-123" },              // Custom property for test tracking
    { "DeveloperNotes", "Testing email delivery" }, // Custom property for debugging
    { "Environment", "development" }        // Custom property for environment identification
};

// These will pass validation - unknown parameters/properties are allowed
var settingsResults = developmentEmailSchema.ValidateConnectionSettings(developmentSettings);
var messageResults = developmentEmailSchema.ValidateMessageProperties(developmentMessage);

Console.WriteLine($"Settings validation passed: {!settingsResults.Any()}"); // True
Console.WriteLine($"Message validation passed: {!messageResults.Any()}");   // True
```

### Migration Scenario

```csharp
// Legacy system schema (flexible) for backwards compatibility
var legacySchema = new ChannelSchema("Legacy", "Email", "1.0.0")
    .WithFlexibleMode()
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true });

// New system schema (strict) for forward compatibility (default behavior)
var modernSchema = new ChannelSchema("Modern", "Email", "2.0.0")
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { IsRequired = true })
    .AddParameter(new ChannelParameter("EnableTLS", ParameterType.Boolean) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("ContentType", ParameterType.String) { IsRequired = true });

// Legacy configuration with old parameters
var legacySettings = new ConnectionSettings()
    .SetParameter("Host", "old.smtp.com")
    .SetParameter("OldTimeoutSetting", 30000)    // Legacy parameter
    .SetParameter("LegacySSLMode", "auto");      // Legacy parameter

var legacyMessage = new Dictionary<string, object?>
{
    { "Subject", "Legacy Message" },
    { "OldPriority", "high" },               // Legacy property
    { "LegacyFormat", "text" }               // Legacy property
};

// Legacy schema accepts old parameters (flexible mode)
var legacyValidation = legacySchema.ValidateConnectionSettings(legacySettings);
Console.WriteLine($"Legacy validation errors: {legacyValidation.Count()}"); // 0

// Modern schema rejects unknown parameters (strict mode)
var modernValidation = modernSchema.ValidateConnectionSettings(legacySettings);
Console.WriteLine($"Modern validation errors: {modernValidation.Count()}"); // 2+ errors
```

## Schema Derivation with Strict Mode

```csharp
// Base schema in strict mode (default)
var baseSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AddParameter(new ChannelParameter("BaseParam1", ParameterType.String))
    .AddParameter(new ChannelParameter("BaseParam2", ParameterType.String));

// Derived schema inherits strict mode from base
var strictDerived = new ChannelSchema(baseSchema, "Strict Derived")
    .RemoveParameter("BaseParam2");

Console.WriteLine($"Derived schema is strict: {strictDerived.IsStrict}"); // True

// But derived schema can override strict mode
var flexibleDerived = new ChannelSchema(baseSchema, "Flexible Derived")
    .WithFlexibleMode()
    .RemoveParameter("BaseParam2");

Console.WriteLine($"Base schema is strict: {baseSchema.IsStrict}");      // True
Console.WriteLine($"Derived schema is strict: {flexibleDerived.IsStrict}"); // False
```

## Best Practices

### Use Strict Mode When:
- **Production environments** where security and consistency are critical
- **Well-defined APIs** where only specific parameters should be accepted
- **Validation is important** and unknown parameters might indicate errors
- **Schema compliance** needs to be enforced

### Use Flexible Mode When:
- **Development and testing** where custom parameters aid debugging
- **Migration scenarios** where legacy parameters need to be supported
- **Extensibility is needed** and unknown parameters should be preserved
- **Backwards compatibility** is required

### Example: Environment-Specific Configuration

```csharp
public static ChannelSchema CreateEmailSchema(string environment)
{
    var baseSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
        .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
        .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
        .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true });

    return environment.ToLower() switch
    {
        "production" => baseSchema.WithDisplayName("Production Email"),      // Strict by default
        "staging" => baseSchema.WithDisplayName("Staging Email"),           // Strict by default
        "development" => baseSchema.WithFlexibleMode().WithDisplayName("Development Email"),
        "testing" => baseSchema.WithFlexibleMode().WithDisplayName("Testing Email"),
        _ => baseSchema.WithDisplayName("Default Email")                     // Strict by default
    };
}

// Usage
var prodSchema = CreateEmailSchema("production");    // Strict mode (default)
var devSchema = CreateEmailSchema("development");    // Flexible mode
```

This approach provides maximum flexibility while maintaining validation integrity based on your specific use case requirements.