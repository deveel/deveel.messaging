//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides high-performance logging extensions for Facebook Messenger Connector operations using source-generated logging methods.
    /// </summary>
    internal static partial class LoggerExtensions
    {
        #region Initialization Logging

        [LoggerMessage(
            EventId = 5001,
            Level = LogLevel.Information,
            Message = "Initializing Facebook Messenger connector...")]
        internal static partial void LogInitializingConnector(this ILogger logger);

        [LoggerMessage(
            EventId = 5002,
            Level = LogLevel.Information,
            Message = "Facebook Messenger connector initialized successfully with Graph API validation")]
        internal static partial void LogConnectorInitialized(this ILogger logger);

        [LoggerMessage(
            EventId = 5003,
            Level = LogLevel.Error,
            Message = "Facebook authentication validation failed: {ErrorMessage}")]
        internal static partial void LogAuthenticationValidationFailed(this ILogger logger, string errorMessage, Exception exception);

        [LoggerMessage(
            EventId = 5004,
            Level = LogLevel.Error,
            Message = "Failed to initialize Facebook Messenger connector")]
        internal static partial void LogInitializationFailed(this ILogger logger, Exception exception);

        #endregion

        #region Connection Testing Logging

        [LoggerMessage(
            EventId = 5101,
            Level = LogLevel.Debug,
            Message = "Testing Facebook connection using Graph API...")]
        internal static partial void LogTestingConnection(this ILogger logger);

        [LoggerMessage(
            EventId = 5102,
            Level = LogLevel.Debug,
            Message = "Connection test successful. Page: {PageName} (Category: {Category})")]
        internal static partial void LogConnectionTestSuccessful(this ILogger logger, string pageName, string category);

        [LoggerMessage(
            EventId = 5103,
            Level = LogLevel.Error,
            Message = "Facebook Graph API error during connection test: {ErrorMessage}")]
        internal static partial void LogConnectionTestGraphApiError(this ILogger logger, string errorMessage, Exception exception);

        [LoggerMessage(
            EventId = 5104,
            Level = LogLevel.Error,
            Message = "Connection test failed")]
        internal static partial void LogConnectionTestFailed(this ILogger logger, Exception exception);

        #endregion

        #region Message Sending Logging

        [LoggerMessage(
            EventId = 5201,
            Level = LogLevel.Debug,
            Message = "Sending Facebook Messenger message {MessageId} using Graph API")]
        internal static partial void LogSendingMessage(this ILogger logger, string messageId);

        [LoggerMessage(
            EventId = 5202,
            Level = LogLevel.Information,
            Message = "Facebook Messenger message sent successfully via Graph API. MessageId: {MessageId}, FacebookMessageId: {FacebookMessageId}")]
        internal static partial void LogMessageSent(this ILogger logger, string messageId, string facebookMessageId);

        [LoggerMessage(
            EventId = 5203,
            Level = LogLevel.Error,
            Message = "Facebook validation error sending message {MessageId}: {ErrorMessage}")]
        internal static partial void LogMessageValidationError(this ILogger logger, string messageId, string errorMessage, Exception exception);

        [LoggerMessage(
            EventId = 5204,
            Level = LogLevel.Error,
            Message = "Facebook Graph API error sending message {MessageId}: {ErrorMessage}")]
        internal static partial void LogMessageGraphApiError(this ILogger logger, string messageId, string errorMessage, Exception exception);

        [LoggerMessage(
            EventId = 5205,
            Level = LogLevel.Error,
            Message = "Failed to send Facebook Messenger message {MessageId}")]
        internal static partial void LogMessageSendFailed(this ILogger logger, string messageId, Exception exception);

        #endregion

        #region Webhook Message Receiving Logging

        [LoggerMessage(
            EventId = 5301,
            Level = LogLevel.Debug,
            Message = "Receiving Facebook Messenger message from webhook")]
        internal static partial void LogReceivingMessage(this ILogger logger);

        [LoggerMessage(
            EventId = 5302,
            Level = LogLevel.Error,
            Message = "Failed to receive Facebook Messenger message from webhook")]
        internal static partial void LogReceiveMessageFailed(this ILogger logger, Exception exception);

        #endregion

        #region Status and Health Logging

        [LoggerMessage(
            EventId = 5401,
            Level = LogLevel.Error,
            Message = "Failed to get connector status")]
        internal static partial void LogGetStatusFailed(this ILogger logger, Exception exception);

        #endregion

        #region Quick Replies Logging

        [LoggerMessage(
            EventId = 5501,
            Level = LogLevel.Warning,
            Message = "Failed to parse quick replies JSON: {Json}")]
        internal static partial void LogQuickRepliesParsingFailed(this ILogger logger, string json, Exception exception);

        #endregion
    }
}