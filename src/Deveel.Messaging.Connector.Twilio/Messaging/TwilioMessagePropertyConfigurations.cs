//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides enhanced message property configurations for Twilio messaging services.
    /// </summary>
    public static class TwilioMessagePropertyConfigurations
    {
        /// <summary>
        /// E.164 format regular expression pattern for phone numbers.
        /// </summary>
        public static readonly string E164Pattern = @"^\+[1-9]\d{1,14}$";

        /// <summary>
        /// WhatsApp phone number format regular expression pattern.
        /// </summary>
        public static readonly string WhatsAppPattern = @"^whatsapp:\+[1-9]\d{1,14}$";

        /// <summary>
        /// Creates a phone number message property configuration with E.164 format validation.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="isRequired">Whether the property is required.</param>
        /// <param name="description">The description of the property.</param>
        /// <returns>A configured MessagePropertyConfiguration with phone number validation.</returns>
        public static MessagePropertyConfiguration PhoneNumber(string name, bool isRequired, string description)
        {
            return new MessagePropertyConfiguration(name, DataType.String)
            {
                IsRequired = isRequired,
                Description = description,
                Pattern = E164Pattern,
                CustomValidator = value => ValidatePhoneNumberCustom(value, name)
            };
        }

        /// <summary>
        /// Creates a WhatsApp phone number message property configuration.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="isRequired">Whether the property is required.</param>
        /// <param name="description">The description of the property.</param>
        /// <returns>A configured MessagePropertyConfiguration for WhatsApp phone numbers.</returns>
        public static MessagePropertyConfiguration WhatsAppPhoneNumber(string name, bool isRequired, string description)
        {
            return new MessagePropertyConfiguration(name, DataType.String)
            {
                IsRequired = isRequired,
                Description = description,
                Pattern = WhatsAppPattern,
                CustomValidator = value => ValidateWhatsAppPhoneNumberCustom(value, name)
            };
        }

        /// <summary>
        /// Custom validator for phone numbers that provides more detailed error messages than pattern matching alone.
        /// </summary>
        /// <param name="value">The phone number to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>A collection of validation results for any failures.</returns>
        private static IEnumerable<ValidationResult> ValidatePhoneNumberCustom(object? value, string propertyName)
        {
            if (value == null)
                yield break; // Required validation is handled separately

            var phoneNumber = value.ToString();
            if (string.IsNullOrWhiteSpace(phoneNumber))
                yield break; // Required validation is handled separately

            // E.164 format validation with detailed error messages
            if (!Regex.IsMatch(phoneNumber, E164Pattern))
            {
                if (!phoneNumber.StartsWith("+"))
                {
                    yield return new ValidationResult(
                        $"Property '{propertyName}' must start with '+' for E.164 format (e.g., +1234567890).",
                        new[] { propertyName });
                }
                else if (phoneNumber.Length < 2 || phoneNumber.Length > 16)
                {
                    yield return new ValidationResult(
                        $"Property '{propertyName}' must be between 2 and 16 characters long in E.164 format.",
                        new[] { propertyName });
                }
                else if (!phoneNumber.Substring(1).All(char.IsDigit))
                {
                    yield return new ValidationResult(
                        $"Property '{propertyName}' must contain only digits after the '+' sign for E.164 format.",
                        new[] { propertyName });
                }
                else if (phoneNumber[1] == '0')
                {
                    yield return new ValidationResult(
                        $"Property '{propertyName}' cannot start with '+0' in E.164 format.",
                        new[] { propertyName });
                }
                else
                {
                    yield return new ValidationResult(
                        $"Property '{propertyName}' must be a valid phone number in E.164 format (e.g., +1234567890).",
                        new[] { propertyName });
                }
            }
        }

        /// <summary>
        /// Custom validator for WhatsApp phone numbers that provides more detailed error messages than pattern matching alone.
        /// </summary>
        /// <param name="value">The WhatsApp phone number to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>A collection of validation results for any failures.</returns>
        private static IEnumerable<ValidationResult> ValidateWhatsAppPhoneNumberCustom(object? value, string propertyName)
        {
            if (value == null)
                yield break; // Required validation is handled separately

            var phoneNumber = value.ToString();
            if (string.IsNullOrWhiteSpace(phoneNumber))
                yield break; // Required validation is handled separately

            // WhatsApp format validation with detailed error messages
            if (!Regex.IsMatch(phoneNumber, WhatsAppPattern))
            {
                if (!phoneNumber.StartsWith("whatsapp:"))
                {
                    yield return new ValidationResult(
                        $"Property '{propertyName}' must start with 'whatsapp:' for WhatsApp format (e.g., whatsapp:+1234567890).",
                        new[] { propertyName });
                }
                else
                {
                    var phoneNumberPart = phoneNumber.Substring(9); // Remove "whatsapp:" prefix
                    if (!phoneNumberPart.StartsWith("+"))
                    {
                        yield return new ValidationResult(
                            $"Property '{propertyName}' must have phone number in E.164 format after 'whatsapp:' prefix (e.g., whatsapp:+1234567890).",
                            new[] { propertyName });
                    }
                    else if (phoneNumberPart.Length < 2 || phoneNumberPart.Length > 16)
                    {
                        yield return new ValidationResult(
                            $"Property '{propertyName}' phone number part must be between 2 and 16 characters long in E.164 format.",
                            new[] { propertyName });
                    }
                    else if (!phoneNumberPart.Substring(1).All(char.IsDigit))
                    {
                        yield return new ValidationResult(
                            $"Property '{propertyName}' phone number part must contain only digits after the '+' sign.",
                            new[] { propertyName });
                    }
                    else if (phoneNumberPart[1] == '0')
                    {
                        yield return new ValidationResult(
                            $"Property '{propertyName}' phone number part cannot start with '+0' in E.164 format.",
                            new[] { propertyName });
                    }
                    else
                    {
                        yield return new ValidationResult(
                            $"Property '{propertyName}' must be a valid WhatsApp phone number in format 'whatsapp:+1234567890'.",
                            new[] { propertyName });
                    }
                }
            }
        }

        /// <summary>
        /// Validates that a phone number is in E.164 format.
        /// </summary>
        /// <param name="value">The phone number to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>A ValidationResult if validation fails; null if validation passes.</returns>
        [Obsolete("Use MessagePropertyConfiguration with Pattern and CustomValidator properties instead. This method is kept for backward compatibility.")]
        public static ValidationResult? ValidatePhoneNumber(object? value, string propertyName)
        {
            var results = ValidatePhoneNumberCustom(value, propertyName).ToList();
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Validates that a WhatsApp phone number is in the correct format.
        /// </summary>
        /// <param name="value">The WhatsApp phone number to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>A ValidationResult if validation fails; null if validation passes.</returns>
        [Obsolete("Use MessagePropertyConfiguration with Pattern and CustomValidator properties instead. This method is kept for backward compatibility.")]
        public static ValidationResult? ValidateWhatsAppPhoneNumber(object? value, string propertyName)
        {
            var results = ValidateWhatsAppPhoneNumberCustom(value, propertyName).ToList();
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Validates message properties for Twilio SMS using the Twilio-specific validation rules.
        /// </summary>
        /// <param name="messageProperties">The message properties to validate.</param>
        /// <returns>An enumerable of validation results for any failures.</returns>
        [Obsolete("Use ChannelSchema.ValidateMessageProperties() instead. Validation is now handled through MessagePropertyConfiguration. This method is kept for backward compatibility.")]
        public static IEnumerable<ValidationResult> ValidateTwilioSmsProperties(IDictionary<string, object?> messageProperties)
        {
            // Note: This method is now obsolete as validation should be done through the schema's MessagePropertyConfiguration
            // which supports Pattern and CustomValidator properties. The individual property validations are now
            // handled automatically when the schema validates the message properties.
            return Enumerable.Empty<ValidationResult>();
        }

        /// <summary>
        /// Validates message properties for Twilio WhatsApp using the WhatsApp-specific validation rules.
        /// </summary>
        /// <param name="messageProperties">The message properties to validate.</param>
        /// <returns>An enumerable of validation results for any failures.</returns>
        [Obsolete("Use ChannelSchema.ValidateMessageProperties() instead. Validation is now handled through MessagePropertyConfiguration. This method is kept for backward compatibility.")]
        public static IEnumerable<ValidationResult> ValidateTwilioWhatsAppProperties(IDictionary<string, object?> messageProperties)
        {
            // Note: This method is now obsolete as validation should be done through the schema's MessagePropertyConfiguration
            // which supports Pattern and CustomValidator properties. The individual property validations are now
            // handled automatically when the schema validates the message properties.
            return Enumerable.Empty<ValidationResult>();
        }
    }
}