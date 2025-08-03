# Validation Extension Methods Usage Examples

## Overview

The validation methods have been moved from `ChannelSchema` to `ChannelSchemaExtensions`, making them available for **any** implementation of `IChannelSchema`. This document provides examples of how to use these extension methods.

## ? Extension Methods Available

### **Connection Settings Validation**
```csharp
IEnumerable<ValidationResult> ValidateConnectionSettings(this IChannelSchema schema, ConnectionSettings connectionSettings)
```

### **Message Properties Validation**
```csharp
IEnumerable<ValidationResult> ValidateMessageProperties(this IChannelSchema schema, IDictionary<string, object?> messageProperties)
```

### **Schema Compatibility**
```csharp
string GetLogicalIdentity(this IChannelSchema schema)
bool IsCompatibleWith(this IChannelSchema schema, IChannelSchema otherSchema)
IEnumerable<ValidationResult> ValidateAsRestrictionOf(this IChannelSchema schema, IChannelSchema targetSchema)
```

## ?? Usage Examples

### **With ChannelSchema Class**
```csharp
// Standard ChannelSchema implementation
var schema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .AddAuthenticationType(AuthenticationType.Basic)
    .AddParameter(new ChannelParameter("AccountSid", DataType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", DataType.String) { IsRequired = true });

var connectionSettings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC123456789")
    .SetParameter("AuthToken", "token123");

// Extension method works seamlessly
var validationResults = schema.ValidateConnectionSettings(connectionSettings);

if (!validationResults.Any())
{
    Console.WriteLine("? Connection settings are valid!");
}
```

### **With Custom IChannelSchema Implementation**
```csharp
// Custom implementation of IChannelSchema
public class CustomChannelSchema : IChannelSchema
{
    public string ChannelProvider => "Custom";
    public string ChannelType => "Api";
    public string Version => "1.0.0";
    public string? DisplayName => "Custom API Schema";
    public bool IsStrict => true;
    public ChannelCapability Capabilities => ChannelCapability.SendMessages;
    
    public IList<ChannelParameter> Parameters { get; } = new List<ChannelParameter>
    {
        new ChannelParameter("ApiKey", DataType.String) { IsRequired = true },
        new ChannelParameter("BaseUrl", DataType.String) { IsRequired = true }
    };
    
    public IList<MessagePropertyConfiguration> MessageProperties { get; } = new List<MessagePropertyConfiguration>
    {
        new MessagePropertyConfiguration("Content", DataType.String) { IsRequired = true }
    };
    
    public IList<MessageContentType> ContentTypes { get; } = new List<MessageContentType>
    {
        MessageContentType.Json
    };
    
    public IList<AuthenticationType> AuthenticationTypes { get; } = new List<AuthenticationType>
    {
        AuthenticationType.ApiKey
    };
    
    public IList<ChannelEndpointConfiguration> Endpoints { get; } = new List<ChannelEndpointConfiguration>
    {
        new ChannelEndpointConfiguration(EndpointType.Url)
    };
}

// Usage - Extension methods work with ANY IChannelSchema implementation
var customSchema = new CustomChannelSchema();

var settings = new ConnectionSettings()
    .SetParameter("ApiKey", "sk-abc123")
    .SetParameter("BaseUrl", "https://api.example.com");

var messageProperties = new Dictionary<string, object?>
{
    { "Content", "{\"message\": \"Hello World\"}" }
};

// All extension methods work seamlessly
var connectionResults = customSchema.ValidateConnectionSettings(settings);
var messageResults = customSchema.ValidateMessageProperties(messageProperties);
var identity = customSchema.GetLogicalIdentity();

Console.WriteLine($"Schema Identity: {identity}"); // "Custom/Api/1.0.0"
Console.WriteLine($"Connection Valid: {!connectionResults.Any()}");
Console.WriteLine($"Message Valid: {!messageResults.Any()}");
```

### **Polymorphic Usage**
```csharp
public void ValidateSchemaSettings(IChannelSchema schema, ConnectionSettings settings)
{
    // Works with ANY IChannelSchema implementation - ChannelSchema, custom implementations, etc.
    var results = schema.ValidateConnectionSettings(settings);
    
    foreach (var error in results)
    {
        Console.WriteLine($"? Validation Error: {error.ErrorMessage}");
    }
    
    if (!results.Any())
    {
        Console.WriteLine($"? Schema '{schema.GetLogicalIdentity()}' settings are valid!");
    }
}

// Can be called with any schema implementation
ValidateSchemaSettings(new ChannelSchema("Provider", "Type", "1.0"), settings);
ValidateSchemaSettings(new CustomChannelSchema(), settings);
ValidateSchemaSettings(someOtherSchemaImplementation, settings);
```

