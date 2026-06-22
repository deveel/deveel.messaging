---
sidebar_position: 9
---

# Authentication

Every messaging provider requires authentication — an API key, bearer token, username/password, or OAuth flow. The framework provides a unified authentication system that works consistently across all connectors.

## Quick Start

Most connectors require simple credentials. Configure them in your connection settings:

```csharp
// API Key (SendGrid)
var settings = new ConnectionSettings()
    .SetParameter("ApiKey", "SG.xxxxx");

// Basic Auth (Twilio)
var settings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC...")
    .SetParameter("AuthToken", "secret");

// Bearer Token (Telegram)
var settings = new ConnectionSettings()
    .SetParameter("BotToken", "123456:ABC...");
```

Authentication happens automatically when you initialize the connector — no manual auth calls needed.

## How It Works

The framework separates authentication from connector logic:

1. **Connectors declare** what authentication they support in their schema
2. **You provide** credentials in connection settings
3. **The framework** automatically authenticates during initialization
4. **Credentials are cached** and refreshed when expired

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithSettings("Twilio"));  // Reads AccountSid + AuthToken from config

// Authentication happens automatically here:
var connector = serviceProvider.GetRequiredService<IChannelConnector>();
await connector.InitializeAsync(ct);  // ← Auto-authenticates
```

## Authentication Types

The framework supports these authentication schemes:

| Type | Used By | Credentials Needed |
|------|---------|-------------------|
| **API Key** | SendGrid, generic APIs | `ApiKey` |
| **Bearer Token** | Facebook, Telegram | `Token` or `AccessToken` |
| **Basic Auth** | Twilio, SMTP | `Username` + `Password` (or `AccountSid` + `AuthToken`) |
| **OAuth 2.0** | Future connectors | `ClientId` + `ClientSecret` + `TokenEndpoint` |
| **Certificate** | Firebase | `ServiceAccountKey` (JSON) |

### API Key Authentication

Used by SendGrid and many REST APIs:

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxxx"
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<SendGridEmailConnector>(cfg => cfg
        .WithSettings("SendGrid"));
```

### Basic Authentication

Used by Twilio (SMS/WhatsApp):

```json
{
  "Twilio": {
    "AccountSid": "AC123456",
    "AuthToken": "your_auth_token"
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TwilioSmsConnector>(cfg => cfg
        .WithSettings("Twilio"));
```

The framework automatically combines `AccountSid` and `AuthToken` into a Basic auth header.

### Bearer Token

Used by Facebook Messenger and Telegram Bot:

```json
{
  "Telegram": {
    "BotToken": "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"
  },
  "Facebook": {
    "PageAccessToken": "EAAxxxxxxxxxxxx",
    "PageId": "123456789"
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<TelegramBotConnector>(cfg => cfg
        .WithSettings("Telegram"))
    .AddConnector<FacebookMessengerConnector>(cfg => cfg
        .WithSettings("Facebook"));
```

### Certificate Authentication

Used by Firebase Cloud Messaging:

```json
{
  "Firebase": {
    "ProjectId": "my-project",
    "ServiceAccountKey": "{\"type\":\"service_account\",\"project_id\":\"my-project\",...}"
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<FirebasePushConnector>(cfg => cfg
        .WithSettings("Firebase"));
```

The `ServiceAccountKey` is the complete JSON content of your Firebase service account key file.

## Credential Management

### Automatic Caching

Credentials are automatically cached in memory after the first authentication. The framework:

- ✅ Caches credentials by connection settings
- ✅ Detects when credentials are about to expire
- ✅ Automatically refreshes expired credentials
- ✅ Shares cached credentials across connector instances with the same settings

### Manual Cache Control

Clear all cached credentials:

```csharp
var authManager = serviceProvider.GetRequiredService<IAuthenticationManager>();
authManager.ClearCache();
```

Invalidate a specific credential:

```csharp
authManager.InvalidateCredential(settings, authConfiguration);
```

### Manual Authentication

Authentication happens automatically, but you can manually authenticate if needed:

