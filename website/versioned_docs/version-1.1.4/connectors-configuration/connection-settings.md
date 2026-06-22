# Connection Settings

Connection settings are the configuration parameters used to establish and customize connector behavior.

## What are Connection Settings

The `ConnectionSettings` class is the primary mechanism for configuring connectors. It provides:

- **Key-value parameter storage** - All settings are stored as string keys with object values
- **Schema-based validation** - Optional validation against channel schemas
- **Type-safe retrieval** - Generic `GetParameter<T>()` methods with automatic type conversion
- **Default value support** - Schema-defined defaults for optional parameters

```csharp
var settings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC123456")
    .SetParameter("AuthToken", "secret")
    .SetParameter("MaxRetries", 3);
```

## Parameter Types

Connection settings support four fundamental data types defined by the `DataType` enumeration:

| DataType | Description | Compatible C# Types | Example |
|----------|-------------|---------------------|---------|
| `String` | Text values | `string` | `"AC123456"` |
| `Integer` | Whole numbers | `int`, `long`, `byte`, `short`, `sbyte` | `3` |
| `Number` | Decimal values | `double`, `decimal`, `float` | `0.01` |
| `Boolean` | True/false flags | `bool` | `true` |

Each connector schema defines which parameters it accepts and their expected data types.

## Type Conversion

Connection settings support automatic type conversion between compatible types, making it flexible to work with configuration from different sources.

### String to Numeric Conversion

String values are automatically converted to numeric types when the schema expects a number:

```csharp
// Schema expects Integer
settings.SetParameter("MaxRetries", "3");  // String "3" → int 3

// Schema expects Number  
settings.SetParameter("MaxPrice", "0.01"); // String "0.01" → decimal 0.01
```

This is particularly useful when loading from configuration files or connection strings where all values are strings.

### Numeric to String Conversion

Numeric values can be stored and retrieved as strings:

```csharp
settings.SetParameter("Timeout", 60);  // int 60 stored
var timeout = settings.GetParameter<string>("Timeout"); // Returns "60"
```

### Boolean Parsing

Boolean values support case-insensitive string parsing:

```csharp
settings.SetParameter("DryRun", "true");   // String "true" → bool true
settings.SetParameter("DryRun", "TRUE");   // Also works (case-insensitive)
settings.SetParameter("DryRun", "false");  // String "false" → bool false
```

### Type-Safe Retrieval

Use generic `GetParameter<T>()` for type-safe access:

```csharp
var maxRetries = settings.GetParameter<int>("MaxRetries");
var dryRun = settings.GetParameter<bool>("DryRun");
var authToken = settings.GetParameter<string>("AuthToken");
```

The method validates that the stored value can be converted to the requested type and throws `InvalidCastException` if conversion is not possible.

## Schema Validation

When a `ConnectionSettings` instance is created with a schema, all parameter operations are validated:

```csharp
var settings = new ConnectionSettings(schema);
```

### Required Parameters

Parameters marked as `IsRequired = true` must have non-null values:

```csharp
// Schema definition example
new ChannelParameter("AuthToken", DataType.String) 
{ 
    IsRequired = true 
}

// Validation - throws ArgumentException if null
settings.SetParameter("AuthToken", null); // ❌ Throws ArgumentException
```

**Validation occurs at `SetParameter()` time**, not at connector initialization, providing immediate feedback.

### Allowed Values (Enumerated Constraints)

Parameters can restrict values to a specific set:

```csharp
// Schema definition
new ChannelParameter("ParseMode", DataType.String)
{
    AllowedValues = new[] { "Markdown", "HTML", "Plain" }
}

// Validation
settings.SetParameter("ParseMode", "Markdown"); // ✅ Valid
settings.SetParameter("ParseMode", "XML");      // ❌ Throws - not in allowed list
```

### Min/Max Constraints

Numeric and string parameters can define range constraints:

```csharp
// Numeric constraints
new ChannelParameter("MaxPrice", DataType.Number)
{
    MinValue = 0,
    MaxValue = 1000
}

settings.SetParameter("MaxPrice", 500);  // ✅ Valid
settings.SetParameter("MaxPrice", 1500); // ❌ Throws - exceeds max

// String length constraints
new ChannelParameter("SenderName", DataType.String)
{
    MinLength = 1,
    MaxLength = 11  // SMS sender ID limit
}

settings.SetParameter("SenderName", "MyCompany");     // ✅ Valid
settings.SetParameter("SenderName", "MyVeryLongName123"); // ❌ Throws - too long
```

### Data Type Validation

The schema enforces type compatibility:

```csharp
// Schema expects Integer
new ChannelParameter("MaxRetries", DataType.Integer)

settings.SetParameter("MaxRetries", "not-a-number"); // ❌ Throws - not convertible
settings.SetParameter("MaxRetries", "3");            // ✅ Valid - convertible string
```

## Sensitive Parameters

Parameters marked as `IsSensitive = true` are automatically redacted in framework logs:

```csharp
// Schema definition
new ChannelParameter("AuthToken", DataType.String)
{
    IsSensitive = true
}

// In framework logs:
// AuthToken="***" instead of the actual value
```

**What gets redacted:**
- Parameter values in logging scopes
- Connection settings dump in debug logs
- Error messages that include parameter values

**Best Practices:**
- ✅ Mark all credentials as sensitive (API keys, tokens, passwords)
- ✅ Mark authentication tokens and secrets
- ✅ Mark webhook secret tokens
- ❌ Don't mark non-sensitive configuration (URLs, IDs, feature flags)

