# Getting Started with Deveel Messaging Framework

This guide will walk you through setting up and using the Deveel Messaging Framework for the first time.

## Prerequisites

- **.NET 8.0** or **.NET 9.0** SDK
- Basic understanding of C# and async/await patterns
- An IDE like Visual Studio 2022 or VS Code

## Installation

### 1. Create a New Project

```bash
dotnet new console -n MyMessagingApp
cd MyMessagingApp
```

### 2. Install Core Packages

```bash
# Required: Core messaging abstractions
dotnet add package Deveel.Messaging.Abstractions

# Required for custom connectors: Connector base classes
dotnet add package Deveel.Messaging.Connector.Abstractions

# Optional: Pre-built connectors
dotnet add package Deveel.Messaging.Connector.Twilio
dotnet add package Deveel.Messaging.Connector.Sendgrid
```

### 3. Add Required Using Statements

```csharp
using Deveel.Messaging;
using Deveel.Messaging.Connector;
```

## Your First Message

### Step 1: Create a Simple Email Schema

```csharp
using Deveel.Messaging;

// Define an email connector schema
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithDisplayName("Simple SMTP Email")
    .WithCapabilities(ChannelCapability.SendMessages)
    .AddParameter(new ChannelParameter("Host", ParameterType.String)
    {
        IsRequired = true,
        Description = "SMTP server hostname"
    })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer)
    {
        DefaultValue = 587,
        Description = "SMTP server port"
    })
    .AddParameter(new ChannelParameter("Username", ParameterType.String)
    {
        IsRequired = true,
        Description = "SMTP username"
    })
    .AddParameter(new ChannelParameter("Password", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "SMTP password"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AddAuthenticationType(AuthenticationType.Basic);
```

### Step 2: Create a Message

```csharp
// Build a message using the fluent API
var message = new MessageBuilder()
    .WithId("my-first-message")
    .WithEmailSender("sender@example.com")
    .WithEmailReceiver("recipient@example.com")
    .WithTextContent("Hello from Deveel Messaging Framework!")
    .WithProperty("Subject", "My First Message")
    .Message;

// Validate the message (optional, but recommended)
var validationResult = message.Validate();
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Validation Error: {error}");
    }
    return;
}
```

### Step 3: Use a Connector (Mock Example)

Since this is a getting started guide, we'll create a simple mock connector:

```csharp
// For demonstration purposes - real connectors would implement actual sending
public class MockEmailConnector : ChannelConnectorBase
{
    public MockEmailConnector(IChannelSchema schema) : base(schema) { }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("MockEmailConnector: Initializing...");
        SetState(ConnectorState.Connected);
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<bool>> TestConnectionCoreAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("MockEmailConnector: Testing connection...");
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"MockEmailConnector: Sending message from {message.Sender} to {message.Receiver}");
        Console.WriteLine($"Content: {message.Content?.Value}");
        
        var messageId = Guid.NewGuid().ToString();
        var result = new MessageResult(messageId, MessageStatus.Sent)
        {
            SentAt = DateTime.UtcNow
        };
        
        return ConnectorResult<MessageResult>.Success(result);
    }

    // Other required implementations...
    protected override async Task<ConnectorResult<IEnumerable<MessageResult>>> SendMessagesAsync(
        IEnumerable<IMessage> messages, CancellationToken cancellationToken)
    {
        var results = new List<MessageResult>();
        foreach (var message in messages)
        {
            var result = await SendMessageCoreAsync(message, cancellationToken);
            if (result.IsSuccess && result.Value != null)
            {
                results.Add(result.Value);
            }
        }
        return ConnectorResult<IEnumerable<MessageResult>>.Success(results);
    }

    protected override async Task<ConnectorResult<MessageResult?>> GetMessageStatusAsync(
        string messageId, CancellationToken cancellationToken)
    {
        return ConnectorResult<MessageResult?>.Success(null);
    }

    protected override async Task<ConnectorResult<bool>> DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        SetState(ConnectorState.Disconnected);
        return ConnectorResult<bool>.Success(true);
    }
}
```

### Step 4: Put It All Together

```csharp
using Deveel.Messaging;

class Program
{
    static async Task Main(string[] args)
    {
        // Create schema
        var schema = new ChannelSchema("Mock", "Email", "1.0.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .AddContentType(MessageContentType.PlainText)
            .AllowsMessageEndpoint(EndpointType.EmailAddress);

        // Create connector
        var connector = new MockEmailConnector(schema);

        try
        {
            // Initialize
            var initResult = await connector.InitializeAsync(CancellationToken.None);
            if (!initResult.IsSuccess)
            {
                Console.WriteLine($"Failed to initialize: {initResult.ErrorMessage}");
                return;
            }

            // Create message
            var message = new MessageBuilder()
                .WithId("welcome-001")
                .WithEmailSender("welcome@myapp.com")
                .WithEmailReceiver("user@example.com")
                .WithTextContent("Welcome to our service! Thanks for signing up.")
                .Message;

            // Send message
            var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
            if (sendResult.IsSuccess)
            {
                Console.WriteLine($"? Message sent successfully!");
                Console.WriteLine($"Message ID: {sendResult.Value?.MessageId}");
                Console.WriteLine($"Status: {sendResult.Value?.Status}");
            }
            else
            {
                Console.WriteLine($"? Failed to send message: {sendResult.ErrorMessage}");
            }
        }
        finally
        {
            // Clean up
            await connector.DisconnectAsync(CancellationToken.None);
        }
    }
}
```

