# Migration Guide

This guide helps you migrate from previous versions of the Deveel Messaging Framework to the latest version with strongly-typed endpoints and enhanced features.

## Migration Overview

### What's Changed

- **Endpoint Types**: String-based endpoints replaced with strongly-typed `EndpointType` enumeration
- **Message Builder**: Enhanced fluent API with type-safe methods
- **Schema Configuration**: Improved validation and capability management
- **Error Handling**: More comprehensive error result objects
- **Performance**: Better async/await patterns and resource management

### Breaking Changes

1. **Endpoint Creation**: `new Endpoint(value, type)` ? `Endpoint.EmailAddress(value)` or `EndpointType.EmailAddress`
2. **Message Builder**: Generic `WithReceiver()` ? Specific `WithEmailReceiver()`, `WithPhoneReceiver()`, etc.
3. **Schema Configuration**: Enhanced validation requirements
4. **Connector Interface**: Updated method signatures with better cancellation support

## Step-by-Step Migration

### 1. Update Package References

```xml
<!-- Before -->
<PackageReference Include="Deveel.Messaging" Version="1.x.x" />

<!-- After -->
<PackageReference Include="Deveel.Messaging.Abstractions" Version="2.x.x" />
<PackageReference Include="Deveel.Messaging.Connector.Abstractions" Version="2.x.x" />
```

### 2. Migrate Endpoint Creation

#### Before (v1.x)

```csharp
// Old string-based approach
var emailEndpoint = new Endpoint("user@example.com", "email");
var phoneEndpoint = new Endpoint("+1234567890", "phone");
var webhookEndpoint = new Endpoint("https://api.example.com/webhook", "url");

var message = new MessageBuilder()
    .WithSender(emailEndpoint)
    .WithReceiver(phoneEndpoint)
    .WithContent("Hello World")
    .Build();
```

#### After (v2.x)

```csharp
// New strongly-typed approach
var emailEndpoint = Endpoint.EmailAddress("user@example.com");
var phoneEndpoint = Endpoint.PhoneNumber("+1234567890");
var webhookEndpoint = Endpoint.Url("https://api.example.com/webhook");

var message = new MessageBuilder()
    .WithEmailSender("sender@company.com")  // or .WithSender(emailEndpoint)
    .WithPhoneReceiver("+1234567890")       // or .WithReceiver(phoneEndpoint)
    .WithTextContent("Hello World")
    .Message; // Note: .Message instead of .Build()
```

### 3. Update Schema Configuration

#### Before (v1.x)

```csharp
var schema = new ChannelSchema("Twilio", "SMS", "1.0.0");
schema.AddCapability(ChannelCapability.SendMessages);
schema.AddEndpointType("phone");
schema.AddContentType("text/plain");
```

#### After (v2.x)

```csharp
var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AddContentType(MessageContentType.PlainText);
```

### 4. Migrate Message Building

#### Before (v1.x)

```csharp
var message = new MessageBuilder()
    .WithId("msg-001")
    .WithSender(new Endpoint("sender@company.com", "email"))
    .WithReceiver(new Endpoint("user@example.com", "email"))
    .WithContent(new MessageContent("Hello", "text/plain"))
    .WithProperty("subject", "Welcome")
    .Build();
```

#### After (v2.x)

```csharp
var message = new MessageBuilder()
    .WithId("msg-001")
    .WithEmailSender("sender@company.com")
    .WithEmailReceiver("user@example.com")
    .WithTextContent("Hello")
    .WithProperty("Subject", "Welcome")
    .Message;
```

### 5. Update Connector Implementation

#### Before (v1.x)

```csharp
public class MyConnector : IChannelConnector
{
    public async Task<MessageResult> SendMessageAsync(IMessage message)
    {
        // Old implementation
        return new MessageResult { MessageId = Guid.NewGuid().ToString() };
    }
}
```

#### After (v2.x)

```csharp
public class MyConnector : ChannelConnectorBase
{
    public MyConnector(IChannelSchema schema) : base(schema) { }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        SetState(ConnectorState.Connected);
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        var result = new MessageResult(Guid.NewGuid().ToString(), MessageStatus.Sent);
        return ConnectorResult<MessageResult>.Success(result);
    }

    // Implement other required methods...
}
```

## Migration Tools

### Automatic Endpoint Conversion

