//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Tests for authentication validation functionality in the <see cref="ChannelSchema"/> class.
/// </summary>
public class ChannelSchemaAuthenticationValidationTests
{
	[Fact]
	public void ValidateConnectionSettings_NoAuthenticationTypes_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var connectionSettings = new ConnectionSettings();

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_NoneAuthenticationType_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddAuthenticationType(AuthenticationType.None);
		var connectionSettings = new ConnectionSettings();

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_BasicAuth_TwilioStyle_ValidCredentials_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true });

		var connectionSettings = new ConnectionSettings()
			.SetParameter("AccountSid", "AC123456789")
			.SetParameter("AuthToken", "auth_token_123");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_BasicAuth_StandardStyle_ValidCredentials_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("SMTP", "Email", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("Password", ParameterType.String) { IsRequired = true, IsSensitive = true });

		var connectionSettings = new ConnectionSettings()
			.SetParameter("Username", "user@example.com")
			.SetParameter("Password", "password123");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_BasicAuth_MissingCredentials_FailsValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic);

		var connectionSettings = new ConnectionSettings();
		// Don't add any parameters to avoid unknown parameter validation issues

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Basic authentication requires", results[0].ErrorMessage);
		Assert.Contains("Authentication", results[0].MemberNames);
	}

	[Fact]
	public void ValidateConnectionSettings_ApiKeyAuth_ValidKey_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.ApiKey)
			.AddParameter(new ChannelParameter("ApiKey", ParameterType.String) { IsRequired = true, IsSensitive = true });

		var connectionSettings = new ConnectionSettings()
			.SetParameter("ApiKey", "api_key_12345");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_ApiKeyAuth_AlternativeKeyNames_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.ApiKey);

		// Test different key parameter names
		var testCases = new[]
		{
			new { ParamName = "Key", Value = "key_123" },
			new { ParamName = "AccessKey", Value = "access_key_456" }
		};

		foreach (var testCase in testCases)
		{
			var connectionSettings = new ConnectionSettings()
				.SetParameter(testCase.ParamName, testCase.Value);

			// Act
			var results = schema.ValidateConnectionSettings(connectionSettings);

			// Assert
			Assert.Empty(results);
		}
	}

	[Fact]
	public void ValidateConnectionSettings_ApiKeyAuth_MissingKey_FailsValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.ApiKey);

		var connectionSettings = new ConnectionSettings();

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("API Key authentication requires", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_TokenAuth_ValidToken_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("OAuth", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Token)
			.AddParameter(new ChannelParameter("AccessToken", ParameterType.String) { IsRequired = true, IsSensitive = true });

		var connectionSettings = new ConnectionSettings()
			.SetParameter("AccessToken", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_TokenAuth_AlternativeTokenNames_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("OAuth", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Token);

		var testCases = new[]
		{
			new { ParamName = "Token", Value = "token_123" },
			new { ParamName = "BearerToken", Value = "bearer_456" },
			new { ParamName = "AuthToken", Value = "auth_789" }
		};

		foreach (var testCase in testCases)
		{
			var connectionSettings = new ConnectionSettings()
				.SetParameter(testCase.ParamName, testCase.Value);

			// Act
			var results = schema.ValidateConnectionSettings(connectionSettings);

			// Assert
			Assert.Empty(results);
		}
	}

	[Fact]
	public void ValidateConnectionSettings_ClientCredentialsAuth_ValidCredentials_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("OAuth2", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.ClientCredentials)
			.AddParameter(new ChannelParameter("ClientId", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("ClientSecret", ParameterType.String) { IsRequired = true, IsSensitive = true });

		var connectionSettings = new ConnectionSettings()
			.SetParameter("ClientId", "client_12345")
			.SetParameter("ClientSecret", "secret_67890");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_ClientCredentialsAuth_MissingCredentials_FailsValidation()
	{
		// Arrange
		var schema = new ChannelSchema("OAuth2", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.ClientCredentials);

		// Test missing ClientId
		var connectionSettingsMissingId = new ConnectionSettings()
			.SetParameter("ClientSecret", "secret_67890");

		// Test missing ClientSecret
		var connectionSettingsMissingSecret = new ConnectionSettings()
			.SetParameter("ClientId", "client_12345");

		// Act & Assert - Missing ClientId
		var resultsMissingId = schema.ValidateConnectionSettings(connectionSettingsMissingId).ToList();
		Assert.Single(resultsMissingId);
		Assert.Contains("Client Credentials authentication requires both ClientId and ClientSecret", resultsMissingId[0].ErrorMessage);

		// Act & Assert - Missing ClientSecret
		var resultsMissingSecret = schema.ValidateConnectionSettings(connectionSettingsMissingSecret).ToList();
		Assert.Single(resultsMissingSecret);
		Assert.Contains("Client Credentials authentication requires both ClientId and ClientSecret", resultsMissingSecret[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_CertificateAuth_ValidCertificate_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Secure", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Certificate);

		var testCases = new[]
		{
			new { ParamName = "Certificate", Value = "cert_data_here" },
			new { ParamName = "CertificatePath", Value = "/path/to/cert.pem" },
			new { ParamName = "CertificateThumbprint", Value = "1234567890ABCDEF" },
			new { ParamName = "PfxFile", Value = "/path/to/cert.pfx" }
		};

		foreach (var testCase in testCases)
		{
			var connectionSettings = new ConnectionSettings()
				.SetParameter(testCase.ParamName, testCase.Value);

			// Act
			var results = schema.ValidateConnectionSettings(connectionSettings);

			// Assert
			Assert.Empty(results);
		}
	}

	[Fact]
	public void ValidateConnectionSettings_CertificateAuth_FileBasedWithPassword_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Secure", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Certificate);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("PfxFile", "/path/to/cert.pfx")
			.SetParameter("PfxPassword", "password123");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_CustomAuth_ValidCustomParameters_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Custom", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Custom);

		var testCases = new[]
		{
			new { ParamName = "CustomAuth", Value = "custom_auth_data" },
			new { ParamName = "AuthenticationData", Value = "auth_data" },
			new { ParamName = "Credentials", Value = "creds" },
			new { ParamName = "SecretKey", Value = "secret" }
		};

		foreach (var testCase in testCases)
		{
			var connectionSettings = new ConnectionSettings()
				.SetParameter(testCase.ParamName, testCase.Value);

			// Act
			var results = schema.ValidateConnectionSettings(connectionSettings);

			// Assert
			Assert.Empty(results);
		}
	}

	[Fact]
	public void ValidateConnectionSettings_MultipleAuthTypes_OneValid_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Flexible", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddAuthenticationType(AuthenticationType.ApiKey)
			.AddAuthenticationType(AuthenticationType.Token);

		// Provide only API Key authentication
		var connectionSettings = new ConnectionSettings()
			.SetParameter("ApiKey", "api_key_12345");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results); // Should pass because API Key auth is satisfied
	}

	[Fact]
	public void ValidateConnectionSettings_MultipleAuthTypes_NoneValid_FailsValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Flexible", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddAuthenticationType(AuthenticationType.ApiKey)
			.AddAuthenticationType(AuthenticationType.Token);

		// Provide no authentication parameters to avoid unknown parameter validation
		var connectionSettings = new ConnectionSettings();

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Connection settings do not satisfy any of the supported authentication types", results[0].ErrorMessage);
		Assert.Contains("Basic, ApiKey, Token", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_MultipleAuthTypesWithNone_IncompleteAuth_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Optional", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.None)
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddAuthenticationType(AuthenticationType.ApiKey)
			.AddParameter(new ChannelParameter("SomeOtherParam", ParameterType.String)); // Define the parameter to avoid unknown parameter error

		// Provide no authentication parameters
		var connectionSettings = new ConnectionSettings()
			.SetParameter("SomeOtherParam", "value");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results); // Should pass because None authentication is supported
	}

	[Fact]
	public void ValidateConnectionSettings_TwilioLikeProvider_RealisticScenario()
	{
		// Arrange - Simulate a Twilio-like provider schema
		var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddParameter(new ChannelParameter("AccountSid", ParameterType.String) 
			{ 
				IsRequired = true,
				Description = "Twilio Account SID"
			})
			.AddParameter(new ChannelParameter("AuthToken", ParameterType.String) 
			{ 
				IsRequired = true,
				IsSensitive = true,
				Description = "Twilio Auth Token"
			})
			.AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
			{
				IsRequired = true,
				Description = "Sender phone number"
			});

		// Valid Twilio-style configuration
		var validSettings = new ConnectionSettings()
			.SetParameter("AccountSid", "AC123456789abcdef123456789abcdef12")
			.SetParameter("AuthToken", "your_auth_token_here")
			.SetParameter("FromNumber", "+1234567890");

		// Invalid configuration - missing AuthToken
		var invalidSettings = new ConnectionSettings()
			.SetParameter("AccountSid", "AC123456789abcdef123456789abcdef12")
			.SetParameter("FromNumber", "+1234567890");

		// Act
		var validResults = schema.ValidateConnectionSettings(validSettings);
		var invalidResults = schema.ValidateConnectionSettings(invalidSettings).ToList();

		// Assert
		Assert.Empty(validResults);
		Assert.Equal(2, invalidResults.Count); // Missing required parameter + authentication failure
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Required parameter 'AuthToken'"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Basic authentication requires"));
	}

	[Fact]
	public void ValidateConnectionSettings_EmailProvider_RealisticScenario()
	{
		// Arrange - Simulate an SMTP email provider schema
		var schema = new ChannelSchema("SMTP", "Email", "1.0.0")
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
			.AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("Password", ParameterType.String) 
			{ 
				IsRequired = true,
				IsSensitive = true
			})
			.AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean) { DefaultValue = true });

		// Valid SMTP configuration
		var validSettings = new ConnectionSettings()
			.SetParameter("Host", "smtp.gmail.com")
			.SetParameter("Port", 587)
			.SetParameter("Username", "user@gmail.com")
			.SetParameter("Password", "app_password_here")
			.SetParameter("EnableSsl", true);

		// Invalid configuration - missing password
		var invalidSettings = new ConnectionSettings()
			.SetParameter("Host", "smtp.gmail.com")
			.SetParameter("Username", "user@gmail.com");

		// Act
		var validResults = schema.ValidateConnectionSettings(validSettings);
		var invalidResults = schema.ValidateConnectionSettings(invalidSettings).ToList();

		// Assert
		Assert.Empty(validResults);
		Assert.Equal(2, invalidResults.Count); // Missing required parameter + authentication failure
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Required parameter 'Password'"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Basic authentication requires"));
	}

	[Fact]
	public void ValidateConnectionSettings_OAuthProvider_RealisticScenario()
	{
		// Arrange - Simulate an OAuth-based API provider schema
		var schema = new ChannelSchema("Google", "API", "1.0.0")
			.AddAuthenticationType(AuthenticationType.ClientCredentials)
			.AddAuthenticationType(AuthenticationType.Token)
			.AddParameter(new ChannelParameter("ClientId", ParameterType.String))
			.AddParameter(new ChannelParameter("ClientSecret", ParameterType.String) { IsSensitive = true })
			.AddParameter(new ChannelParameter("AccessToken", ParameterType.String) { IsSensitive = true })
			.AddParameter(new ChannelParameter("BaseUrl", ParameterType.String) { DefaultValue = "https://api.google.com" });

		// Valid with Client Credentials
		var clientCredentialsSettings = new ConnectionSettings()
			.SetParameter("ClientId", "client_id_123")
			.SetParameter("ClientSecret", "client_secret_456")
			.SetParameter("BaseUrl", "https://api.google.com");

		// Valid with Access Token
		var tokenSettings = new ConnectionSettings()
			.SetParameter("AccessToken", "ya29.access_token_here")
			.SetParameter("BaseUrl", "https://api.google.com");

		// Invalid - neither authentication method satisfied
		var invalidSettings = new ConnectionSettings()
			.SetParameter("BaseUrl", "https://api.google.com");

		// Act
		var clientCredentialsResults = schema.ValidateConnectionSettings(clientCredentialsSettings);
		var tokenResults = schema.ValidateConnectionSettings(tokenSettings);
		var invalidResults = schema.ValidateConnectionSettings(invalidSettings).ToList();

		// Assert
		Assert.Empty(clientCredentialsResults);
		Assert.Empty(tokenResults);
		Assert.Single(invalidResults);
		Assert.Contains("Connection settings do not satisfy any of the supported authentication types", invalidResults[0].ErrorMessage);
	}
}