## Using Real Connectors

### Twilio SMS Example

```csharp
// Install: dotnet add package Deveel.Messaging.Connector.Twilio

var twilioSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String)
    {
        IsRequired = true,
        Description = "Your Twilio Account SID"
    })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Your Twilio Auth Token"
    })
    .AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Your Twilio phone number"
    })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

// Configure with your Twilio credentials
var configuration = new Dictionary<string, object>
{
    ["AccountSid"] = "your_account_sid",
    ["AuthToken"] = "your_auth_token",
    ["FromNumber"] = "+1234567890"
};

var twilioConnector = new TwilioConnector(twilioSchema, configuration);

var smsMessage = new MessageBuilder()
    .WithId("sms-001")
    .WithPhoneSender("+1234567890")
    .WithPhoneReceiver("+0987654321")
    .WithTextContent("Your verification code is: 123456")
    .Message;

await twilioConnector.InitializeAsync(CancellationToken.None);
var result = await twilioConnector.SendMessageAsync(smsMessage, CancellationToken.None);
```

### SendGrid Email Example

```csharp
// Install: dotnet add package Deveel.Messaging.Connector.Sendgrid

var sendGridSchema = new ChannelSchema("SendGrid", "Email", "1.0.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.Templates)
    .AddParameter(new ChannelParameter("ApiKey", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Your SendGrid API key"
    })
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint(EndpointType.EmailAddress);

var configuration = new Dictionary<string, object>
{
    ["ApiKey"] = "your_sendgrid_api_key"
};

var sendGridConnector = new SendGridConnector(sendGridSchema, configuration);

var emailMessage = new MessageBuilder()
    .WithId("email-001")
    .WithEmailSender("noreply@yourapp.com")
    .WithEmailReceiver("customer@example.com")
    .WithHtmlContent("<h1>Welcome!</h1><p>Thanks for joining our service.</p>")
    .WithProperty("Subject", "Welcome to Our Service")
    .Message;
```

## Key Concepts to Remember

### 1. Type Safety
Always use strongly-typed endpoints:
```csharp
// ? Good - Type safe
.WithEmailReceiver("user@example.com")
.WithPhoneReceiver("+1234567890")

// ? Avoid - Error prone
.WithReceiver(new Endpoint("user@example.com", "email"))
```

### 2. Schema Configuration
Define schemas that accurately represent your connector's capabilities:
```csharp
var schema = new ChannelSchema("Provider", "Type", "Version")
    .WithCapabilities(/* only what you actually support */)
    .AddContentType(/* only supported content types */)
    .AllowsMessageEndpoint(/* only supported endpoint types */);
```

### 3. Error Handling
Always check results and handle errors appropriately:
```csharp
var result = await connector.SendMessageAsync(message, cancellationToken);
if (!result.IsSuccess)
{
    // Log error, retry, or handle gracefully
    logger.LogError($"Message send failed: {result.ErrorMessage}");
}
```

### 4. Resource Management
Properly initialize and dispose connectors:
```csharp
try
{
    await connector.InitializeAsync(cancellationToken);
    // Use connector...
}
finally
{
    await connector.DisconnectAsync(cancellationToken);
}
```

## Next Steps

1. **[Channel Schema Guide](ChannelSchema-Usage.md)** - Learn advanced schema configuration
2. **[Endpoint Type Guide](EndpointType-Usage.md)** - Master type-safe endpoints
3. **[Connector Implementation](ChannelConnector-Usage.md)** - Build custom connectors
4. **[Examples](../test/)** - Study the test projects for more examples

## Troubleshooting

### Common Issues

**Issue**: "Endpoint type not supported"
```csharp
// ? Wrong endpoint type for schema
var schema = new ChannelSchema("SMS", "Provider", "1.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress); // SMS should use PhoneNumber

var message = new MessageBuilder()
    .WithPhoneReceiver("+1234567890") // This will fail validation
    .Message;
```

**Solution**: Match endpoint types with schema configuration
```csharp
// ? Correct
var schema = new ChannelSchema("SMS", "Provider", "1.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

var message = new MessageBuilder()
    .WithPhoneReceiver("+1234567890") // This works
    .Message;
```

**Issue**: "Required parameter missing"
```csharp
// Make sure all required parameters are provided
var schema = new ChannelSchema("Provider", "Type", "1.0")
    .AddParameter(new ChannelParameter("ApiKey", ParameterType.String)
    {
        IsRequired = true // This must be provided during initialization
    });
```

## Support

If you encounter issues:
1. Check the [documentation](../docs/)
2. Review [examples](../test/)
3. [Open an issue](https://github.com/deveel/deveel.message.model/issues)
4. Join our [discussions](https://github.com/deveel/deveel.message.model/discussions)