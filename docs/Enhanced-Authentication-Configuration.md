# Enhanced Channel Schema Authentication Configuration

This document demonstrates the new enhanced authentication configuration system for Channel Schemas, which allows precise mapping of connection settings fields to authentication requirements.

## Overview

The new authentication configuration system provides:

- **Explicit Field Mapping**: Specify exactly which connection settings parameters are required for each authentication method
- **Flexible Authentication**: Support multiple alternative field names for the same logical credential
- **Provider-Specific Authentication**: Define custom authentication patterns that match provider APIs
- **Backward Compatibility**: Legacy `AuthenticationType` enum-based validation still works

## Key Classes

### AuthenticationConfiguration
Defines authentication requirements with explicit field mappings:

```csharp
var twilioAuth = new AuthenticationConfiguration(AuthenticationType.Basic, "Twilio Authentication")
    .WithRequiredField("AccountSid", DataType.String, field =>
    {
        field.DisplayName = "Account SID";
        field.Description = "Twilio Account SID (acts as username)";
        field.AuthenticationRole = "Username";
    })
    .WithRequiredField("AuthToken", DataType.String, field =>
    {
        field.DisplayName = "Auth Token";
        field.Description = "Twilio Auth Token (acts as password)";
        field.AuthenticationRole = "Password";
        field.IsSensitive = true;
    });
```

### AuthenticationField
Represents a single authentication field with validation:

```csharp
var apiKeyField = new AuthenticationField("ApiKey", DataType.String)
{
    DisplayName = "API Key",
    Description = "The API key for authentication",
    AuthenticationRole = "ApiKey",
    IsSensitive = true,
    AllowedValues = null // Any value allowed
};
```

### FlexibleAuthenticationConfiguration
Supports "any one of" validation for multiple alternative field names:

```csharp
var flexibleApiKey = new FlexibleAuthenticationConfiguration(AuthenticationType.ApiKey, "Flexible API Key")
    .WithOptionalField("ApiKey", DataType.String)
    .WithOptionalField("Key", DataType.String)
    .WithOptionalField("AccessKey", DataType.String);
// Only one of these fields needs to be present
```

## Usage Examples

### 1. Twilio SMS Channel with Custom Authentication

```csharp
var twilioSmsSchema = new ChannelSchema("Twilio", "SMS", "1.0.0")
    .WithDisplayName("Twilio SMS Connector")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    
    // Use predefined Twilio authentication configuration
    .AddAuthenticationConfiguration(AuthenticationConfigurations.TwilioBasicAuthentication())
    
    // Define required parameters
    .AddRequiredParameter("AccountSid", DataType.String)
    .AddRequiredParameter("AuthToken", DataType.String, true) // sensitive
    .AddParameter("FromNumber", DataType.String, param => param.IsRequired = true)
    
    .AddContentType(MessageContentType.PlainText)
    .HandlesMessageEndpoint(EndpointType.PhoneNumber);

// Validation will now check for AccountSid/AuthToken specifically, not generic username/password
var connectionSettings = new ConnectionSettings()
    .SetParameter("AccountSid", "AC123456789")
    .SetParameter("AuthToken", "token123")
    .SetParameter("FromNumber", "+1234567890");

var validationResults = twilioSmsSchema.ValidateConnectionSettings(connectionSettings);
// Returns empty - validation passes
```

### 2. Multi-Authentication Provider

```csharp
var flexibleApiSchema = new ChannelSchema("FlexibleAPI", "API", "1.0.0")
    .WithDisplayName("Flexible API Connector")
    
    // Support multiple authentication methods
    .AddAuthenticationConfiguration(AuthenticationConfigurations.BasicAuthentication())
    .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleApiKeyAuthentication("ApiKey", "Key", "AccessKey"))
    .AddAuthenticationConfiguration(AuthenticationConfigurations.TokenAuthentication("BearerToken"))
    
    .AddParameter("BaseUrl", DataType.String, param => param.IsRequired = true);

// Any of these connection settings would pass validation:

// Option 1: Basic Authentication
var basicAuth = new ConnectionSettings()
    .SetParameter("Username", "user123")
    .SetParameter("Password", "pass456")
    .SetParameter("BaseUrl", "https://api.example.com");

// Option 2: API Key Authentication (any of the key names)
var apiKeyAuth = new ConnectionSettings()
    .SetParameter("AccessKey", "ak_123456789")
    .SetParameter("BaseUrl", "https://api.example.com");

// Option 3: Token Authentication
var tokenAuth = new ConnectionSettings()
    .SetParameter("BearerToken", "eyJhbGciOiJIUzI1...")
    .SetParameter("BaseUrl", "https://api.example.com");
```

### 3. Custom Multi-Tenant Authentication

