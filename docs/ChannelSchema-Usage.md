# Channel Schema Usage Guide

The `ChannelSchema` class provides a fluent API for defining connector capabilities, configuration parameters, supported content types, and endpoint configurations. This guide covers all aspects of creating and using channel schemas.

## Table of Contents

1. [Basic Schema Creation](#basic-schema-creation)
2. [Configuration Parameters](#configuration-parameters)
3. [Endpoint Configuration](#endpoint-configuration)
4. [Content Types](#content-types)
5. [Capabilities](#capabilities)
6. [Message Properties](#message-properties)
7. [Authentication Types](#authentication-types)
8. [Schema Validation](#schema-validation)
9. [Best Practices](#best-practices)

## Basic Schema Creation

Every channel schema requires three core properties: provider, type, and version.

```csharp
using Deveel.Messaging;

// Minimal schema
var basicSchema = new ChannelSchema("MyProvider", "Email", "1.0.0");

// Schema with display name
var namedSchema = new ChannelSchema("SMTP", "Email", "1.2.0")
    .WithDisplayName("Corporate SMTP Connector");
```

### Core Properties

- **ChannelProvider**: Identifies the service provider (e.g., "Twilio", "SMTP", "SendGrid")
- **ChannelType**: Specifies the type of messaging channel (e.g., "SMS", "Email", "Push")
- **Version**: Semantic version for schema compatibility

## Configuration Parameters

Parameters define the connection and configuration settings required by the connector.

```csharp
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithDisplayName("SMTP Email Connector")
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
    });
```

### Parameter Types

- `ParameterType.String` - Text values
- `ParameterType.Integer` - Whole numbers (int, long, byte, short)
- `ParameterType.Number` - Decimal numbers (double, decimal, float) and integers
- `ParameterType.Boolean` - True/false values

### Parameter Properties

- **IsRequired**: Must be provided during connector initialization
- **DefaultValue**: Value used when parameter is not provided
- **IsSensitive**: Marks parameter as containing sensitive data (passwords, tokens)
- **Description**: Human-readable description for documentation

## Endpoint Configuration

Endpoints define what types of addresses the connector can send to or receive from. All endpoint types are strongly-typed using the `EndpointType` enumeration.

### Basic Endpoint Configuration

```csharp
var schema = new ChannelSchema("Provider", "Multi", "1.0.0")
    // Email addresses - bidirectional
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    
    // Phone numbers - send only
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: false)
    
    // URLs/Webhooks - receive only
    .AllowsMessageEndpoint(EndpointType.Url, asSender: false, asReceiver: true);
```

### Available Endpoint Types

```csharp
// Common endpoint types
.AllowsMessageEndpoint(EndpointType.EmailAddress)    // Email addresses
.AllowsMessageEndpoint(EndpointType.PhoneNumber)     // Phone numbers for SMS
.AllowsMessageEndpoint(EndpointType.Url)             // URLs for webhooks

// Identity-based endpoints
.AllowsMessageEndpoint(EndpointType.UserId)          // User identifiers
.AllowsMessageEndpoint(EndpointType.ApplicationId)   // Application identifiers
.AllowsMessageEndpoint(EndpointType.DeviceId)        // Device identifiers

// Generic endpoints
.AllowsMessageEndpoint(EndpointType.Id)              // Generic identifiers
.AllowsMessageEndpoint(EndpointType.Label)           // Alpha-numeric labels
.AllowsMessageEndpoint(EndpointType.Topic)           // Queue/topic names

// Wildcard endpoint
.AllowsAnyMessageEndpoint()                          // Accept any endpoint type
```

### Advanced Endpoint Configuration

```csharp
var twilioSchema = new ChannelSchema("Twilio", "SMS", "2.0.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: true)
    .AllowsMessageEndpoint(EndpointType.Url, asSender: false, asReceiver: true)
    
    // Use HandlesMessageEndpoint for more control
    .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.ApplicationId)
    {
        CanSend = true,
        CanReceive = false,
        IsRequired = true,
        Description = "Twilio application identifier"
    });
```

### Endpoint Configuration Rules

- **Unique Types**: Each endpoint type can only be configured once per schema
- **Exception on Duplicate**: Adding duplicate endpoint types throws `InvalidOperationException`
- **Wildcard Exclusive**: Using `AllowsAnyMessageEndpoint()` prevents adding specific endpoint types

```csharp
// ? This works
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: false);

// ? This throws InvalidOperationException
schema.AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: false, asReceiver: true);
```

## Content Types

Content types specify what kinds of message content the connector supports.

```csharp
var multiChannelSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AddContentType(MessageContentType.PlainText)    // Simple text messages
    .AddContentType(MessageContentType.Html)         // HTML formatted messages
    .AddContentType(MessageContentType.Multipart)    // Messages with attachments
    .AddContentType(MessageContentType.Template)     // Template-based messages
    .AddContentType(MessageContentType.Media)        // Media files
    .AddContentType(MessageContentType.Json)         // JSON structured data
    .AddContentType(MessageContentType.Binary);      // Binary data streams
```

### Content Type Descriptions

- **PlainText**: Simple text messages without formatting
- **Html**: Rich HTML content with formatting and styling
- **Multipart**: Messages with multiple parts/attachments
- **Template**: Template-based content for dynamic generation
- **Media**: Media files (images, audio, video, documents)
- **Json**: JSON structured data messages
- **Binary**: Binary data streams

## Capabilities

Capabilities declare what operations the connector supports.

```csharp
var advancedSchema = new ChannelSchema("SendGrid", "Email", "3.0.0")
    .WithCapabilities(
        ChannelCapability.SendMessages |           // Can send messages
        ChannelCapability.ReceiveMessages |        // Can receive messages  
        ChannelCapability.MessageStatusQuery |     // Can query message status
        ChannelCapability.BulkMessaging |          // Supports batch operations
        ChannelCapability.Templates |              // Supports templates
        ChannelCapability.MediaAttachments |       // Supports attachments
        ChannelCapability.HealthCheck)             // Provides health monitoring
    
    // Or add capabilities individually
    .WithCapability(ChannelCapability.HandlerMessageState);
```

### Available Capabilities

- `ChannelCapability.SendMessages` - Basic message sending (default)
- `ChannelCapability.ReceiveMessages` - Can receive incoming messages
- `ChannelCapability.MessageStatusQuery` - Can check message delivery status
- `ChannelCapability.BulkMessaging` - Supports sending multiple messages at once
- `ChannelCapability.Templates` - Supports template-based content
- `ChannelCapability.MediaAttachments` - Can handle media attachments
- `ChannelCapability.HealthCheck` - Provides health/status monitoring
- `ChannelCapability.HandlerMessageState` - Can handle message state updates

## Message Properties

Message properties define what properties messages should have when processed by the connector.

```csharp
var emailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
    {
        IsRequired = true,
        Description = "Email subject line"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer)
    {
        IsRequired = false,
        Description = "Email priority level (1-5)"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean)
    {
        IsRequired = false,
        Description = "Whether email content is HTML formatted"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("ReplyTo", ParameterType.String)
    {
        IsRequired = false,
        Description = "Reply-to email address"
    });
```

### Message Property Validation

```csharp
// Validate message properties against schema
var messageProperties = new Dictionary<string, object?>
{
    { "Subject", "Important Update" },
    { "Priority", 2 },
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

### Message Property Rules

- **Unique Names**: Each property name can only be configured once per schema
- **Case Insensitive**: Property names are compared case-insensitively
- **Type Safety**: Properties use the same `ParameterType` enum as parameters
- **Required Properties**: Must be present and not null in messages
- **Type Compatibility**: Values must match the defined `ParameterType`
- **Unknown Properties**: Properties not defined in schema are flagged as errors

## Authentication Types

Specify what authentication methods the connector supports.

```csharp
var schema = new ChannelSchema("API", "Generic", "1.0.0")
    .AddAuthenticationType(AuthenticationType.None)      // No authentication
    .AddAuthenticationType(AuthenticationType.Basic)     // Basic authentication
    .AddAuthenticationType(AuthenticationType.Token)     // Token-based
    .AddAuthenticationType(AuthenticationType.OAuth)     // OAuth flow
    .AddAuthenticationType(AuthenticationType.Custom);   // Custom authentication
```

## Schema Validation

The framework provides built-in validation for schema configurations:

```csharp
// Validation happens automatically when:
// 1. Adding duplicate endpoint types
// 2. Adding duplicate message properties
// 3. Validating message properties against schema

try
{
    var schema = new ChannelSchema("Provider", "Type", "1.0.0")
        .AllowsMessageEndpoint(EndpointType.EmailAddress)
        .AllowsMessageEndpoint(EndpointType.EmailAddress); // Throws InvalidOperationException
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Schema validation error: {ex.Message}");
}
```

## Complete Examples

### SMS Connector Schema

```csharp
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
        Description = "Sender phone number in E.164 format"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Recipient phone number in E.164 format"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String)
    {
        IsRequired = false,
        Description = "SMS message type (transactional, promotional)"
    })
    .AddAuthenticationType(AuthenticationType.Token);
```

### Email Connector Schema

```csharp
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
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
    {
        IsRequired = true,
        Description = "Email subject line"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer)
    {
        IsRequired = false,
        Description = "Email priority level (1-5)"
    })
    .AddAuthenticationType(AuthenticationType.Basic);
```

## Best Practices

### 1. Use Descriptive Names

```csharp
// ? Good - Clear and descriptive
var schema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Connector for Customer Notifications");

// ? Avoid - Too generic
var schema = new ChannelSchema("Provider", "Type", "1.0.0");
```

### 2. Provide Comprehensive Descriptions

```csharp
// ? Good - Detailed descriptions
.AddParameter(new ChannelParameter("ApiKey", ParameterType.String)
{
    IsRequired = true,
    IsSensitive = true,
    Description = "API key for authentication - obtain from provider dashboard"
})

// ? Avoid - Missing or vague descriptions
.AddParameter(new ChannelParameter("Key", ParameterType.String))
```

### 3. Use Appropriate Default Values

```csharp
// ? Good - Sensible defaults
.AddParameter(new ChannelParameter("Port", ParameterType.Integer)
{
    DefaultValue = 587,  // Standard SMTP port
    Description = "SMTP server port"
})
```

### 4. Mark Sensitive Parameters

```csharp
// ? Good - Mark sensitive data
.AddParameter(new ChannelParameter("Password", ParameterType.String)
{
    IsRequired = true,
    IsSensitive = true,  // Important for security
    Description = "Authentication password"
})
```

### 5. Configure Appropriate Capabilities

```csharp
// ? Good - Only declare supported capabilities
.WithCapabilities(
    ChannelCapability.SendMessages |        // Basic requirement
    ChannelCapability.MessageStatusQuery |  // If status tracking is supported
    ChannelCapability.HealthCheck)          // If health monitoring is available

// ? Avoid - Don't declare unsupported capabilities
.WithCapabilities(ChannelCapability.BulkMessaging) // Don't add if not supported
```

### 6. Use Semantic Versioning

```csharp
// ? Good - Follow semantic versioning
new ChannelSchema("Provider", "Type", "1.2.0")  // Major.Minor.Patch
new ChannelSchema("Provider", "Type", "2.0.0")  // Breaking changes
new ChannelSchema("Provider", "Type", "1.2.1")  // Bug fixes
```

### 7. Validate Schema Configuration

```csharp
// ? Good - Test your schema
var schema = CreateMySchema();

// Test endpoint configuration
Assert.Contains(schema.Endpoints, e => e.Type == EndpointType.EmailAddress);

// Test required parameters
Assert.Contains(schema.Parameters, p => p.Name == "ApiKey" && p.IsRequired);

// Test capabilities
Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
```

## Schema Interface Usage

```csharp
// Use as interface for polymorphic behavior
IChannelSchema[] schemas = {
    CreateSmsSchema(),
    CreateEmailSchema(),
    CreatePushSchema()
};

foreach (var schema in schemas)
{
    Console.WriteLine($"Provider: {schema.ChannelProvider}");
    Console.WriteLine($"Type: {schema.ChannelType}");
    Console.WriteLine($"Version: {schema.Version}");
    Console.WriteLine($"Capabilities: {schema.Capabilities}");
    Console.WriteLine($"Endpoints: {schema.Endpoints.Count}");
    Console.WriteLine($"Parameters: {schema.Parameters.Count}");
    Console.WriteLine();
}
```

This comprehensive guide covers all aspects of creating and configuring channel schemas. The fluent API design makes it easy to build complex configurations while maintaining type safety and validation.