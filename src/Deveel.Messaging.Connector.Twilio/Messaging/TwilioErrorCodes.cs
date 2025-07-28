//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines error codes specific to Twilio SMS connector operations.
    /// </summary>
    /// <remarks>
    /// This class provides Twilio-specific error codes that complement the standard
    /// connector error codes defined in <see cref="ConnectorErrorCodes"/>. These codes
    /// are used to identify specific failure scenarios related to Twilio API integration.
    /// </remarks>
    public static class TwilioErrorCodes
    {
        #region Authentication and Credentials

        /// <summary>
        /// Indicates that required Twilio credentials (Account SID and Auth Token) are missing.
        /// </summary>
        /// <remarks>
        /// This error occurs during initialization when the connection settings do not
        /// contain valid Account SID and Auth Token parameters required for Twilio API access.
        /// </remarks>
        public const string MissingCredentials = "MISSING_CREDENTIALS";

        /// <summary>
        /// Indicates that connection settings validation failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the provided connection settings do not meet the
        /// requirements defined by the Twilio SMS channel schema, such as missing
        /// required parameters or invalid parameter values.
        /// </remarks>
        public const string InvalidConnectionSettings = "INVALID_CONNECTION_SETTINGS";

        #endregion

        #region Sender Configuration

        /// <summary>
        /// Indicates that a sender phone number is required but not provided.
        /// </summary>
        /// <remarks>
        /// This error occurs when no MessagingServiceSid is configured and the
        /// FromNumber parameter is missing or empty. At least one of these parameters
        /// must be provided to send SMS messages through Twilio.
        /// </remarks>
        public const string MissingFromNumber = "MISSING_FROM_NUMBER";

        #endregion

        #region Message Validation

        /// <summary>
        /// Indicates that the recipient phone number is invalid or missing.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message receiver endpoint does not contain
        /// a valid phone number in E.164 format, which is required for SMS delivery.
        /// </remarks>
        public const string InvalidRecipient = "INVALID_RECIPIENT";

        #endregion

        #region API Operations

        /// <summary>
        /// Indicates that the Twilio API connection test failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when attempting to test connectivity by fetching account
        /// information from the Twilio API fails, typically due to invalid credentials
        /// or network connectivity issues.
        /// </remarks>
        public const string ConnectionFailed = "CONNECTION_FAILED";

        /// <summary>
        /// Indicates that the connection test operation failed.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the connection
        /// testing process, preventing verification of Twilio API connectivity.
        /// </remarks>
        public const string ConnectionTestFailed = "CONNECTION_TEST_FAILED";

        /// <summary>
        /// Indicates that sending an SMS message through Twilio failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Twilio API call to send an SMS message fails,
        /// either due to API errors, invalid parameters, or service unavailability.
        /// </remarks>
        public const string SendMessageFailed = "SEND_MESSAGE_FAILED";

        /// <summary>
        /// Indicates that querying message status from Twilio failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when attempting to retrieve the status of a previously
        /// sent message from the Twilio API fails, typically due to invalid message
        /// SID or API connectivity issues.
        /// </remarks>
        public const string StatusQueryFailed = "STATUS_QUERY_FAILED";

        /// <summary>
        /// Indicates that retrieving connector status information failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when an exception is thrown while attempting to
        /// gather and return the current status information of the Twilio connector.
        /// </remarks>
        public const string StatusError = "STATUS_ERROR";

        #endregion
    }
}