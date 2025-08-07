using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the new authentication configuration functionality in the <see cref="ChannelSchema"/> class.
/// </summary>
public class ChannelSchemaAuthenticationConfigurationTests
{
	[Fact]
	public void ValidateConnectionSettings_TwilioAuthConfiguration_ValidCredentials_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.TwilioBasicAuthentication())
			.AddRequiredParameter("AccountSid", DataType.String)
			.AddRequiredParameter("AuthToken", DataType.String);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("AccountSid", "AC123456789")
			.SetParameter("AuthToken", "auth_token_123");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_CustomBasicAuthConfiguration_ValidCredentials_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("CustomProvider", "API", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.CustomBasicAuthentication("UserId", "SecretKey"))
			.AddRequiredParameter("UserId", DataType.String)
			.AddRequiredParameter("SecretKey", DataType.String);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("UserId", "user123")
			.SetParameter("SecretKey", "secret456");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_FlexibleApiKeyConfiguration_ValidKey_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "API", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleApiKeyAuthentication("ApiKey", "Key", "AccessKey"));

		// Test different key parameter names
		var testCases = new[]
		{
			new { ParamName = "ApiKey", Value = "api_key_123" },
			new { ParamName = "Key", Value = "key_456" },
			new { ParamName = "AccessKey", Value = "access_key_789" }
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
	public void ValidateConnectionSettings_AuthConfiguration_MissingRequiredField_FailsValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.TwilioBasicAuthentication());

		var connectionSettings = new ConnectionSettings()
			.SetParameter("AccountSid", "AC123456789");
			// Missing AuthToken

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Twilio Basic Authentication", results[0].ErrorMessage);
		Assert.Contains("Required authentication field 'AuthToken'", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_MultipleAuthConfigurations_OneValid_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Flexible", "API", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.BasicAuthentication())
			.AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication())
			.AddAuthenticationConfiguration(AuthenticationConfigurations.TokenAuthentication());

		// Provide only API Key authentication
		var connectionSettings = new ConnectionSettings()
			.SetParameter("ApiKey", "api_key_12345");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results); // Should pass because API Key auth is satisfied
	}

	[Fact]
	public void ValidateConnectionSettings_CustomAuthConfiguration_ValidFields_PassesValidation()
	{
		// Arrange
		var requiredFields = new[]
		{
			new AuthenticationField("TenantId", DataType.String) 
			{ 
				DisplayName = "Tenant ID", 
				Description = "The tenant identifier",
				AuthenticationRole = "TenantId"
			},
			new AuthenticationField("ApiSecret", DataType.String) 
			{ 
				DisplayName = "API Secret", 
				Description = "The secret key for the tenant",
				AuthenticationRole = "Secret",
				IsSensitive = true
			}
		};

		var optionalFields = new[]
		{
			new AuthenticationField("Region", DataType.String) 
			{ 
				DisplayName = "Region", 
				Description = "The deployment region",
				AuthenticationRole = "Region"
			}
		};

		var customAuth = AuthenticationConfigurations.CustomAuthentication(
			"Multi-Tenant Authentication", 
			requiredFields, 
			optionalFields);

		var schema = new ChannelSchema("CustomProvider", "API", "1.0.0")
			.AddAuthenticationConfiguration(customAuth);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("TenantId", "tenant123")
			.SetParameter("ApiSecret", "secret456")
			.SetParameter("Region", "us-east-1");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_FlexibleCertificateAuth_ValidCertificateThumbprint_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Secure", "API", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleCertificateAuthentication());

		var connectionSettings = new ConnectionSettings()
			.SetParameter("CertificateThumbprint", "1234567890ABCDEF");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_FlexibleCertificateAuth_ValidPfxFile_PassesValidation()
	{
		// Arrange
		var schema = new ChannelSchema("Secure", "API", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleCertificateAuthentication());

		var connectionSettings = new ConnectionSettings()
			.SetParameter("PfxFile", "/path/to/cert.pfx")
			.SetParameter("PfxPassword", "password123");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void AddAuthenticationConfiguration_DuplicateAuthenticationType_ThrowsException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.BasicAuthentication());

		// Act & Assert
		var exception = Assert.Throws<InvalidOperationException>(() =>
			schema.AddAuthenticationConfiguration(AuthenticationConfigurations.TwilioBasicAuthentication()));

		Assert.Contains("An authentication configuration for 'Basic' authentication type already exists", exception.Message);
	}

	[Fact]
	public void AuthenticationConfiguration_BackwardCompatibility_AuthenticationTypesIncluded()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.BasicAuthentication())
			.AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication());

		// Assert
		Assert.Equal(2, schema.AuthenticationConfigurations.Count);
		Assert.Contains(AuthenticationType.Basic, schema.GetAuthenticationTypes());
		Assert.Contains(AuthenticationType.ApiKey, schema.GetAuthenticationTypes());
		
		Assert.Equal(2, schema.AuthenticationConfigurations.Count);
		Assert.Contains(schema.AuthenticationConfigurations, c => c.AuthenticationType == AuthenticationType.Basic);
		Assert.Contains(schema.AuthenticationConfigurations, c => c.AuthenticationType == AuthenticationType.ApiKey);
	}

	[Fact]
	public void RemoveAuthenticationConfiguration_ExistingConfiguration_RemovesBothConfigurationAndType()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.BasicAuthentication())
			.AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication());

		// Act
		schema.RemoveAuthenticationConfiguration(AuthenticationType.Basic);

		// Assert
		Assert.Single(schema.AuthenticationConfigurations);
		Assert.Single(schema.GetAuthenticationTypes());
		Assert.Contains(AuthenticationType.ApiKey, schema.GetAuthenticationTypes());
		Assert.DoesNotContain(AuthenticationType.Basic, schema.GetAuthenticationTypes());
	}

	[Fact]
	public void RestrictAuthenticationConfigurations_NewConfigurations_ReplacesExisting()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.BasicAuthentication())
			.AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication())
			.AddAuthenticationConfiguration(AuthenticationConfigurations.TokenAuthentication());

		var restrictedConfigs = new[]
		{
			AuthenticationConfigurations.TwilioBasicAuthentication(),
			AuthenticationConfigurations.FlexibleTokenAuthentication()
		};

		// Act
		schema.RestrictAuthenticationConfigurations(restrictedConfigs);

		// Assert
		Assert.Equal(2, schema.AuthenticationConfigurations.Count);
		Assert.Equal(2, schema.GetAuthenticationTypes().Count());
		Assert.Contains(AuthenticationType.Basic, schema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.Token, schema.AuthenticationTypes);
		Assert.DoesNotContain(AuthenticationType.ApiKey, schema.AuthenticationTypes);
		
		// Verify the configurations are the new ones
		var basicConfig = schema.AuthenticationConfigurations.First(c => c.AuthenticationType == AuthenticationType.Basic);
		Assert.Equal("Twilio Basic Authentication", basicConfig.DisplayName);
	}

	[Fact]
	public void ValidateConnectionSettings_RealisticTwilioScenario_WithConfiguration()
	{
		// Arrange - Simulate a Twilio-like provider schema with authentication configuration
		var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.AddAuthenticationConfiguration(AuthenticationConfigurations.TwilioBasicAuthentication())
			.AddParameter("AccountSid", DataType.String, param =>
			{
				param.IsRequired = true;
				param.Description = "Twilio Account SID";
			})
			.AddParameter("AuthToken", DataType.String, param =>
			{
				param.IsRequired = true;
				param.IsSensitive = true;
				param.Description = "Twilio Auth Token";
			})
			.AddParameter("FromNumber", DataType.String, param =>
			{
				param.IsRequired = true;
				param.Description = "Sender phone number";
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
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Twilio Basic Authentication"));
	}
}