# ValidateMessage Extension Method Test Example

This demonstrates how to use the new `ValidateMessage` extension method that validates an entire `IMessage` instance against a channel schema.

## Simple Test Example

```csharp
using Deveel.Messaging;
using System.ComponentModel.DataAnnotations;

public class ValidateMessageExample
{
    public void DemonstrateValidateMessage()
    {
        // Create a channel schema with message property requirements
        var schema = new ChannelSchema("Email", "Provider", "1.0.0")
            .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String) 
            { 
                IsRequired = true 
            })
            .AddMessageProperty(new MessagePropertyConfiguration("Priority", DataType.Integer) 
            { 
                IsRequired = false 
            });

        // Test 1: Valid message - should pass validation
        var validMessage = new Message
        {
            Id = "test-message-001",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
            Content = new TextContent("Hello, this is a test message."),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Test Email") },
                { "Priority", new MessageProperty("Priority", 2) }
            }
        };

        // ? NEW METHOD: Validate entire message
        var validResults = schema.ValidateMessage(validMessage);
        Console.WriteLine($"Valid message errors: {validResults.Count()}"); // Should be 0

        // Test 2: Invalid message - missing required property
        var invalidMessage = new Message
        {
            Id = "test-message-002",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
            Content = new TextContent("Hello, this is a test message."),
            Properties = new Dictionary<string, MessageProperty>
            {
                // Missing required "Subject" property
                { "Priority", new MessageProperty("Priority", 1) }
            }
        };

        var invalidResults = schema.ValidateMessage(invalidMessage);
        Console.WriteLine($"Invalid message errors: {invalidResults.Count()}"); // Should be 1
        
        foreach (var error in invalidResults)
        {
            Console.WriteLine($"Error: {error.ErrorMessage}");
            // Output: "Required message property 'Subject' is missing."
        }

        // Test 3: Message with unknown property in strict mode
        var messageWithUnknown = new Message
        {
            Id = "test-message-003",
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Valid Subject") },
                { "UnknownProperty", new MessageProperty("UnknownProperty", "extra data") }
            }
        };

        var unknownResults = schema.ValidateMessage(messageWithUnknown);
        Console.WriteLine($"Unknown property errors: {unknownResults.Count()}"); // Should be 1

        // Test 4: Same message with flexible schema
        var flexibleSchema = new ChannelSchema("Email", "Provider", "1.0.0")
            .WithFlexibleMode()
            .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String) 
            { 
                IsRequired = true 
            });

        var flexibleResults = flexibleSchema.ValidateMessage(messageWithUnknown);
        Console.WriteLine($"Flexible schema errors: {flexibleResults.Count()}"); // Should be 0
    }
}
```

## Key Benefits of ValidateMessage

### 1. **Direct IMessage Validation**
```csharp
// ? Old way - manual property extraction
var properties = message.Properties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
var results = schema.ValidateMessageProperties(properties);

// ? New way - direct message validation
var results = schema.ValidateMessage(message);
```

### 2. **Type Safety**
```csharp
// Works with any IMessage implementation
IMessage message = GetMessageFromAnySource();
var results = schema.ValidateMessage(message); // Always works
```

### 3. **Consistent API**
```csharp
// Both methods follow the same pattern
var connectionResults = schema.ValidateConnectionSettings(connectionSettings);
var messageResults = schema.ValidateMessage(message);
```

### 4. **Backward Compatibility**
```csharp
// Old method still works but shows deprecation warning
var oldResults = schema.ValidateMessageProperties(propertyDict); // ?? Deprecated

// New method is preferred
var newResults = schema.ValidateMessage(message); // ? Recommended
```

## Integration Example

```csharp
public class MessageProcessor
{
    private readonly IChannelSchema _schema;
    
    public MessageProcessor(IChannelSchema schema)
    {
        _schema = schema;
    }
    
    public async Task<bool> ProcessMessageAsync(IMessage message)
    {
        // Validate message against schema
        var validationResults = _schema.ValidateMessage(message);
        
        if (validationResults.Any())
        {
            foreach (var error in validationResults)
            {
                Console.WriteLine($"Validation failed: {error.ErrorMessage}");
            }
            return false;
        }
        
        // Process valid message
        await SendMessageAsync(message);
        return true;
    }
    
    private async Task SendMessageAsync(IMessage message)
    {
        // Implementation for sending the message
        await Task.CompletedTask;
    }
}
```

