//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Extends AuthenticationConfiguration to support flexible validation where 
	/// at least one of multiple optional fields must be present (but not all).
	/// </summary>
	/// <remarks>
	/// This class is useful for authentication methods that accept multiple alternative 
	/// field names for the same logical credential (e.g., "ApiKey", "Key", or "AccessKey" 
	/// for the same API key parameter).
	/// </remarks>
	public class FlexibleAuthenticationConfiguration : AuthenticationConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FlexibleAuthenticationConfiguration"/> class.
		/// </summary>
		/// <param name="authenticationType">The type of authentication this configuration defines.</param>
		/// <param name="displayName">An optional display name for this authentication method.</param>
		/// <exception cref="ArgumentException">Thrown when authenticationType is None.</exception>
		public FlexibleAuthenticationConfiguration(AuthenticationType authenticationType, string? displayName = null)
			: base(authenticationType, displayName)
		{
		}

		/// <summary>
		/// Validates connection settings against this flexible authentication configuration.
		/// </summary>
		/// <param name="connectionSettings">The connection settings to validate.</param>
		/// <returns>A list of validation error messages. Empty if validation passes.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
		public override IList<string> Validate(ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			var errors = new List<string>();

			// Validate all required fields are present and valid (same as base class)
			foreach (var requiredField in RequiredFields)
			{
				var fieldErrors = requiredField.Validate(connectionSettings);
				errors.AddRange(fieldErrors);
			}

			// For optional fields, validate that at least one valid pair/combination is present
			if (OptionalFields.Any())
			{
				bool hasValidCombination = false;
				var combinationErrors = new List<string>();

				// For Basic authentication, check for valid field pairs
				if (AuthenticationType == AuthenticationType.Basic)
				{
					hasValidCombination = ValidateBasicAuthenticationCombinations(connectionSettings, combinationErrors);
				}
				else
				{
					// For other authentication types, just require at least one optional field
					foreach (var optionalField in OptionalFields)
					{
						var value = connectionSettings.GetParameter(optionalField.FieldName);
						if (value != null)
						{
							hasValidCombination = true;
							// Validate the field that is present
							var fieldErrors = optionalField.Validate(connectionSettings);
							combinationErrors.AddRange(fieldErrors);
							break; // Found one valid field, no need to check others
						}
					}
				}

				// If no valid combinations are present, this is an error
				if (!hasValidCombination)
				{
					if (AuthenticationType == AuthenticationType.Basic)
					{
						errors.Add("Basic authentication requires one of the following parameter pairs: " +
								  "(Username, Password), (AccountSid, AuthToken), (User, Pass), or (ClientId, ClientSecret)");
					}
					else
					{
						var fieldNames = string.Join(", ", OptionalFields.Select(f => f.FieldName));
						errors.Add($"At least one of the following fields must be provided: {fieldNames}");
					}
				}
				else
				{
					// Add any validation errors from the fields that were present
					errors.AddRange(combinationErrors);
				}
			}

			return errors;
		}

		/// <summary>
		/// Validates Basic authentication field combinations.
		/// </summary>
		/// <param name="connectionSettings">The connection settings to validate.</param>
		/// <param name="errors">List to collect validation errors.</param>
		/// <returns>True if at least one valid pair is found.</returns>
		private bool ValidateBasicAuthenticationCombinations(ConnectionSettings connectionSettings, List<string> errors)
		{
			// Check for valid pairs
			var validPairs = new[]
			{
				new[] { "Username", "Password" },
				new[] { "AccountSid", "AuthToken" },
				new[] { "User", "Pass" },
				new[] { "ClientId", "ClientSecret" }
			};

			foreach (var pair in validPairs)
			{
				var field1 = connectionSettings.GetParameter(pair[0]);
				var field2 = connectionSettings.GetParameter(pair[1]);

				if (field1 != null && field2 != null)
				{
					// Found a valid pair, validate both fields
					var field1Config = OptionalFields.FirstOrDefault(f => f.FieldName == pair[0]);
					var field2Config = OptionalFields.FirstOrDefault(f => f.FieldName == pair[1]);

					if (field1Config != null)
					{
						var field1Errors = field1Config.Validate(connectionSettings);
						errors.AddRange(field1Errors);
					}

					if (field2Config != null)
					{
						var field2Errors = field2Config.Validate(connectionSettings);
						errors.AddRange(field2Errors);
					}

					return true; // Found at least one valid pair
				}
			}

			return false; // No valid pairs found
		}

		/// <summary>
		/// Determines whether this flexible authentication configuration is satisfied by the given connection settings.
		/// </summary>
		/// <param name="connectionSettings">The connection settings to check.</param>
		/// <returns>True if all required fields are present and at least one optional field is present and valid; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
		public override bool IsSatisfiedBy(ConnectionSettings connectionSettings)
		{
			ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));

			// Check that all required fields are present and valid (same as base class)
			foreach (var requiredField in RequiredFields)
			{
				var errors = requiredField.Validate(connectionSettings);
				if (errors.Any())
				{
					return false;
				}
			}

			// For optional fields, check that at least one valid combination is present
			if (OptionalFields.Any())
			{
				if (AuthenticationType == AuthenticationType.Basic)
				{
					// For Basic authentication, check for valid field pairs
					var validPairs = new[]
					{
						new[] { "Username", "Password" },
						new[] { "AccountSid", "AuthToken" },
						new[] { "User", "Pass" },
						new[] { "ClientId", "ClientSecret" }
					};

					foreach (var pair in validPairs)
					{
						var field1 = connectionSettings.GetParameter(pair[0]);
						var field2 = connectionSettings.GetParameter(pair[1]);

						if (field1 != null && field2 != null)
						{
							// Check if both fields are valid
							var field1Config = OptionalFields.FirstOrDefault(f => f.FieldName == pair[0]);
							var field2Config = OptionalFields.FirstOrDefault(f => f.FieldName == pair[1]);

							var field1Valid = field1Config == null || !field1Config.Validate(connectionSettings).Any();
							var field2Valid = field2Config == null || !field2Config.Validate(connectionSettings).Any();

							if (field1Valid && field2Valid)
							{
								return true; // Found at least one valid pair
							}
						}
					}

					return false; // No valid pairs found
				}
				else
				{
					// For other authentication types, check that at least one optional field is valid
					foreach (var optionalField in OptionalFields)
					{
						var value = connectionSettings.GetParameter(optionalField.FieldName);
						if (value != null)
						{
							var errors = optionalField.Validate(connectionSettings);
							if (!errors.Any()) // If this field is present and valid, we're satisfied
							{
								return true;
							}
						}
					}
					
					// No valid optional field was found
					return false;
				}
			}

			// If there are no optional fields, we're satisfied if required fields are valid
			return true;
		}
	}
}