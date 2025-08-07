//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides pre-defined authentication configurations for common authentication patterns.
	/// </summary>
	/// <remarks>
	/// This static class offers factory methods to create authentication configurations 
	/// for standard authentication types, while also allowing for custom field mappings 
	/// to accommodate provider-specific authentication requirements.
	/// </remarks>
	public static class AuthenticationConfigurations
	{
		/// <summary>
		/// Creates a standard Basic authentication configuration requiring username and password.
		/// </summary>
		/// <returns>An authentication configuration for standard Basic authentication.</returns>
		public static AuthenticationConfiguration BasicAuthentication()
		{
			return new AuthenticationConfiguration(AuthenticationType.Basic, "Basic Authentication")
				.WithRequiredField("Username", DataType.String, field =>
				{
					field.DisplayName = "Username";
					field.Description = "The username for authentication";
					field.AuthenticationRole = "Username";
				})
				.WithRequiredField("Password", DataType.String, field =>
				{
					field.DisplayName = "Password";
					field.Description = "The password for authentication";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a Twilio-style Basic authentication configuration using AccountSid and AuthToken.
		/// </summary>
		/// <returns>An authentication configuration for Twilio-style Basic authentication.</returns>
		public static AuthenticationConfiguration TwilioBasicAuthentication()
		{
			return new AuthenticationConfiguration(AuthenticationType.Basic, "Twilio Basic Authentication")
				.WithRequiredField("AccountSid", DataType.String, field =>
				{
					field.DisplayName = "Account SID";
					field.Description = "The Twilio Account SID (acts as username)";
					field.AuthenticationRole = "Username";
				})
				.WithRequiredField("AuthToken", DataType.String, field =>
				{
					field.DisplayName = "Auth Token";
					field.Description = "The Twilio Auth Token (acts as password)";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a custom Basic authentication configuration with specified field names.
		/// </summary>
		/// <param name="usernameField">The name of the username field in connection settings.</param>
		/// <param name="passwordField">The name of the password field in connection settings.</param>
		/// <param name="displayName">Optional display name for this authentication method.</param>
		/// <returns>An authentication configuration for custom Basic authentication.</returns>
		public static AuthenticationConfiguration CustomBasicAuthentication(string usernameField, string passwordField, string? displayName = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(usernameField, nameof(usernameField));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(passwordField, nameof(passwordField));

			return new AuthenticationConfiguration(AuthenticationType.Basic, displayName ?? "Custom Basic Authentication")
				.WithRequiredField(usernameField, DataType.String, field =>
				{
					field.DisplayName = usernameField;
					field.Description = $"The {usernameField} for authentication";
					field.AuthenticationRole = "Username";
				})
				.WithRequiredField(passwordField, DataType.String, field =>
				{
					field.DisplayName = passwordField;
					field.Description = $"The {passwordField} for authentication";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a flexible Basic authentication configuration that accepts multiple possible field name combinations.
		/// </summary>
		/// <returns>An authentication configuration for flexible Basic authentication.</returns>
		public static AuthenticationConfiguration FlexibleBasicAuthentication()
		{
			var config = new FlexibleAuthenticationConfiguration(AuthenticationType.Basic, "Flexible Basic Authentication");

			// Add all possible field combinations as alternatives (only one pair needs to be present)
			config.WithOptionalField("Username", DataType.String, field =>
				{
					field.DisplayName = "Username";
					field.Description = "Standard username for authentication";
					field.AuthenticationRole = "Username";
				})
				.WithOptionalField("Password", DataType.String, field =>
				{
					field.DisplayName = "Password";
					field.Description = "Standard password for authentication";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				})
				.WithOptionalField("AccountSid", DataType.String, field =>
				{
					field.DisplayName = "Account SID";
					field.Description = "Account SID for authentication (Twilio-style)";
					field.AuthenticationRole = "Username";
				})
				.WithOptionalField("AuthToken", DataType.String, field =>
				{
					field.DisplayName = "Auth Token";
					field.Description = "Auth Token for authentication (Twilio-style)";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				})
				.WithOptionalField("User", DataType.String, field =>
				{
					field.DisplayName = "User";
					field.Description = "Alternative username field";
					field.AuthenticationRole = "Username";
				})
				.WithOptionalField("Pass", DataType.String, field =>
				{
					field.DisplayName = "Pass";
					field.Description = "Alternative password field";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				})
				.WithOptionalField("ClientId", DataType.String, field =>
				{
					field.DisplayName = "Client ID";
					field.Description = "Client ID for authentication";
					field.AuthenticationRole = "Username";
				})
				.WithOptionalField("ClientSecret", DataType.String, field =>
				{
					field.DisplayName = "Client Secret";
					field.Description = "Client Secret for authentication";
					field.AuthenticationRole = "Password";
					field.IsSensitive = true;
				});

			return config;
		}

		/// <summary>
		/// Creates a standard API Key authentication configuration.
		/// </summary>
		/// <param name="keyFieldName">The name of the API key field in connection settings. Defaults to "ApiKey".</param>
		/// <returns>An authentication configuration for API Key authentication.</returns>
		public static AuthenticationConfiguration ApiKeyAuthentication(string keyFieldName = "ApiKey")
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(keyFieldName, nameof(keyFieldName));

			return new AuthenticationConfiguration(AuthenticationType.ApiKey, "API Key Authentication")
				.WithRequiredField(keyFieldName, DataType.String, field =>
				{
					field.DisplayName = "API Key";
					field.Description = "The API key for authentication";
					field.AuthenticationRole = "ApiKey";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a flexible API Key authentication configuration that accepts multiple possible field names.
		/// </summary>
		/// <param name="possibleFieldNames">The possible field names for the API key.</param>
		/// <returns>An authentication configuration for flexible API Key authentication.</returns>
		public static AuthenticationConfiguration FlexibleApiKeyAuthentication(params string[] possibleFieldNames)
		{
			if (possibleFieldNames == null || possibleFieldNames.Length == 0)
			{
				possibleFieldNames = new[] { "ApiKey", "Key", "AccessKey" };
			}

			var config = new FlexibleAuthenticationConfiguration(AuthenticationType.ApiKey, "Flexible API Key Authentication");

			// Add all possible field names as alternatives (only one needs to be present)
			foreach (var fieldName in possibleFieldNames)
			{
				config.WithOptionalField(fieldName, DataType.String, field =>
				{
					field.DisplayName = fieldName;
					field.Description = $"The {fieldName} for authentication";
					field.AuthenticationRole = "ApiKey";
					field.IsSensitive = true;
				});
			}

			return config;
		}

		/// <summary>
		/// Creates a standard Token authentication configuration.
		/// </summary>
		/// <param name="tokenFieldName">The name of the token field in connection settings. Defaults to "Token".</param>
		/// <returns>An authentication configuration for Token authentication.</returns>
		public static AuthenticationConfiguration TokenAuthentication(string tokenFieldName = "Token")
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(tokenFieldName, nameof(tokenFieldName));

			return new AuthenticationConfiguration(AuthenticationType.Token, "Token Authentication")
				.WithRequiredField(tokenFieldName, DataType.String, field =>
				{
					field.DisplayName = "Token";
					field.Description = "The authentication token";
					field.AuthenticationRole = "Token";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a flexible Token authentication configuration that accepts multiple possible field names.
		/// </summary>
		/// <param name="possibleFieldNames">The possible field names for the token.</param>
		/// <returns>An authentication configuration for flexible Token authentication.</returns>
		public static AuthenticationConfiguration FlexibleTokenAuthentication(params string[] possibleFieldNames)
		{
			if (possibleFieldNames == null || possibleFieldNames.Length == 0)
			{
				possibleFieldNames = new[] { "Token", "AccessToken", "BearerToken", "AuthToken" };
			}

			var config = new FlexibleAuthenticationConfiguration(AuthenticationType.Token, "Flexible Token Authentication");

			// Add all possible field names as alternatives (only one needs to be present)
			foreach (var fieldName in possibleFieldNames)
			{
				config.WithOptionalField(fieldName, DataType.String, field =>
				{
					field.DisplayName = fieldName;
					field.Description = $"The {fieldName} for authentication";
					field.AuthenticationRole = "Token";
					field.IsSensitive = true;
				});
			}

			return config;
		}

		/// <summary>
		/// Creates a Client Credentials authentication configuration.
		/// </summary>
		/// <param name="clientIdField">The name of the client ID field in connection settings. Defaults to "ClientId".</param>
		/// <param name="clientSecretField">The name of the client secret field in connection settings. Defaults to "ClientSecret".</param>
		/// <returns>An authentication configuration for Client Credentials authentication.</returns>
		public static AuthenticationConfiguration ClientCredentialsAuthentication(string clientIdField = "ClientId", string clientSecretField = "ClientSecret")
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(clientIdField, nameof(clientIdField));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(clientSecretField, nameof(clientSecretField));

			return new AuthenticationConfiguration(AuthenticationType.ClientCredentials, "Client Credentials Authentication")
				.WithRequiredField(clientIdField, DataType.String, field =>
				{
					field.DisplayName = "Client ID";
					field.Description = "The client identifier for OAuth authentication";
					field.AuthenticationRole = "ClientId";
				})
				.WithRequiredField(clientSecretField, DataType.String, field =>
				{
					field.DisplayName = "Client Secret";
					field.Description = "The client secret for OAuth authentication";
					field.AuthenticationRole = "ClientSecret";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a Certificate authentication configuration.
		/// </summary>
		/// <param name="certificateFieldName">The name of the certificate field in connection settings. Defaults to "Certificate".</param>
		/// <returns>An authentication configuration for Certificate authentication.</returns>
		public static AuthenticationConfiguration CertificateAuthentication(string certificateFieldName = "Certificate")
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(certificateFieldName, nameof(certificateFieldName));

			return new AuthenticationConfiguration(AuthenticationType.Certificate, "Certificate Authentication")
				.WithRequiredField(certificateFieldName, DataType.String, field =>
				{
					field.DisplayName = "Certificate";
					field.Description = "The certificate data or path for authentication";
					field.AuthenticationRole = "Certificate";
					field.IsSensitive = true;
				})
				.WithOptionalField("CertificatePassword", DataType.String, field =>
				{
					field.DisplayName = "Certificate Password";
					field.Description = "The password for the certificate (if required)";
					field.AuthenticationRole = "CertificatePassword";
					field.IsSensitive = true;
				});
		}

		/// <summary>
		/// Creates a flexible Certificate authentication configuration that accepts multiple possible field combinations.
		/// </summary>
		/// <returns>An authentication configuration for flexible Certificate authentication.</returns>
		public static AuthenticationConfiguration FlexibleCertificateAuthentication()
		{
			var config = new FlexibleAuthenticationConfiguration(AuthenticationType.Certificate, "Flexible Certificate Authentication");
			
			config.WithOptionalField("Certificate", DataType.String, field =>
				{
					field.DisplayName = "Certificate Data";
					field.Description = "The certificate data for authentication";
					field.AuthenticationRole = "Certificate";
					field.IsSensitive = true;
				})
				.WithOptionalField("CertificatePath", DataType.String, field =>
				{
					field.DisplayName = "Certificate Path";
					field.Description = "The path to the certificate file";
					field.AuthenticationRole = "CertificatePath";
				})
				.WithOptionalField("CertificateThumbprint", DataType.String, field =>
				{
					field.DisplayName = "Certificate Thumbprint";
					field.Description = "The thumbprint of the certificate";
					field.AuthenticationRole = "CertificateThumbprint";
				})
				.WithOptionalField("PfxFile", DataType.String, field =>
				{
					field.DisplayName = "PFX File";
					field.Description = "The path to the PFX certificate file";
					field.AuthenticationRole = "PfxFile";
				})
				.WithOptionalField("PfxPassword", DataType.String, field =>
				{
					field.DisplayName = "PFX Password";
					field.Description = "The password for the PFX file";
					field.AuthenticationRole = "PfxPassword";
					field.IsSensitive = true;
				})
				.WithOptionalField("CertificatePassword", DataType.String, field =>
				{
					field.DisplayName = "Certificate Password";
					field.Description = "The password for the certificate";
					field.AuthenticationRole = "CertificatePassword";
					field.IsSensitive = true;
				});

			return config;
		}

		/// <summary>
		/// Creates a custom authentication configuration for provider-specific authentication methods.
		/// </summary>
		/// <param name="displayName">The display name for this authentication method.</param>
		/// <param name="requiredFields">The required authentication fields.</param>
		/// <param name="optionalFields">The optional authentication fields.</param>
		/// <returns>An authentication configuration for custom authentication.</returns>
		public static AuthenticationConfiguration CustomAuthentication(string displayName, 
			IEnumerable<AuthenticationField>? requiredFields = null, 
			IEnumerable<AuthenticationField>? optionalFields = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

			var config = new AuthenticationConfiguration(AuthenticationType.Custom, displayName);

			if (requiredFields != null)
			{
				foreach (var field in requiredFields)
				{
					config.WithRequiredField(field);
				}
			}

			if (optionalFields != null)
			{
				foreach (var field in optionalFields)
				{
					config.WithOptionalField(field);
				}
			}

			return config;
		}
	}
}