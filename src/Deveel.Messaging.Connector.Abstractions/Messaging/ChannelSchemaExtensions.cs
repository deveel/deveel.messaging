//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides extension methods for <see cref="IChannelSchema"/> to support
	/// logical identity operations, compatibility validation, and schema validation.
	/// </summary>
	public static class ChannelSchemaExtensions
	{
		/// <summary>
		/// Gets the logical identity of the schema as a string in the format "Provider/Type/Version".
		/// </summary>
		/// <param name="schema">The schema to get the logical identity for.</param>
		/// <returns>A string representing the logical identity of the schema.</returns>
		/// <exception cref="ArgumentNullException">Thrown when schema is null.</exception>
		public static string GetLogicalIdentity(this IChannelSchema schema)
		{
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));
			return $"{schema.ChannelProvider}/{schema.ChannelType}/{schema.Version}";
		}

		/// <summary>
		/// Determines whether the schema is logically compatible with another schema.
		/// Two schemas are compatible if they have the same ChannelProvider, ChannelType, and Version.
		/// </summary>
		/// <param name="schema">The schema to check compatibility for.</param>
		/// <param name="otherSchema">The schema to compare with.</param>
		/// <returns>True if the schemas are logically compatible; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when either schema is null.</exception>
		public static bool IsCompatibleWith(this IChannelSchema schema, IChannelSchema otherSchema)
		{
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));
			ArgumentNullException.ThrowIfNull(otherSchema, nameof(otherSchema));
			
			return string.Equals(schema.ChannelProvider, otherSchema.ChannelProvider, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(schema.ChannelType, otherSchema.ChannelType, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(schema.Version, otherSchema.Version, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Validates whether the schema can be considered a valid restriction of another schema.
		/// A schema is a valid restriction if it's compatible and all its configurations are 
		/// subsets of the target schema's configurations.
		/// </summary>
		/// <param name="schema">The schema to validate as a restriction.</param>
		/// <param name="targetSchema">The schema to validate against.</param>
		/// <returns>An enumerable of validation results. Empty if the schema is a valid restriction.</returns>
		/// <exception cref="ArgumentNullException">Thrown when either schema is null.</exception>
		public static IEnumerable<ValidationResult> ValidateAsRestrictionOf(this IChannelSchema schema, IChannelSchema targetSchema)
		{
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));
			ArgumentNullException.ThrowIfNull(targetSchema, nameof(targetSchema));
			
			var validationResults = new List<ValidationResult>();

			// First check if schemas are compatible
			if (!schema.IsCompatibleWith(targetSchema))
			{
				validationResults.Add(new ValidationResult(
					$"Schema is not compatible. Expected: {targetSchema.ChannelProvider}/{targetSchema.ChannelType}/{targetSchema.Version}, " +
					$"Actual: {schema.ChannelProvider}/{schema.ChannelType}/{schema.Version}"));
				return validationResults;
			}

			// Validate capabilities are a subset
			if ((schema.Capabilities & targetSchema.Capabilities) != schema.Capabilities)
			{
				validationResults.Add(new ValidationResult(
					$"Schema capabilities ({schema.Capabilities}) are not a subset of target capabilities ({targetSchema.Capabilities})"));
			}

			// Validate parameters are a subset
			foreach (var parameter in schema.Parameters)
			{
				var targetParam = targetSchema.Parameters.FirstOrDefault(p => 
					string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
				
				if (targetParam == null)
				{
					validationResults.Add(new ValidationResult(
						$"Parameter '{parameter.Name}' is not defined in target schema",
						new[] { parameter.Name }));
				}
			}

			// Validate content types are a subset
			foreach (var contentType in schema.ContentTypes)
			{
				if (!targetSchema.ContentTypes.Contains(contentType))
				{
					validationResults.Add(new ValidationResult(
						$"Content type '{contentType}' is not supported by target schema"));
				}
			}

			// Validate authentication types are a subset
			foreach (var authType in schema.AuthenticationTypes)
			{
				if (!targetSchema.AuthenticationTypes.Contains(authType))
				{
					validationResults.Add(new ValidationResult(
						$"Authentication type '{authType}' is not supported by target schema"));
				}
			}

			// Validate endpoints are a subset
			foreach (var endpoint in schema.Endpoints)
			{
				var targetEndpoint = targetSchema.Endpoints.FirstOrDefault(e => e.Type == endpoint.Type);
				
				if (targetEndpoint == null)
				{
					validationResults.Add(new ValidationResult(
						$"Endpoint type '{endpoint.Type}' is not defined in target schema"));
				}
			}

			// Validate message properties are a subset
			foreach (var messageProperty in schema.MessageProperties)
			{
				var targetProperty = targetSchema.MessageProperties.FirstOrDefault(p => 
					string.Equals(p.Name, messageProperty.Name, StringComparison.OrdinalIgnoreCase));
				
				if (targetProperty == null)
				{
					validationResults.Add(new ValidationResult(
						$"Message property '{messageProperty.Name}' is not defined in target schema",
						new[] { messageProperty.Name }));
				}
			}

			return validationResults;
		}

		/// <summary>
		/// Validates the specified connection settings against this channel schema
		/// to ensure compatibility and compliance with the defined requirements.
		/// </summary>
		/// <param name="schema">The schema to validate against.</param>
		/// <param name="connectionSettings">
		/// The connection settings to validate. Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{ValidationResult}"/> containing validation errors.
		/// If the enumerable is empty, the validation was successful.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="schema"/> or <paramref name="connectionSettings"/> is <see langword="null"/>.
		/// </exception>
		public static IEnumerable<ValidationResult> ValidateConnectionSettings(this IChannelSchema schema, ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			var validationResults = new List<ValidationResult>();

			// Validate required parameters
			ValidateRequiredParameters(schema, connectionSettings, validationResults);

			// Validate parameter types and constraints
			ValidateParameterTypesAndConstraints(schema, connectionSettings, validationResults);

			// Validate authentication requirements
			ValidateAuthenticationRequirements(schema, connectionSettings, validationResults);

			// Validate unknown parameters (parameters not defined in schema) - only in strict mode
			if (schema.IsStrict)
			{
				ValidateUnknownParameters(schema, connectionSettings, validationResults);
			}

			return validationResults;
		}

		/// <summary>
		/// Validates a message against this channel schema to ensure compatibility 
		/// and compliance with the defined requirements.
		/// </summary>
		/// <param name="schema">The schema to validate against.</param>
		/// <param name="message">
		/// The message to validate. Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{ValidationResult}"/> containing validation errors.
		/// If the enumerable is empty, the validation was successful.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="schema"/> or <paramref name="message"/> is <see langword="null"/>.
		/// </exception>
		public static IEnumerable<ValidationResult> ValidateMessage(this IChannelSchema schema, IMessage message)
		{
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));
			ArgumentNullException.ThrowIfNull(message, nameof(message));

			var validationResults = new List<ValidationResult>();

			// Validate message ID is present
			if (string.IsNullOrWhiteSpace(message.Id))
			{
				validationResults.Add(new ValidationResult(
					"Message ID is required.",
					new[] { "Id" }));
			}

			// Validate sender endpoint
			if (message.Sender != null)
			{
				ValidateSenderEndpoint(schema, message.Sender, validationResults);
			}

			// Validate receiver endpoint
			if (message.Receiver != null)
			{
				ValidateReceiverEndpoint(schema, message.Receiver, validationResults);
			}

			// Validate message content type
			if (message.Content != null)
			{
				ValidateMessageContentType(schema, message.Content, validationResults);
			}

			// Convert IMessage.Properties to the expected dictionary format
			var messageProperties = message.Properties?.ToDictionary(
				kvp => kvp.Key, 
				kvp => kvp.Value.Value,
				StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, object?>();

			// Validate required message properties
			ValidateRequiredMessageProperties(schema, messageProperties, validationResults);

			// Validate message property types and constraints
			ValidateMessagePropertyTypesAndConstraints(schema, messageProperties, validationResults);

			// Validate unknown message properties (properties not defined in schema) - only in strict mode
			if (schema.IsStrict)
			{
				ValidateUnknownMessageProperties(schema, messageProperties, validationResults);
			}

			return validationResults;
		}

		/// <summary>
		/// Validates the properties of a message against this channel schema
		/// to ensure compatibility and compliance with the defined requirements.
		/// </summary>
		/// <param name="schema">The schema to validate against.</param>
		/// <param name="messageProperties">
		/// The message properties to validate. Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{ValidationResult}"/> containing validation errors.
		/// If the enumerable is empty, the validation was successful.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="schema"/> or <paramref name="messageProperties"/> is <see langword="null"/>.
		/// </exception>
		[Obsolete("Use ValidateMessage(IMessage) instead. This method will be removed in a future version.")]
		public static IEnumerable<ValidationResult> ValidateMessageProperties(this IChannelSchema schema, IDictionary<string, object?> messageProperties)
		{
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));
			ArgumentNullException.ThrowIfNull(messageProperties, nameof(messageProperties));

			var validationResults = new List<ValidationResult>();

			// Validate required message properties
			ValidateRequiredMessageProperties(schema, messageProperties, validationResults);

			// Validate message property types and constraints
			ValidateMessagePropertyTypesAndConstraints(schema, messageProperties, validationResults);

			// Validate unknown message properties (properties not defined in schema) - only in strict mode
			if (schema.IsStrict)
			{
				ValidateUnknownMessageProperties(schema, messageProperties, validationResults);
			}

			return validationResults;
		}

		#region Private Helper Methods

		private static void ValidateRequiredParameters(IChannelSchema schema, ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			foreach (var parameter in schema.Parameters.Where(p => p.IsRequired))
			{
				var value = connectionSettings.GetParameter(parameter.Name);
				
				if (value == null)
				{
					validationResults.Add(new ValidationResult(
						$"Required parameter '{parameter.Name}' is missing.",
						new[] { parameter.Name }));
				}
			}
		}

		private static void ValidateParameterTypesAndConstraints(IChannelSchema schema, ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			foreach (var parameter in schema.Parameters)
			{
				var value = connectionSettings.GetParameter(parameter.Name);
				
				// Skip validation if value is null and parameter is not required
				// (required validation is handled separately)
				if (value == null && !parameter.IsRequired)
					continue;

				// Skip if value is null but has a default value in schema
				if (value == null && parameter.DefaultValue != null)
					continue;

				if (value != null)
				{
					// Validate type compatibility
					if (!IsTypeCompatible(parameter.DataType, value))
					{
						validationResults.Add(new ValidationResult(
							$"Parameter '{parameter.Name}' has an incompatible type. Expected: {parameter.DataType}, Actual: {value.GetType().Name}.",
							new[] { parameter.Name }));
					}

					// Validate allowed values constraint
					if (parameter.AllowedValues?.Any() == true)
					{
						if (!parameter.AllowedValues.Any(allowedValue => Equals(allowedValue, value)))
						{
							var allowedValuesStr = string.Join(", ", parameter.AllowedValues.Select(v => v?.ToString() ?? "null"));
							validationResults.Add(new ValidationResult(
								$"Parameter '{parameter.Name}' has an invalid value '{value}'. Allowed values: [{allowedValuesStr}].",
								new[] { parameter.Name }));
						}
					}
				}
			}
		}

		private static void ValidateAuthenticationRequirements(IChannelSchema schema, ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			// Skip authentication validation if no authentication types are defined
			if (!schema.AuthenticationTypes.Any())
			{
				return;
			}

			// If None is the only authentication type, no authentication parameters are required
			if (schema.AuthenticationTypes.Count == 1 && schema.AuthenticationTypes.Contains(AuthenticationType.None))
			{
				return;
			}

			// Check if at least one authentication type's requirements are satisfied
			bool hasValidAuthentication = false;
			var authenticationErrors = new List<string>();

			foreach (var authType in schema.AuthenticationTypes.Where(a => a != AuthenticationType.None))
			{
				var authValidationResults = ValidateAuthenticationTypeRequirements(authType, connectionSettings);
				
				if (!authValidationResults.Any())
				{
					hasValidAuthentication = true;
					break; // Found valid authentication, no need to check others
				}
				else
				{
					authenticationErrors.Add($"{authType}: {string.Join(", ", authValidationResults)}");
				}
			}

			// If no valid authentication was found and None is not supported, add validation error
			if (!hasValidAuthentication && !schema.AuthenticationTypes.Contains(AuthenticationType.None))
			{
				validationResults.Add(new ValidationResult(
					$"Connection settings do not satisfy any of the supported authentication types. " +
					$"Supported types: {string.Join(", ", schema.AuthenticationTypes)}. " +
					$"Validation errors: {string.Join("; ", authenticationErrors)}",
					new[] { "Authentication" }));
			}
		}

		private static List<string> ValidateAuthenticationTypeRequirements(AuthenticationType authenticationType, ConnectionSettings connectionSettings)
		{
			var errors = new List<string>();
			switch (authenticationType)
			{
				case AuthenticationType.Basic:
					ValidateBasicAuthentication(connectionSettings, errors);
					break;
				case AuthenticationType.ApiKey:
					ValidateApiKeyAuthentication(connectionSettings, errors);
					break;
				case AuthenticationType.Token:
					ValidateTokenAuthentication(connectionSettings, errors);
					break;
				case AuthenticationType.ClientCredentials:
					ValidateClientCredentialsAuthentication(connectionSettings, errors);
					break;
				case AuthenticationType.Certificate:
					ValidateCertificateAuthentication(connectionSettings, errors);
					break;
				case AuthenticationType.Custom:
					ValidateCustomAuthentication(connectionSettings, errors);
					break;
			}

			return errors;
		}

		private static void ValidateBasicAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			// Check for standard Basic authentication (username/password)
			var username = connectionSettings.GetParameter("Username");
			var password = connectionSettings.GetParameter("Password");
			
			// Check for Twilio-style Basic authentication (AccountSid/AuthToken)
			var accountSid = connectionSettings.GetParameter("AccountSid");
			var authToken = connectionSettings.GetParameter("AuthToken");

			// Check for other common Basic auth variations
			var user = connectionSettings.GetParameter("User");
			var pass = connectionSettings.GetParameter("Pass");
			var clientId = connectionSettings.GetParameter("ClientId");
			var clientSecret = connectionSettings.GetParameter("ClientSecret");

			bool hasValidBasicAuth = 
				(username != null && password != null) ||
				(accountSid != null && authToken != null) ||
				(user != null && pass != null) ||
				(clientId != null && clientSecret != null);

			if (!hasValidBasicAuth)
			{
				errors.Add("Basic authentication requires one of the following parameter pairs: " +
						  "(Username, Password), (AccountSid, AuthToken), (User, Pass), or (ClientId, ClientSecret)");
			}
		}

		private static void ValidateApiKeyAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var apiKey = connectionSettings.GetParameter("ApiKey");
			var key = connectionSettings.GetParameter("Key");
			var accessKey = connectionSettings.GetParameter("AccessKey");

			if (apiKey == null && key == null && accessKey == null)
			{
				errors.Add("API Key authentication requires one of the following parameters: ApiKey, Key, or AccessKey");
			}
		}

		private static void ValidateTokenAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var token = connectionSettings.GetParameter("Token");
			var accessToken = connectionSettings.GetParameter("AccessToken");
			var bearerToken = connectionSettings.GetParameter("BearerToken");
			var authToken = connectionSettings.GetParameter("AuthToken");

			if (token == null && accessToken == null && bearerToken == null && authToken == null)
			{
				errors.Add("Token authentication requires one of the following parameters: Token, AccessToken, BearerToken, or AuthToken");
			}
		}

		private static void ValidateClientCredentialsAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var clientId = connectionSettings.GetParameter("ClientId");
			var clientSecret = connectionSettings.GetParameter("ClientSecret");

			if (clientId == null || clientSecret == null)
			{
				errors.Add("Client Credentials authentication requires both ClientId and ClientSecret parameters");
			}
		}

		private static void ValidateCertificateAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var certificate = connectionSettings.GetParameter("Certificate");
			var certificatePath = connectionSettings.GetParameter("CertificatePath");
			var certificateThumbprint = connectionSettings.GetParameter("CertificateThumbprint");
			var pfxFile = connectionSettings.GetParameter("PfxFile");

			if (certificate == null && certificatePath == null && certificateThumbprint == null && pfxFile == null)
			{
				errors.Add("Certificate authentication requires one of the following parameters: " +
						  "Certificate, CertificatePath, CertificateThumbprint, or PfxFile");
			}
		}

		private static void ValidateCustomAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			// For custom authentication, we look for any authentication-related parameters
			// This is more flexible to accommodate various custom authentication schemes
			var authParams = new []
			{
				"CustomAuth", "AuthenticationData", "Credentials", "AuthConfig",
				"SecretKey", "PrivateKey", "Signature", "Hash"
			};

			bool hasCustomAuthParam = authParams.Any(param => connectionSettings.GetParameter(param) != null);

			if (!hasCustomAuthParam)
			{
				errors.Add("Custom authentication requires at least one authentication parameter. " +
						  $"Common parameters include: {string.Join(", ", authParams)}");
			}
		}

		private static void ValidateUnknownParameters(IChannelSchema schema, ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			var schemaParameterNames = schema.Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
			
			// Add authentication-related parameter names that should be considered "known"
			var authenticationParameterNames = GetAllAuthenticationParameterNames(schema);
			foreach (var authParam in authenticationParameterNames)
			{
				schemaParameterNames.Add(authParam);
			}
			
			foreach (var parameterKey in connectionSettings.Parameters.Keys)
			{
				if (!schemaParameterNames.Contains(parameterKey))
				{
					validationResults.Add(new ValidationResult(
						$"Unknown parameter '{parameterKey}' is not supported by this schema.",
						new[] { parameterKey }));
				}
			}
		}

		private static HashSet<string> GetAllAuthenticationParameterNames(IChannelSchema schema)
		{
			var authParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Only include authentication parameters if corresponding authentication types are supported
			foreach (var authType in schema.AuthenticationTypes)
			{
				switch (authType)
				{
					case AuthenticationType.Basic:
						authParams.Add("Username");
						authParams.Add("Password");
						authParams.Add("AccountSid");
						authParams.Add("AuthToken");
						authParams.Add("User");
						authParams.Add("Pass");
						authParams.Add("ClientId");
						authParams.Add("ClientSecret");
						break;
					case AuthenticationType.ApiKey:
						authParams.Add("ApiKey");
						authParams.Add("Key");
						authParams.Add("AccessKey");
						break;
					case AuthenticationType.Token:
						authParams.Add("Token");
						authParams.Add("AccessToken");
						authParams.Add("BearerToken");
						authParams.Add("AuthToken");
						break;
					case AuthenticationType.ClientCredentials:
						authParams.Add("ClientId");
						authParams.Add("ClientSecret");
						break;
					case AuthenticationType.Certificate:
						authParams.Add("Certificate");
						authParams.Add("CertificatePath");
						authParams.Add("CertificateThumbprint");
						authParams.Add("PfxFile");
						authParams.Add("CertificatePassword");
						authParams.Add("PfxPassword");
						break;
					case AuthenticationType.Custom:
						authParams.Add("CustomAuth");
						authParams.Add("AuthenticationData");
						authParams.Add("Credentials");
						authParams.Add("AuthConfig");
						authParams.Add("SecretKey");
						authParams.Add("PrivateKey");
						authParams.Add("Signature");
						authParams.Add("Hash");
						break;
				}
			}

			return authParams;
		}

		private static void ValidateRequiredMessageProperties(IChannelSchema schema, IDictionary<string, object?> messageProperties, List<ValidationResult> validationResults)
		{
			foreach (var propertyConfig in schema.MessageProperties.Where(p => p.IsRequired))
			{
				if (!messageProperties.ContainsKey(propertyConfig.Name) || messageProperties[propertyConfig.Name] == null)
				{
					validationResults.Add(new ValidationResult(
						$"Required message property '{propertyConfig.Name}' is missing.",
						new[] { propertyConfig.Name }));
				}
			}
		}

		private static void ValidateMessagePropertyTypesAndConstraints(IChannelSchema schema, IDictionary<string, object?> messageProperties, List<ValidationResult> validationResults)
		{
			foreach (var propertyConfig in schema.MessageProperties)
			{
				if (messageProperties.TryGetValue(propertyConfig.Name, out var value))
				{
					// Use the property configuration's built-in validation
					var propertyValidationResults = propertyConfig.Validate(value);
					validationResults.AddRange(propertyValidationResults);
				}
			}
		}

		private static void ValidateUnknownMessageProperties(IChannelSchema schema, IDictionary<string, object?> messageProperties, List<ValidationResult> validationResults)
		{
			var schemaPropertyNames = schema.MessageProperties.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
			
			foreach (var propertyKey in messageProperties.Keys)
			{
				if (!schemaPropertyNames.Contains(propertyKey))
				{
					validationResults.Add(new ValidationResult(
						$"Unknown message property '{propertyKey}' is not supported by this schema.",
						new[] { propertyKey }));
				}
			}
		}

		private static void ValidateSenderEndpoint(IChannelSchema schema, IEndpoint sender, List<ValidationResult> validationResults)
		{
			// Skip validation if no endpoints are defined in schema
			if (!schema.Endpoints.Any())
			{
				return;
			}

			// Check if sender endpoint type is supported and can send
			var supportedEndpoint = schema.Endpoints.FirstOrDefault(e => 
				(e.Type == EndpointType.Any || e.Type == sender.Type) && e.CanSend);

			if (supportedEndpoint == null)
			{
				validationResults.Add(new ValidationResult(
					$"Sender endpoint type '{sender.Type}' is not supported or cannot send messages according to this schema. " +
					$"Supported sender types: [{string.Join(", ", schema.Endpoints.Where(e => e.CanSend).Select(e => e.Type))}]",
					new[] { "Sender" }));
			}
		}

		private static void ValidateReceiverEndpoint(IChannelSchema schema, IEndpoint receiver, List<ValidationResult> validationResults)
		{
			// Skip validation if no endpoints are defined in schema
			if (!schema.Endpoints.Any())
			{
				return;
			}

			// Check if receiver endpoint type is supported and can receive
			var supportedEndpoint = schema.Endpoints.FirstOrDefault(e => 
				(e.Type == EndpointType.Any || e.Type == receiver.Type) && e.CanReceive);

			if (supportedEndpoint == null)
			{
				validationResults.Add(new ValidationResult(
					$"Receiver endpoint type '{receiver.Type}' is not supported or cannot receive messages according to this schema. " +
					$"Supported receiver types: [{string.Join(", ", schema.Endpoints.Where(e => e.CanReceive).Select(e => e.Type))}]",
					new[] { "Receiver" }));
			}
		}

		private static void ValidateMessageContentType(IChannelSchema schema, IMessageContent content, List<ValidationResult> validationResults)
		{
			// Skip validation if no content types are defined in schema
			if (!schema.ContentTypes.Any())
			{
				return;
			}

			// Check if message content type is supported by the schema
			if (!schema.ContentTypes.Contains(content.ContentType))
			{
				validationResults.Add(new ValidationResult(
					$"Message content type '{content.ContentType}' is not supported by this schema. " +
					$"Supported content types: [{string.Join(", ", schema.ContentTypes)}]",
					new[] { "Content" }));
			}
		}

		private static bool IsTypeCompatible(DataType parameterType, object value)
		{
			return parameterType switch
			{
				DataType.Boolean => value is bool,
				DataType.String => value is string,
				DataType.Integer => value is int || value is long || value is byte || value is short || value is sbyte,
				DataType.Number => value is double || value is decimal || value is float || 
									   value is int || value is long || value is byte || value is short || value is sbyte,
				_ => false,
			};
		}

		#endregion
	}
}