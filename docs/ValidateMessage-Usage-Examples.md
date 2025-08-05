# ValidateMessage Method Usage Examples

## Overview

The `ValidateMessage` extension method validates an entire `IMessage` instance against a channel schema, ensuring comprehensive compliance with schema requirements including:

- **Sender Endpoint Validation** - Ensures sender can send messages via supported endpoint types
- **Receiver Endpoint Validation** - Ensures receiver can receive messages via supported endpoint types  
- **Content Type Validation** - Validates message content type is supported by the schema
- **Message Properties Validation** - Validates message properties against schema definitions
- **Message ID Validation** - Ensures message has a valid identifier

## Basic Usage

### Creating a Message and Validating It

```csharp
// Create a channel schema with endpoint and content type requirements
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String) 
    { 
        IsRequired = true,
        Description = "Email subject line"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", DataType.Integer) 
    { 
        IsRequired = false,
        Description = "Email priority level (1-5)"
    });

// Create a valid message
var message = new Message
{
    Id = "msg-123",
    Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
    Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
    Content = new TextContent("Hello, this is a test message."), // PlainText content
    Properties = new Dictionary<string, MessageProperty>
    {
        { "Subject", new MessageProperty("Subject", "Important Update") },
        { "Priority", new MessageProperty("Priority", 2) }
    }
};

// Comprehensive message validation
var validationResults = emailSchema.ValidateMessage(message);

if (!validationResults.Any())
{
    Console.WriteLine("? Message validation passed!");
}
else
{
    foreach (var error in validationResults)
    {
        Console.WriteLine($"? Validation Error: {error.ErrorMessage}");
    }
}
```

## Enhanced Validation Features

### 1. Sender Endpoint Validation

```csharp
// Schema that only allows email addresses as senders
var emailOnlySchema = new ChannelSchema("Email", "Provider", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    });

// Valid sender
var validMessage = new Message
{
    Id = "msg-001",
    Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"), // ? Valid
    Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
    Content = new TextContent("Valid message")
};

// Invalid sender  
var invalidSenderMessage = new Message
{
    Id = "msg-002",
    Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Not supported as sender
    Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
    Content = new TextContent("Invalid sender message")
};

var results = emailOnlySchema.ValidateMessage(invalidSenderMessage);
// Error: "Sender endpoint type 'PhoneNumber' is not supported or cannot send messages"
```

### 2. Receiver Endpoint Validation

```csharp
// Schema that supports different endpoint types for sending vs receiving
var hybridSchema = new ChannelSchema("Hybrid", "Service", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
    {
        e.CanSend = true;
        e.CanReceive = false; // Email can only send, not receive
    })
    .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => 
    {
        e.CanSend = false; // Phone can only receive, not send
        e.CanReceive = true;
    });

// Valid hybrid message
var hybridMessage = new Message
{
    Id = "msg-003",
    Sender = new Endpoint(EndpointType.EmailAddress, "system@company.com"), // ? Can send
    Receiver = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Can receive
    Content = new TextContent("Notification message")
};

var hybridResults = hybridSchema.ValidateMessage(hybridMessage);
// Should pass validation

// Invalid receiver
var invalidReceiverMessage = new Message
{
    Id = "msg-004",
    Sender = new Endpoint(EndpointType.EmailAddress, "system@company.com"),
    Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"), // ? Email cannot receive
    Content = new TextContent("Invalid receiver message")
};

var invalidResults = hybridSchema.ValidateMessage(invalidReceiverMessage);
// Error: "Receiver endpoint type 'EmailAddress' is not supported or cannot receive messages"
```

### 3. Content Type Validation

