//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents an authentication field that maps a connection settings parameter 
	/// to a specific authentication requirement.
	/// </summary>
	/// <remarks>
	/// This class defines how a connection settings parameter should be used for 
	/// authentication, including its expected data type, validation rules, and role 
	/// in the authentication process.
	/// </remarks>
	public class AuthenticationField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationField"/> class.
		/// </summary>
		/// <param name="fieldName">The name of the connection settings parameter.</param>
		/// <param name="dataType">The expected data type of the field value.</param>
		/// <exception cref="ArgumentNullException">Thrown when fieldName is null or whitespace.</exception>
		public AuthenticationField(string fieldName, DataType dataType)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
			FieldName = fieldName;
			DataType = dataType;
			IsSensitive = false;
			AllowedValues = null;
		}

		/// <summary>
		/// Gets the name of the connection settings parameter this field represents.
		/// </summary>
		public string FieldName { get; }

		/// <summary>
		/// Gets the expected data type of the field value.
		/// </summary>
		public DataType DataType { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this field contains sensitive information.
		/// </summary>
		/// <remarks>
		/// Sensitive fields (like passwords, tokens, or secrets) should be handled 
		/// with special care for logging, storage, and display purposes.
		/// </remarks>
		public bool IsSensitive { get; set; }

		/// <summary>
		/// Gets or sets the display name for this authentication field.
		/// </summary>
		/// <remarks>
		/// If not specified, the field name will be used as the display name.
		/// </remarks>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of this authentication field.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the collection of allowed values for this field.
		/// </summary>
		/// <remarks>
		/// If specified, the field value must be one of the allowed values.
		/// If null or empty, any value of the correct data type is allowed.
		/// </remarks>
		public IList<object>? AllowedValues { get; set; }

		/// <summary>
		/// Gets or sets the authentication role this field plays.
		/// </summary>
		/// <remarks>
		/// This provides semantic meaning to help with authentication processing.
		/// For example, a field might be a "Username", "Password", "Token", etc.
		/// </remarks>
		public string? AuthenticationRole { get; set; }

		/// <summary>
		/// Validates the field value from connection settings.
		/// </summary>
		/// <param name="connectionSettings">The connection settings to validate against.</param>
		/// <returns>A list of validation error messages. Empty if validation passes.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
		public IList<string> Validate(ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			var errors = new List<string>();
			var value = connectionSettings.GetParameter(FieldName);

			// Check if value is present
			if (value == null)
			{
				errors.Add($"Required authentication field '{FieldName}' is missing.");
				return errors;
			}

			// Validate data type
			if (!IsTypeCompatible(DataType, value))
			{
				errors.Add($"Authentication field '{FieldName}' has an incompatible type. Expected: {DataType}, Actual: {value.GetType().Name}.");
			}

			// Validate allowed values if specified
			if (AllowedValues?.Any() == true)
			{
				if (!AllowedValues.Any(allowedValue => Equals(allowedValue, value)))
				{
					var allowedValuesStr = string.Join(", ", AllowedValues.Select(v => v?.ToString() ?? "null"));
					errors.Add($"Authentication field '{FieldName}' has an invalid value '{value}'. Allowed values: [{allowedValuesStr}].");
				}
			}

			return errors;
		}

		/// <summary>
		/// Determines whether the specified value is compatible with this field's data type.
		/// </summary>
		/// <param name="dataType">The expected data type.</param>
		/// <param name="value">The value to check.</param>
		/// <returns>True if the value is compatible with the data type; otherwise, false.</returns>
		private static bool IsTypeCompatible(DataType dataType, object value)
		{
			return dataType switch
			{
				DataType.Boolean => value is bool,
				DataType.String => value is string,
				DataType.Integer => value is int or long or byte or short or sbyte,
				DataType.Number => value is double or decimal or float or int or long or byte or short or sbyte,
				_ => false,
			};
		}

		/// <summary>
		/// Returns a string representation of this authentication field.
		/// </summary>
		/// <returns>A string containing the field name and data type.</returns>
		public override string ToString()
		{
			var displayText = DisplayName ?? FieldName;
			var role = AuthenticationRole != null ? $" ({AuthenticationRole})" : "";
			return $"{displayText}: {DataType}{role}";
		}
	}
}