---
sidebar_position: 8
---

# Message Validation

Every connector validates messages internally before sending — the base class checks the message against the schema and returns validation failures as `OperationResult<T>.ValidationFailed`. But waiting until the connector call to discover validation errors is wasteful: the message may be invalid in ways that are obvious from the schema alone, without any provider interaction.

Validation extensions on `IChannelSchema` let you push this check earlier in the pipeline — into your service layer, a controller action, or even a client-side form validator. The same validation logic that runs inside the connector is available as standalone extension methods, so you can catch errors before they reach the connector boundary.

This separation matters: validation failures are not transient errors you can retry — they indicate a bug in the caller. Catching them early avoids unnecessary provider API calls, simplifies error handling in business logic, and makes unit tests more precise.

Validation helpers are extension methods on `IChannelSchema`. They work with any implementation — `ChannelSchema`, derived schemas, or custom implementations.

## Extension method reference

```csharp
// Connection settings validation
IEnumerable<ValidationResult> ValidateConnectionSettings(
    this IChannelSchema schema, ConnectionSettings settings)

// Message property validation only
IEnumerable<ValidationResult> ValidateMessageProperties(
    this IChannelSchema schema, IDictionary<string, object?> properties)

// Full message validation
IEnumerable<ValidationResult> ValidateMessage(
    this IChannelSchema schema, IMessage message)

// Schema identity
string GetLogicalIdentity(this IChannelSchema schema)

// Schema compatibility
bool IsCompatibleWith(this IChannelSchema schema, IChannelSchema other)

// Restriction validation
IEnumerable<ValidationResult> ValidateAsRestrictionOf(
    this IChannelSchema schema, IChannelSchema target)

// Authentication support
ICollection<AuthenticationType> GetAuthenticationTypes(this IChannelSchema)
bool SupportsAuthenticationType(this IChannelSchema, AuthenticationType)
```

## ValidateMessage

Full message validation in a single pass. This is the most useful method — it checks everything at once.

### What it checks

1. **Message ID** — must be non-empty
2. **Sender endpoint** — type must be declared in the schema, and `CanSend` must be `true` for that type
3. **Receiver endpoint** — type must be declared in the schema, and `CanReceive` must be `true`
4. **Content type** — `message.Content.ContentType` must be in the schema's content type list
5. **Required message properties** — every `MessagePropertyConfiguration` with `IsRequired = true` must have a matching property on the message
6. **Property types** — property values are checked against `DataType` (e.g., an `Integer` property rejects a string value)
7. **Property constraints** — `MinLength`, `MaxLength`, `MinValue`, `MaxValue`, `Pattern`, `AllowedValues`, and `CustomValidator` are all evaluated
8. **Unknown properties** — in strict mode, any message property not defined in the schema is flagged as an error

### Example

```csharp
var schema = new ChannelSchema("SMTP", "Email", "1.0")
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.EmailAddress)
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String)
    {
        IsRequired = true
    });

var message = new MessageBuilder()
    .WithId("msg-1")
    .FromEmail("alice@example.com")
    .ToEmail("bob@example.com")
    .WithText("Hello")
    .WithSubject("Welcome")
    .Build();

var issues = schema.ValidateMessage(message);
if (issues.Any())
{
    foreach (var issue in issues)
        Console.WriteLine($"Validation error: {issue.ErrorMessage}");
    return;
}

// Proceed with connector send
```

### Common validation failures

| Scenario | Error message |
|---|---|
| Receiver is `Url` but schema only allows `PhoneNumber` | `"Endpoint type 'Url' is not supported as receiver"` |
| Content is `Html` but schema only allows `PlainText` | `"Content type 'Html' is not supported by the schema"` |
| Required property `Subject` is missing | `"The property 'Subject' is required"` |
| Unknown property in strict mode | `"The property 'UnknownProp' is not defined in the schema"` |
| Property value wrong type | `"The property 'Count' expects a value of type 'Integer'"` |

## ValidateConnectionSettings

Check that connection parameters satisfy the schema's requirements before creating a connector. This is useful in configuration validation at startup:

```csharp
var settings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC...");

var issues = schema.ValidateConnectionSettings(settings);
if (issues.Any())
{
    foreach (var issue in issues)
        Console.WriteLine($"Config error: {issue.ErrorMessage}");
    throw new ConfigurationErrorsException("Invalid connector settings");
}
```

