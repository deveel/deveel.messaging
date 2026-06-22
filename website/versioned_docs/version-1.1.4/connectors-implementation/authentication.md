# Authentication in Custom Connectors

This guide covers authentication implementation for custom connectors. For general authentication concepts, see [Authentication](../authentication.md).

## Declaring Authentication in Schema

Declare what authentication your connector supports in the schema:

### Simple API Key

```csharp
new ChannelSchemaBuilder("MyProvider", "REST", "1.0.0")
    .AddAuthenticationScheme(AuthenticationScheme.ApiKey)
```

This automatically accepts parameters like `ApiKey`, `Key`, or `AccessKey`.

### Explicit Configuration

For precise control over field names:

```csharp
new ChannelSchemaBuilder("MyProvider", "REST", "1.0.0")
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
            .WithField("ApiKey", DataType.String, f =>
            {
                f.AuthenticationRole = "principal";
                f.IsSensitive = true;
                f.Description = "API key for authentication";
            }))
```

### Multiple Authentication Options

Support multiple auth methods:

```csharp
new ChannelSchemaBuilder("MyProvider", "REST", "1.0.0")
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
            .WithField("ApiKey", DataType.String, f =>
            {
                f.AuthenticationRole = "principal";
                f.IsSensitive = true;
            }))
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(AuthenticationScheme.Basic, "Basic Auth")
            .WithField("Username", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("Password", DataType.String, f =>
            {
                f.AuthenticationRole = "credential";
                f.IsSensitive = true;
            }))
```

The framework selects the first configuration that matches the provided connection settings.

## Using Authentication in Your Connector

Authentication happens automatically during `InitializeAsync()`. Access credentials via the base class:

### Access Credential Value

```csharp
protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    // AuthenticationCredential is populated by base class
    var token = AuthenticationCredential?.Value;
    var scheme = AuthenticationCredential?.Scheme;
    
    if (token != null)
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
    
    SetState(ConnectorState.Ready);
    return ValueTask.CompletedTask;
}
```

### Use Helper Methods

Base class provides helpers for common auth patterns:

```csharp
protected override ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    // Get Authorization header value
    var authHeader = GetAuthenticationHeader();
    // Returns: "Bearer token" | "Basic base64" | null
    
    // Get raw API key
    var apiKey = GetApiKey();
    // Returns: raw API key value | null
    
    if (authHeader != null)
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
    }
    else if (apiKey != null)
    {
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }
    
    SetState(ConnectorState.Ready);
    return ValueTask.CompletedTask;
}
```

### Manual Authentication

If auto-authentication fails or you need manual control:

```csharp
protected override async ValueTask InitializeConnectorAsync(CancellationToken ct)
{
    try
    {
        // Attempt manual authentication
        var result = await AuthenticateAsync(ct);
        
        if (!result.IsSuccess())
        {
            throw new ConnectorException(
                "AUTH_FAILED",
                "Unable to authenticate: " + result.Error?.Message);
        }
        
        // Credential is now available
        var credential = AuthenticationCredential;
        
        SetState(ConnectorState.Ready);
    }
    catch (Exception ex)
    {
        SetState(ConnectorState.Error);
        throw;
    }
}
```

### Refresh Credentials

Refresh expired credentials:

```csharp
public async Task EnsureValidCredentialsAsync(CancellationToken ct)
{
    if (AuthenticationCredential?.IsExpired == true)
    {
        var refreshResult = await RefreshAuthenticationAsync(ct);
        
        if (!refreshResult.IsSuccess())
        {
            throw new ConnectorException(
                "REFRESH_FAILED",
                "Failed to refresh credentials");
        }
    }
}
```

## Custom Authentication Providers

For non-standard authentication, implement `IAuthenticationProvider`:

### Create Custom Provider

```csharp
public class HmacSignatureProvider : AuthenticationProviderBase
{
    public HmacSignatureProvider()
        : base(AuthenticationScheme.Of("hmac-sha256"), "HMAC-SHA256 Signature")
    {
    }

    public override Task<AuthenticationResult> ObtainCredentialAsync(
        ConnectionSettings settings,
        AuthenticationConfiguration configuration,
        CancellationToken ct)
    {
        var apiKey = GetStringParameter(settings, "ApiKey");
        var secretKey = GetStringParameter(settings, "SecretKey");

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            return Task.FromResult(
                AuthenticationResult.Failure("ApiKey and SecretKey required", "MISSING_CREDENTIALS"));

        // Create credential with both values
        var credential = new AuthenticationCredential(
            AuthenticationScheme.Of("hmac-sha256"),
            $"{apiKey}:{secretKey}");

        credential.Properties["ApiKey"] = apiKey;
        credential.Properties["SecretKey"] = secretKey;

        return Task.FromResult(AuthenticationResult.Success(credential));
    }
}
```