The framework provides automatic conversion for backward compatibility:

```csharp
// This still works but is deprecated
var endpoint = new Endpoint("user@example.com", "email");

// Framework automatically converts to:
var convertedEndpoint = endpoint.ToTypedEndpoint(); // Returns EndpointType.EmailAddress
```

### Schema Migration Helper

```csharp
public static class SchemaMigrationHelper
{
    public static ChannelSchema MigrateFromV1(LegacyChannelSchema legacySchema)
    {
        var newSchema = new ChannelSchema(
            legacySchema.Provider, 
            legacySchema.Type, 
            legacySchema.Version);

        // Migrate capabilities
        foreach (var capability in legacySchema.Capabilities)
        {
            newSchema = newSchema.WithCapabilities(ConvertCapability(capability));
        }

        // Migrate endpoint types
        foreach (var endpointType in legacySchema.SupportedEndpointTypes)
        {
            newSchema = newSchema.AllowsMessageEndpoint(ConvertEndpointType(endpointType));
        }

        return newSchema;
    }

    private static EndpointType ConvertEndpointType(string legacyType)
    {
        return legacyType.ToLower() switch
        {
            "email" => EndpointType.EmailAddress,
            "phone" => EndpointType.PhoneNumber,
            "url" => EndpointType.Url,
            "webhook" => EndpointType.Url,
            "user" => EndpointType.UserId,
            "device" => EndpointType.DeviceId,
            "app" => EndpointType.ApplicationId,
            "topic" => EndpointType.Topic,
            _ => EndpointType.Any
        };
    }
}
```

## Common Migration Scenarios

### Email Connector Migration

#### Before

```csharp
var emailConnector = new EmailConnector();
emailConnector.Configure(new Dictionary<string, string>
{
    ["host"] = "smtp.gmail.com",
    ["port"] = "587",
    ["username"] = "user@gmail.com",
    ["password"] = "password"
});

var message = new Message
{
    From = "sender@company.com",
    To = "recipient@example.com",
    Subject = "Hello",
    Body = "Hello World"
};

var result = await emailConnector.SendAsync(message);
```

#### After

```csharp
var schema = new ChannelSchema("SMTP", "Email", "2.0.0")
    .WithCapabilities(ChannelCapability.SendMessages)
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
    .AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Password", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint(EndpointType.EmailAddress);

var configuration = new Dictionary<string, object>
{
    ["Host"] = "smtp.gmail.com",
    ["Port"] = 587,
    ["Username"] = "user@gmail.com",
    ["Password"] = "password"
};

var emailConnector = new SmtpConnector(schema, configuration);
await emailConnector.InitializeAsync(CancellationToken.None);

var message = new MessageBuilder()
    .WithId("email-001")
    .WithEmailSender("sender@company.com")
    .WithEmailReceiver("recipient@example.com")
    .WithProperty("Subject", "Hello")
    .WithTextContent("Hello World")
    .Message;

var result = await emailConnector.SendMessageAsync(message, CancellationToken.None);
```

### SMS Connector Migration

#### Before

```csharp
var smsConnector = new TwilioConnector("account_sid", "auth_token", "+1234567890");

var smsMessage = new SmsMessage
{
    To = "+0987654321",
    Body = "Your code: 123456"
};

await smsConnector.SendSmsAsync(smsMessage);
```

#### After

```csharp
var schema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddParameter(new ChannelParameter("FromNumber", ParameterType.String) { IsRequired = true })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

var configuration = new Dictionary<string, object>
{
    ["AccountSid"] = "account_sid",
    ["AuthToken"] = "auth_token",
    ["FromNumber"] = "+1234567890"
};

var smsConnector = new TwilioConnector(schema, configuration);
await smsConnector.InitializeAsync(CancellationToken.None);

var smsMessage = new MessageBuilder()
    .WithId("sms-001")
    .WithPhoneSender("+1234567890")
    .WithPhoneReceiver("+0987654321")
    .WithTextContent("Your code: 123456")
    .Message;

var result = await smsConnector.SendMessageAsync(smsMessage, CancellationToken.None);
```

## Validation and Testing

### Update Your Tests

#### Before

```csharp
[Test]
public async Task Should_Send_Email()
{
    var connector = new EmailConnector();
    var message = new Message { /* old properties */ };
    
    var result = await connector.SendAsync(message);
    
    Assert.IsTrue(result.Success);
}
```

