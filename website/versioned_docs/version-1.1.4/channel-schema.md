---
sidebar_position: 6
---

# Channel Schema

A connector without a schema is a black box: you have no way to know what parameters it needs, what capabilities it offers, what message shapes it accepts, or what authentication it requires without reading its source code. The schema solves this by making every connector self-describing.

`IChannelSchema` is the contract that defines a connector's capabilities, requirements, and constraints. Every connector has exactly one schema that describes:
- **What it can do** — capabilities like send, receive, status query, bulk messaging
- **What it needs** — connection parameters like API keys, endpoints, timeouts
- **What it accepts** — endpoint types, content types, message property constraints
- **How it authenticates** — supported authentication methods

This contract serves multiple purposes: it drives pre-flight validation so errors are caught before provider API calls, it enables the schema registry for runtime discovery, it provides the blueprint for schema derivation (environment-specific restrictions), and it serves as living documentation that evolves with the connector.

`ChannelSchema` is the built-in fluent implementation. You construct it inline, via a schema factory, or derive it from a base schema (see [Schema derivation](schema-derivation.md)).

## IChannelSchema interface

```csharp
public interface IChannelSchema
{
    string ChannelProvider { get; }      // e.g., "Twilio"
    string ChannelType { get; }          // e.g., "SMS"
    string Version { get; }              // e.g., "1.0.0"
    string? DisplayName { get; }
    bool IsStrict { get; }
    ChannelCapability Capabilities { get; }
    IReadOnlyList<ChannelEndpointConfiguration> Endpoints { get; }
    IReadOnlyList<ChannelParameter> Parameters { get; }
    IReadOnlyList<MessagePropertyConfiguration> MessageProperties { get; }
    IReadOnlyList<MessageContentType> ContentTypes { get; }
    IReadOnlyList<AuthenticationConfiguration> AuthenticationConfigurations { get; }
}
```

The logical identity `ChannelProvider/ChannelType/Version` (e.g., `"Twilio/SMS/1.0.0"`) is used for schema discovery and compatibility checks.

## Minimal schema

```csharp
var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
    .WithDisplayName("Twilio SMS")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery)
    .AddRequiredParameter("AccountSid", DataType.String)
    .AddRequiredParameter("AuthToken", DataType.String)
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.PhoneNumber);
```

## Capabilities

Not every connector supports every operation. An SMS connector may not support receiving messages; a push notification connector may not support delivery status queries. Capabilities are flags that advertise exactly what operations the connector supports. The base class uses these flags to guard operations at runtime: calling a method whose capability is not set throws `NotSupportedException`, failing fast instead of producing confusing errors at the provider level.

Capabilities are flags that advertise what operations the connector supports. When you call a method on `IChannelConnector` whose capability is not set, the base class throws `NotSupportedException`.

| Flag | Method guarded |
|---|---|
| `SendMessages` | `SendMessageAsync` |
| `ReceiveMessages` | `ReceiveMessagesAsync` |
| `MessageStatusQuery` | `GetMessageStatusAsync` |
| `HandleMessageState` | `ReceiveMessageStatusAsync` |
| `MediaAttachments` | Content type checks for `MediaContent` |
| `Templates` | Content type checks for `TemplateContent` |
| `BulkMessaging` | `SendBatchAsync` |
| `HealthCheck` | `GetHealthAsync` |

```csharp
schema.WithCapabilities(
    ChannelCapability.SendMessages |
    ChannelCapability.ReceiveMessages |
    ChannelCapability.MessageStatusQuery |
    ChannelCapability.HandleMessageState |
    ChannelCapability.MediaAttachments |
    ChannelCapability.Templates |
    ChannelCapability.BulkMessaging |
    ChannelCapability.HealthCheck
);
```

Add individual capabilities:

```csharp
schema
    .WithCapability(ChannelCapability.SendMessages)
    .WithCapability(ChannelCapability.ReceiveMessages);
```

## Parameters

Connectors need configuration to operate: API keys, endpoint URLs, timeouts, feature flags. Parameters declare what configuration each connector expects, including whether the value is required, what data type it should be, and any constraints on acceptable values. This enables the framework to validate connection settings at startup (before any provider call) and to generate documentation automatically.

Parameters define what connection settings the connector needs at runtime. Each parameter has a name, data type, and optional constraints.