### Register Provider

```csharp
var authManager = serviceProvider.GetRequiredService<IAuthenticationManager>();
authManager.RegisterProvider(new HmacSignatureProvider());
```

### Declare in Schema

```csharp
new ChannelSchemaBuilder("MyProvider", "REST", "1.0.0")
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(
            AuthenticationScheme.Of("hmac-sha256"), "HMAC Signature")
            .WithField("ApiKey", DataType.String, f =>
            {
                f.AuthenticationRole = "principal";
                f.IsSensitive = true;
            })
            .WithField("SecretKey", DataType.String, f =>
            {
                f.AuthenticationRole = "credential";
                f.IsSensitive = true;
            }))
```

## OAuth 2.0 Client Credentials

For OAuth 2.0 flows, use the built-in provider:

### Schema Configuration

```csharp
new ChannelSchemaBuilder("MyProvider", "REST", "1.0.0")
    .AddAuthenticationConfiguration(
        new AuthenticationConfiguration(
            AuthenticationScheme.OAuthClientCredentials, "OAuth 2.0")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f =>
            {
                f.AuthenticationRole = "credential";
                f.IsSensitive = true;
            })
            .WithField("TokenEndpoint", DataType.String, _ => { })
            .WithField("Scope", DataType.String, _ => { }))
```

### Connection Settings

```json
{
  "MyProvider": {
    "ClientId": "client-123",
    "ClientSecret": "secret-456",
    "TokenEndpoint": "https://provider.com/oauth/token",
    "Scope": "read write"
  }
}
```

The framework automatically:
- Requests access tokens
- Caches credentials
- Refreshes before expiration

## Authentication Field Roles

Use roles to clarify field purposes:

| Role | Meaning | Example Fields |
|------|---------|----------------|
| `"principal"` | Primary identifier | ApiKey, Token, Username, AccountSid |
| `"credential"` | Corresponding secret | Password, AuthToken, ClientSecret |
| (other) | Auxiliary | ProjectId, CertificatePassword |

### Example with Roles

```csharp
new AuthenticationConfiguration(AuthenticationScheme.Basic, "Basic Auth")
    .WithField("AccountSid", DataType.String, f => 
    {
        f.AuthenticationRole = "principal";
        f.Description = "Account SID";
    })
    .WithField("AuthToken", DataType.String, f =>
    {
        f.AuthenticationRole = "credential";
        f.IsSensitive = true;
        f.Description = "Authentication token";
    })
```

## Best Practices

### ✅ DO: Mark Sensitive Fields

```csharp
.WithField("ApiKey", DataType.String, f =>
{
    f.IsSensitive = true;  // Value redacted in logs
})
```

### ✅ DO: Provide Clear Descriptions

```csharp
.WithField("TokenEndpoint", DataType.String, f =>
{
    f.Description = "OAuth 2.0 token endpoint URL";
})
```

### ✅ DO: Use Standard Field Names

Use common names for automatic matching:
- `ApiKey`, `Key`, `AccessKey` for API key auth
- `Token`, `AccessToken`, `BearerToken` for bearer auth
- `Username`/`Password` or `AccountSid`/`AuthToken` for basic auth

### ❌ DON'T: Hardcode Credentials

```csharp
// ❌ Bad - hardcoded secret
.WithField("ApiKey", DataType.String)
    .DefaultValue = "hardcoded-secret";

// ✅ Good - require configuration
.WithField("ApiKey", DataType.String, f =>
{
    f.IsRequired = true;
})
```

## Troubleshooting

### Authentication Failed

**Error:** `AUTHENTICATION_FAILED`

**Causes:**
- Missing required credentials
- Invalid credentials
- Expired credentials

**Solution:**
1. Verify credentials in connection settings
2. Check credential validity in provider console
3. Enable credential refresh if applicable

### No Authentication Configuration Found

**Error:** `No suitable authentication configuration found`

**Cause:** Connection settings don't match any auth configuration

**Solution:**
- Verify parameter names match schema field names
- Ensure all required fields are provided
- Check for typos in configuration

## See Also

- [Authentication Overview](../authentication.md) - General authentication concepts
- [Minimum Implementation](minimum-implementation.md) - Basic connector setup
- [Connection Settings](../connectors-configuration/connection-settings.md) - Configuring credentials
