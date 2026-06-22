# Connection Strings

Connection strings provide a compact, semicolon-delimited format for specifying connection settings. They are ideal for environment variables, command-line configuration, and simple setups.

## Format

Connection strings use a simple key-value format:

```
key1=value1;key2=value2;key3=value3
```

**Example:**
```
AccountSid=AC123456;AuthToken=secret;MaxRetries=3
```

## Parsing Rules

The connection string parser follows these rules:

### Key-Value Pairs

- Keys are **case-insensitive** (`AccountSid` = `accountsid`)
- Values can be unquoted or quoted
- Whitespace around `=` is optional and trimmed
- Pairs are separated by semicolons

```
AccountSid=AC123456;AuthToken=secret
AccountSid = AC123456 ; AuthToken = secret  // Same result
```

### Quoted Values

Use quotes for values containing special characters:

```csharp
// Double quotes
"Key=\"value with spaces\";Next=simple"

// Single quotes  
"Key='value with spaces';Next=simple"

// Quotes preserve internal semicolons
"Callback='url;with;semicolons';Next=value"
```

### Escape Sequences

Escape special characters within quoted values:

```csharp
// Escaped quote
"Key=\"value with \\\" quote\""

// Escaped backslash
"Path='C:\\\\path\\\\to\\\\file'"

// Result: Key = value with " quote
//         Path = C:\path\to\file
```

### Whitespace Handling

Leading and trailing whitespace is automatically trimmed:

```csharp
"Key = Value ; Next = Value"
// Parsed as: Key="Value", Next="Value"
```

### Boolean Values

Boolean parameters accept common true/false representations:

```
DryRun=true
DryRun=false
DryRun=True     // Case-insensitive
DryRun=FALSE    // Case-insensitive
```

## Type Inference

Connection string values are strings but are converted based on the connector schema:

| Schema DataType | Connection String Example | Converted To |
|-----------------|---------------------------|--------------|
| `String` | `AuthToken=secret` | `string` |
| `Integer` | `MaxRetries=3` | `int` |
| `Number` | `MaxPrice=0.01` | `decimal` |
| `Boolean` | `DryRun=true` | `bool` |
| `TimeSpan` | `Timeout.Send=00:01:00` | `TimeSpan` |

**Example:**
```csharp
// Schema defines MaxRetries as Integer
var settings = ConnectionSettings.Parse("MaxRetries=3");
var retries = settings.GetParameter<int>("MaxRetries"); // Returns int 3

// Schema defines MaxPrice as Number
var settings = ConnectionSettings.Parse("MaxPrice=0.01");
var price = settings.GetParameter<decimal>("MaxPrice"); // Returns decimal 0.01m
```

## Connector Examples

### Twilio SMS

**Basic Configuration:**
```
AccountSid=AC123456;AuthToken=your_auth_token
```

**With Webhook:**
```
AccountSid=AC123456;AuthToken=your_auth_token;WebhookUrl=https://myapp.com/twilio
```

**With Timeout Configuration:**
```
AccountSid=AC123456;AuthToken=your_auth_token;Timeout.Send=00:01:00;Timeout.Receive=00:00:30
```

**With Retry Policy:**
```
AccountSid=AC123456;AuthToken=your_auth_token;Retry.MaxAttempts=3;Retry.BackoffType=Exponential
```

### Twilio WhatsApp

**Basic Configuration:**
```
AccountSid=AC123456;AuthToken=your_auth_token
```

**With Status Callback:**
```
AccountSid=AC123456;AuthToken=your_auth_token;StatusCallback=https://myapp.com/status
```

### SendGrid Email

**Basic Configuration:**
```
ApiKey=SG.xxxxxxxxxxxx
```

**With Sandbox Mode:**
```
ApiKey=SG.xxxxxxxxxxxx;SandboxMode=true
```

**With Default Settings:**
```
ApiKey=SG.xxxxxxxxxxxx;DefaultFromName=Support;DefaultReplyTo=support@example.com
```

### Firebase Push

**Basic Configuration:**
```
ProjectId=my-project-id
```

**With Dry Run:**
```
ProjectId=my-project-id;DryRun=true
```

**With Timeout:**
```
ProjectId=my-project-id;Timeout.Send=00:00:30;Timeout.RetryOnTimeout=true
```

### Facebook Messenger

**Basic Configuration:**
```
PageAccessToken=EAAxxxxxxxxxxxx;PageId=123456789
```

**With Webhook:**
```
PageAccessToken=EAAxxxxxxxxxxxx;PageId=123456789;WebhookUrl=https://myapp.com/fb;VerifyToken=mytoken
```

### Telegram Bot

**Basic Configuration:**
```
BotToken=123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11
```

**With Parse Mode:**
```
BotToken=123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11;ParseMode=Markdown
```

**With Features Disabled:**
```
BotToken=123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11;DisableWebPagePreview=true;DisableNotification=true
```

## Advanced Configuration in Connection Strings

### Timeout Configuration

Configure per-operation timeouts:

```
# Timeout values as TimeSpan strings
Timeout.Send=00:01:00           # 1 minute
Timeout.Receive=00:00:30        # 30 seconds
Timeout.StatusQuery=00:00:15    # 15 seconds

# With retry on timeout
Timeout.RetryOnTimeout=true
```

**Complete timeout configuration:**
```
AccountSid=AC123;AuthToken=secret;
Timeout.Send=00:01:00;Timeout.Receive=00:00:30;Timeout.StatusQuery=00:00:15;Timeout.RetryOnTimeout=true
```

### Retry Policy Configuration

Configure retry behavior:

