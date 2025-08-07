//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Defines the authentication configuration for a channel, specifying 
	/// which connection settings fields are required for a specific authentication type.
	/// </summary>
	/// <remarks>
	/// This class allows channel schemas to define precise mappings between 
	/// connection settings parameters and authentication requirements, providing 
	/// more flexible and explicit authentication validation than the generic approach.
	/// </remarks>
	public class AuthenticationConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationConfiguration"/> class.
		/// </summary>
		/// <param name="authenticationType">The type of authentication this configuration defines.</param>
		/// <param name="displayName">An optional display name for this authentication method.</param>
		public AuthenticationConfiguration(AuthenticationType authenticationType, string? displayName = null)
		{
			AuthenticationType = authenticationType;
			DisplayName = displayName ?? (authenticationType == AuthenticationType.None ? "No Authentication" : authenticationType.ToString());
			RequiredFields = new List<AuthenticationField>();
			OptionalFields = new List<AuthenticationField>();
		}

		/// <summary>
		/// Gets the authentication type this configuration defines.
		/// </summary>
		public AuthenticationType AuthenticationType { get; }

		/// <summary>
		/// Gets the display name for this authentication method.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Gets the collection of required authentication fields.
		/// </summary>
		/// <remarks>
		/// All required fields must be present in connection settings for this 
		/// authentication method to be considered valid.
		/// </remarks>
		public IList<AuthenticationField> RequiredFields { get; }

		/// <summary>
		/// Gets the collection of optional authentication fields.
		/// </summary>
		/// <remarks>
		/// Optional fields can be used to provide additional authentication 
		/// parameters but are not required for basic authentication validation.
		/// </remarks>
		public IList<AuthenticationField> OptionalFields { get; }

		/// <summary>
		/// Adds a required authentication field to this configuration.
		/// </summary>
		/// <param name="field">The authentication field to add as required.</param>
		/// <returns>The current authentication configuration for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when field is null.</exception>
		public AuthenticationConfiguration WithRequiredField(AuthenticationField field)
		{
			ArgumentNullException.ThrowIfNull(field, nameof(field));
			RequiredFields.Add(field);
			return this;
		}

		/// <summary>
		/// Adds a required authentication field with the specified name and data type.
		/// </summary>
		/// <param name="fieldName">The name of the connection settings parameter.</param>
		/// <param name="dataType">The expected data type of the field.</param>
		/// <param name="configure">Optional action to configure additional field properties.</param>
		/// <returns>The current authentication configuration for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when fieldName is null or whitespace.</exception>
		public AuthenticationConfiguration WithRequiredField(string fieldName, DataType dataType, Action<AuthenticationField>? configure = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
			var field = new AuthenticationField(fieldName, dataType);
			configure?.Invoke(field);
			return WithRequiredField(field);
		}

		/// <summary>
		/// Adds an optional authentication field to this configuration.
		/// </summary>
		/// <param name="field">The authentication field to add as optional.</param>
		/// <returns>The current authentication configuration for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when field is null.</exception>
		public AuthenticationConfiguration WithOptionalField(AuthenticationField field)
		{
			ArgumentNullException.ThrowIfNull(field, nameof(field));
			OptionalFields.Add(field);
			return this;
		}

		/// <summary>
		/// Adds an optional authentication field with the specified name and data type.
		/// </summary>
		/// <param name="fieldName">The name of the connection settings parameter.</param>
		/// <param name="dataType">The expected data type of the field.</param>
		/// <param name="configure">Optional action to configure additional field properties.</param>
		/// <returns>The current authentication configuration for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when fieldName is null or whitespace.</exception>
		public AuthenticationConfiguration WithOptionalField(string fieldName, DataType dataType, Action<AuthenticationField>? configure = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
			var field = new AuthenticationField(fieldName, dataType);
			configure?.Invoke(field);
			return WithOptionalField(field);
		}

		/// <summary>
		/// Validates connection settings against this authentication configuration.
		/// </summary>
		/// <param name="connectionSettings">The connection settings to validate.</param>
		/// <returns>A list of validation error messages. Empty if validation passes.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
		public virtual IList<string> Validate(ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			var errors = new List<string>();

			// Validate all required fields are present and valid
			foreach (var requiredField in RequiredFields)
			{
				var fieldErrors = requiredField.Validate(connectionSettings);
				errors.AddRange(fieldErrors);
			}

			// Validate optional fields if they are present
			foreach (var optionalField in OptionalFields)
			{
				var value = connectionSettings.GetParameter(optionalField.FieldName);
				if (value != null)
				{
					var fieldErrors = optionalField.Validate(connectionSettings);
					errors.AddRange(fieldErrors);
				}
			}

			return errors;
		}

		/// <summary>
		/// Determines whether this authentication configuration is satisfied by the given connection settings.
		/// </summary>
		/// <param name="connectionSettings">The connection settings to check.</param>
		/// <returns>True if all required fields are present and valid; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
		public virtual bool IsSatisfiedBy(ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			// Check that all required fields are present and valid
			foreach (var requiredField in RequiredFields)
			{
				var errors = requiredField.Validate(connectionSettings);
				if (errors.Any())
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets all field names (both required and optional) defined in this authentication configuration.
		/// </summary>
		/// <returns>An enumerable of all field names.</returns>
		public IEnumerable<string> GetAllFieldNames()
		{
			return RequiredFields.Select(f => f.FieldName)
				.Concat(OptionalFields.Select(f => f.FieldName));
		}
	}
}