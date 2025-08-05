//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides the configuration settings for a property contained within a message.
	/// </summary>
	public sealed class MessagePropertyConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessagePropertyConfiguration"/> class 
		/// with the specified property name.
		/// </summary>
		/// <param name="name">The name of the message property.</param>
		/// <param name="dataType">The type of data for the message property.</param>
		public MessagePropertyConfiguration(string name, DataType dataType)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
			Name = name;
			DataType = dataType;
		}

		/// <summary>
		/// Gets the of the message property to which this 
		/// configuration applies.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		public DataType DataType { get; }

		/// <summary>
		/// Gets or sets the display name of the property.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of the property, which 
		/// provides additional context or information.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets whether the property is required.
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the data 
		/// is considered sensitive.
		/// </summary>
		/// <remarks>
		/// A sensitive property typically contains information
		/// that should be handled with care, such as personal
		/// identification numbers or financial data.
		/// </remarks>
		public bool IsSensitive { get; set; }

		/// <summary>
		/// Gets or sets the minimum length for string properties.
		/// Null means no minimum length restriction.
		/// </summary>
		public int? MinLength { get; set; }

		/// <summary>
		/// Gets or sets the maximum length for string properties.
		/// Null means no maximum length restriction.
		/// </summary>
		public int? MaxLength { get; set; }

		/// <summary>
		/// Gets or sets the minimum value for numeric properties.
		/// Null means no minimum value restriction.
		/// </summary>
		public object? MinValue { get; set; }

		/// <summary>
		/// Gets or sets the maximum value for numeric properties.
		/// Null means no maximum value restriction.
		/// </summary>
		public object? MaxValue { get; set; }

		/// <summary>
		/// Gets or sets a regular expression pattern that string values must match.
		/// Null means no pattern validation.
		/// </summary>
		public string? Pattern { get; set; }

		/// <summary>
		/// Gets or sets the collection of allowed values for this property.
		/// Null or empty means any value is allowed (subject to other constraints).
		/// </summary>
		public ICollection<object>? AllowedValues { get; set; }

		/// <summary>
		/// Gets or sets a custom validation function that can perform complex validation logic.
		/// The function receives the property value and should return validation errors if any.
		/// </summary>
		public Func<object?, IEnumerable<ValidationResult>>? CustomValidator { get; set; }

		/// <summary>
		/// Validates the specified value against this property configuration.
		/// </summary>
		/// <param name="value">The value to validate.</param>
		/// <returns>A collection of validation results. Empty if validation passes.</returns>
		public IEnumerable<ValidationResult> Validate(object? value)
		{
			var validationResults = new List<ValidationResult>();

			// Check if value is required
			if (IsRequired && value == null)
			{
				validationResults.Add(new ValidationResult(
					$"Required message property '{Name}' is missing.",
					new[] { Name }));
				return validationResults; // Early return for required validation
			}

			// Skip further validation if value is null and not required
			if (value == null)
				return validationResults;

			// Special case: If custom validator exists, run it first and determine if type validation should be skipped
			if (CustomValidator != null)
			{
				var customResults = CustomValidator(value);
				validationResults.AddRange(customResults);
				
				// For flexible properties that accept multiple types (like SendAt accepting DateTime or string),
				// skip strict type validation if the custom validator handled the value without type-related errors
				if (DataType == DataType.String && value is DateTime)
				{
					// Skip type validation for DateTime on String properties when custom validator is present
					// The custom validator is responsible for handling the type conversion
					return validationResults;
				}
			}

			// Validate type compatibility
			if (!IsTypeCompatible(DataType, value))
			{
				validationResults.Add(new ValidationResult(
					$"Message property '{Name}' has an incompatible type. Expected: {DataType}, Actual: {value.GetType().Name}.",
					new[] { Name }));
				return validationResults; // Early return for type validation
			}

			// Validate string-specific constraints
			if (DataType == DataType.String && value is string stringValue)
			{
				ValidateStringConstraints(stringValue, validationResults);
			}

			// Validate numeric constraints
			if ((DataType == DataType.Integer || DataType == DataType.Number) && IsNumeric(value))
			{
				ValidateNumericConstraints(value, validationResults);
			}

			// Validate allowed values
			if (AllowedValues != null && AllowedValues.Any())
			{
				if (!AllowedValues.Any(allowedValue => Equals(allowedValue, value)))
				{
					var allowedValuesStr = string.Join(", ", AllowedValues.Select(v => v?.ToString() ?? "null"));
					validationResults.Add(new ValidationResult(
						$"Message property '{Name}' has an invalid value '{value}'. Allowed values: [{allowedValuesStr}].",
						new[] { Name }));
				}
			}

			return validationResults;
		}

		private void ValidateStringConstraints(string stringValue, List<ValidationResult> validationResults)
		{
			// Check minimum length
			if (MinLength.HasValue && stringValue.Length < MinLength.Value)
			{
				validationResults.Add(new ValidationResult(
					$"Message property '{Name}' must be at least {MinLength.Value} characters long.",
					new[] { Name }));
			}

			// Check maximum length
			if (MaxLength.HasValue && stringValue.Length > MaxLength.Value)
			{
				validationResults.Add(new ValidationResult(
					$"Message property '{Name}' cannot exceed {MaxLength.Value} characters.",
					new[] { Name }));
			}

			// Check pattern
			if (!string.IsNullOrEmpty(Pattern))
			{
				if (!System.Text.RegularExpressions.Regex.IsMatch(stringValue, Pattern))
				{
					validationResults.Add(new ValidationResult(
						$"Message property '{Name}' does not match the required pattern.",
						new[] { Name }));
				}
			}

			// Check for empty string if not allowed
			if (MinLength.HasValue && MinLength.Value > 0 && string.IsNullOrWhiteSpace(stringValue))
			{
				validationResults.Add(new ValidationResult(
					$"Message property '{Name}' cannot be empty.",
					new[] { Name }));
			}
		}

		private void ValidateNumericConstraints(object value, List<ValidationResult> validationResults)
		{
			var numericValue = Convert.ToDouble(value);

			// Check minimum value
			if (MinValue != null)
			{
				var minNumericValue = Convert.ToDouble(MinValue);
				if (numericValue < minNumericValue)
				{
					validationResults.Add(new ValidationResult(
						$"Message property '{Name}' must be at least {MinValue}.",
						new[] { Name }));
				}
			}

			// Check maximum value
			if (MaxValue != null)
			{
				var maxNumericValue = Convert.ToDouble(MaxValue);
				if (numericValue > maxNumericValue)
				{
					validationResults.Add(new ValidationResult(
						$"Message property '{Name}' cannot exceed {MaxValue}.",
						new[] { Name }));
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

		private static bool IsNumeric(object value)
		{
			return value is int || value is long || value is byte || value is short || value is sbyte ||
				   value is double || value is decimal || value is float;
		}
	}
}
