//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides a base implementation of <see cref="IChannelSchema"/> 
	/// that defines the schema for a communication channel.
	/// </summary>
	/// <remarks>
	/// This class serves as a foundation for creating channel schemas 
	/// with customizable properties, capabilities, and configuration parameters.
	/// Implementers can inherit from this class to provide specific 
	/// channel configurations while benefiting from the default implementations.
	/// </remarks>
	public class ChannelSchema : IChannelSchema
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelSchema"/> class
		/// with the specified channel provider, type, and version.
		/// </summary>
		/// <param name="channelProvider">The channel provider identifier.</param>
		/// <param name="channelType">The type of communication channel.</param>
		/// <param name="version">The version of the schema or connector.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when any of the required parameters is null or whitespace.
		/// </exception>
		/// <remarks>
		/// The schema is created in strict mode by default. Use <see cref="WithFlexibleMode"/> 
		/// to allow unknown parameters and properties in validation.
		/// </remarks>
		public ChannelSchema(string channelProvider, string channelType, string version)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelProvider, nameof(channelProvider));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelType, nameof(channelType));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(version, nameof(version));

			ChannelProvider = channelProvider;
			ChannelType = channelType;
			Version = version;
			IsStrict = true; // Default to strict mode
			Parameters = new List<ChannelParameter>();
			MessageProperties = new List<MessagePropertyConfiguration>();
			ContentTypes = new List<MessageContentType>();
			AuthenticationTypes = new List<AuthenticationType>();
			Endpoints = new List<ChannelEndpointConfiguration>();
			Capabilities = ChannelCapability.SendMessages; // Default capability
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelSchema"/> class
		/// with the same core identity as another schema, copying its configuration.
		/// </summary>
		/// <param name="sourceSchema">The source schema to copy from.</param>
		/// <param name="derivedDisplayName">An optional display name for the new schema to distinguish it from the source.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when sourceSchema is null.
		/// </exception>
		/// <remarks>
		/// This constructor creates a new schema with the same ChannelProvider, ChannelType, and Version 
		/// as the source schema, ensuring logical compatibility. The new schema is independent and can be 
		/// modified without affecting the source schema. The application layer determines relationships 
		/// between schemas based on their core identity properties.
		/// </remarks>
		public ChannelSchema(IChannelSchema sourceSchema, string? derivedDisplayName = null)
		{
			ArgumentNullException.ThrowIfNull(sourceSchema, nameof(sourceSchema));

			// New schema has the same logical identity as the source
			ChannelProvider = sourceSchema.ChannelProvider;
			ChannelType = sourceSchema.ChannelType;
			Version = sourceSchema.Version;
			IsStrict = sourceSchema.IsStrict; // Copy strict mode from source
			
			// Set display name - use provided name or derive from source
			DisplayName = derivedDisplayName ?? $"{sourceSchema.DisplayName} (Copy)";
			
			// Copy capabilities from source schema
			Capabilities = sourceSchema.Capabilities;
			
			// Create deep copies of collections to allow independent modifications
			Parameters = new List<ChannelParameter>();
			foreach (var param in sourceSchema.Parameters)
			{
				// Create a new ChannelParameter instance with copied values
				var newParam = new ChannelParameter(param.Name, param.DataType)
				{
					IsRequired = param.IsRequired,
					IsSensitive = param.IsSensitive,
					DefaultValue = param.DefaultValue,
					Description = param.Description,
					AllowedValues = param.AllowedValues?.ToArray() // Create a copy of allowed values if present
				};
				Parameters.Add(newParam);
			}

			MessageProperties = new List<MessagePropertyConfiguration>();
			foreach (var msgProp in sourceSchema.MessageProperties)
			{
				// Create a new MessagePropertyConfiguration instance with copied values
				var newMsgProp = new MessagePropertyConfiguration(msgProp.Name, msgProp.DataType)
				{
					IsRequired = msgProp.IsRequired,
					IsSensitive = msgProp.IsSensitive,
					Description = msgProp.Description
				};
				MessageProperties.Add(newMsgProp);
			}

			ContentTypes = new List<MessageContentType>(sourceSchema.ContentTypes);
			AuthenticationTypes = new List<AuthenticationType>(sourceSchema.AuthenticationTypes);
			
			Endpoints = new List<ChannelEndpointConfiguration>();
			foreach (var endpoint in sourceSchema.Endpoints)
			{
				// Create a new ChannelEndpointConfiguration instance with copied values
				var newEndpoint = new ChannelEndpointConfiguration(endpoint.Type)
				{
					CanSend = endpoint.CanSend,
					CanReceive = endpoint.CanReceive,
					IsRequired = endpoint.IsRequired
				};
				Endpoints.Add(newEndpoint);
			}
		}

		/// <inheritdoc/>
		public string ChannelProvider { get; }

		/// <inheritdoc/>
		public string ChannelType { get; }

		/// <inheritdoc/>
		public string Version { get; }

		/// <inheritdoc/>
		public string? DisplayName { get; set; }

		/// <inheritdoc/>
		public bool IsStrict { get; set; }

		/// <inheritdoc/>
		public ChannelCapability Capabilities { get; set; }

		/// <inheritdoc/>
		public IList<ChannelParameter> Parameters { get; }

		/// <inheritdoc/>
		public IList<MessagePropertyConfiguration> MessageProperties { get; }

		/// <inheritdoc/>
		public IList<MessageContentType> ContentTypes { get; }

		/// <inheritdoc/>
		public IList<AuthenticationType> AuthenticationTypes { get; }

		/// <inheritdoc/>
		public IList<ChannelEndpointConfiguration> Endpoints { get; }

		/// <summary>
		/// Adds a parameter to the schema configuration.
		/// </summary>
		/// <param name="parameter">The parameter to add.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the parameter is null.
		/// </exception>
		public ChannelSchema AddParameter(ChannelParameter parameter)
		{
			ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
			Parameters.Add(parameter);
			return this;
		}

		/// <summary>
		/// Adds a new parameter to the schema configuration with 
		/// the specified name and type.
		/// </summary>
		/// <param name="parameterName">
		/// The name of the parameter to add.
		/// </param>
		/// <param name="parameterType">
		/// The data type of the parameter to add.
		/// </param>
		/// <param name="configure">
		/// A callback to configure additional properties of the parameter.
		/// </param>
		/// <returns>
		/// Returns the current schema instance for method chaining.
		/// </returns>
		public ChannelSchema AddParameter(string parameterName, DataType parameterType, Action<ChannelParameter>? configure = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(parameterName, nameof(parameterName));

			var parameter = new ChannelParameter(parameterName, parameterType);
			configure?.Invoke(parameter);
			return AddParameter(parameter);
		}

		/// <summary>
		/// Adds a new required parameter to the schema configuration with
		/// the specified name and type.
		/// </summary>
		/// <param name="parameterName">
		/// The name of the parameter to add.
		/// </param>
		/// <param name="parameterType">
		/// The data type of the parameter to add.
		/// </param>
		/// <param name="sensitive">
		/// A value indicating whether the parameter is sensitive.
		/// </param>
		/// <returns>
		/// Returns the current schema instance for method chaining.
		/// </returns>
		public ChannelSchema AddRequiredParameter(string parameterName, DataType parameterType, bool sensitive = false)
			=> AddParameter(parameterName, parameterType, param => { param.IsRequired = true; param.IsSensitive = sensitive; });

		/// <summary>
		/// Adds to the schema configuration a new definition of a property of 
		/// messages handled by the channel.
		/// </summary>
		/// <param name="property">
		/// The property configuration to add.
		/// </param>
		/// <returns>
		/// The current schema instance for method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the property configuration is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a message property configuration with the same name already exists.
		/// </exception>
		public ChannelSchema AddMessageProperty(MessagePropertyConfiguration property)
		{
			ArgumentNullException.ThrowIfNull(property, nameof(property));
			
			if (MessageProperties.Any(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidOperationException($"A message property configuration with name '{property.Name}' already exists in the schema.");
			}
			
			MessageProperties.Add(property);

			return this;
		}

		/// <summary>
		/// Adds a new message property to the channel schema with 
		/// the specified name and type.
		/// </summary>
		/// <param name="propertyName">The name of the message property to add.</param>
		/// <param name="propertyType">The data type of the message property.</param>
		/// <param name="configure">An optional configuration action to further customize 
		/// the message property.</param>
		/// <returns>
		/// Returns the updated <see cref="ChannelSchema"/> instance, including the newly 
		/// added message property.
		/// </returns>
		public ChannelSchema AddMessageProperty(string propertyName, DataType propertyType, Action<MessagePropertyConfiguration>? configure = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
			var property = new MessagePropertyConfiguration(propertyName, propertyType);
			configure?.Invoke(property);
			return AddMessageProperty(property);
		}

		/// <summary>
		/// Adds a content type to the list of supported content types.
		/// </summary>
		/// <param name="contentType">The content type to add.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema AddContentType(MessageContentType contentType)
		{
			ContentTypes.Add(contentType);
			return this;
		}

		/// <summary>
		/// Adds an authentication type to the list of supported authentication types.
		/// </summary>
		/// <param name="authenticationType">The authentication type to add.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema AddAuthenticationType(AuthenticationType authenticationType)
		{
			AuthenticationTypes.Add(authenticationType);
			return this;
		}

		/// <summary>
		/// Adds the specified message endpoint configuration to the current channel schema.
		/// </summary>
		/// <param name="endpoint">
		/// The configuration of the message endpoint to be added.
		/// </param>
		/// <returns>
		/// The updated <see cref="ChannelSchema"/> instance with the new endpoint 
		/// configuration included.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="endpoint"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when an endpoint configuration with the same type already exists.
		/// </exception>
		public ChannelSchema HandlesMessageEndpoint(ChannelEndpointConfiguration endpoint)
		{
			ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));
			
			if (Endpoints.Any(e => e.Type == endpoint.Type))
			{
				throw new InvalidOperationException($"An endpoint configuration with type '{endpoint.Type}' already exists in the schema.");
			}
			
			Endpoints.Add(endpoint);
			return this;
		}

		/// <summary>
		/// Adds the specified message endpoint type to the current channel schema.
		/// </summary>
		/// <param name="endpointType">
		/// The type of the message endpoint to be added.
		/// </param>
		/// <param name="configure">
		/// An optional action used to configure the endpoint configuration.
		/// </param>
		/// <returns>
		/// The updated <see cref="ChannelSchema"/> instance with the new endpoint 
		/// configuration included.
		/// </returns>
		public ChannelSchema HandlesMessageEndpoint(EndpointType endpointType, Action<ChannelEndpointConfiguration>? configure = null)
		{
			var endpoint = new ChannelEndpointConfiguration(endpointType);
			configure?.Invoke(endpoint);
			return HandlesMessageEndpoint(endpoint);
		}

		/// <summary>
		/// Configures the channel schema to handle any message endpoint.
		/// </summary>
		/// <returns>A <see cref="ChannelSchema"/> that is set to handle any message endpoint.</returns>
		public ChannelSchema AllowsAnyMessageEndpoint()
		{
			if (Endpoints.Any(e => e.Type == EndpointType.Any))
			{
				throw new InvalidOperationException($"An endpoint configuration with type '{EndpointType.Any}' already exists in the schema.");
			}
			
			var endpoint = new ChannelEndpointConfiguration(EndpointType.Any)
			{
				CanSend = true,
				CanReceive = true
			};
			Endpoints.Add(endpoint);
			return this;
		}

		/// <summary>
		/// Sets the capabilities for the connector.
		/// </summary>
		/// <param name="capabilities">The capabilities to set.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema WithCapabilities(ChannelCapability capabilities)
		{
			Capabilities = capabilities;
			return this;
		}

		/// <summary>
		/// Adds the specified capability to the current channel schema.
		/// </summary>
		/// <param name="capability">The capability to add to the channel schema.</param>
		/// <returns>The updated <see cref="ChannelSchema"/> instance with the added capability.</returns>
		public ChannelSchema WithCapability(ChannelCapability capability)
		{
			Capabilities |= capability;
			return this;
		}

		/// <summary>
		/// Sets the display name for the schema.
		/// </summary>
		/// <param name="displayName">The display name to set.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema WithDisplayName(string? displayName)
		{
			DisplayName = displayName;
			return this;
		}

		/// <summary>
		/// Sets the strict mode for the schema.
		/// </summary>
		/// <param name="isStrict">
		/// A value indicating whether the schema operates in strict mode.
		/// When <c>true</c>, validation will reject unknown parameters and properties.
		/// When <c>false</c>, unknown parameters and properties are allowed.
		/// </param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema WithStrictMode(bool isStrict)
		{
			IsStrict = isStrict;
			return this;
		}

		/// <summary>
		/// Enables strict mode for the schema.
		/// In strict mode, validation will reject unknown parameters and properties.
		/// </summary>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema WithStrictMode()
		{
			return WithStrictMode(true);
		}

		/// <summary>
		/// Disables strict mode for the schema.
		/// When strict mode is disabled, unknown parameters and properties are allowed.
		/// </summary>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema WithFlexibleMode()
		{
			return WithStrictMode(false);
		}

		/// <summary>
		/// Validates the specified connection settings against this channel schema
		/// to ensure compatibility and compliance with the defined requirements.
		/// </summary>
		/// <param name="connectionSettings">
		/// The connection settings to validate. Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{ValidationResult}"/> containing validation errors.
		/// If the enumerable is empty, the validation was successful.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="connectionSettings"/> is <see langword="null"/>.
		/// </exception>
		public IEnumerable<ValidationResult> ValidateConnectionSettings(ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			var validationResults = new List<ValidationResult>();

			// Validate required parameters
			ValidateRequiredParameters(connectionSettings, validationResults);

			// Validate parameter types and constraints
			ValidateParameterTypesAndConstraints(connectionSettings, validationResults);

			// Validate authentication requirements
			ValidateAuthenticationRequirements(connectionSettings, validationResults);

			// Validate unknown parameters (parameters not defined in schema) - only in strict mode
			if (IsStrict)
			{
				ValidateUnknownParameters(connectionSettings, validationResults);
			}

			return validationResults;
		}

		/// <summary>
		/// Validates the properties of a message against this channel schema
		/// to ensure compatibility and compliance with the defined requirements.
		/// </summary>
		/// <param name="messageProperties">
		/// The message properties to validate. Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{ValidationResult}"/> containing validation errors.
		/// If the enumerable is empty, the validation was successful.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="messageProperties"/> is <see langword="null"/>.
		/// </exception>
		public IEnumerable<ValidationResult> ValidateMessageProperties(IDictionary<string, object?> messageProperties)
		{
			ArgumentNullException.ThrowIfNull(messageProperties, nameof(messageProperties));

			var validationResults = new List<ValidationResult>();

			// Validate required message properties
			ValidateRequiredMessageProperties(messageProperties, validationResults);

			// Validate message property types and constraints
			ValidateMessagePropertyTypesAndConstraints(messageProperties, validationResults);

			// Validate unknown message properties (properties not defined in schema) - only in strict mode
			if (IsStrict)
			{
				ValidateUnknownMessageProperties(messageProperties, validationResults);
			}

			return validationResults;
		}

		private void ValidateRequiredParameters(ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			foreach (var parameter in Parameters.Where(p => p.IsRequired))
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

		private void ValidateParameterTypesAndConstraints(ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			foreach (var parameter in Parameters)
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

		private void ValidateAuthenticationRequirements(ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			// Skip authentication validation if no authentication types are defined
			if (!AuthenticationTypes.Any())
			{
				return;
			}

			// If None is the only authentication type, no authentication parameters are required
			if (AuthenticationTypes.Count == 1 && AuthenticationTypes.Contains(AuthenticationType.None))
			{
				return;
			}

			// Check if at least one authentication type's requirements are satisfied
			bool hasValidAuthentication = false;
			var authenticationErrors = new List<string>();

			foreach (var authType in AuthenticationTypes.Where(a => a != AuthenticationType.None))
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
			if (!hasValidAuthentication && !AuthenticationTypes.Contains(AuthenticationType.None))
			{
				validationResults.Add(new ValidationResult(
					$"Connection settings do not satisfy any of the supported authentication types. " +
					$"Supported types: {string.Join(", ", AuthenticationTypes)}. " +
					$"Validation errors: {string.Join("; ", authenticationErrors)}",
					new[] { "Authentication" }));
			}
		}

		/// <summary>
		/// Validates the authentication requirements for a specific authentication type.
		/// </summary>
		/// <param name="authenticationType">The authentication type to validate.</param>
		/// <param name="connectionSettings">The connection settings to check.</param>
		/// <returns>A list of validation error messages for this authentication type.</returns>
		private List<string> ValidateAuthenticationTypeRequirements(AuthenticationType authenticationType, ConnectionSettings connectionSettings)
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

		/// <summary>
		/// Validates Basic authentication requirements (username/password or AccountSid/AuthToken for Twilio-like services).
		/// </summary>
		private void ValidateBasicAuthentication(ConnectionSettings connectionSettings, List<string> errors)
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

		/// <summary>
		/// Validates API Key authentication requirements.
		/// </summary>
		private void ValidateApiKeyAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var apiKey = connectionSettings.GetParameter("ApiKey");
			var key = connectionSettings.GetParameter("Key");
			var accessKey = connectionSettings.GetParameter("AccessKey");

			if (apiKey == null && key == null && accessKey == null)
			{
				errors.Add("API Key authentication requires one of the following parameters: ApiKey, Key, or AccessKey");
			}
		}

		/// <summary>
		/// Validates Token authentication requirements.
		/// </summary>
		private void ValidateTokenAuthentication(ConnectionSettings connectionSettings, List<string> errors)
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

		/// <summary>
		/// Validates Client Credentials authentication requirements.
		/// </summary>
		private void ValidateClientCredentialsAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var clientId = connectionSettings.GetParameter("ClientId");
			var clientSecret = connectionSettings.GetParameter("ClientSecret");

			if (clientId == null || clientSecret == null)
			{
				errors.Add("Client Credentials authentication requires both ClientId and ClientSecret parameters");
			}
		}

		/// <summary>
		/// Validates Certificate authentication requirements.
		/// </summary>
		private void ValidateCertificateAuthentication(ConnectionSettings connectionSettings, List<string> errors)
		{
			var certificate = connectionSettings.GetParameter("Certificate");
			var certificatePath = connectionSettings.GetParameter("CertificatePath");
			var certificateThumbprint = connectionSettings.GetParameter("CertificateThumbprint");
			var pfxFile = connectionSettings.GetParameter("PfxFile");

			if (certificate == null && certificatePath == null && certificateThumbprint == null && pfxFile == null)
			{
				errors.Add("Certificate authentication requires one of the following parameters: " +
						  "Certificate, CertificatePath, CertificateThumbprint, or PfxFile");
				return; // Don't check for passwords if no certificate is provided
			}

			// Only check for password if PFX file is specifically provided
			// Other certificate types may not require passwords
			if (pfxFile != null)
			{
				var certificatePassword = connectionSettings.GetParameter("CertificatePassword");
				var pfxPassword = connectionSettings.GetParameter("PfxPassword");
				
				// Note: We make password optional as some PFX files may not be password protected
				// This is just a warning in the comment, not an actual validation error
			}
		}

		/// <summary>
		/// Validates Custom authentication requirements.
		/// </summary>
		private void ValidateCustomAuthentication(ConnectionSettings connectionSettings, List<string> errors)
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

		private void ValidateUnknownParameters(ConnectionSettings connectionSettings, List<ValidationResult> validationResults)
		{
			var schemaParameterNames = Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
			
			// Add authentication-related parameter names that should be considered "known"
			var authenticationParameterNames = GetAllAuthenticationParameterNames();
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

		/// <summary>
		/// Gets all parameter names that are considered valid for authentication purposes.
		/// </summary>
		/// <returns>A set of authentication parameter names that should not be considered "unknown".</returns>
		private HashSet<string> GetAllAuthenticationParameterNames()
		{
			var authParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Only include authentication parameters if corresponding authentication types are supported
			foreach (var authType in AuthenticationTypes)
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

		private void ValidateRequiredMessageProperties(IDictionary<string, object?> messageProperties, List<ValidationResult> validationResults)
		{
			foreach (var propertyConfig in MessageProperties.Where(p => p.IsRequired))
			{
				if (!messageProperties.ContainsKey(propertyConfig.Name) || messageProperties[propertyConfig.Name] == null)
				{
					validationResults.Add(new ValidationResult(
						$"Required message property '{propertyConfig.Name}' is missing.",
						new[] { propertyConfig.Name }));
				}
			}
		}

		private void ValidateMessagePropertyTypesAndConstraints(IDictionary<string, object?> messageProperties, List<ValidationResult> validationResults)
		{
			foreach (var propertyConfig in MessageProperties)
			{
				if (messageProperties.TryGetValue(propertyConfig.Name, out var value))
				{
					// Use the property configuration's built-in validation
					var propertyValidationResults = propertyConfig.Validate(value);
					validationResults.AddRange(propertyValidationResults);
				}
			}
		}

		private void ValidateUnknownMessageProperties(IDictionary<string, object?> messageProperties, List<ValidationResult> validationResults)
		{
			var schemaPropertyNames = MessageProperties.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
			
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

		/// <summary>
		/// Removes a parameter from the schema configuration.
		/// This is useful when deriving schemas to restrict certain parameters.
		/// </summary>
		/// <param name="parameterName">The name of the parameter to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the parameter name is null or whitespace.
		/// </exception>
		public ChannelSchema RemoveParameter(string parameterName)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(parameterName, nameof(parameterName));
			
			var parameter = Parameters.FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));
			if (parameter != null)
			{
				Parameters.Remove(parameter);
			}
			
			return this;
		}

		/// <summary>
		/// Removes a message property from the schema configuration.
		/// This is useful when deriving schemas to restrict certain message properties.
		/// </summary>
		/// <param name="propertyName">The name of the message property to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the property name is null or whitespace.
		/// </exception>
		public ChannelSchema RemoveMessageProperty(string propertyName)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
			
			var property = MessageProperties.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
			if (property != null)
			{
				MessageProperties.Remove(property);
			}
			
			return this;
		}

		/// <summary>
		/// Removes a content type from the list of supported content types.
		/// This is useful when deriving schemas to restrict certain content types.
		/// </summary>
		/// <param name="contentType">The content type to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema RemoveContentType(MessageContentType contentType)
		{
			ContentTypes.Remove(contentType);
			return this;
		}

		/// <summary>
		/// Removes an authentication type from the list of supported authentication types.
		/// This is useful when deriving schemas to restrict certain authentication types.
		/// </summary>
		/// <param name="authenticationType">The authentication type to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema RemoveAuthenticationType(AuthenticationType authenticationType)
		{
			AuthenticationTypes.Remove(authenticationType);
			return this;
		}

		/// <summary>
		/// Removes an endpoint configuration from the schema.
		/// This is useful when deriving schemas to restrict certain endpoints.
		/// </summary>
		/// <param name="endpointType">The type of endpoint to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema RemoveEndpoint(EndpointType endpointType)
		{			
			var endpoint = Endpoints.FirstOrDefault(e => e.Type == endpointType);
			if (endpoint != null)
			{
				Endpoints.Remove(endpoint);
			}
			
			return this;
		}

		/// <summary>
		/// Restricts the capabilities to only those specified, removing any capabilities
		/// that are not included in the provided flags.
		/// This is useful when deriving schemas to limit functionality.
		/// </summary>
		/// <param name="allowedCapabilities">The capabilities to allow.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema RestrictCapabilities(ChannelCapability allowedCapabilities)
		{
			Capabilities &= allowedCapabilities;
			return this;
		}

		/// <summary>
		/// Removes a specific capability from the current capabilities.
		/// This is useful when deriving schemas to remove certain functionality.
		/// </summary>
		/// <param name="capability">The capability to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema RemoveCapability(ChannelCapability capability)
		{
			Capabilities &= ~capability;
			return this;
		}

		/// <summary>
		/// Clears all content types and adds only the specified ones.
		/// This is useful when deriving schemas to restrict content types.
		/// </summary>
		/// <param name="allowedContentTypes">The content types to allow.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when allowedContentTypes is null.
		/// </exception>
		public ChannelSchema RestrictContentTypes(params MessageContentType[] allowedContentTypes)
		{
			ArgumentNullException.ThrowIfNull(allowedContentTypes, nameof(allowedContentTypes));
			
			ContentTypes.Clear();
			foreach (var contentType in allowedContentTypes)
			{
				ContentTypes.Add(contentType);
			}
			
			return this;
		}

		/// <summary>
		/// Clears all authentication types and adds only the specified ones.
		/// This is useful when deriving schemas to restrict authentication methods.
		/// </summary>
		/// <param name="allowedAuthenticationTypes">The authentication types to allow.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when allowedAuthenticationTypes is null.
		/// </exception>
		public ChannelSchema RestrictAuthenticationTypes(params AuthenticationType[] allowedAuthenticationTypes)
		{
			ArgumentNullException.ThrowIfNull(allowedAuthenticationTypes, nameof(allowedAuthenticationTypes));
			
			AuthenticationTypes.Clear();
			foreach (var authType in allowedAuthenticationTypes)
			{
				AuthenticationTypes.Add(authType);
			}
			
			return this;
		}

		/// <summary>
		/// Updates an existing parameter's configuration.
		/// This is useful when deriving schemas to modify parameter requirements or defaults.
		/// </summary>
		/// <param name="parameterName">The name of the parameter to update.</param>
		/// <param name="updateAction">The action to perform on the parameter.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when parameterName is null or whitespace, or updateAction is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the parameter with the specified name is not found.
		/// </exception>
		public ChannelSchema UpdateParameter(string parameterName, Action<ChannelParameter> updateAction)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(parameterName, nameof(parameterName));
			ArgumentNullException.ThrowIfNull(updateAction, nameof(updateAction));
			
			var parameter = Parameters.FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));
			if (parameter == null)
			{
				throw new InvalidOperationException($"Parameter with name '{parameterName}' not found in the schema.");
			}
			
			updateAction(parameter);
			return this;
		}

		/// <summary>
		/// Updates an existing message property's configuration.
		/// This is useful when deriving schemas to modify property requirements.
		/// </summary>
		/// <param name="propertyName">The name of the message property to update.</param>
		/// <param name="updateAction">The action to perform on the message property.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when propertyName is null or whitespace, or updateAction is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the message property with the specified name is not found.
		/// </exception>
		public ChannelSchema UpdateMessageProperty(string propertyName, Action<MessagePropertyConfiguration> updateAction)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
			ArgumentNullException.ThrowIfNull(updateAction, nameof(updateAction));
			
			var property = MessageProperties.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
			if (property == null)
			{
				throw new InvalidOperationException($"Message property with name '{propertyName}' not found in the schema.");
			}
			
			updateAction(property);
			return this;
		}

		/// <summary>
		/// Updates an existing endpoint configuration.
		/// This is useful when deriving schemas to modify endpoint capabilities.
		/// </summary>
		/// <param name="endpointType">The type of endpoint to update.</param>
		/// <param name="updateAction">The action to perform on the endpoint configuration.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when updateAction is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the endpoint with the specified type is not found.
		/// </exception>
		public ChannelSchema UpdateEndpoint(EndpointType endpointType, Action<ChannelEndpointConfiguration> updateAction)
		{
			ArgumentNullException.ThrowIfNull(updateAction, nameof(updateAction));
			
			var endpoint = Endpoints.FirstOrDefault(e => e.Type == endpointType);
			if (endpoint == null)
			{
				throw new InvalidOperationException($"Endpoint with type '{endpointType}' not found in the schema.");
			}
			
			updateAction(endpoint);
			return this;
		}

		/// <summary>
		/// Determines whether this schema is logically compatible with another schema.
		/// Two schemas are compatible if they have the same ChannelProvider, ChannelType, and Version.
		/// </summary>
		/// <param name="otherSchema">The schema to compare with.</param>
		/// <returns>True if the schemas are logically compatible; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when otherSchema is null.</exception>
		public bool IsCompatibleWith(IChannelSchema otherSchema)
		{
			ArgumentNullException.ThrowIfNull(otherSchema, nameof(otherSchema));
			
			return string.Equals(ChannelProvider, otherSchema.ChannelProvider, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(ChannelType, otherSchema.ChannelType, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(Version, otherSchema.Version, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Validates whether this schema can be considered a valid restriction of another schema.
		/// A schema is a valid restriction if it's compatible and all its configurations are 
		/// subsets of the target schema's configurations.
		/// </summary>
		/// <param name="targetSchema">The schema to validate against.</param>
		/// <returns>An enumerable of validation results. Empty if this schema is a valid restriction.</returns>
		/// <exception cref="ArgumentNullException">Thrown when targetSchema is null.</exception>
		public IEnumerable<ValidationResult> ValidateAsRestrictionOf(IChannelSchema targetSchema)
		{
			ArgumentNullException.ThrowIfNull(targetSchema, nameof(targetSchema));
			
			var validationResults = new List<ValidationResult>();

			// First check if schemas are compatible
			if (!IsCompatibleWith(targetSchema))
			{
				validationResults.Add(new ValidationResult(
					$"Schema is not compatible. Expected: {targetSchema.ChannelProvider}/{targetSchema.ChannelType}/{targetSchema.Version}, " +
					$"Actual: {ChannelProvider}/{ChannelType}/{Version}"));
				return validationResults;
			}

			// Validate capabilities are a subset
			if ((Capabilities & targetSchema.Capabilities) != Capabilities)
			{
				validationResults.Add(new ValidationResult(
					$"Schema capabilities ({Capabilities}) are not a subset of target capabilities ({targetSchema.Capabilities})"));
			}

			// Validate parameters are a subset
			foreach (var parameter in Parameters)
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
			foreach (var contentType in ContentTypes)
			{
				if (!targetSchema.ContentTypes.Contains(contentType))
				{
					validationResults.Add(new ValidationResult(
						$"Content type '{contentType}' is not supported by target schema"));
				}
			}

			// Validate authentication types are a subset
			foreach (var authType in AuthenticationTypes)
			{
				if (!targetSchema.AuthenticationTypes.Contains(authType))
				{
					validationResults.Add(new ValidationResult(
						$"Authentication type '{authType}' is not supported by target schema"));
				}
			}

			// Validate endpoints are a subset
			foreach (var endpoint in Endpoints)
			{
				var targetEndpoint = targetSchema.Endpoints.FirstOrDefault(e => e.Type == endpoint.Type);
				
				if (targetEndpoint == null)
				{
					validationResults.Add(new ValidationResult(
						$"Endpoint type '{endpoint.Type}' is not defined in target schema"));
				}
			}

			// Validate message properties are a subset
			foreach (var messageProperty in MessageProperties)
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
		/// Gets the logical identity of this schema as a string in the format "Provider/Type/Version".
		/// This can be used for comparison and identification purposes.
		/// </summary>
		/// <returns>A string representing the logical identity of the schema.</returns>
		public string GetLogicalIdentity()
		{
			return $"{ChannelProvider}/{ChannelType}/{Version}";
		}
	}
}