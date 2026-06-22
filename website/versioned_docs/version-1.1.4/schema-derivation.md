---
sidebar_position: 7
---

# Schema Derivation

A single connector type — say, Twilio SMS — can serve many different use cases. A development environment should not send real messages. A basic-tier environment should not have access to media messaging. An outbound-only integration does not need webhook parameters. These are all variations of the same connector, differing only in which capabilities, parameters, and constraints apply.

Schema derivation lets you model this without duplication. You define a master schema that represents the full capability of the connector, then derive restricted schemas that inherit the base structure while removing or tightening specific aspects. The derived schema keeps the same logical identity as the base, so the registry and compatibility checks continue to work — but the connector created from a derived schema behaves according to the restrictions applied.

This is the primary mechanism for environment-specific settings and feature-tier management.

## Copy constructor

The copy constructor clones all properties from the source schema:

- Channel provider, type, and version (the logical identity)
- Capabilities
- Parameters (full deep copy with all constraints)
- Content types
- Message property configurations
- Endpoint configurations
- Authentication configurations
- Strict/flexible mode

The new schema is fully independent — changes to the derived schema do not affect the source.

```csharp
var baseSchema = new ChannelSchema("Twilio", "SMS", "1.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    .AddRequiredParameter("AccountSid", DataType.String)
    .AddRequiredParameter("AuthToken", DataType.String)
    .AddParameter(new ChannelParameter("WebhookUrl", DataType.String))
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .HandlesMessageEndpoint(EndpointType.PhoneNumber);

var outboundOnly = new ChannelSchema(baseSchema, "Outbound only")
    .RemoveCapability(ChannelCapability.ReceiveMessages)
    .RemoveParameter("WebhookUrl")
    .RestrictContentTypes(MessageContentType.PlainText);
```

The derived schema retains `"Twilio/SMS/1.0"` as its logical identity. This means `IsCompatibleWith(baseSchema)` returns `true`, and `ValidateAsRestrictionOf(baseSchema)` can validate the restriction is valid.

## Restriction methods

| Method | Effect | Use case |
|---|---|---|
| `RemoveCapability(ChannelCapability)` | Remove one or more capability flags | Disable receive/status for outbound-only connectors |
| `RemoveParameter(string)` | Drop a parameter | Remove webhook URL when not receiving |
| `UpdateParameter(string, Action<ChannelParameter>)` | Change parameter properties | Set environment-specific defaults |
| `RemoveMessageProperty(string)` | Drop a message property | Remove unused properties |
| `UpdateMessageProperty(string, Action<MessagePropertyConfiguration>)` | Change property constraints | Tighten validation rules |
| `RemoveContentType(MessageContentType)` | Remove a content type | Disable media for basic tier |
| `RestrictContentTypes(params MessageContentType[])` | Replace the content type set | Allow only text for SMS-only tier |
| `RemoveEndpoint(EndpointType)` | Remove an endpoint configuration | Disable email endpoints on SMS connector |
| `UpdateEndpoint(EndpointType, Action<ChannelEndpointConfiguration>)` | Modify endpoint settings | Change send/receive direction |
| `RemoveAuthenticationType(AuthenticationType)` | Remove an auth configuration | Remove unsupported auth methods |
| `RestrictAuthenticationTypes(params AuthenticationType[])` | Replace auth types | Force a specific auth method |
| `RemoveAuthenticationConfiguration(...)` | Remove by configuration | Remove a specific auth config |
| `RestrictAuthenticationConfigurations(...)` | Replace auth configs | Set allowed auth configs |
| `RestrictCapabilities(params ChannelCapability[])` | Replace capabilities | Set exact capability set |

## Validating restrictions

Derivation is a powerful tool, but unrestricted schemas can produce confusing errors at runtime — for example, a derived schema that removes a required parameter the connector expects, or that adds a capability the base never had. The framework provides `ValidateAsRestrictionOf` to catch these issues at configuration time, before connectors are created.

Before using a derived schema in production, always validate it is a compatible restriction of its base:

```csharp
var issues = outboundOnly.ValidateAsRestrictionOf(baseSchema);
if (issues.Any())
{
    foreach (var issue in issues)
        Console.WriteLine($"Schema violation: {issue.ErrorMessage}");
    throw new InvalidOperationException("Incompatible schema");
}
```

`ValidateAsRestrictionOf` checks:

- Same channel provider
- Same channel type  
- Same version
- Derived capabilities are a subset of base capabilities
- Derived parameters are a subset of base parameters (same names, compatible types)
- Derived content types are a subset of base content types
- Derived endpoints are a subset of base endpoints
- Derived message properties are a subset of base message properties

## Multi-level derivation

You can chain derivations:

```csharp
var baseSchema = CreateMasterSchema();

var enterprise = new ChannelSchema(baseSchema, "Enterprise")
    .RemoveCapability(ChannelCapability.ReceiveMessages);

var enterpriseGold = new ChannelSchema(enterprise, "Enterprise Gold")
    .UpdateParameter("Timeout", p => p.DefaultValue = 10000)
    .AddContentType(MessageContentType.Media);
```

Keep chains shallow (1-3 levels). Each level adds complexity; document what each level changes.

## Practical examples

### Environment-specific schemas with runtime settings

```csharp
var master = LoadMasterSchema("twilio-sms");

var instanceSchema = new ChannelSchema(master, $"Instance {instanceId}")
    .UpdateParameter("WebhookUrl", p =>
        p.DefaultValue = $"https://{instanceId}.example.com/webhook")
    .UpdateParameter("AccountSid", p =>
        p.DefaultValue = settings.AccountSid);
```

### Environment-specific schemas

```csharp
var development = new ChannelSchema(productionSchema, "Development")
    .UpdateParameter("Timeout", p => p.DefaultValue = 60000)
    .UpdateParameter("SandboxMode", p => p.DefaultValue = true);
```

### Feature-tier schemas

```csharp
var basic = new ChannelSchema(baseSchema, "Basic")
    .RemoveCapability(ChannelCapability.MediaAttachments)
    .RemoveCapability(ChannelCapability.Templates)
    .RestrictContentTypes(MessageContentType.PlainText);

var premium = new ChannelSchema(baseSchema, "Premium")
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Template);
```

### Runtime schema selection from registry

```csharp
var registry = serviceProvider.GetRequiredService<IChannelSchemaRegistry>();
var master = registry.FindSchema("Twilio", "SMS");

var instanceSchema = new ChannelSchema(master, instanceName)
    .UpdateParameter("WebhookUrl", p =>
        p.DefaultValue = webhookUrl);
```

## Good practices

- **Start from a complete master schema** — the base should represent the full capability of the connector. Restrict from there.
- **Validate every derived schema** before creating connectors from it. Use `ValidateAsRestrictionOf`.
- **Use descriptive display names** — the `derivedDisplayName` parameter appears in logs and registry queries.
- **Keep derivation chains shallow** — 1-3 levels is usually enough.
- **Document what each level changes** — especially when schemas are used across different deployment environments.
