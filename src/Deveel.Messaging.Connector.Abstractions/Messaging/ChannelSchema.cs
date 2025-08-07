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
			AuthenticationConfigurations = new List<AuthenticationConfiguration>();
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
			
			// Copy authentication configurations
			AuthenticationConfigurations = new List<AuthenticationConfiguration>();
			foreach (var authConfig in sourceSchema.AuthenticationConfigurations)
			{
				// Create a deep copy of the authentication configuration
				var newAuthConfig = new AuthenticationConfiguration(authConfig.AuthenticationType, authConfig.DisplayName);
				
				// Copy required fields
				foreach (var field in authConfig.RequiredFields)
				{
					var newField = new AuthenticationField(field.FieldName, field.DataType)
					{
						IsSensitive = field.IsSensitive,
						DisplayName = field.DisplayName,
						Description = field.Description,
						AuthenticationRole = field.AuthenticationRole,
						AllowedValues = field.AllowedValues?.ToList()
					};
					newAuthConfig.WithRequiredField(newField);
				}
				
				// Copy optional fields
				foreach (var field in authConfig.OptionalFields)
				{
					var newField = new AuthenticationField(field.FieldName, field.DataType)
					{
						IsSensitive = field.IsSensitive,
						DisplayName = field.DisplayName,
						Description = field.Description,
						AuthenticationRole = field.AuthenticationRole,
						AllowedValues = field.AllowedValues?.ToList()
					};
					newAuthConfig.WithOptionalField(newField);
				}
				
				AuthenticationConfigurations.Add(newAuthConfig);
			}
			
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
		public IList<AuthenticationConfiguration> AuthenticationConfigurations { get; }

		/// <inheritdoc/>
		public IList<ChannelEndpointConfiguration> Endpoints { get; }

		/// <summary>
		/// Gets the collection of authentication types supported by the channel.
		/// </summary>
		/// <remarks>
		/// This property provides backward compatibility by extracting authentication types 
		/// from the authentication configurations. For new implementations, use 
		/// <see cref="AuthenticationConfigurations"/> directly.
		/// </remarks>
		public IEnumerable<AuthenticationType> AuthenticationTypes => 
			AuthenticationConfigurations.Select(c => c.AuthenticationType).Distinct();

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
		/// <remarks>
		/// This method is maintained for backward compatibility. It creates a basic 
		/// authentication configuration for the specified type with default validation logic.
		/// For new implementations, use <see cref="AddAuthenticationConfiguration"/> to define 
		/// detailed authentication requirements with field mappings.
		/// </remarks>
		public ChannelSchema AddAuthenticationType(AuthenticationType authenticationType)
		{
			// Create a basic authentication configuration for backward compatibility
			var config = CreateBasicAuthenticationConfiguration(authenticationType);
			return AddAuthenticationConfiguration(config);
		}

		/// <summary>
		/// Creates a basic authentication configuration for backward compatibility.
		/// </summary>
		/// <param name="authenticationType">The authentication type to create a configuration for.</param>
		/// <returns>A basic authentication configuration.</returns>
		private static AuthenticationConfiguration CreateBasicAuthenticationConfiguration(AuthenticationType authenticationType)
		{
			return authenticationType switch
			{
				AuthenticationType.None => new AuthenticationConfiguration(AuthenticationType.None, "No Authentication"),
				AuthenticationType.Basic => Messaging.AuthenticationConfigurations.FlexibleBasicAuthentication(),
				AuthenticationType.ApiKey => Messaging.AuthenticationConfigurations.FlexibleApiKeyAuthentication(),
				AuthenticationType.Token => Messaging.AuthenticationConfigurations.FlexibleTokenAuthentication(),
				AuthenticationType.ClientCredentials => Messaging.AuthenticationConfigurations.ClientCredentialsAuthentication(),
				AuthenticationType.Certificate => Messaging.AuthenticationConfigurations.FlexibleCertificateAuthentication(),
				AuthenticationType.Custom => CreateFlexibleCustomAuthentication(),
				_ => throw new ArgumentException($"Unsupported authentication type: {authenticationType}", nameof(authenticationType))
			};
		}

		/// <summary>
		/// Creates a flexible custom authentication configuration that accepts common custom authentication parameters.
		/// </summary>
		/// <returns>A flexible custom authentication configuration.</returns>
		private static AuthenticationConfiguration CreateFlexibleCustomAuthentication()
		{
			var config = new FlexibleAuthenticationConfiguration(AuthenticationType.Custom, "Custom Authentication");

			// Add common custom authentication fields as optional
			config.WithOptionalField("CustomAuth", DataType.String, field =>
				{
					field.DisplayName = "Custom Auth";
					field.Description = "Custom authentication data";
					field.AuthenticationRole = "CustomAuth";
				})
				.WithOptionalField("AuthenticationData", DataType.String, field =>
				{
					field.DisplayName = "Authentication Data";
					field.Description = "Custom authentication data";
					field.AuthenticationRole = "AuthenticationData";
				})
				.WithOptionalField("Credentials", DataType.String, field =>
				{
					field.DisplayName = "Credentials";
					field.Description = "Custom credentials";
					field.AuthenticationRole = "Credentials";
				})
				.WithOptionalField("AuthConfig", DataType.String, field =>
				{
					field.DisplayName = "Auth Config";
					field.Description = "Authentication configuration";
					field.AuthenticationRole = "AuthConfig";
				})
				.WithOptionalField("SecretKey", DataType.String, field =>
				{
					field.DisplayName = "Secret Key";
					field.Description = "Secret key for authentication";
					field.AuthenticationRole = "SecretKey";
					field.IsSensitive = true;
				})
				.WithOptionalField("PrivateKey", DataType.String, field =>
				{
					field.DisplayName = "Private Key";
					field.Description = "Private key for authentication";
					field.AuthenticationRole = "PrivateKey";
					field.IsSensitive = true;
				})
				.WithOptionalField("Signature", DataType.String, field =>
				{
					field.DisplayName = "Signature";
					field.Description = "Authentication signature";
					field.AuthenticationRole = "Signature";
				})
				.WithOptionalField("Hash", DataType.String, field =>
				{
					field.DisplayName = "Hash";
					field.Description = "Authentication hash";
					field.AuthenticationRole = "Hash";
				});

			return config;
		}

		/// <summary>
		/// Adds an authentication configuration that defines detailed authentication requirements.
		/// </summary>
		/// <param name="authenticationConfiguration">The authentication configuration to add.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when authenticationConfiguration is null.</exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when an authentication configuration for the same authentication type already exists.
		/// </exception>
		public ChannelSchema AddAuthenticationConfiguration(AuthenticationConfiguration authenticationConfiguration)
		{
			ArgumentNullException.ThrowIfNull(authenticationConfiguration, nameof(authenticationConfiguration));
			
			if (AuthenticationConfigurations.Any(c => c.AuthenticationType == authenticationConfiguration.AuthenticationType))
			{
				throw new InvalidOperationException($"An authentication configuration for '{authenticationConfiguration.AuthenticationType}' authentication type already exists in the schema.");
			}
			
			AuthenticationConfigurations.Add(authenticationConfiguration);
			return this;
		}

		/// <summary>
		/// Adds an authentication configuration using a factory method.
		/// </summary>
		/// <param name="configurationFactory">A factory method that creates the authentication configuration.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when configurationFactory is null.</exception>
		public ChannelSchema AddAuthenticationConfiguration(Func<AuthenticationConfiguration> configurationFactory)
		{
			ArgumentNullException.ThrowIfNull(configurationFactory, nameof(configurationFactory));
			var config = configurationFactory();
			return AddAuthenticationConfiguration(config);
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
			var configToRemove = AuthenticationConfigurations.FirstOrDefault(c => c.AuthenticationType == authenticationType);
			if (configToRemove != null)
			{
				AuthenticationConfigurations.Remove(configToRemove);
			}
			
			return this;
		}

		/// <summary>
		/// Removes an authentication configuration from the schema.
		/// This is useful when deriving schemas to restrict certain authentication methods.
		/// </summary>
		/// <param name="authenticationType">The authentication type of the configuration to remove.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		public ChannelSchema RemoveAuthenticationConfiguration(AuthenticationType authenticationType)
		{
			var configToRemove = AuthenticationConfigurations.FirstOrDefault(c => c.AuthenticationType == authenticationType);
			if (configToRemove != null)
			{
				AuthenticationConfigurations.Remove(configToRemove);
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
			
			// Remove authentication configurations that are not in the allowed list
			var configurationsToRemove = AuthenticationConfigurations
				.Where(c => !allowedAuthenticationTypes.Contains(c.AuthenticationType))
				.ToList();
			
			foreach (var config in configurationsToRemove)
			{
				AuthenticationConfigurations.Remove(config);
			}
			
			return this;
		}

		/// <summary>
		/// Clears all authentication configurations and adds only the specified ones.
		/// This is useful when deriving schemas to restrict authentication methods.
		/// </summary>
		/// <param name="allowedConfigurations">The authentication configurations to allow.</param>
		/// <returns>The current schema instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when allowedConfigurations is null.
		/// </exception>
		public ChannelSchema RestrictAuthenticationConfigurations(params AuthenticationConfiguration[] allowedConfigurations)
		{
			ArgumentNullException.ThrowIfNull(allowedConfigurations, nameof(allowedConfigurations));
			
			AuthenticationConfigurations.Clear();
			
			foreach (var config in allowedConfigurations)
			{
				AuthenticationConfigurations.Add(config);
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
	}
}