## Default Values

Schema can define default values for optional parameters:

```csharp
// Schema definition
new ChannelParameter("MaxRetries", DataType.Integer)
{
    DefaultValue = 3,
    IsRequired = false
}

// Retrieval - returns default if not explicitly set
var retries = settings.GetParameter<int>("MaxRetries"); // Returns 3
```

**Default value behavior:**
- Defaults are used when `GetParameter()` is called for a parameter that wasn't set
- Defaults do NOT populate the `Parameters` dictionary - they're applied on retrieval
- You can override defaults by explicitly setting a value

## Setting Parameters

### Programmatic Approach

Direct parameter setting with method chaining:

```csharp
var settings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC123456")
    .SetParameter("AuthToken", "secret")
    .SetParameter("MaxRetries", 3)
    .SetParameter("DryRun", true);
```

### Indexer Syntax

Alternative syntax using indexer:

```csharp
var settings = new ConnectionSettings();
settings["AccountSid"] = "AC123456";
settings["AuthToken"] = "secret";
settings["MaxRetries"] = 3;
```

### From Configuration Sections

Load settings from `appsettings.json` or other `IConfiguration` sources:

```json
{
  "Twilio": {
    "AccountSid": "AC123456",
    "AuthToken": "secret",
    "MaxRetries": 3
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithSettings("Twilio")); // Reads from "Twilio" section
```

**Nested configuration** is flattened:

```json
{
  "Twilio": {
    "AccountSid": "AC123456",
    "Timeout": {
      "Send": "00:01:00",
      "Receive": "00:00:30"
    }
  }
}
```

Results in parameters: `AccountSid`, `Timeout.Send`, `Timeout.Receive`.

### From Connection Strings

Compact semicolon-delimited format:

```csharp
cfg.WithConnectionString("AccountSid=AC123;AuthToken=secret;MaxRetries=3");
```

See [Connection Strings](connection-strings.md) for detailed format documentation.

### From Typed Options

Use strongly-typed options classes:

```csharp
var options = new TwilioSmsOptions 
{ 
    AccountSid = "AC123456",
    AuthToken = "secret",
    SendTimeout = TimeSpan.FromSeconds(60),
    ReceiveTimeout = TimeSpan.FromSeconds(30)
};

cfg.WithOptions(options);
```

The `ToConnectionSettings()` method converts typed options to parameter dictionaries.

## Retrieving Parameters

### Basic Retrieval

```csharp
var accountSid = settings.GetParameter("AccountSid"); // Returns object?
var maxRetries = settings.GetParameter<int>("MaxRetries"); // Returns int
var dryRun = settings.GetParameter<bool>("DryRun"); // Returns bool
```

### With Default Fallback

```csharp
// Returns default from schema if not set
var retries = settings.GetParameter<int>("MaxRetries");

// Returns your default if schema has none
var timeout = settings.Parameters.TryGetValue("Timeout", out var val) 
    ? (TimeSpan)val! 
    : TimeSpan.FromSeconds(30);
```

### Checking for Parameter Existence

```csharp
if (settings.Parameters.ContainsKey("AccountSid"))
{
    var accountSid = settings.GetParameter<string>("AccountSid");
}
```

## Validation Errors

When validation fails, clear error messages are provided:

```
ArgumentException: The parameter 'AuthToken' is required by this schema
ArgumentException: The parameter 'ParseMode' is not supported by this schema
ArgumentException: The value 'XML' is not allowed for the parameter 'ParseMode'
ArgumentException: The value provided for the key 'MaxRetries' is not compatible with the type 'Integer'
```

## Security Considerations

### Credential Management

Never store secrets in source code. Use environment variables, user secrets (development), or a vault (production):

```csharp
// appsettings.json — use placeholders, not real values
{
  "Twilio": {
    "AccountSid": "",
    "AuthToken": ""
  }
}

// Environment variables override at runtime
export Twilio__AccountSid="AC..."
export Twilio__AuthToken="..."
```

### Sensitive Parameter Redaction

Mark schema parameters as `IsSensitive` — the framework redacts their values in logs:

```csharp
new ChannelParameter("AuthToken", DataType.String)
{
    IsRequired = true,
    IsSensitive = true
};
```

When logging, sensitive parameter values appear as `"***"` instead of the actual value.

### Webhook Signature Validation

Inbound webhooks from providers include cryptographic signatures. Always validate them:

- **Twilio**: validate `X-Twilio-Signature` header using your auth token
- **Telegram**: set `SecretToken` and validate `X-Telegram-Bot-Api-Secret-Token`
- **Facebook**: validate `X-Hub-Signature-256` using your app secret
- **SendGrid**: validate `X-Twilio-Email-Event-Webhook-Signature`

### Named Connector Isolation

Use named connectors to isolate different connector instances:

```csharp
services.AddMessaging()
    .AddConnector<TwilioSmsConnector>("primary", cfg => cfg
        .WithSettings("Twilio:Primary"))
    .AddConnector<TwilioSmsConnector>("secondary", cfg => cfg
        .WithSettings("Twilio:Secondary"));
```

## See Also

- [Connection Strings](connection-strings.md) - connection string format and usage
- [Authentication](../authentication.md) - configuring authentication parameters
- [Channel Schema](../channel-schema.md) - defining parameter constraints
- [Webhook Signature Validation](../connectors-implementation/advanced-topics.md) - Validating inbound webhooks (see Security section)