### **Authentication Validation Examples**

#### **API Key Authentication**
```csharp
var apiSchema = new ChannelSchema("Service", "API", "1.0.0")
    .AddAuthenticationType(AuthenticationType.ApiKey);

// Valid - has API key
var validSettings = new ConnectionSettings()
    .SetParameter("ApiKey", "sk-1234567890");

var results = apiSchema.ValidateConnectionSettings(validSettings);
Assert.Empty(results); // No validation errors

// Invalid - missing API key
var invalidSettings = new ConnectionSettings()
    .SetParameter("SomeOtherParam", "value");

var invalidResults = apiSchema.ValidateConnectionSettings(invalidSettings);
Assert.NotEmpty(invalidResults); // Will have authentication validation error
```

#### **Basic Authentication**
```csharp
var basicSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AddAuthenticationType(AuthenticationType.Basic);

// Valid - has username/password
var validBasicSettings = new ConnectionSettings()
    .SetParameter("Username", "user@example.com")
    .SetParameter("Password", "secret123");

var basicResults = basicSchema.ValidateConnectionSettings(validBasicSettings);
Assert.Empty(basicResults); // Valid

// Also valid - Twilio-style AccountSid/AuthToken
var twilioSettings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC123456789")
    .SetParameter("AuthToken", "token123");

var twilioResults = basicSchema.ValidateConnectionSettings(twilioSettings);
Assert.Empty(twilioResults); // Also valid for Basic auth
```

### **Strict Mode Validation**
```csharp
// Strict schema (default)
var strictSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AddParameter(new ChannelParameter("RequiredParam", DataType.String) { IsRequired = true });

// Flexible schema
var flexibleSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithFlexibleMode()
    .AddParameter(new ChannelParameter("RequiredParam", DataType.String) { IsRequired = true });

var settings = new ConnectionSettings()
    .SetParameter("RequiredParam", "value")
    .SetParameter("UnknownParam", "should be rejected in strict mode");

// Strict mode rejects unknown parameters
var strictResults = strictSchema.ValidateConnectionSettings(settings);
Assert.NotEmpty(strictResults); // Has error for "UnknownParam"

// Flexible mode allows unknown parameters  
var flexibleResults = flexibleSchema.ValidateConnectionSettings(settings);
Assert.Empty(flexibleResults); // No errors
```

### **Message Properties Validation**
```csharp
var messageSchema = new ChannelSchema("Email", "Provider", "1.0.0")
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", DataType.Integer) { IsRequired = false });

var messageProperties = new Dictionary<string, object?>
{
    { "Subject", "Important Update" },
    { "Priority", 2 },
    { "IsHtml", true } // Unknown property in strict mode
};

// Extension method validates message properties
var validationResults = messageSchema.ValidateMessageProperties(messageProperties);

// Check if strict mode would reject unknown property
if (messageSchema.IsStrict)
{
    Assert.Contains(validationResults, r => r.ErrorMessage!.Contains("Unknown message property 'IsHtml'"));
}
```

## ?? Key Benefits

### **? Universal Compatibility**
- Works with `ChannelSchema` class
- Works with any custom `IChannelSchema` implementation
- Same validation logic regardless of implementation

### **? Polymorphic Support**
- Methods accept `IChannelSchema` interface
- No coupling to specific concrete classes
- Perfect for dependency injection scenarios

### **? Consistent Validation**
- Same authentication validation logic across all implementations
- Same strict/flexible mode behavior
- Same error messages and validation rules

### **? Easy to Use**
- Extension methods appear in IntelliSense naturally
- No need to remember which class has which methods
- Fluent, discoverable API

## ?? Migration Notes

### **No Breaking Changes**
```csharp
// This code continues to work exactly the same
var schema = new ChannelSchema("Provider", "Type", "1.0.0");
var results = schema.ValidateConnectionSettings(settings);

// The compiler automatically resolves to the extension method
// No code changes required!
```

### **New Capabilities**
```csharp
// Now you can validate ANY IChannelSchema implementation
IChannelSchema anySchema = GetSchemaFromSomewhere();
var results = anySchema.ValidateConnectionSettings(settings); // Works!
```

This refactoring makes the validation system more flexible and reusable while maintaining full backward compatibility.