```csharp
// Define custom authentication fields
var tenantIdField = new AuthenticationField("TenantId", DataType.String)
{
    DisplayName = "Tenant ID",
    Description = "The tenant identifier for multi-tenant authentication",
    AuthenticationRole = "TenantId"
};

var apiSecretField = new AuthenticationField("ApiSecret", DataType.String)
{
    DisplayName = "API Secret",
    Description = "The secret key for the tenant",
    AuthenticationRole = "Secret",
    IsSensitive = true
};

var regionField = new AuthenticationField("Region", DataType.String)
{
    DisplayName = "Region",
    Description = "The deployment region (optional)",
    AuthenticationRole = "Region",
    AllowedValues = new object[] { "us-east-1", "us-west-2", "eu-west-1" }
};

// Create custom authentication configuration
var multiTenantAuth = AuthenticationConfigurations.CustomAuthentication(
    "Multi-Tenant Authentication",
    requiredFields: new[] { tenantIdField, apiSecretField },
    optionalFields: new[] { regionField }
);

var multiTenantSchema = new ChannelSchema("MultiTenant", "API", "1.0.0")
    .AddAuthenticationConfiguration(multiTenantAuth);

// Validation requires TenantId and ApiSecret, allows Region
var settings = new ConnectionSettings()
    .SetParameter("TenantId", "tenant123")
    .SetParameter("ApiSecret", "secret456")
    .SetParameter("Region", "us-east-1"); // Optional but validated if present

var results = multiTenantSchema.ValidateConnectionSettings(settings);
// Passes validation
```

### 4. Advanced Certificate Authentication

```csharp
var certificateSchema = new ChannelSchema("SecureAPI", "API", "1.0.0")
    .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleCertificateAuthentication());

// Multiple ways to provide certificate authentication:

// Option 1: Certificate thumbprint
var thumbprintAuth = new ConnectionSettings()
    .SetParameter("CertificateThumbprint", "1234567890ABCDEF");

// Option 2: PFX file with password
var pfxAuth = new ConnectionSettings()
    .SetParameter("PfxFile", "/path/to/cert.pfx")
    .SetParameter("PfxPassword", "password123");

// Option 3: Certificate data
var certDataAuth = new ConnectionSettings()
    .SetParameter("Certificate", "-----BEGIN CERTIFICATE-----...");

// All would pass validation because FlexibleCertificateAuthentication 
// accepts any one of the certificate-related fields
```

## Migration from Legacy Authentication

### Before (Legacy Approach)
```csharp
var oldSchema = new ChannelSchema("Provider", "API", "1.0.0")
    .AddAuthenticationType(AuthenticationType.Basic); // Generic validation

// Validation was based on hardcoded field name assumptions
// - For Basic: looked for Username/Password OR AccountSid/AuthToken OR various other combinations
// - Not provider-specific
// - No control over which fields are actually required
```

### After (New Approach)
```csharp
var newSchema = new ChannelSchema("Provider", "API", "1.0.0")
    .AddAuthenticationConfiguration(AuthenticationConfigurations.CustomBasicAuthentication(
        "ApiUsername", "ApiPassword", "Custom Basic Authentication"));

// Validation is now explicit and provider-specific
// - Exactly specifies which fields are required
// - Provides clear error messages
// - Supports custom field names
```

## Backward Compatibility

The new system maintains full backward compatibility:

```csharp
// This still works exactly as before
var legacySchema = new ChannelSchema("Provider", "API", "1.0.0")
    .AddAuthenticationType(AuthenticationType.Basic);

// Validation falls back to legacy hardcoded field name checking
var legacySettings = new ConnectionSettings()
    .SetParameter("Username", "user")
    .SetParameter("Password", "pass");

var results = legacySchema.ValidateConnectionSettings(legacySettings);
// Still passes validation using legacy logic
```

## Benefits

1. **Provider Specificity**: Define exactly which fields your provider requires
2. **Clear Error Messages**: Authentication errors specify the exact required fields
3. **Flexible Validation**: Support multiple alternative field names for the same credential
4. **Documentation**: Field descriptions and roles provide clear guidance
5. **Type Safety**: Field validation includes data type checking
6. **Security Awareness**: Mark sensitive fields appropriately
7. **Extensibility**: Easy to add new authentication patterns

## Predefined Authentication Configurations

The `AuthenticationConfigurations` static class provides these factory methods:

- `BasicAuthentication()` - Standard username/password
- `TwilioBasicAuthentication()` - AccountSid/AuthToken
- `CustomBasicAuthentication(username, password)` - Custom field names
- `ApiKeyAuthentication(keyField)` - Single API key field
- `FlexibleApiKeyAuthentication(possibleFields...)` - Multiple alternative key fields
- `TokenAuthentication(tokenField)` - Single token field  
- `FlexibleTokenAuthentication(possibleFields...)` - Multiple alternative token fields
- `ClientCredentialsAuthentication()` - ClientId/ClientSecret
- `CertificateAuthentication()` - Single certificate field
- `FlexibleCertificateAuthentication()` - Multiple certificate options
- `CustomAuthentication()` - Fully custom field definitions

This enhancement provides the precision and flexibility needed to accurately represent real-world authentication requirements while maintaining the simplicity of the existing API.