```csharp
schema
    .AddRequiredParameter("ApiKey", DataType.String)
    .AddParameter(new ChannelParameter("WebhookUrl", DataType.String)
    {
        IsRequired = false,
        DefaultValue = "https://default.webhook.url",
        AllowedValues = ["https://eu.api.example.com", "https://us.api.example.com"]
    })
    .AddParameter(new ChannelParameter("Timeout", DataType.Integer)
    {
        IsRequired = false,
        DefaultValue = 30000
    });
```

### DataType enum

| Value | CLR type |
|---|---|
| `String` | `string` |
| `Integer` | `int` |
| `Number` | `double` |
| `Boolean` | `bool` |

### Sensitive parameters

Mark secrets like tokens and passwords as sensitive — the framework redacts their values in logs:

```csharp
new ChannelParameter("AuthToken", DataType.String)
{
    IsRequired = true,
    IsSensitive = true
};
```

### Updating parameters after creation

For derived schemas that need to change parameter properties:

```csharp
schema.UpdateParameter("Timeout", p =>
{
    p.DefaultValue = 60000;
    p.Description = "Timeout for slow networks";
});
```

## Endpoint configurations

Different connectors handle different addressing schemes: SMS uses phone numbers, email uses email addresses, push notifications use device tokens, and chat apps use user or chat identifiers. Endpoint configurations declare which addressing schemes the connector supports and whether each can be used for sending, receiving, or both. The schema validator uses this to reject messages that use an endpoint type the connector cannot handle.

### Standard endpoints

Declare which `EndpointType` values the connector handles and in which direction:

```csharp
schema
    .HandlesMessageEndpoint(EndpointType.PhoneNumber, cfg =>
    {
        cfg.CanSend = true;
        cfg.CanReceive = true;
    })
    .HandlesMessageEndpoint(EndpointType.EmailAddress, cfg =>
    {
        cfg.CanSend = true;
        cfg.CanReceive = false;
    })
    .HandlesMessageEndpoint(EndpointType.Url, cfg =>
    {
        cfg.CanSend = false;
        cfg.CanReceive = true;    // webhook callbacks
    });
```

Use the simpler overload when you accept both directions:

```csharp
schema.HandlesMessageEndpoint(EndpointType.PhoneNumber);
// CanSend = true, CanReceive = true by default
```

For connectors that accept any endpoint type (e.g., a generic webhook relay):

```csharp
schema.AllowsAnyMessageEndpoint();
```

### Sender identity endpoints