```csharp
// Schema that only supports specific content types
var restrictiveSchema = new ChannelSchema("SMS", "Service", "1.0.0")
    .AddContentType(MessageContentType.PlainText) // Only plain text allowed
    .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    });

// Valid content type
var textMessage = new Message
{
    Id = "msg-005",
    Sender = new Endpoint(EndpointType.PhoneNumber, "+1111111111"),
    Receiver = new Endpoint(EndpointType.PhoneNumber, "+2222222222"),
    Content = new TextContent("Plain text message") // ? PlainText supported
};

// Invalid content type
var htmlMessage = new Message
{
    Id = "msg-006",
    Sender = new Endpoint(EndpointType.PhoneNumber, "+1111111111"),
    Receiver = new Endpoint(EndpointType.PhoneNumber, "+2222222222"),
    Content = new HtmlContent("<h1>HTML message</h1>") // ? Html not supported
};

var textResults = restrictiveSchema.ValidateMessage(textMessage);
// Should pass validation

var htmlResults = restrictiveSchema.ValidateMessage(htmlMessage);
// Error: "Message content type 'Html' is not supported by this schema"
```

### 4. Comprehensive Validation Failures

```csharp
// Schema with specific requirements
var strictSchema = new ChannelSchema("Strict", "Service", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String) 
    { 
        IsRequired = true 
    });

// Message with multiple validation failures
var failingMessage = new Message
{
    Id = "", // ? Empty ID
    Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Unsupported sender type
    Receiver = new Endpoint(EndpointType.Url, "https://webhook.com"), // ? Unsupported receiver type
    Content = new HtmlContent("<p>HTML content</p>"), // ? Unsupported content type
    Properties = new Dictionary<string, MessageProperty>
    {
        // ? Missing required "Subject" property
    }
};

var comprehensiveResults = strictSchema.ValidateMessage(failingMessage);

Console.WriteLine($"Total validation errors: {comprehensiveResults.Count()}");
foreach (var error in comprehensiveResults)
{
    Console.WriteLine($"- {error.ErrorMessage}");
}

// Output might include:
// - Message ID is required.
// - Sender endpoint type 'PhoneNumber' is not supported or cannot send messages
// - Receiver endpoint type 'Url' is not supported or cannot receive messages  
// - Message content type 'Html' is not supported by this schema
// - Required message property 'Subject' is missing.
```

## Advanced Usage Scenarios

### SMS Message Validation with Endpoint Constraints

```csharp
var smsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    })
    .HandlesMessageEndpoint(EndpointType.Url, e => 
    {
        e.CanSend = false; // URLs can only receive (webhooks)
        e.CanReceive = true;
    })
    .AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", DataType.String) 
    { 
        IsRequired = true,
        Description = "Recipient phone number in E.164 format"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("MessageType", DataType.String) 
    { 
        IsRequired = false,
        AllowedValues = new[] { "transactional", "promotional", "verification" }
    });

var smsMessage = new Message
{
    Id = "sms-789",
    Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Phone can send
    Receiver = new Endpoint(EndpointType.PhoneNumber, "+0987654321"), // ? Phone can receive
    Content = new TextContent("Your verification code is: 123456"), // ? PlainText supported
    Properties = new Dictionary<string, MessageProperty>
    {
        { "PhoneNumber", new MessageProperty("PhoneNumber", "+0987654321") },
        { "MessageType", new MessageProperty("MessageType", "verification") } // ? Allowed value
    }
};

var results = smsSchema.ValidateMessage(smsMessage);
// Should pass all validations
```

### Flexible Schema with Any Endpoint Support

```csharp
// Schema that accepts any endpoint type
var flexibleSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Json)
    .AllowsAnyMessageEndpoint(); // Accepts any endpoint type for send/receive

var flexibleMessage = new Message
{
    Id = "flexible-001",
    Sender = new Endpoint(EndpointType.ApplicationId, "mobile-app-v2"), // ? Any type allowed
    Receiver = new Endpoint(EndpointType.DeviceId, "device-12345"), // ? Any type allowed
    Content = new JsonContent("{\"notification\": \"test\"}"), // ? Json supported
    Properties = new Dictionary<string, MessageProperty>()
};

var flexibleResults = flexibleSchema.ValidateMessage(flexibleMessage);
// Should pass validation due to flexible endpoint configuration
```

### Strict Mode vs Flexible Mode with Endpoints

