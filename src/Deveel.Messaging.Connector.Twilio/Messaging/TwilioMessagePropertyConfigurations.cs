//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides enhanced message property configurations for Twilio messaging services.
    /// </summary>
    public static class TwilioMessagePropertyConfigurations
    {
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
                Description = description
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
                Description = description
            };
        }

        /// <summary>
        /// Validates that a phone number is in E.164 format.
        /// </summary>
        /// <param name="value">The phone number to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>A ValidationResult if validation fails; null if validation passes.</returns>
        public static ValidationResult? ValidatePhoneNumber(object? value, string propertyName)
        {
            if (value == null)
                return null; // Required validation is handled separately

            var phoneNumber = value.ToString();
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null; // Required validation is handled separately

            // E.164 format validation: should start with + followed by 1-15 digits
            var e164Pattern = @"^\+[1-9]\d{1,14}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, e164Pattern))
            {
                return new ValidationResult(
                    $"Property '{propertyName}' must be a valid phone number in E.164 format (e.g., +1234567890).",
                    new[] { propertyName });
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates that a WhatsApp phone number is in the correct format.
        /// </summary>
        /// <param name="value">The WhatsApp phone number to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>A ValidationResult if validation fails; null if validation passes.</returns>
        public static ValidationResult? ValidateWhatsAppPhoneNumber(object? value, string propertyName)
        {
            if (value == null)
                return null; // Required validation is handled separately

            var phoneNumber = value.ToString();
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null; // Required validation is handled separately

            // WhatsApp format validation: should start with "whatsapp:" followed by E.164 format
            var whatsAppPattern = @"^whatsapp:\+[1-9]\d{1,14}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, whatsAppPattern))
            {
                return new ValidationResult(
                    $"Property '{propertyName}' must be a valid WhatsApp phone number in format 'whatsapp:+1234567890'.",
                    new[] { propertyName });
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates message properties for Twilio SMS using the Twilio-specific validation rules.
        /// </summary>
        /// <param name="messageProperties">The message properties to validate.</param>
        /// <returns>An enumerable of validation results for any failures.</returns>
        public static IEnumerable<ValidationResult> ValidateTwilioSmsProperties(IDictionary<string, object?> messageProperties)
        {
            var validationResults = new List<ValidationResult>();

            // Note: Sender and To validation is now handled at the endpoint level (message.Sender/message.Receiver)
            // rather than as message properties. This method now validates only channel-specific message properties.

            // Validate other Twilio-specific properties as needed
            // For now, return empty as the main validation is done through the schema

            return validationResults;
        }

        /// <summary>
        /// Validates message properties for Twilio WhatsApp using the WhatsApp-specific validation rules.
        /// </summary>
        /// <param name="messageProperties">The message properties to validate.</param>
        /// <returns>An enumerable of validation results for any failures.</returns>
        public static IEnumerable<ValidationResult> ValidateTwilioWhatsAppProperties(IDictionary<string, object?> messageProperties)
        {
            var validationResults = new List<ValidationResult>();

            // Note: Sender and To validation is now handled at the endpoint level (message.Sender/message.Receiver)
            // rather than as message properties. This method now validates only channel-specific message properties.

            // Validate other WhatsApp-specific properties as needed
            // For now, return empty as the main validation is done through the schema

            return validationResults;
        }
    }
}