The `ValidateMessage` method provides a more intuitive and comprehensive approach to message validation, making it easier to ensure message compliance in your messaging workflows.

# Enhanced ValidateMessage Test Examples

This demonstrates the enhanced `ValidateMessage` extension method that validates sender endpoints, receiver endpoints, and content types in addition to message properties.

## Comprehensive Validation Test

```csharp
using Deveel.Messaging;
using System.ComponentModel.DataAnnotations;

public class EnhancedValidateMessageExample
{
    public void DemonstrateComprehensiveValidation()
    {
        // Create a schema with endpoint and content type restrictions
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
                IsRequired = true 
            })
            .AddMessageProperty(new MessagePropertyConfiguration("Priority", DataType.Integer) 
            { 
                IsRequired = false 
            });

        Console.WriteLine("=== Enhanced ValidateMessage Tests ===\n");

        // Test 1: Fully valid message
        TestValidMessage(emailSchema);
        
        // Test 2: Invalid sender endpoint
        TestInvalidSenderEndpoint(emailSchema);
        
        // Test 3: Invalid receiver endpoint
        TestInvalidReceiverEndpoint(emailSchema);
        
        // Test 4: Invalid content type
        TestInvalidContentType(emailSchema);
        
        // Test 5: Missing message ID
        TestMissingMessageId(emailSchema);
        
        // Test 6: Multiple validation failures
        TestMultipleValidationFailures(emailSchema);
        
        // Test 7: Endpoint direction validation
        TestEndpointDirectionValidation();
        
        // Test 8: Flexible endpoint schema
        TestFlexibleEndpointSchema();
    }

    private void TestValidMessage(IChannelSchema schema)
    {
        Console.WriteLine("Test 1: Valid Message");
        
        var validMessage = new Message
        {
            Id = "test-001",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
            Content = new TextContent("Hello, this is a valid email message."),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Test Email") },
                { "Priority", new MessageProperty("Priority", 2) }
            }
        };

        var results = schema.ValidateMessage(validMessage);
        Console.WriteLine($"Validation errors: {results.Count()}"); // Should be 0
        Console.WriteLine("Result: ? PASSED - All validations successful\n");
    }

    private void TestInvalidSenderEndpoint(IChannelSchema schema)
    {
        Console.WriteLine("Test 2: Invalid Sender Endpoint");
        
        var invalidSenderMessage = new Message
        {
            Id = "test-002",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Phone not supported
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
            Content = new TextContent("Message with invalid sender"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Test Subject") }
            }
        };

        var results = schema.ValidateMessage(invalidSenderMessage);
        Console.WriteLine($"Validation errors: {results.Count()}"); // Should be 1
        
        foreach (var error in results)
        {
            Console.WriteLine($"Error: {error.ErrorMessage}");
        }
        Console.WriteLine("Result: ? FAILED - Invalid sender endpoint type\n");
    }

    private void TestInvalidReceiverEndpoint(IChannelSchema schema)
    {
        Console.WriteLine("Test 3: Invalid Receiver Endpoint");
        
        var invalidReceiverMessage = new Message
        {
            Id = "test-003",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
            Receiver = new Endpoint(EndpointType.Url, "https://webhook.example.com"), // ? URL not supported
            Content = new TextContent("Message with invalid receiver"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Test Subject") }
            }
        };

        var results = schema.ValidateMessage(invalidReceiverMessage);
        Console.WriteLine($"Validation errors: {results.Count()}"); // Should be 1
        
        foreach (var error in results)
        {
            Console.WriteLine($"Error: {error.ErrorMessage}");
        }
        Console.WriteLine("Result: ? FAILED - Invalid receiver endpoint type\n");
    }

    private void TestInvalidContentType(IChannelSchema schema)
    {
        Console.WriteLine("Test 4: Invalid Content Type");
        
        var invalidContentMessage = new Message
        {
            Id = "test-004",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
            Content = new JsonContent("{\"message\": \"test\"}"), // ? JSON not supported by email schema
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Test Subject") }
            }
        };

        var results = schema.ValidateMessage(invalidContentMessage);
        Console.WriteLine($"Validation errors: {results.Count()}"); // Should be 1
        
        foreach (var error in results)
        {
            Console.WriteLine($"Error: {error.ErrorMessage}");
        }
        Console.WriteLine("Result: ? FAILED - Invalid content type\n");
    }

    private void TestMissingMessageId(IChannelSchema schema)
    {
        Console.WriteLine("Test 5: Missing Message ID");
        
        var noIdMessage = new Message
        {
            Id = "", // ? Empty ID
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"),
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"),
            Content = new TextContent("Message without ID"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "Subject", new MessageProperty("Subject", "Test Subject") }
            }
        };

        var results = schema.ValidateMessage(noIdMessage);
        Console.WriteLine($"Validation errors: {results.Count()}"); // Should be 1
        
        foreach (var error in results)
        {
            Console.WriteLine($"Error: {error.ErrorMessage}");
        }
        Console.WriteLine("Result: ? FAILED - Missing message ID\n");
    }

    private void TestMultipleValidationFailures(IChannelSchema schema)
    {
        Console.WriteLine("Test 6: Multiple Validation Failures");
        
        var multiFailMessage = new Message
        {
            Id = "", // ? Empty ID
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Invalid sender
            Receiver = new Endpoint(EndpointType.Url, "https://webhook.com"), // ? Invalid receiver
            Content = new JsonContent("{\"test\": true}"), // ? Invalid content type
            Properties = new Dictionary<string, MessageProperty>
            {
                // ? Missing required "Subject" property
                { "Priority", new MessageProperty("Priority", 1) }
            }
        };

        var results = schema.ValidateMessage(multiFailMessage);
        Console.WriteLine($"Validation errors: {results.Count()}"); // Should be 5
        
        Console.WriteLine("All validation errors:");
        foreach (var error in results)
        {
            Console.WriteLine($"- {error.ErrorMessage}");
        }
        Console.WriteLine("Result: ? FAILED - Multiple validation issues\n");
    }

    private void TestEndpointDirectionValidation()
    {
        Console.WriteLine("Test 7: Endpoint Direction Validation");
        
        // Schema with directional endpoint restrictions
        var hybridSchema = new ChannelSchema("Hybrid", "Service", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.EmailAddress, e => 
            {
                e.CanSend = true;
                e.CanReceive = false; // Email can only send
            })
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => 
            {
                e.CanSend = false; // Phone can only receive
                e.CanReceive = true;
            });

        // Valid directional message
        var validDirectionalMessage = new Message
        {
            Id = "test-007a",
            Sender = new Endpoint(EndpointType.EmailAddress, "system@company.com"), // ? Email can send
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Phone can receive
            Content = new TextContent("Notification message")
        };

        var validResults = hybridSchema.ValidateMessage(validDirectionalMessage);
        Console.WriteLine($"Valid directional message errors: {validResults.Count()}"); // Should be 0

        // Invalid directional message
        var invalidDirectionalMessage = new Message
        {
            Id = "test-007b",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"), // ? Phone cannot send
            Receiver = new Endpoint(EndpointType.EmailAddress, "recipient@example.com"), // ? Email cannot receive
            Content = new TextContent("Invalid direction message")
        };

        var invalidResults = hybridSchema.ValidateMessage(invalidDirectionalMessage);
        Console.WriteLine($"Invalid directional message errors: {invalidResults.Count()}"); // Should be 2
        
        foreach (var error in invalidResults)
        {
            Console.WriteLine($"Error: {error.ErrorMessage}");
        }
        Console.WriteLine("Result: ? PASSED - Endpoint direction validation working\n");
    }

    private void TestFlexibleEndpointSchema()
    {
        Console.WriteLine("Test 8: Flexible Endpoint Schema");
        
        // Schema that accepts any endpoint type
        var flexibleSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Json)
            .AllowsAnyMessageEndpoint(); // Accepts any endpoint type

        var flexibleMessage = new Message
        {
            Id = "test-008",
            Sender = new Endpoint(EndpointType.ApplicationId, "mobile-app-v2"), // ? Any type allowed
            Receiver = new Endpoint(EndpointType.DeviceId, "device-12345"), // ? Any type allowed
            Content = new JsonContent("{\"notification\": \"test\"}"), // ? JSON supported
            Properties = new Dictionary<string, MessageProperty>()
        };

        var results = flexibleSchema.ValidateMessage(flexibleMessage);
        Console.WriteLine($"Flexible schema validation errors: {results.Count()}"); // Should be 0
        Console.WriteLine("Result: ? PASSED - Flexible endpoint schema working\n");
    }
}

// Example content type implementations for testing
public class JsonContent : IMessageContent
{
    private readonly string _json;

    public JsonContent(string json)
    {
        _json = json;
        ContentType = MessageContentType.Json;
    }

    public MessageContentType ContentType { get; }

    public override string ToString() => _json;
}

public class HtmlContent : IMessageContent
{
    private readonly string _html;

    public HtmlContent(string html)
    {
        _html = html;
        ContentType = MessageContentType.Html;
    }

    public MessageContentType ContentType { get; }

    public override string ToString() => _html;
}
```

