# ChannelSchema Implementation Usage Examples

This document demonstrates how to use the `ChannelSchema` base implementation of `IChannelSchema`.

## Basic Usage

```csharp
// Create a simple schema
var schema = new ChannelSchema("MyProvider", "Email", "1.0.0");
```

## Advanced Configuration with Fluent API

```csharp
// Configure an email connector schema
var emailSchema = new ChannelSchema("SMTP", "Email", "1.2.0")
    .WithDisplayName("SMTP Email Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.Templates | 
        ChannelCapability.MediaAttachments |
        ChannelCapability.HealthCheck)
    .AddParameter(new ConnectorParameter("Host", ParameterType.String)
    {
        IsRequired = true,
        Description = "SMTP server hostname"
    })
    .AddParameter(new ConnectorParameter("Port", ParameterType.Integer)
    {
        IsRequired = true,
        DefaultValue = 587,
        Description = "SMTP server port"
    })
    .AddParameter(new ConnectorParameter("Username", ParameterType.String)
    {
        IsRequired = true,
        Description = "SMTP authentication username"
    })
    .AddParameter(new ConnectorParameter("Password", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "SMTP authentication password"
    })
    .AddParameter(new ConnectorParameter("EnableSsl", ParameterType.Boolean)
    {
        DefaultValue = true,
        Description = "Enable SSL/TLS encryption"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AddAuthenticationType(AuthenticationType.Basic);
```

## SMS Connector Example

```csharp
// Configure an SMS connector schema
var smsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery |
        ChannelCapability.BulkMessaging)
    .AddParameter(new ConnectorParameter("AccountSid", ParameterType.String)
    {
        IsRequired = true,
        Description = "Twilio Account SID"
    })
    .AddParameter(new ConnectorParameter("AuthToken", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Twilio Auth Token"
    })
    .AddParameter(new ConnectorParameter("FromNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Sender phone number"
    })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint("sms", asSender: true, asReceiver: true)
    .AllowsMessageEndpoint("webhook", asSender: false, asReceiver: true)
    .AddAuthenticationType(AuthenticationType.Token);
```

## Endpoint Configuration

The `ChannelSchema` supports configuring different types of endpoints that the connector can handle. **Each endpoint type can only be configured once per schema** to avoid conflicts.

### Adding Specific Endpoint Types

```csharp
var schema = new ChannelSchema("Provider", "Multi", "1.0.0")
    .AllowsMessageEndpoint("email", asSender: true, asReceiver: false)
    .AllowsMessageEndpoint("sms", asSender: false, asReceiver: true)
    .AllowsMessageEndpoint("webhook", asSender: true, asReceiver: true);
```

### Adding Any Endpoint (Wildcard)

```csharp
var flexibleSchema = new ChannelSchema("Universal", "Flexible", "1.0.0")
    .AllowsAnyMessageEndpoint(); // Accepts any endpoint type
```

### Endpoint Configuration Rules

- **Unique Types**: Each endpoint type can only be added once to a schema
- **Case Insensitive**: Endpoint types are compared case-insensitively ("EMAIL" and "email" are considered the same)
- **Exception on Duplicate**: Attempting to add a duplicate endpoint type throws `InvalidOperationException`

```csharp
// ? This works
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint("email", asSender: true, asReceiver: false);

// ? This throws InvalidOperationException
schema.AllowsMessageEndpoint("EMAIL", asSender: false, asReceiver: true); // Case insensitive duplicate
```

## Message Content Types

The `AddContentType` method accepts `MessageContentType` enum values, providing better type safety:

```csharp
var schema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AddContentType(MessageContentType.PlainText)    // Plain text messages
    .AddContentType(MessageContentType.Html)         // HTML formatted messages
    .AddContentType(MessageContentType.Multipart)    // Multipart messages with attachments
    .AddContentType(MessageContentType.Template)     // Template-based messages
    .AddContentType(MessageContentType.Media)        // Media content (images, videos, etc.)
    .AddContentType(MessageContentType.Json)         // JSON structured data
    .AddContentType(MessageContentType.Binary);      // Binary data
```

## Available Message Content Types

- **PlainText**: Simple text messages without formatting
- **Html**: Rich HTML content with formatting and styling
- **Multipart**: Messages with multiple parts/attachments
- **Template**: Template-based content for dynamic generation
- **Media**: Media files (images, audio, video, documents)
- **Json**: JSON structured data messages
- **Binary**: Binary data streams

## Polymorphic Usage

