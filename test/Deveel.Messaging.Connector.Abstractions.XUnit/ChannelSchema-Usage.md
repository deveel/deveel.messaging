# ChannelSchema Implementation Usage Examples

This document demonstrates how to use the `ChannelSchema` base implementation of `IChannelSchema` with the updated strongly-typed endpoint system.

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
    .AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean)
    {
        DefaultValue = true,
        Description = "Enable SSL/TLS encryption"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
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
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String)
    {
        IsRequired = true,
        Description = "Twilio Account SID"
    })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Twilio Auth Token"
    })
    .AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Sender phone number"
    })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: true)
    .AllowsMessageEndpoint(EndpointType.Url, asSender: false, asReceiver: true)
    .AddAuthenticationType(AuthenticationType.Token);
```

## Endpoint Configuration

The `ChannelSchema` supports configuring different types of endpoints using strongly-typed `EndpointType` enumeration. **Each endpoint type can only be configured once per schema** to avoid conflicts.

### Adding Specific Endpoint Types

```csharp
var schema = new ChannelSchema("Provider", "Multi", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: false)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: false, asReceiver: true)
    .AllowsMessageEndpoint(EndpointType.Url, asSender: true, asReceiver: true);
```

### Available Endpoint Types

```csharp
// Communication endpoints
.AllowsMessageEndpoint(EndpointType.EmailAddress)    // Email addresses
.AllowsMessageEndpoint(EndpointType.PhoneNumber)     // Phone numbers for SMS/voice
.AllowsMessageEndpoint(EndpointType.Url)             // URLs for webhooks

// Identity endpoints
.AllowsMessageEndpoint(EndpointType.UserId)          // User identifiers
.AllowsMessageEndpoint(EndpointType.ApplicationId)   // Application identifiers
.AllowsMessageEndpoint(EndpointType.DeviceId)        // Device identifiers

// System endpoints
.AllowsMessageEndpoint(EndpointType.Topic)           // Queue/topic names
.AllowsMessageEndpoint(EndpointType.Id)              // Generic identifiers
.AllowsMessageEndpoint(EndpointType.Label)           // Alpha-numeric labels
```

### Adding Any Endpoint (Wildcard)

```csharp
var flexibleSchema = new ChannelSchema("Universal", "Flexible", "1.0.0")
    .AllowsAnyMessageEndpoint(); // Accepts any endpoint type
```

### Endpoint Configuration Rules

- **Unique Types**: Each endpoint type can only be added once to a schema
- **Exception on Duplicate**: Attempting to add a duplicate endpoint type throws `InvalidOperationException`
- **Wildcard Exclusive**: Using `AllowsAnyMessageEndpoint()` prevents adding specific endpoint types

```csharp
// ? This works
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: false);

// ? This throws InvalidOperationException
schema.AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: false, asReceiver: true); // Duplicate
```

### Advanced Endpoint Configuration

```csharp
// Use HandlesMessageEndpoint for detailed configuration
var advancedSchema = new ChannelSchema("Provider", "Push", "1.0.0")
    .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.ApplicationId)
    {
        CanSend = true,
        CanReceive = false,
        IsRequired = true,
        Description = "Application identifier for push notifications"
    })
    .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.DeviceId)
    {
        CanSend = true,
        CanReceive = true,
        IsRequired = false,
        Description = "Optional device identifier for targeted messaging"
    });
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
        .AddContentType(MessageContentType.PlainText)
        .AllowsMessageEndpoint(EndpointType.EmailAddress),
    new ChannelSchema("Provider2", "SMS", "2.0.0")
        .AddContentType(MessageContentType.PlainText)
        .AllowsMessageEndpoint(EndpointType.PhoneNumber),
    new ChannelSchema("Provider3", "Push", "1.5.0")
        .AddContentType(MessageContentType.Json)
        .AllowsMessageEndpoint(EndpointType.ApplicationId)
        .AllowsMessageEndpoint(EndpointType.DeviceId)
};

foreach (var schema in schemas)
{
    Console.WriteLine($"Provider: {schema.ChannelProvider}, Type: {schema.ChannelType}");
    Console.WriteLine($"Capabilities: {schema.Capabilities}");
    Console.WriteLine($"Parameters: {schema.Parameters.Count}");
    Console.WriteLine($"Content Types: [{string.Join(", ", schema.ContentTypes)}]");
    Console.WriteLine($"Endpoints: [{string.Join(", ", schema.Endpoints.Select(e => e.Type))}]");
}
```

## Key Features

1. **Fluent API**: Method chaining for easy configuration
2. **Type Safety**: Strongly typed content types and endpoint types using enums
3. **Validation**: Automatic validation of required parameters and endpoint configurations
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
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: true);

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

## Multi-Channel Connector Example

```csharp
// Complex multi-channel connector supporting multiple endpoint types
var multiChannelSchema = new ChannelSchema("Universal", "MultiChannel", "2.0.0")
    .WithDisplayName("Universal Multi-Channel Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery |
        ChannelCapability.BulkMessaging |
        ChannelCapability.Templates |
        ChannelCapability.MediaAttachments |
        ChannelCapability.HealthCheck)
    .AddParameter(new ChannelParameter("ApiKey", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Universal API key for authentication"
    })
    .AddParameter(new ChannelParameter("Region", ParameterType.String)
    {
        IsRequired = true,
        DefaultValue = "us-east-1",
        Description = "Service region for message delivery"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Json)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)     // Email delivery
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)      // SMS delivery
    .AllowsMessageEndpoint(EndpointType.Url)              // Webhook callbacks
    .AllowsMessageEndpoint(EndpointType.ApplicationId)    // Push notifications
    .AllowsMessageEndpoint(EndpointType.Topic)            // Queue messaging
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer)
    {
        IsRequired = false,
        Description = "Message priority (1=Low, 2=Normal, 3=High)"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("TrackingId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Optional tracking identifier for analytics"
    })
    .AddAuthenticationType(AuthenticationType.Token)
    .AddAuthenticationType(AuthenticationType.OAuth);
```

## Using Endpoints with Messages

```csharp
// Create endpoints using strongly-typed factory methods
var emailEndpoint = Endpoint.EmailAddress("user@example.com");
var phoneEndpoint = Endpoint.PhoneNumber("+1234567890");
var webhookEndpoint = Endpoint.Url("https://api.example.com/webhook");
var appEndpoint = Endpoint.Application("mobile-app-v2");

// Create message using typed endpoints
var message = new MessageBuilder()
    .WithId("multi-001")
    .WithSender(emailEndpoint)
    .WithReceiver(phoneEndpoint)
    .WithHtmlContent("<h1>Welcome!</h1><p>Your account has been activated.</p>")
    .WithProperty("Priority", 3)
    .WithProperty("TrackingId", "TRACK-12345")
    .Message;

// Alternative using MessageBuilder convenience methods
var message2 = new MessageBuilder()
    .WithId("multi-002")
    .WithEmailSender("system@company.com")
    .WithWebhookReceiver("https://customer.com/notifications")
    .WithJsonContent(new { event = "user.signup", userId = "user123" })
    .Message;
```

This updated documentation reflects the new strongly-typed endpoint system while maintaining clarity and comprehensive coverage of all ChannelSchema features.