# Authentication Mechanism for Messaging Connectors

## Overview

This document describes the authentication mechanism implemented for messaging connectors, which allows connectors to use authentication parameters (like Client ID and Client Secret) to obtain final authentication factors (like Access Tokens) during initialization.

## Key Components

### 1. Authentication Abstractions

#### `IAuthenticationProvider`
The core interface that defines how authentication providers work:
- `ObtainCredentialAsync()` - Gets credentials from connection settings
- `RefreshCredentialAsync()` - Refreshes existing credentials
- `CanHandle()` - Determines if the provider can handle a specific authentication configuration

#### `AuthenticationResult`
Contains the result of an authentication operation:
- `IsSuccessful` - Whether authentication succeeded
- `Credential` - The obtained authentication credential
- `ErrorMessage` / `ErrorCode` - Error details if authentication failed

#### `AuthenticationCredential`
Represents an obtained authentication credential:
- `AuthenticationType` - The type of authentication (Token, ApiKey, Basic, etc.)
- `CredentialValue` - The primary credential value (token, key, etc.)
- `ExpiresAt` - Optional expiration time
- `Properties` - Additional metadata

### 2. Built-in Authentication Providers

#### `DirectCredentialAuthenticationProvider`
Handles authentication types that don't require token exchange:
- **API Key Authentication** - Extracts API keys from connection settings
- **Token Authentication** - Handles pre-obtained tokens
- **Basic Authentication** - Creates Basic auth credentials from username/password

#### `ClientCredentialsAuthenticationProvider`
Implements OAuth 2.0 Client Credentials flow:
- Uses Client ID and Client Secret to obtain access tokens
- Supports token refresh when refresh tokens are available
- Handles various OAuth endpoints and scopes

#### `FirebaseServiceAccountAuthenticationProvider`
Specialized provider for Firebase/Google Service Account authentication:
- Validates and processes service account JSON keys
- Prepares credentials for Firebase SDK initialization

### 3. Authentication Manager

The `AuthenticationManager` coordinates multiple authentication providers:
- **Provider Registration** - Registers and manages authentication providers
- **Automatic Provider Selection** - Finds the right provider for each authentication configuration
- **Credential Caching** - Caches credentials to avoid unnecessary re-authentication
- **Automatic Refresh** - Refreshes credentials when they're about to expire

### 4. Connector Integration

#### Enhanced `ChannelConnectorBase`
The base connector class now includes:
- **Authentication Support** - Built-in authentication management
- **Helper Methods** - `AuthenticateAsync()`, `RefreshAuthenticationAsync()`
- **Credential Access** - `GetAuthenticationHeader()`, `GetApiKey()`
- **Automatic Integration** - Authentication happens during connector initialization

## Usage Patterns

### 1. API Key Authentication

```csharp
// Define schema with API key authentication
var schema = new ChannelSchema("Provider", "Email", "1.0.0")
    .AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication());

// Connection settings
var connectionSettings = new ConnectionSettings()
    .SetParameter("ApiKey", "sk-live-api-key-12345");

// Create and initialize connector
var connector = new EmailConnector(schema, connectionSettings);
await connector.InitializeAsync(); // Authentication happens here
```

### 2. OAuth 2.0 Client Credentials

```csharp
// Define schema with OAuth authentication
var schema = new ChannelSchema("Provider", "API", "1.0.0")
    .AddAuthenticationConfiguration(AuthenticationConfigurations.ClientCredentialsAuthentication());

// Connection settings
var connectionSettings = new ConnectionSettings()
    .SetParameter("ClientId", "oauth-client-id")
    .SetParameter("ClientSecret", "oauth-client-secret")
    .SetParameter("TokenEndpoint", "https://auth.provider.com/token");

// Create and initialize connector
var connector = new ApiConnector(schema, connectionSettings);
await connector.InitializeAsync(); // Obtains access token automatically
```

### 3. Flexible Authentication

```csharp
// Schema supporting multiple authentication methods
var schema = new ChannelSchema("Provider", "Multi", "1.0.0")
    .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleApiKeyAuthentication())
    .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleBasicAuthentication());

// Connection settings - will use API key auth
var connectionSettings = new ConnectionSettings()
    .SetParameter("ApiKey", "api-key-123");

// Alternative - will use basic auth
// var connectionSettings = new ConnectionSettings()
//     .SetParameter("Username", "user")
//     .SetParameter("Password", "pass");
```

### 4. Custom Authentication Providers

```csharp
// Create custom authentication manager
var authManager = new AuthenticationManager();

// Register custom provider
authManager.RegisterProvider(new CustomOAuthProvider());

// Use with connector
var connector = new CustomConnector(schema, connectionSettings, authManager);
```

## Implementation in Connectors

### Basic Implementation

```csharp
public class MyConnector : ChannelConnectorBase
{
    private readonly ConnectionSettings _connectionSettings;

    public MyConnector(IChannelSchema schema, ConnectionSettings connectionSettings)
        : base(schema)
    {
        _connectionSettings = connectionSettings;
    }

    protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
    {
        // Authenticate using connection settings
        var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
        if (!authResult.Successful)
        {
            return authResult;
        }

        // Additional initialization logic...
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
    {
        // Use authenticated credentials
        var apiKey = GetApiKey(); // For API key auth
        var authHeader = GetAuthenticationHeader(); // For token/basic auth

        // Make authenticated API calls...
    }
}
```

## Benefits

1. **Separation of Concerns** - Authentication logic is separated from connector business logic
2. **Extensibility** - Easy to add new authentication methods via providers
3. **Consistency** - Uniform authentication handling across all connectors
4. **Automatic Token Management** - Handles token refresh and expiration automatically
5. **Configuration-Driven** - Authentication method determined by schema configuration
6. **Testability** - Authentication can be mocked and tested independently

## Supported Authentication Types

- **None** - No authentication required
- **ApiKey** - API key authentication
- **Basic** - HTTP Basic authentication (username/password)
- **Token** - Bearer token authentication
- **ClientCredentials** - OAuth 2.0 Client Credentials flow
- **Certificate** - Certificate-based authentication
- **Custom** - Custom authentication methods

## Error Handling

The authentication mechanism provides comprehensive error handling:
- **Missing Parameters** - Clear error messages for missing required parameters
- **Invalid Credentials** - Specific error codes for authentication failures
- **Network Errors** - Handling of network issues during token exchange
- **Token Expiration** - Automatic refresh when tokens expire

## Testing

The authentication mechanism includes comprehensive tests:
- **Unit Tests** - Individual provider and component tests
- **Integration Tests** - End-to-end authentication flows
- **Configuration Tests** - Authentication configuration validation
- **Mock Support** - Easy mocking for testing connector implementations

This authentication mechanism provides a robust, extensible foundation for handling various authentication methods in messaging connectors, while maintaining simplicity for connector developers and users.