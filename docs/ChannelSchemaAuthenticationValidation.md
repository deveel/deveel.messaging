# Channel Schema Authentication Validation

This document describes the authentication-specific validation functionality that has been implemented for the `ChannelSchema` class to ensure that connection settings include the required parameters for each authentication type.

## Overview

Each channel schema can now specify one or more authentication types, and the validation system will ensure that the connection settings provide the necessary authentication parameters based on the supported authentication methods.

## Authentication Types Supported

The system supports the following authentication types defined in the `AuthenticationType` enum:

- **None**: No authentication required
- **Basic**: Username/password or similar credential pairs
- **ApiKey**: API key-based authentication
- **Token**: Bearer tokens, access tokens, etc.
- **ClientCredentials**: OAuth2 client credentials flow
- **Certificate**: X.509 certificates and related authentication
- **Custom**: Custom authentication schemes

## Authentication Parameter Validation

### Basic Authentication
Validates for one of the following parameter combinations:
- `Username` + `Password`
- `AccountSid` + `AuthToken` (Twilio-style)
- `User` + `Pass`
- `ClientId` + `ClientSecret`

### API Key Authentication
Validates for one of:
- `ApiKey`
- `Key`
- `AccessKey`

### Token Authentication
Validates for one of:
- `Token`
- `AccessToken`
- `BearerToken`
- `AuthToken`

### Client Credentials Authentication
Requires both:
- `ClientId`
- `ClientSecret`

### Certificate Authentication
Validates for one of:
- `Certificate`
- `CertificatePath`
- `CertificateThumbprint`
- `PfxFile`

Optional password parameters:
- `CertificatePassword`
- `PfxPassword`

### Custom Authentication
Validates for at least one of:
- `CustomAuth`
- `AuthenticationData`
- `Credentials`
- `AuthConfig`
- `SecretKey`
- `PrivateKey`
- `Signature`
- `Hash`

## Integration with Strict Mode

The authentication validation is intelligently integrated with the schema's strict mode:

- **Strict Mode**: Only validates authentication parameters that correspond to the authentication types defined in the schema
- **Authentication Parameters**: Are not considered "unknown" parameters when their corresponding authentication type is supported
- **Validation Order**: Authentication validation runs alongside regular parameter validation

## Example Usage

### Twilio SMS Channel
```csharp
var twilioSchema = new ChannelSchema("Twilio", "SMS", "1.0.0")
    .AddAuthenticationType(AuthenticationType.Basic)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true });

// Valid connection settings
var validSettings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC123456789")
    .SetParameter("AuthToken", "token123");

var results = twilioSchema.ValidateConnectionSettings(validSettings);
// Results will be empty (no validation errors)
```

### Multi-Authentication Channel
```csharp
var flexibleSchema = new ChannelSchema("API", "Generic", "1.0.0")
    .AddAuthenticationType(AuthenticationType.Basic)
    .AddAuthenticationType(AuthenticationType.ApiKey)
    .AddAuthenticationType(AuthenticationType.Token);

// Using API Key authentication
var apiKeySettings = new ConnectionSettings()
    .SetParameter("ApiKey", "api_key_12345");

var results = flexibleSchema.ValidateConnectionSettings(apiKeySettings);
// Results will be empty (API Key authentication satisfied)
```

### OAuth2 Client Credentials
```csharp
var oauthSchema = new ChannelSchema("OAuth", "API", "1.0.0")
    .AddAuthenticationType(AuthenticationType.ClientCredentials);

var oauthSettings = new ConnectionSettings()
    .SetParameter("ClientId", "client_12345")
    .SetParameter("ClientSecret", "secret_67890");

var results = oauthSchema.ValidateConnectionSettings(oauthSettings);
// Results will be empty (both required parameters provided)
```

## Error Messages

When authentication validation fails, descriptive error messages are provided:

- Lists all supported authentication types
- Explains which parameters are required for each authentication type
- Provides specific guidance on parameter combinations

Example error message:
```
Connection settings do not satisfy any of the supported authentication types. 
Supported types: Basic, ApiKey, Token. 
Validation errors: Basic: Basic authentication requires one of the following parameter pairs: (Username, Password), (AccountSid, AuthToken), (User, Pass), or (ClientId, ClientSecret); ApiKey: API Key authentication requires one of the following parameters: ApiKey, Key, or AccessKey; Token: Token authentication requires one of the following parameters: Token, AccessToken, BearerToken, or AuthToken
```

## Benefits

1. **Type-Safe Authentication**: Ensures channels receive the authentication parameters they expect
2. **Multiple Authentication Support**: Channels can support multiple authentication methods
3. **Provider-Specific Patterns**: Supports provider-specific authentication patterns (e.g., Twilio's AccountSid/AuthToken)
4. **Flexible Validation**: Works with both strict and flexible schema modes
5. **Clear Error Messages**: Provides actionable feedback when authentication is missing or incomplete
6. **Backward Compatibility**: Existing schemas continue to work without modification

## Testing

Comprehensive test coverage has been added in `ChannelSchemaAuthenticationValidationTests.cs` covering:

- All authentication types individually
- Multiple authentication type scenarios
- Integration with strict/flexible mode
- Real-world scenarios (Twilio, SMTP, OAuth)
- Edge cases and error conditions

All existing tests continue to pass, ensuring backward compatibility.