Checks include:
- All required parameters are present
- Parameter values match their declared `DataType`
- Parameter values satisfy `MinValue`/`MaxValue`/`AllowedValues` constraints
- In strict mode: no unknown parameters

## ValidateMessageProperties

Validate only the property dictionary, without building a full message:

```csharp
var properties = new Dictionary<string, object?>
{
    ["Subject"] = "Hello",
    ["ValidityPeriod"] = 3600
};

var issues = schema.ValidateMessageProperties(properties);
```

This is useful when properties come from a separate source (e.g., user input, API request body) and you want to validate them independently.

## Schema identity and compatibility

```csharp
// Returns "Twilio/SMS/1.0.0"
var id = schema.GetLogicalIdentity();

// Two schemas are compatible if they share the same provider, type, and version
bool compatible = schemaA.IsCompatibleWith(schemaB);

// Validate a derived schema is a valid restriction of its master
var issues = derivedSchema.ValidateAsRestrictionOf(masterSchema);
```

## Authentication support queries

```csharp
// List which authentication types the schema supports
var authTypes = schema.GetAuthenticationTypes();
// e.g., { AuthenticationType.ApiKey, AuthenticationType.Basic }

// Check a specific type
bool supportsToken = schema.SupportsAuthenticationType(AuthenticationType.Token);
```

## Early validation pattern

The most effective use of validation extensions is early in the service layer, before any connector interaction. This keeps your business logic clean: the send method either succeeds or returns a validation error, without needing to distinguish between "the message was bad" and "the provider rejected it."

Validate in your service layer to fail fast before calling the connector:

```csharp
public class NotificationService
{
    private readonly IChannelConnector _connector;

    public NotificationService(IChannelConnector connector)
    {
        _connector = connector;
    }

    public async Task<OperationResult<SendResult>> SendAsync(IMessage message)
    {
        // Validate before any provider interaction
        var issues = _connector.Schema.ValidateMessage(message);
        if (issues.Any())
        {
            return OperationResult<SendResult>.ValidationFailed(
                "VALIDATION_ERROR", "Messaging",
                issues);
        }

        // Send via the connector
        return await _connector.SendMessageAsync(message, CancellationToken.None);
    }
}
```

### Batch validation

```csharp
public async Task SendBatchAsync(IEnumerable<IMessage> messages)
{
    var batch = new MessageBatch();
    foreach (var msg in messages)
    {
        var issues = _connector.Schema.ValidateMessage(msg);
        if (issues.Any())
        {
            logger.LogWarning("Skipping message {Id}: {Errors}",
                msg.Id, string.Join("; ", issues.Select(x => x.ErrorMessage)));
            continue;
        }
        batch.Messages.Add(msg);
    }

    if (batch.Messages.Count > 0)
        await _connector.SendBatchAsync(batch, CancellationToken.None);
}
```

## Unit testing validation

```csharp
[Fact]
public void ValidMessage_PassesValidation()
{
    var schema = new ChannelSchema("SMTP", "Email", "1.0")
        .AddContentType(MessageContentType.PlainText)
        .HandlesMessageEndpoint(EndpointType.EmailAddress);

    var message = new MessageBuilder()
        .WithId("test-1")
        .FromEmail("a@b.com")
        .ToEmail("c@d.com")
        .WithText("Hello")
        .Build();

    var issues = schema.ValidateMessage(message);
    Assert.Empty(issues);
}

[Fact]
public void MissingRequiredProperty_FailsValidation()
{
    var schema = new ChannelSchema("SMTP", "Email", "1.0")
        .AddContentType(MessageContentType.PlainText)
        .HandlesMessageEndpoint(EndpointType.EmailAddress)
        .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String)
        {
            IsRequired = true
        });

    var message = new MessageBuilder()
        .WithId("test-2")
        .FromEmail("a@b.com")
        .ToEmail("c@d.com")
        .WithText("Hello")
        .Build();

    var issues = schema.ValidateMessage(message).ToList();
    Assert.Contains(issues, x =>
        x.ErrorMessage?.Contains("Subject", StringComparison.OrdinalIgnoreCase) == true);
}
```

## ValidationResult

The extension methods return `System.ComponentModel.DataAnnotations.ValidationResult` instances:

```csharp
public class ValidationResult
{
    public string? ErrorMessage { get; set; }
    public IEnumerable<string>? MemberNames { get; set; }
}
```

Each validation failure carries a human-readable error message and optionally the member names involved.
