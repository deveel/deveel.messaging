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
        /// Sender property is missing or empty. At least one of these parameters
        /// must be provided to send SMS messages through Twilio.
        /// </remarks>
        public const string MissingFromNumber = "MISSING_FROM_NUMBER";

        /// <summary>
        /// Indicates that the sender phone number is invalid.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message sender endpoint does not contain
        /// a valid phone number in E.164 format, which is required for SMS delivery.
        /// </remarks>
        public const string InvalidSender = "INVALID_SENDER";

        #endregion

        #region Message Validation

        /// <summary>
        /// Indicates that message validation failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message properties do not meet the
        /// requirements defined by the Twilio SMS channel schema, such as missing
        /// required properties or invalid property values.
        /// </remarks>
        public const string InvalidMessage = "INVALID_MESSAGE";

        /// <summary>
        /// Indicates that the recipient phone number is invalid or missing.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message receiver endpoint does not contain
        /// a valid phone number in E.164 format, which is required for SMS delivery.
        /// </remarks>
        public const string InvalidRecipient = "INVALID_RECIPIENT";

        #endregion

        #region WhatsApp Specific

        /// <summary>
        /// Indicates that a required WhatsApp template Content SID is missing.
        /// </summary>
        /// <remarks>
        /// This error occurs when attempting to send a WhatsApp template message
        /// without providing the required Twilio Content SID for the approved template.
        /// </remarks>
        public const string MissingContentSid = "MISSING_CONTENT_SID";

        /// <summary>
        /// Indicates that the WhatsApp phone number format is invalid.
        /// </summary>
        /// <remarks>
        /// This error occurs when the WhatsApp phone number does not follow the
        /// required format (whatsapp:+1234567890) or is not a verified WhatsApp Business number.
        /// </remarks>
        public const string InvalidWhatsAppNumber = "INVALID_WHATSAPP_NUMBER";

        /// <summary>
        /// Indicates that sending a WhatsApp message through Twilio failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Twilio API call to send a WhatsApp message fails,
        /// either due to API errors, invalid parameters, or WhatsApp-specific restrictions.
        /// </remarks>
        public const string SendWhatsAppMessageFailed = "SEND_WHATSAPP_MESSAGE_FAILED";

        /// <summary>
        /// Indicates that querying WhatsApp message status from Twilio failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when attempting to retrieve the status of a previously
        /// sent WhatsApp message from the Twilio API fails, typically due to invalid message
        /// SID or API connectivity issues.
        /// </remarks>
        public const string WhatsAppStatusQueryFailed = "WHATSAPP_STATUS_QUERY_FAILED";

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

        #region Message Receiving

        /// <summary>
        /// Indicates that receiving a message from Twilio webhook failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when processing an incoming message webhook from Twilio fails,
        /// typically due to malformed webhook data or processing errors.
        /// </remarks>
        public const string ReceiveMessageFailed = "RECEIVE_MESSAGE_FAILED";

        /// <summary>
        /// Indicates that receiving a message status update from Twilio webhook failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when processing a status callback webhook from Twilio fails,
        /// typically due to malformed callback data or processing errors.
        /// </remarks>
        public const string ReceiveStatusFailed = "RECEIVE_STATUS_FAILED";

        /// <summary>
        /// Indicates that the webhook data provided is invalid or malformed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the webhook payload from Twilio does not contain
        /// the expected fields or has invalid data that cannot be processed.
        /// </remarks>
        public const string InvalidWebhookData = "INVALID_WEBHOOK_DATA";

        /// <summary>
        /// Indicates that the content type for webhook data is not supported.
        /// </summary>
        /// <remarks>
        /// This error occurs when the webhook content type is neither form data nor JSON,
        /// which are the only supported formats for Twilio webhooks.
        /// </remarks>
        public const string UnsupportedContentType = "UNSUPPORTED_CONTENT_TYPE";

        #endregion
    }
}