```csharp
// Use as interface for polymorphic behavior
IChannelSchema[] schemas = {
    new ChannelSchema("Provider1", "Email", "1.0.0")
        .AddContentType(MessageContentType.Html)
        .AddContentType(MessageContentType.PlainText),
    new ChannelSchema("Provider2", "SMS", "2.0.0")
        .AddContentType(MessageContentType.PlainText),
    new ChannelSchema("Provider3", "Push", "1.5.0")
        .AddContentType(MessageContentType.Json)
};

foreach (var schema in schemas)
{
    Console.WriteLine($"Provider: {schema.ChannelProvider}, Type: {schema.ChannelType}");
    Console.WriteLine($"Capabilities: {schema.Capabilities}");
    Console.WriteLine($"Parameters: {schema.Parameters.Count}");
    Console.WriteLine($"Content Types: [{string.Join(", ", schema.ContentTypes)}]");
}
```

## Key Features

1. **Fluent API**: Method chaining for easy configuration
2. **Type Safety**: Strongly typed content types using enum instead of strings
3. **Validation**: Automatic validation of required parameters
4. **Extensibility**: Can be inherited for custom implementations
5. **Standard Compliance**: Implements IChannelSchema interface
6. **Parameter Support**: All parameter types (Boolean, Integer, Number, String)
7. **Capability Flags**: Support for all connector capabilities
8. **Authentication Types**: Support for all authentication methods
9. **Content Type Safety**: Enum-based content types prevent typos and provide IntelliSense support
10. **Unique Endpoint Types**: Prevents duplicate endpoint configurations with automatic validation
11. **Message Property Configuration**: Define and validate properties expected in messages
12. **Comprehensive Validation**: Built-in validation for both connection settings and message properties

## Message Property Configuration

The `ChannelSchema` supports defining properties that messages processed by the connector should have. **Each message property can only be configured once per schema** to avoid conflicts.

### Adding Message Properties

```csharp
var emailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer)
    {
        IsRequired = true,
        Description = "Email priority level (1-5)"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
    {
        IsRequired = true,
        Description = "Email subject line"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean)
    {
        IsRequired = false,
        Description = "Whether email content is HTML formatted"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Sensitivity", ParameterType.String)
    {
        IsRequired = false,
        IsSensitive = true,
        Description = "Email sensitivity level for compliance"
    });
```

### Message Property Configuration Rules

- **Unique Names**: Each message property name can only be added once to a schema
- **Case Insensitive**: Property names are compared case-insensitively ("PRIORITY" and "priority" are considered the same)
- **Exception on Duplicate**: Attempting to add a duplicate property name throws `InvalidOperationException`
- **Type Safety**: Properties use the same `ParameterType` enum as connection parameters

```csharp
// ? This works
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer));

// ? This throws InvalidOperationException
schema.AddMessageProperty(new MessagePropertyConfiguration("PRIORITY", ParameterType.String)); // Case insensitive duplicate
```

### Message Property Validation

You can validate message properties against the schema:

```csharp
var messageProperties = new Dictionary<string, object?>
{
    { "Priority", 2 },
    { "Subject", "Important Update" },
    { "IsHtml", true }
};

var validationResults = emailSchema.ValidateMessageProperties(messageProperties);

if (validationResults.Any())
{
    foreach (var error in validationResults)
    {
        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    }
}
```

### Message Property Validation Rules

- **Required Properties**: Must be present and not null
- **Type Compatibility**: Values must match the defined `ParameterType`
- **Unknown Properties**: Properties not defined in the schema are flagged as errors
- **Sensitive Properties**: Marked with `IsSensitive` flag for special handling

### SMS Connector Example with Message Properties

```csharp
var smsSchema = new ChannelSchema("Twilio", "SMS", "3.0.0")
    .WithDisplayName("Enhanced Twilio SMS Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery)
    .AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Recipient phone number in E.164 format"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String)
    {
        IsRequired = false,
        Description = "Type of SMS message (transactional, promotional, etc.)"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("IsUrgent", ParameterType.Boolean)
    {
        IsRequired = false,
        Description = "Whether message requires urgent delivery"
    })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint("sms", asSender: true, asReceiver: true);

// Validate message properties
var messageProperties = new Dictionary<string, object?>
{
    { "PhoneNumber", "+1234567890" },
    { "MessageType", "transactional" },
    { "IsUrgent", false }
};

var results = smsSchema.ValidateMessageProperties(messageProperties);
// Results will be empty if validation passes
```

### Available Parameter Types for Message Properties

The same parameter types available for connection parameters are also available for message properties:

- **Boolean**: True/false values
- **Integer**: Whole numbers (int, long, byte, short, sbyte)
- **Number**: Decimal numbers (double, decimal, float) and integers
- **String**: Text values