```csharp
// Strict schema - rejects unknown properties but validates endpoints
var strictEndpointSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    })
    .AddMessageProperty(new MessagePropertyConfiguration("KnownProperty", DataType.String));

// Flexible schema - allows unknown properties and validates endpoints
var flexibleEndpointSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithFlexibleMode()
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
    {
        e.CanSend = true;
        e.CanReceive = true;
    })
    .AddMessageProperty(new MessagePropertyConfiguration("KnownProperty", DataType.String));

var messageWithUnknownProperty = new Message
{
    Id = "msg-unknown",
    Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"), // ? Valid endpoint
    Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"), // ? Valid endpoint
    Content = new TextContent("Message content"), // ? Valid content type
    Properties = new Dictionary<string, MessageProperty>
    {
        { "KnownProperty", new MessageProperty("KnownProperty", "valid value") },
        { "UnknownProperty", new MessageProperty("UnknownProperty", "extra data") } // ? Unknown in strict mode
    }
};

// Strict mode validation
var strictResults = strictEndpointSchema.ValidateMessage(messageWithUnknownProperty);
Console.WriteLine($"Strict mode errors: {strictResults.Count()}"); // 1 error (unknown property)

// Flexible mode validation  
var flexibleResults = flexibleEndpointSchema.ValidateMessage(messageWithUnknownProperty);
Console.WriteLine($"Flexible mode errors: {flexibleResults.Count()}"); // 0 errors
```

## Migration from ValidateMessageProperties

### Old Method (Deprecated)
```csharp
// ? Old way - requires manual property extraction and no endpoint/content validation
var messageProperties = new Dictionary<string, object?>
{
    { "Subject", "Test Subject" },
    { "Priority", 2 }
};

var results = schema.ValidateMessageProperties(messageProperties); // Deprecated
// Only validates properties, not endpoints or content type
```

### New Method (Recommended)
```csharp
// ? New way - comprehensive message validation
var message = new Message
{
    Id = "msg-001",
    Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
    Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
    Content = new TextContent("Message content"),
    Properties = new Dictionary<string, MessageProperty>
    {
        { "Subject", new MessageProperty("Subject", "Test Subject") },
        { "Priority", new MessageProperty("Priority", 2) }
    }
};

var results = schema.ValidateMessage(message); // Recommended
// Validates: ID, endpoints, content type, AND properties
```

## Key Benefits

1. **?? Comprehensive Validation**: Validates the entire message including endpoints and content
2. **?? Endpoint Awareness**: Ensures sender can send and receiver can receive
3. **?? Content Type Safety**: Validates message content is supported
4. **?? Schema Compliance**: Enforces all schema requirements in one call
5. **?? Clean API**: Single method for complete message validation

## Error Handling

```csharp
public bool ValidateAndLogMessage(IChannelSchema schema, IMessage message, ILogger logger)
{
    try
    {
        var validationResults = schema.ValidateMessage(message);
        
        if (validationResults.Any())
        {
            foreach (var error in validationResults)
            {
                logger.LogWarning("Message validation failed: {ErrorMessage} (Members: {MemberNames})", 
                    error.ErrorMessage, 
                    string.Join(", ", error.MemberNames ?? Array.Empty<string>()));
            }
            return false;
        }
        
        logger.LogInformation("Message {MessageId} validated successfully against schema {SchemaId}", 
            message.Id, 
            schema.GetLogicalIdentity());
        return true;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error validating message {MessageId}", message.Id);
        return false;
    }
}
```

## Validation Categories

The `ValidateMessage` method performs validation in these categories:

1. **? Message Structure**
   - Message ID presence and validity

2. **? Endpoint Validation**
   - Sender endpoint type support and send capability
   - Receiver endpoint type support and receive capability

3. **? Content Type Validation**
   - Message content type support by schema

4. **? Property Validation**
   - Required properties presence
   - Property type compatibility
   - Custom property validation rules
   - Unknown properties (in strict mode)

The enhanced `ValidateMessage` method provides a comprehensive validation framework that ensures messages fully comply with channel schema requirements before processing or transmission.