```csharp
public class MyService
{
    private readonly IChannelConnector _connector;

    public async Task ReauthenticateAsync(CancellationToken ct)
    {
        // Force re-authentication (e.g., after credential rotation)
        var result = await ((ChannelConnectorBase)_connector).AuthenticateAsync(ct);
        
        if (!result.IsSuccess())
            throw new InvalidOperationException("Authentication failed");
    }
}
```

## Security Best Practices

### ✅ DO: Store Secrets Securely

**Use environment variables:**
```bash
export Twilio__AccountSid="AC..."
export Twilio__AuthToken="secret"
```

**Use Azure Key Vault:**
```json
{
  "KeyVault": {
    "Endpoint": "https://myvault.vault.azure.net/"
  }
}
```

**Use user secrets (development):**
```bash
dotnet user-secrets set "Twilio:AuthToken" "secret"
```

### ❌ DON'T: Hardcode Secrets

```csharp
// ❌ Bad - secrets in source code
var settings = new ConnectionSettings()
    .SetParameter("AuthToken", "hardcoded-secret");

// ✅ Good - load from configuration
var settings = new ConnectionSettings()
    .SetParameter("AuthToken", configuration["Twilio:AuthToken"]);
```

### Sensitive Parameter Redaction

Parameters marked as sensitive are automatically redacted in logs:

```
// In logs:
AuthToken="***"  instead of  AuthToken="actual-secret-value"
```

All authentication credentials (tokens, keys, passwords) are automatically marked as sensitive by the framework.

## Troubleshooting

### Authentication Failed

**Error:** `AUTHENTICATION_FAILED`

**Common causes:**
- ❌ Missing required credentials in connection settings
- ❌ Invalid API key or token
- ❌ Expired credentials

**Solution:**
1. Verify credentials are present in configuration
2. Check credentials in provider's console
3. Ensure credentials haven't expired

### No Authentication Configuration Found

**Error:** `No suitable authentication configuration found`

**Cause:** Connection settings don't match any auth configuration in the connector's schema.

**Solution:**
- Check connector documentation for required parameter names
- Verify parameter names match exactly (case-insensitive)
- Ensure all required fields are provided

### Credential Expiration

If credentials expire frequently:

1. **Check token lifetime** - Some providers issue short-lived tokens
2. **Enable automatic refresh** - OAuth providers auto-refresh by default
3. **Monitor expiration** - Use `AuthenticationCredential.IsExpired` property

```csharp
var credential = connector.AuthenticationCredential;
if (credential?.IsExpired == true)
{
    await ((ChannelConnectorBase)connector).RefreshAuthenticationAsync(ct);
}
```

## Connector-Specific Guides

For detailed authentication setup for each connector:

- [Twilio SMS](connectors/twilio-sms.md) - AccountSid + AuthToken
- [Twilio WhatsApp](connectors/twilio-whatsapp.md) - AccountSid + AuthToken
- [SendGrid Email](connectors/sendgrid-email.md) - API Key
- [Firebase Push](connectors/firebase-push.md) - Service Account Key
- [Facebook Messenger](connectors/facebook-messenger.md) - Page Access Token
- [Telegram Bot](connectors/telegram-bot.md) - Bot Token

## Advanced Topics

### Custom Authentication Providers

For providers with non-standard authentication, you can implement custom auth providers. See [Connector Implementation](connectors-implementation/overview.md) for guidance.

### OAuth 2.0 Client Credentials

The framework supports OAuth 2.0 client credentials flow for server-to-server authentication. This requires:

```json
{
  "OAuth": {
    "ClientId": "client-123",
    "ClientSecret": "secret-456",
    "TokenEndpoint": "https://provider.com/oauth/token",
    "Scope": "read write"
  }
}
```

The framework automatically:
- Requests access tokens
- Caches them
- Refreshes before expiration

### Multiple Authentication Options

Some connectors support multiple auth methods. The framework selects the first matching configuration:

```csharp
// Connector schema can declare multiple auth options
new ChannelSchemaBuilder("Provider", "Type", "1.0")
    .AddAuthenticationConfiguration(apiKeyConfig)    // Try this first
    .AddAuthenticationConfiguration(basicAuthConfig); // Fallback to this
```

The framework selects based on which credentials you provide in connection settings.
