# Endpoint Type Usage Guide

The Deveel Messaging Framework uses strongly-typed endpoint configurations through the `EndpointType` enumeration. This guide covers the complete endpoint type system and how to use it effectively.

## Table of Contents

1. [Overview](#overview)
2. [Available Endpoint Types](#available-endpoint-types)
3. [Schema Configuration](#schema-configuration)
4. [Message Creation](#message-creation)
5. [Endpoint Validation](#endpoint-validation)
6. [Backward Compatibility](#backward-compatibility)
7. [Best Practices](#best-practices)
8. [Migration Guide](#migration-guide)

## Overview

The `EndpointType` enumeration provides type safety and IntelliSense support for endpoint configurations. Instead of using error-prone string literals, you now use strongly-typed enum values that prevent typos and provide better developer experience.

### Benefits of Endpoint Type Safety

- **Type Safety**: Compile-time validation prevents runtime errors
- **IntelliSense Support**: IDE provides auto-completion and documentation
- **Consistency**: Eliminates string-based variations and typos
- **Validation**: Automatic validation against supported endpoint types
- **Documentation**: Self-documenting code with clear type intentions

## Available Endpoint Types

```csharp
public enum EndpointType
{
    PhoneNumber,     // Phone numbers for SMS/voice
    EmailAddress,    // Email addresses
    Url,             // URLs for webhooks/web services
    Topic,           // Queue/topic names for message brokers
    Id,              // Generic identifiers
    UserId,          // User identifiers within a platform
    ApplicationId,   // Application identifiers
    DeviceId,        // Device identifiers for push notifications
    Label,           // Alpha-numeric labels
    Any = 122        // Wildcard for any endpoint type
}
```

### Endpoint Type Descriptions

| Endpoint Type | Description | Example Usage |
|---------------|-------------|---------------|
| `EmailAddress` | Email addresses | SMTP, email services |
| `PhoneNumber` | Phone numbers with country codes | SMS, voice services |
| `Url` | URLs for webhooks and web services | HTTP callbacks, webhooks |
| `Topic` | Queue/topic names in messaging systems | RabbitMQ, Kafka, Service Bus |
| `UserId` | User identifiers within platforms | Social media, internal systems |
| `ApplicationId` | Application identifiers | Push notifications, app-to-app |
| `DeviceId` | Device identifiers | Mobile push, IoT devices |
| `Id` | Generic identifiers | Custom systems, flexible usage |
| `Label` | Alpha-numeric labels | Short codes, aliases |
| `Any` | Wildcard accepting any endpoint type | Universal connectors |

## Schema Configuration

### Basic Endpoint Configuration

```csharp
using Deveel.Messaging;

// Configure specific endpoint types
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress);

var smsSchema = new ChannelSchema("Twilio", "SMS", "2.0.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

var webhookSchema = new ChannelSchema("Generic", "Webhook", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.Url);
```

### Directional Endpoint Configuration

```csharp
// Configure send/receive capabilities
var schema = new ChannelSchema("Provider", "Multi", "1.0.0")
    // Email - bidirectional
    .AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: true)
    
    // SMS - send only
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: false)
    
    // Webhooks - receive only
    .AllowsMessageEndpoint(EndpointType.Url, asSender: false, asReceiver: true);
```

### Advanced Endpoint Configuration

```csharp
// Use HandlesMessageEndpoint for detailed configuration
var advancedSchema = new ChannelSchema("Provider", "Advanced", "1.0.0")
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

### Wildcard Endpoint Configuration

```csharp
// Accept any endpoint type
var universalSchema = new ChannelSchema("Universal", "Flexible", "1.0.0")
    .AllowsAnyMessageEndpoint(); // Equivalent to EndpointType.Any

// Note: Cannot add specific endpoints after using wildcard
```

## Message Creation

### Using Endpoint Factory Methods

```csharp
using Deveel.Messaging;

// Create endpoints using factory methods
var emailEndpoint = Endpoint.EmailAddress("user@example.com");
var phoneEndpoint = Endpoint.PhoneNumber("+1234567890");
var urlEndpoint = Endpoint.Url("https://api.example.com/webhook");
var userEndpoint = Endpoint.User("user123");
var appEndpoint = Endpoint.Application("app456");
var deviceEndpoint = Endpoint.Device("device789");
var idEndpoint = Endpoint.Id("custom-id-123");
var labelEndpoint = Endpoint.AlphaNumeric("LABEL123");
```

### Using Endpoint Constructors

```csharp
// Create endpoints using constructors with enum
var endpoints = new[]
{
    new Endpoint(EndpointType.EmailAddress, "user@example.com"),
    new Endpoint(EndpointType.PhoneNumber, "+1234567890"),
    new Endpoint(EndpointType.Url, "https://api.example.com/webhook"),
    new Endpoint(EndpointType.UserId, "user123"),
    new Endpoint(EndpointType.ApplicationId, "app456"),
    new Endpoint(EndpointType.DeviceId, "device789"),
    new Endpoint(EndpointType.Id, "custom-id-123"),
    new Endpoint(EndpointType.Label, "LABEL123")
};
```

### Message Builder Integration

```csharp
// MessageBuilder provides typed methods
var message = new MessageBuilder()
    .WithId("msg-001")
    .WithEmailSender("sender@example.com")           // Uses EndpointType.EmailAddress
    .WithPhoneReceiver("+1234567890")                // Uses EndpointType.PhoneNumber
    .WithTextContent("Hello, World!")
    .Message;

// Alternative using generic methods
var message2 = new MessageBuilder()
    .WithId("msg-002")
    .WithSender(EndpointType.UserId, "user123")
    .WithReceiver(EndpointType.ApplicationId, "app456")
    .WithTextContent("User to app message")
    .Message;
```

### Complex Message Scenarios

```csharp
// IoT device to mobile app notification
var iotMessage = new MessageBuilder()
    .WithId("iot-001")
    .WithSender(EndpointType.DeviceId, "sensor-temp-01")
    .WithReceiver(EndpointType.ApplicationId, "mobile-app-v2")
    .WithJsonContent(new { temperature = 25.5, unit = "C", timestamp = DateTime.UtcNow })
    .Message;

// Queue-based messaging
var queueMessage = new MessageBuilder()
    .WithId("queue-001")
    .WithSender(EndpointType.Topic, "orders.created")
    .WithReceiver(EndpointType.Topic, "inventory.update")
    .WithJsonContent(new { orderId = "ORDER-123", items = new[] { "item1", "item2" } })
    .Message;

// Webhook notification
var webhookMessage = new MessageBuilder()
    .WithId("webhook-001")
    .WithSender(EndpointType.ApplicationId, "payment-system")
    .WithReceiver(EndpointType.Url, "https://merchant.com/webhooks/payment")
    .WithJsonContent(new { paymentId = "PAY-123", status = "completed", amount = 99.99 })
    .Message;
```

## Endpoint Validation

### Automatic Schema Validation

```csharp
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

// This message will validate successfully
var validMessage = new MessageBuilder()
    .WithEmailSender("sender@example.com")
    .WithPhoneReceiver("+1234567890")
    .WithTextContent("Valid message")
    .Message;

// This message will fail validation (Url not supported)
var invalidMessage = new MessageBuilder()
    .WithSender(EndpointType.Url, "https://example.com")
    .WithPhoneReceiver("+1234567890")
    .WithTextContent("Invalid message")
    .Message;
```

### Custom Endpoint Validation

```csharp
public class CustomConnector : ChannelConnectorBase
{
    protected override async IAsyncEnumerable<ValidationResult> ValidateMessageAsync(
        IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Call base validation first
        await foreach (var result in base.ValidateMessageAsync(message, cancellationToken))
        {
            yield return result;
        }

        // Custom endpoint validation
        if (message.Receiver?.Type == EndpointType.PhoneNumber)
        {
            var phoneNumber = message.Receiver.Address;
            if (!IsValidE164Format(phoneNumber))
            {
                yield return new ValidationResult(
                    "Phone number must be in E.164 format (+1234567890)", 
                    new[] { "Receiver" });
            }
        }

        if (message.Receiver?.Type == EndpointType.EmailAddress)
        {
            var email = message.Receiver.Address;
            if (!IsValidEmailFormat(email))
            {
                yield return new ValidationResult(
                    "Invalid email address format", 
                    new[] { "Receiver" });
            }
        }
    }

    private bool IsValidE164Format(string phoneNumber)
    {
        return phoneNumber.StartsWith("+") && phoneNumber.Length >= 10 && 
               phoneNumber.Substring(1).All(char.IsDigit);
    }

    private bool IsValidEmailFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

## Backward Compatibility

### String-to-Enum Conversion

The framework maintains backward compatibility by automatically converting string endpoint types to enum values:

```csharp
// These string values are automatically converted to enum values
var legacyEndpoints = new[]
{
    new Endpoint("email", "user@example.com"),        // ? EndpointType.EmailAddress
    new Endpoint("phone", "+1234567890"),             // ? EndpointType.PhoneNumber
    new Endpoint("url", "https://example.com"),       // ? EndpointType.Url
    new Endpoint("user-id", "user123"),               // ? EndpointType.UserId
    new Endpoint("app-id", "app456"),                 // ? EndpointType.ApplicationId
    new Endpoint("device-id", "device789"),           // ? EndpointType.DeviceId
    new Endpoint("endpoint-id", "id123"),             // ? EndpointType.Id
    new Endpoint("label", "LABEL123")                 // ? EndpointType.Label
};

// All endpoints will have the correct EndpointType enum value
foreach (var endpoint in legacyEndpoints)
{
    Console.WriteLine($"String: {endpoint.Address}, Enum: {endpoint.Type}");
}
```

### String Mapping Table

| String Value | EndpointType Enum | Case Sensitive |
|--------------|-------------------|----------------|
| `"email"` | `EndpointType.EmailAddress` | No |
| `"phone"` | `EndpointType.PhoneNumber` | No |
| `"url"` | `EndpointType.Url` | No |
| `"user-id"` | `EndpointType.UserId` | No |
| `"app-id"` | `EndpointType.ApplicationId` | No |
| `"device-id"` | `EndpointType.DeviceId` | No |
| `"endpoint-id"` | `EndpointType.Id` | No |
| `"label"` | `EndpointType.Label` | No |
| `"topic"` | `EndpointType.Topic` | No |

### Error Handling for Unknown Strings

```csharp
try
{
    // Unknown string types throw ArgumentException
    var invalidEndpoint = new Endpoint("unknown-type", "some-address");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Output: Error: Unknown endpoint type: unknown-type
}
```

## Best Practices

### 1. Use Enum Values in New Code

```csharp
// ? Good - Use enum values for new code
var endpoint = new Endpoint(EndpointType.EmailAddress, "user@example.com");
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

// ? Avoid - Don't use strings in new code
var endpoint = new Endpoint("email", "user@example.com");
```

### 2. Choose Appropriate Endpoint Types

```csharp
// ? Good - Use specific endpoint types
.AllowsMessageEndpoint(EndpointType.EmailAddress)     // For email services
.AllowsMessageEndpoint(EndpointType.PhoneNumber)      // For SMS services
.AllowsMessageEndpoint(EndpointType.Topic)            // For message queues
.AllowsMessageEndpoint(EndpointType.ApplicationId)    // For push notifications

// ? Avoid - Don't overuse generic types
.AllowsMessageEndpoint(EndpointType.Id)               // Too generic for specific use cases
```

### 3. Document Endpoint Usage

```csharp
// ? Good - Document expected endpoint formats
var schema = new ChannelSchema("Twilio", "SMS", "2.0.0")
    .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
    {
        CanSend = true,
        CanReceive = true,
        Description = "Phone number in E.164 format (+1234567890)"
    })
    .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
    {
        CanSend = false,
        CanReceive = true,
        Description = "HTTPS webhook URL for receiving status updates"
    });
```

### 4. Validate Endpoint Formats

```csharp
// ? Good - Validate endpoint address formats
public static bool IsValidEndpointAddress(EndpointType type, string address)
{
    return type switch
    {
        EndpointType.EmailAddress => IsValidEmail(address),
        EndpointType.PhoneNumber => IsValidE164Phone(address),
        EndpointType.Url => Uri.TryCreate(address, UriKind.Absolute, out _),
        EndpointType.UserId => !string.IsNullOrWhiteSpace(address) && address.Length <= 100,
        EndpointType.Topic => IsValidTopicName(address),
        _ => !string.IsNullOrWhiteSpace(address)
    };
}
```

### 5. Use Factory Methods When Available

```csharp
// ? Good - Use typed factory methods
var message = new MessageBuilder()
    .WithEmailSender("sender@example.com")
    .WithPhoneReceiver("+1234567890")
    .Message;

// ? Also good - Use enum constructors
var message = new MessageBuilder()
    .WithSender(EndpointType.EmailAddress, "sender@example.com")
    .WithReceiver(EndpointType.PhoneNumber, "+1234567890")
    .Message;
```

### 6. Handle Endpoint Type Checking

```csharp
// ? Good - Check endpoint types before processing
public async Task ProcessMessage(IMessage message)
{
    switch (message.Receiver?.Type)
    {
        case EndpointType.EmailAddress:
            await SendEmail(message);
            break;
        case EndpointType.PhoneNumber:
            await SendSms(message);
            break;
        case EndpointType.Url:
            await SendWebhook(message);
            break;
        default:
            throw new NotSupportedException($"Endpoint type {message.Receiver?.Type} not supported");
    }
}
```

## Migration Guide

### Migrating from String-Based Endpoints

If you have existing code using string-based endpoint types, here's how to migrate:

#### Step 1: Update Schema Configurations

```csharp
// Before
var oldSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint("email")
    .AllowsMessageEndpoint("phone");

// After
var newSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);
```

#### Step 2: Update Endpoint Creation

```csharp
// Before
var oldEndpoint = new Endpoint("email", "user@example.com");

// After
var newEndpoint = new Endpoint(EndpointType.EmailAddress, "user@example.com");
// Or use factory method
var factoryEndpoint = Endpoint.EmailAddress("user@example.com");
```

#### Step 3: Update Message Building

```csharp
// Before
var oldMessage = new MessageBuilder()
    .WithSender("email", "sender@example.com")
    .WithReceiver("phone", "+1234567890")
    .Message;

// After
var newMessage = new MessageBuilder()
    .WithEmailSender("sender@example.com")
    .WithPhoneReceiver("+1234567890")
    .Message;
```

#### Step 4: Update Endpoint Type Checking

```csharp
// Before
if (endpoint.Type == "email")
{
    // Handle email
}

// After
if (endpoint.Type == EndpointType.EmailAddress)
{
    // Handle email
}
```

### Gradual Migration Strategy

1. **Phase 1**: Update schema configurations to use enum values
2. **Phase 2**: Update new message creation code to use factory methods
3. **Phase 3**: Update endpoint type checking to use enum comparisons
4. **Phase 4**: Replace string constructors with enum constructors over time

The framework's backward compatibility ensures that existing string-based code continues to work during the migration process.

This comprehensive guide covers all aspects of using endpoint types effectively in the Deveel Messaging Framework, providing type safety, better developer experience, and clearer code intentions.