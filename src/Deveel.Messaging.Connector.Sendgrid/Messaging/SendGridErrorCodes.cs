//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides error codes specific to SendGrid connector operations.
    /// </summary>
    public static class SendGridErrorCodes
    {
        /// <summary>
        /// Error code indicating missing SendGrid API key.
        /// </summary>
        public const string MissingApiKey = "SENDGRID_MISSING_API_KEY";

        /// <summary>
        /// Error code indicating invalid SendGrid API key or authentication failure.
        /// </summary>
        public const string InvalidApiKey = "SENDGRID_INVALID_API_KEY";

        /// <summary>
        /// Error code indicating connection test failed.
        /// </summary>
        public const string ConnectionTestFailed = "SENDGRID_CONNECTION_TEST_FAILED";

        /// <summary>
        /// Error code indicating connection failed.
        /// </summary>
        public const string ConnectionFailed = "SENDGRID_CONNECTION_FAILED";

        /// <summary>
        /// Error code indicating invalid connection settings.
        /// </summary>
        public const string InvalidConnectionSettings = "SENDGRID_INVALID_CONNECTION_SETTINGS";

        /// <summary>
        /// Error code indicating message sending failed.
        /// </summary>
        public const string SendMessageFailed = "SENDGRID_SEND_MESSAGE_FAILED";

        /// <summary>
        /// Error code indicating invalid message format.
        /// </summary>
        public const string InvalidMessage = "SENDGRID_INVALID_MESSAGE";

        /// <summary>
        /// Error code indicating invalid email address.
        /// </summary>
        public const string InvalidEmailAddress = "SENDGRID_INVALID_EMAIL_ADDRESS";

        /// <summary>
        /// Error code indicating missing email content.
        /// </summary>
        public const string MissingEmailContent = "SENDGRID_MISSING_EMAIL_CONTENT";

        /// <summary>
        /// Error code indicating status query failed.
        /// </summary>
        public const string StatusQueryFailed = "SENDGRID_STATUS_QUERY_FAILED";

        /// <summary>
        /// Error code indicating general status error.
        /// </summary>
        public const string StatusError = "SENDGRID_STATUS_ERROR";

        /// <summary>
        /// Error code indicating API rate limit exceeded.
        /// </summary>
        public const string RateLimitExceeded = "SENDGRID_RATE_LIMIT_EXCEEDED";

        /// <summary>
        /// Error code indicating invalid recipient email address.
        /// </summary>
        public const string InvalidRecipient = "SENDGRID_INVALID_RECIPIENT";

        /// <summary>
        /// Error code indicating missing sender email address.
        /// </summary>
        public const string MissingSender = "SENDGRID_MISSING_SENDER";
    }
}