```
# Basic retry
Retry.MaxAttempts=3

# Backoff configuration
Retry.BaseDelay=00:00:01              # 1 second base delay
Retry.BackoffType=Exponential         # Exponential, Linear, or Constant
Retry.UseJitter=true                  # Add randomness to prevent thundering herd

# Retryable error codes (comma-separated)
Retry.RetryableErrorCodes=RATE_LIMITED,SERVICE_UNAVAILABLE,TIMEOUT

# Circuit breaker
Retry.EnableCircuitBreaker=true
Retry.CircuitBreaker.FailureRatio=0.5
Retry.CircuitBreaker.SamplingDuration=00:00:30
Retry.CircuitBreaker.MinimumThroughput=10
Retry.CircuitBreaker.BreakDuration=00:00:30
```

**Complete retry configuration:**
```
AccountSid=AC123;AuthToken=secret;
Retry.MaxAttempts=3;Retry.BaseDelay=00:00:01;Retry.BackoffType=Exponential;Retry.UseJitter=true;
Retry.RetryableErrorCodes=RATE_LIMITED,SERVICE_UNAVAILABLE
```

### Telemetry Configuration

Configure tracing and metrics:

```
# Enable/disable telemetry
Telemetry.EnableTracing=true
Telemetry.EnableMetrics=true

# Optional payload size metrics (disabled by default for performance)
Telemetry.EnablePayloadSizeMetrics=false
```

**Complete telemetry configuration:**
```
AccountSid=AC123;AuthToken=secret;
Telemetry.EnableTracing=true;Telemetry.EnableMetrics=true;Telemetry.EnablePayloadSizeMetrics=false
```

### Complete Example

All configuration types combined:

```
AccountSid=AC123456;AuthToken=secret;
WebhookUrl=https://myapp.com/webhook;
Timeout.Send=00:01:00;Timeout.Receive=00:00:30;Timeout.StatusQuery=00:00:15;Timeout.RetryOnTimeout=true;
Retry.MaxAttempts=3;Retry.BaseDelay=00:00:01;Retry.BackoffType=Exponential;Retry.UseJitter=true;
Telemetry.EnableTracing=true;Telemetry.EnableMetrics=true
```

## Usage Methods

### Fluent API with Connection String

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithConnectionString("AccountSid=AC123;AuthToken=secret"));
```

### From Configuration Section

```json
{
  "ConnectionStrings": {
    "Twilio": "AccountSid=AC123;AuthToken=secret;Timeout.Send=00:01:00"
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithConnectionString(Configuration.GetConnectionString("Twilio")));
```

### From Environment Variable

```bash
export TWILIO_CONNECTION_STRING="AccountSid=AC123;AuthToken=secret"
```

```csharp
var connectionString = Environment.GetEnvironmentVariable("TWILIO_CONNECTION_STRING");
var settings = ConnectionSettings.Parse(connectionString);
```

### Direct Parsing

```csharp
var settings = ConnectionSettings.Parse("AccountSid=AC123;AuthToken=secret");
var connector = new TwilioSmsConnector(schema, settings);
```

## Best Practices

### When to Use Connection Strings

✅ **Good fit for:**
- Simple configurations with few parameters
- Environment variable storage (12-factor apps)
- Quick prototyping and development
- Uniform format across different connectors
- CI/CD pipeline configuration

### When to Avoid

❌ **Consider alternatives when:**
- Complex nested configurations needed
- Compile-time type safety required
- Many optional parameters (becomes unwieldy)
- Need for IntelliSense in configuration
- Configuration requires comments or documentation

### Security Considerations

**DO:**
- ✅ Store connection strings in environment variables in production
- ✅ Use Azure Key Vault, AWS Secrets Manager, or similar for secrets
- ✅ Use user secrets during development
- ✅ Rotate credentials regularly

**DON'T:**
- ❌ Commit connection strings with real credentials to source control
- ❌ Hardcode secrets in application code
- ❌ Log connection string values
- ❌ Pass connection strings in URLs or query strings

### Formatting Tips

**For readability, break long connection strings:**

```csharp
// In code - use verbatim strings for readability
var connectionString = @"
    AccountSid=AC123456;
    AuthToken=secret;
    WebhookUrl=https://myapp.com/webhook;
    Timeout.Send=00:01:00;
    Timeout.Receive=00:00:30;
    Retry.MaxAttempts=3;
    Retry.BackoffType=Exponential
";

// Or chain WithSetting calls for complex configs
cfg.WithConnectionString("AccountSid=AC123;AuthToken=secret")
   .WithSetting("Timeout.Send", TimeSpan.FromSeconds(60))
   .WithSetting("Retry.MaxAttempts", 3);
```

## Troubleshooting

### Common Parsing Errors

**"Invalid connection string format"**
- Check for missing `=` between key and value
- Ensure semicolons separate pairs (not commas)
- Verify quoted values are properly closed

**"Parameter not supported by schema"**
- Verify parameter name spelling (case-insensitive)
- Check connector documentation for supported parameters
- Some parameters may only be available via fluent API

**"Cannot convert value to type"**
- Ensure numeric values don't have currency symbols
- Boolean values must be `true`/`false` (not `yes`/`no`)
- TimeSpan values must use proper format (`hh:mm:ss`)

### TimeSpan Format

Timeout values use standard .NET TimeSpan format:

```
# Valid formats
00:01:00          # 1 minute (hh:mm:ss)
00:00:30          # 30 seconds
1.00:00:00        # 1 day
00:01:30.500      # 1 minute 30.5 seconds

# In connection strings
Timeout.Send=00:01:00
Timeout.Receive=00:00:30
```

## See Also

- [Connection Settings](connection-settings.md) - parameter types and validation
- [Installation](installation.md) - configuration from appsettings.json
- [Authentication](authentication.md) - credential management
- [Retry Policies](retry-policies.md) - retry configuration details
- [Telemetry](telemetry.md) - tracing and metrics configuration