The framework defines specialised sender types (see [Message model](messaging-model.md#sender-identities)) that each carry a specific `EndpointType`:

| Sender type | `EndpointType` | When to expect on messages |
|---|---|---|
| `SenderRef` | `Label` | Before resolution — a logical name reference awaiting registry lookup |
| `EmailSender` | `EmailAddress` | After resolution — a resolved email sender |
| `PhoneSender` | `PhoneNumber` | After resolution — a resolved phone sender |
| `AlphaNumericSender` | `Label` | After resolution — a resolved alphanumeric sender ID |
| `BotSender` | `Id` | After resolution — a resolved bot identifier |

#### Resolution-aware validation

When a message arrives at the connector (`SendMessageAsync`), sender resolution happens **before** validation:

```
SendMessageAsync
  → ResolveSenderAsync         // SenderRef → concrete sender
  → ValidateMessageAsync       // validate against schema
```

This means:
- **`SenderRef`** (which has `EndpointType.Label`) is replaced by the concrete sender **before** validation runs. The validator sees the resolved endpoint, not the reference. You do **not** need to declare `EndpointType.Label` in the schema unless you also send `AlphaNumericSender` endpoints after resolution.
- **Resolved senders** must still match the schema's endpoint declarations. For example, if the schema only declares `EndpointType.PhoneNumber` and the registry returns an `EmailSender`, validation fails even though resolution succeeded.

A connector that accepts only phone senders and supports `SenderRef` resolution:

```csharp
schema
    .HandlesMessageEndpoint(EndpointType.PhoneNumber, cfg =>
    {
        cfg.CanSend = true;
        cfg.CanReceive = false;
    });
    // SenderRef (Label) is resolved to PhoneNumber before validation;
    // no need to declare Label unless you also handle alphanumeric senders.
```

A connector that also accepts alphanumeric senders (which stay as `Label` after resolution) must declare both:

```csharp
schema
    .HandlesMessageEndpoint(EndpointType.PhoneNumber, cfg =>
    {
        cfg.CanSend = true;
        cfg.CanReceive = false;
    })
    .HandlesMessageEndpoint(EndpointType.Label, cfg =>
    {
        cfg.CanSend = true;
        cfg.CanReceive = false;
    });
    // Label covers both unresolved SenderRef AND resolved AlphaNumericSender
```

### ChannelEndpointConfiguration properties

| Property | Default | Description |
|---|---|---|
| `Type` | (required) | The endpoint type |
| `CanSend` | `true` | This endpoint type can be used as sender |
| `CanReceive` | `true` | This endpoint type can be used as receiver |
| `IsWildcard` | `false` | Accepts any endpoint type |

## Content type restrictions

A connector that handles SMS cannot deliver HTML email; a connector that handles push notifications cannot process multipart content with file attachments. Content type restrictions declare which message payload formats the connector accepts. When you build a message with a content type the connector does not support, validation catches the mismatch before any provider API call.

Declare which `MessageContentType` values the connector supports:

```csharp
schema
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Template);
```

Available content types:

| Value | Content class |
|---|---|
| `PlainText` | `TextContent` |
| `Html` | `HtmlContent` |
| `Multipart` | `MultipartContent` |
| `Template` | `TemplateContent` |
| `Media` | `MediaContent` |
| `Json` | `JsonContent` |
| `Binary` | `BinaryContent` |
| `Location` | `LocationContent` |

If a message's content type is not in the schema's list, `ValidateMessage` returns a validation error.

## Message property configurations

Connectors accept per-message configuration beyond sender, receiver, and content. Email needs a subject line, SMS supports validity periods, push notifications expect titles and badges. Message property configurations declare which properties the connector recognizes and what validation rules apply — turning what would be undocumented magic strings into a formal contract.

Define what message properties the connector understands, along with validation rules:

```csharp
schema.AddMessageProperty(
    new MessagePropertyConfiguration("ValidityPeriod", DataType.Integer)
    {
        IsRequired = false,
        MinValue = 60,
        MaxValue = 86400,
        Description = "Time in seconds the message is valid"
    });

schema.AddMessageProperty(
    new MessagePropertyConfiguration("Subject", DataType.String)
    {
        IsRequired = true,
        MaxLength = 200,
        Pattern = "^[a-zA-Z0-9 ]+$"
    });
```

### MessagePropertyConfiguration properties

| Property | Description |
|---|---|
| `Name` | Property key name |
| `DataType` | Expected data type (`String`, `Integer`, `Number`, `Boolean`) |
| `IsRequired` | Property must be present |
| `IsSensitive` | Value should be redacted in logs |
| `MinLength` / `MaxLength` | String length constraints |
| `MinValue` / `MaxValue` | Numeric range constraints |
| `Pattern` | Regex pattern for string values |
| `AllowedValues` | Explicit set of allowed values |
| `CustomValidator` | `Func<object?, IEnumerable<ValidationResult>>` for arbitrary validation |

### Custom validator example

```csharp
new MessagePropertyConfiguration("CustomData", DataType.String)
{
    CustomValidator = value =>
    {
        if (value is string str && str.Length > 1000)
            return [new ValidationResult("CustomData must be <= 1000 characters")];
        return [];
    }
};
```

## Authentication configurations

Every provider has its own authentication model: API keys, bearer tokens, basic auth, OAuth 2.0 client credentials, or custom schemes. Authentication configurations declare which methods the connector supports and which `ConnectionSettings` parameters each method requires. The authentication manager uses these declarations to automatically select the right provider and credential flow at initialization time.

### Adding a scheme with default field aliases

```csharp
schema.AddAuthenticationScheme(AuthenticationScheme.Basic);
```

This uses sensible defaults for the scheme (for Basic: Username, Password, AccountSid, AuthToken, User, Pass, ClientId, ClientSecret). The parameters are **not** automatically added as schema parameters — use `AddAuthenticationConfiguration()` with explicit fields to get auto-registration.

### Adding an explicit configuration (recommended)

```csharp
schema.AddAuthenticationConfiguration(
    new AuthenticationConfiguration(AuthenticationScheme.Basic, "Twilio Basic")
        .WithField("AccountSid", DataType.String, f =>
        {
            f.DisplayName = "Account SID";
            f.AuthenticationRole = "principal";
        })
        .WithField("AuthToken", DataType.String, f =>
        {
            f.DisplayName = "Auth Token";
            f.AuthenticationRole = "credential";
            f.IsSensitive = true;
        }));
```

Fields with `AuthenticationRole = "principal"` are automatically registered as optional `ChannelParameter` entries in the schema, making them recognized by strict-mode validation.

### Multiple fallback options

The authentication manager iterates through the schema's configurations and selects the first one where `IsSatisfiedBy(ConnectionSettings)` returns true:

```csharp
schema
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Primary Token")
            .WithField("PrimaryToken", DataType.String, f =>
            {
                f.AuthenticationRole = "principal";
                f.IsSensitive = true;
            }))
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Secondary Token")
            .WithField("SecondaryToken", DataType.String, f =>
            {
                f.AuthenticationRole = "principal";
                f.IsSensitive = true;
            }));
```

### Role-based field matching

The `IsSatisfiedBy` method uses the field's `AuthenticationRole` to determine whether a configuration is fulfilled:

- **Configs with principal + credential roles**: at least one of each must be present
- **Configs with only principal roles**: at least one must be present
- **Auxiliary roles**: all must be present

This allows flexible configurations (e.g., Username+Password OR AccountSid+AuthToken for Basic) without requiring all possible field names to be populated.

### Authentication configuration properties

| Property | Description |
|---|---|
| `Scheme` | The `AuthenticationScheme` identifier |
| `DisplayName` | Human-readable name for the method |
| `Fields` | List of `AuthenticationField` definitions with roles |

See [Authentication](authentication.md) for the full authentication model.

## Strict vs flexible mode

By default, schemas enforce strict validation: any parameter or message property that is not explicitly declared is rejected as an error. This catches typos, deprecated keys, and misconfigurations early.

Some scenarios call for leniency — staged rollouts where new properties are added gradually, generic connectors that forward arbitrary metadata, or integration with systems that send extra fields. Flexible mode allows unknown keys to pass through without error.

By default, schemas are **strict**: any unknown parameter in `ConnectionSettings` or unknown property on a message produces a validation error.

**Flexible mode** allows extra keys — useful during staged rollouts or when the connector forwards arbitrary metadata:

```csharp
var schema = new ChannelSchema("Provider", "Type", "1.0")
    .WithFlexibleMode();     // unknown keys pass validation silently
```

```csharp
var schema = new ChannelSchema("Provider", "Type", "1.0")
    .WithStrictMode();       // the default — explicit is clearer
```

## Schema factory pattern

Connectors declare their schema via the `[ChannelSchema]` attribute:

```csharp
[ChannelSchema(typeof(TwilioSmsSchemaFactory))]
public class TwilioSmsConnector : ChannelConnectorBase { ... }
```

The attribute points to a class that implements `IChannelSchema` or `IChannelSchemaFactory`:

```csharp
public class TwilioSmsSchemaFactory : IChannelSchemaFactory
{
    public IChannelSchema CreateSchema() =>
        new ChannelSchema("Twilio", "SMS", "1.0.0")
            .WithDisplayName("Twilio SMS")
            .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery)
            .AddRequiredParameter("AccountSid", DataType.String)
            .AddRequiredParameter("AuthToken", DataType.String)
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Media)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber);
}
```

Or a static instance:

```csharp
[ChannelSchema(typeof(MySchema))]
public class MyConnector : ChannelConnectorBase { ... }

public class MySchema : IChannelSchema
{
    // implement IChannelSchema directly
}
```

### Schema discovery

When you call `AddConnector<T>()`, the messaging builder:

1. Reads the `[ChannelSchema]` attribute on `TConnector`
2. Instantiates the schema type (or calls `CreateSchema()` on the factory)
3. Registers the schema in `IChannelSchemaRegistry`
4. Passes the schema to the connector constructor

### Schema override at registration time

```csharp
builder.Services.AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithSchema(myCustomSchema));     // replaces auto-discovered schema
```

This is useful for testing, environment-specific customization, or feature-tier restrictions.

## Full schema example

```csharp
var schema = new ChannelSchema("SendGrid", "Email", "1.1.0")
    .WithDisplayName("SendGrid Transactional Email")
    .WithCapabilities(
        ChannelCapability.SendMessages |
        ChannelCapability.BulkMessaging |
        ChannelCapability.Templates |
        ChannelCapability.HealthCheck)
    .AddRequiredParameter("ApiKey", DataType.String)
    .AddParameter(new ChannelParameter("SandboxMode", DataType.Boolean)
    {
        DefaultValue = false
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AddContentType(MessageContentType.Template)
    .HandlesMessageEndpoint(EndpointType.EmailAddress)
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", DataType.String)
    {
        IsRequired = true,
        MaxLength = 998
    })
    .AddMessageProperty(new MessagePropertyConfiguration("TrackingSettings", DataType.String)
    {
        IsRequired = false
    })
    .AddAuthenticationConfiguration(
        AuthenticationConfigurations.ApiKeyAuthentication("ApiKey"));
```
