//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines error codes specific to Facebook Messenger connector operations.
    /// </summary>
    /// <remarks>
    /// This class provides Facebook-specific error codes that complement the standard
    /// connector error codes defined in <see cref="ConnectorErrorCodes"/>. These codes
    /// are used to identify specific failure scenarios related to Facebook Graph API integration.
    /// </remarks>
    public static class FacebookErrorCodes
    {
        #region Authentication and Credentials

        /// <summary>
        /// Indicates that required Facebook credentials (Access Token) are missing.
        /// </summary>
        /// <remarks>
        /// This error occurs during initialization when the connection settings do not
        /// contain a valid Page Access Token required for Facebook Graph API access.
        /// </remarks>
        public const string MissingCredentials = "MISSING_CREDENTIALS";

        /// <summary>
        /// Indicates that connection settings validation failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the provided connection settings do not meet the
        /// requirements defined by the Facebook Messenger channel schema, such as missing
        /// required parameters or invalid parameter values.
        /// </remarks>
        public const string InvalidConnectionSettings = "INVALID_CONNECTION_SETTINGS";

        /// <summary>
        /// Indicates that the provided access token is invalid or expired.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Facebook Graph API returns an authentication error,
        /// typically due to an expired or invalid Page Access Token.
        /// </remarks>
        public const string InvalidAccessToken = "INVALID_ACCESS_TOKEN";

        #endregion

        #region Page Configuration

        /// <summary>
        /// Indicates that a Page ID is required but not provided.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Page ID is not configured in the connection settings,
        /// which is required to send messages through the Facebook Messenger platform.
        /// </remarks>
        public const string MissingPageId = "MISSING_PAGE_ID";

        /// <summary>
        /// Indicates that the configured Page ID is invalid.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Page ID does not exist or the access token
        /// does not have permission to access the specified Facebook Page.
        /// </remarks>
        public const string InvalidPageId = "INVALID_PAGE_ID";

        #endregion

        #region Message Validation

        /// <summary>
        /// Indicates that message validation failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message properties do not meet the
        /// requirements defined by the Facebook Messenger channel schema, such as missing
        /// required properties or invalid property values.
        /// </remarks>
        public const string InvalidMessage = "INVALID_MESSAGE";

        /// <summary>
        /// Indicates that the recipient User ID is invalid or missing.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message receiver endpoint does not contain
        /// a valid Facebook User ID (PSID), which is required for message delivery.
        /// </remarks>
        public const string InvalidRecipient = "INVALID_RECIPIENT";

        /// <summary>
        /// Indicates that the message content is too long.
        /// </summary>
        /// <remarks>
        /// This error occurs when the message text exceeds Facebook's limits,
        /// which is typically 2000 characters for text messages.
        /// </remarks>
        public const string MessageTooLong = "MESSAGE_TOO_LONG";

        #endregion

        #region API Operations

        /// <summary>
        /// Indicates that the Facebook Graph API connection test failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when attempting to test connectivity by fetching page
        /// information from the Facebook Graph API fails, typically due to invalid credentials
        /// or network connectivity issues.
        /// </remarks>
        public const string ConnectionFailed = "CONNECTION_FAILED";

        /// <summary>
        /// Indicates that the connection test operation failed.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the connection
        /// testing process, preventing verification of Facebook Graph API connectivity.
        /// </remarks>
        public const string ConnectionTestFailed = "CONNECTION_TEST_FAILED";

        /// <summary>
        /// Indicates that sending a message through Facebook Messenger failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Facebook Graph API call to send a message fails,
        /// either due to API errors, invalid parameters, or service unavailability.
        /// </remarks>
        public const string SendMessageFailed = "SEND_MESSAGE_FAILED";

        /// <summary>
        /// Indicates that retrieving connector status information failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when an exception is thrown while attempting to
        /// gather and return the current status information of the Facebook connector.
        /// </remarks>
        public const string StatusError = "STATUS_ERROR";

        #endregion

        #region Message Receiving

        /// <summary>
        /// Indicates that receiving a message from Facebook webhook failed.
        /// </summary>
        /// <remarks>
        /// This error occurs when processing an incoming message webhook from Facebook fails,
        /// typically due to malformed webhook data or processing errors.
        /// </remarks>
        public const string ReceiveMessageFailed = "RECEIVE_MESSAGE_FAILED";

        /// <summary>
        /// Indicates that the webhook data provided is invalid or malformed.
        /// </summary>
        /// <remarks>
        /// This error occurs when the webhook payload from Facebook does not contain
        /// the expected fields or has invalid data that cannot be processed.
        /// </remarks>
        public const string InvalidWebhookData = "INVALID_WEBHOOK_DATA";

        /// <summary>
        /// Indicates that the content type for webhook data is not supported.
        /// </summary>
        /// <remarks>
        /// This error occurs when the webhook content type is not JSON,
        /// which is the required format for Facebook webhooks.
        /// </remarks>
        public const string UnsupportedContentType = "UNSUPPORTED_CONTENT_TYPE";

        #endregion

        #region Graph API Specific

        /// <summary>
        /// Indicates that the Facebook Graph API returned an error.
        /// </summary>
        /// <remarks>
        /// This error occurs when the Facebook Graph API returns an error response,
        /// such as rate limiting, invalid parameters, or other API-specific errors.
        /// </remarks>
        public const string GraphApiError = "GRAPH_API_ERROR";

        /// <summary>
        /// Indicates that the requested operation is not supported by the Facebook Graph API.
        /// </summary>
        /// <remarks>
        /// This error occurs when attempting to perform an operation that is not
        /// available or supported for the current Facebook Page or API version.
        /// </remarks>
        public const string OperationNotSupported = "OPERATION_NOT_SUPPORTED";

        #endregion
    }
}