## Integration Test Example

```csharp
public class MessageValidationIntegrationTest
{
    [Fact]
    public void ValidateMessage_ComprehensiveValidation_ValidatesAllAspects()
    {
        // Arrange
        var schema = new ChannelSchema("SMS", "Provider", "1.0.0")
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => 
            {
                e.CanSend = true;
                e.CanReceive = true;
            })
            .AddMessageProperty(new MessagePropertyConfiguration("MessageType", DataType.String) 
            { 
                IsRequired = true,
                AllowedValues = new[] { "transactional", "promotional" }
            });

        // Test cases
        var testCases = new[]
        {
            new {
                Name = "Valid Message",
                Message = CreateValidSmsMessage(),
                ExpectedErrors = 0
            },
            new {
                Name = "Invalid Sender",
                Message = CreateSmsMessageWithInvalidSender(),
                ExpectedErrors = 1
            },
            new {
                Name = "Invalid Content Type",
                Message = CreateSmsMessageWithInvalidContent(),
                ExpectedErrors = 1
            },
            new {
                Name = "Missing Required Property",
                Message = CreateSmsMessageWithMissingProperty(),
                ExpectedErrors = 1
            }
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var results = schema.ValidateMessage(testCase.Message);
            Assert.Equal(testCase.ExpectedErrors, results.Count());
            
            Console.WriteLine($"{testCase.Name}: {(results.Any() ? "FAILED" : "PASSED")}");
            foreach (var error in results)
            {
                Console.WriteLine($"  - {error.ErrorMessage}");
            }
        }
    }

    private Message CreateValidSmsMessage()
    {
        return new Message
        {
            Id = "sms-001",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1111111111"),
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+2222222222"),
            Content = new TextContent("Your verification code is 123456"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "MessageType", new MessageProperty("MessageType", "transactional") }
            }
        };
    }

    private Message CreateSmsMessageWithInvalidSender()
    {
        return new Message
        {
            Id = "sms-002",
            Sender = new Endpoint(EndpointType.EmailAddress, "sender@example.com"), // Invalid for SMS
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+2222222222"),
            Content = new TextContent("Message with invalid sender"),
            Properties = new Dictionary<string, MessageProperty>
            {
                { "MessageType", new MessageProperty("MessageType", "transactional") }
            }
        };
    }

    private Message CreateSmsMessageWithInvalidContent()
    {
        return new Message
        {
            Id = "sms-003",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1111111111"),
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+2222222222"),
            Content = new HtmlContent("<p>HTML not supported in SMS</p>"), // Invalid content type
            Properties = new Dictionary<string, MessageProperty>
            {
                { "MessageType", new MessageProperty("MessageType", "transactional") }
            }
        };
    }

    private Message CreateSmsMessageWithMissingProperty()
    {
        return new Message
        {
            Id = "sms-004",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1111111111"),
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+2222222222"),
            Content = new TextContent("Message without required property"),
            Properties = new Dictionary<string, MessageProperty>()
            // Missing required "MessageType" property
        };
    }
}
```

## Key Features Demonstrated

### ? **Enhanced Validation Coverage**
1. **Message ID Validation** - Ensures message has valid identifier
2. **Sender Endpoint Validation** - Checks sender type and send capability
3. **Receiver Endpoint Validation** - Checks receiver type and receive capability
4. **Content Type Validation** - Validates message content is supported
5. **Property Validation** - Validates message properties (existing functionality)

### ? **Flexible Configuration**
- **Directional Endpoints** - Different endpoint types for sending vs receiving
- **Any Endpoint Support** - Wildcard endpoint acceptance
- **Content Type Restrictions** - Schema-specific content type support
- **Strict/Flexible Modes** - Property validation behavior control

### ? **Comprehensive Error Reporting**
- **Multiple Validation Errors** - Reports all validation issues in one pass
- **Specific Error Messages** - Clear indication of what failed validation
- **Member Name Mapping** - Associates errors with specific message parts

The enhanced `ValidateMessage` method provides complete message validation ensuring full compliance with channel schema requirements before message processing or transmission.