#### After

```csharp
[Test]
public async Task Should_Send_Email()
{
    var schema = CreateEmailSchema();
    var connector = new EmailConnector(schema);
    
    await connector.InitializeAsync(CancellationToken.None);
    
    var message = new MessageBuilder()
        .WithEmailSender("test@example.com")
        .WithEmailReceiver("recipient@example.com")
        .WithTextContent("Test message")
        .Message;
    
    var result = await connector.SendMessageAsync(message, CancellationToken.None);
    
    Assert.IsTrue(result.IsSuccess);
    Assert.IsNotNull(result.Value?.MessageId);
}
```

### Schema Validation

Add validation for your schemas:

```csharp
[Test]
public void Schema_Should_Be_Valid()
{
    var schema = new ChannelSchema("Provider", "Type", "1.0.0")
        .WithCapabilities(ChannelCapability.SendMessages)
        .AllowsMessageEndpoint(EndpointType.EmailAddress)
        .AddContentType(MessageContentType.PlainText);

    // Validate schema configuration
    Assert.IsTrue(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
    Assert.Contains(schema.Endpoints, e => e.Type == EndpointType.EmailAddress);
    Assert.Contains(schema.ContentTypes, ct => ct == MessageContentType.PlainText);
}
```

## Performance Improvements

### Async/Await Patterns

The new version uses better async patterns:

```csharp
// Before - synchronous or basic async
public MessageResult SendMessage(IMessage message)
{
    return SendMessageInternal(message);
}

// After - proper async with cancellation
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    try
    {
        var result = await SendMessageToProviderAsync(message, cancellationToken);
        return ConnectorResult<MessageResult>.Success(result);
    }
    catch (OperationCanceledException)
    {
        return ConnectorResult<MessageResult>.Failure("Operation was cancelled");
    }
    catch (Exception ex)
    {
        return ConnectorResult<MessageResult>.Failure($"Send failed: {ex.Message}");
    }
}
```

### Resource Management

Better resource management with proper disposal:

```csharp
// After - proper resource management
public async ValueTask DisposeAsync()
{
    await DisconnectAsync(CancellationToken.None);
    GC.SuppressFinalize(this);
}
```

## Troubleshooting Migration Issues

### Issue: Compilation Errors

**Problem**: `'Build' does not exist in the current context`

**Solution**: Change `.Build()` to `.Message`:
```csharp
// Before
var message = builder.Build();

// After
var message = builder.Message;
```

### Issue: Endpoint Type Errors

**Problem**: `Cannot convert from 'string' to 'EndpointType'`

**Solution**: Use typed endpoint methods:
```csharp
// Before
.WithReceiver(new Endpoint(email, "email"))

// After
.WithEmailReceiver(email)
```

### Issue: Schema Configuration Errors

**Problem**: Schema validation failures

**Solution**: Ensure all required configurations are present:
```csharp
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(ChannelCapability.SendMessages) // Required
    .AllowsMessageEndpoint(EndpointType.EmailAddress) // Required
    .AddContentType(MessageContentType.PlainText);    // Required
```

## Getting Help

If you encounter issues during migration:

1. **Check Documentation**: Review the [usage guides](README.md)
2. **Study Examples**: Look at the [test projects](../test/)
3. **Search Issues**: Check [GitHub Issues](https://github.com/deveel/deveel.message.model/issues)
4. **Ask Questions**: Use [GitHub Discussions](https://github.com/deveel/deveel.message.model/discussions)
5. **Contact Support**: Email support@deveel.com

## Post-Migration Checklist

- [ ] All compilation errors resolved
- [ ] Unit tests updated and passing
- [ ] Schema configurations validated
- [ ] Endpoint types converted to strongly-typed versions
- [ ] Error handling updated to use `ConnectorResult<T>`
- [ ] Async/await patterns properly implemented
- [ ] Resource disposal implemented correctly
- [ ] Integration tests verify functionality

## Benefits After Migration

- **Type Safety**: Compile-time validation prevents runtime errors
- **Better IntelliSense**: IDE provides better auto-completion and documentation
- **Performance**: Improved async patterns and resource management
- **Extensibility**: Easier to add new connectors and message types
- **Maintainability**: Cleaner code with better separation of concerns
- **Testing**: Better mocking and testing support