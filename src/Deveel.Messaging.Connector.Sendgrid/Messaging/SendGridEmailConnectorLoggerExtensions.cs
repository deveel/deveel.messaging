//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides high-performance logging extensions for SendGrid Email Connector operations using source-generated logging methods.
    /// </summary>
    internal static partial class SendGridEmailConnectorLoggerExtensions
    {
        #region Initialization Logging

        [LoggerMessage(
            EventId = 3001,
            Level = LogLevel.Information,
            Message = "Initializing SendGrid email connector...")]
        internal static partial void LogInitializingConnector(this ILogger logger);

        [LoggerMessage(
            EventId = 3002,
            Level = LogLevel.Information,
            Message = "SendGrid email connector initialized successfully")]
        internal static partial void LogConnectorInitialized(this ILogger logger);

        [LoggerMessage(
            EventId = 3003,
            Level = LogLevel.Error,
            Message = "Connection settings validation failed: {Errors}")]
        internal static partial void LogConnectionSettingsValidationFailed(this ILogger logger, string errors);

        [LoggerMessage(
            EventId = 3004,
            Level = LogLevel.Error,
            Message = "Failed to initialize SendGrid email connector")]
        internal static partial void LogInitializationFailed(this ILogger logger, Exception exception);

        #endregion

        #region Connection Testing Logging

        [LoggerMessage(
            EventId = 3101,
            Level = LogLevel.Debug,
            Message = "Testing SendGrid connection...")]
        internal static partial void LogTestingConnection(this ILogger logger);

        [LoggerMessage(
            EventId = 3102,
            Level = LogLevel.Debug,
            Message = "SendGrid connection test successful")]
        internal static partial void LogConnectionTestSuccessful(this ILogger logger);

        [LoggerMessage(
            EventId = 3103,
            Level = LogLevel.Error,
            Message = "SendGrid connection test failed")]
        internal static partial void LogConnectionTestFailed(this ILogger logger, Exception exception);

        #endregion

        #region Email Sending Logging

        [LoggerMessage(
            EventId = 3201,
            Level = LogLevel.Debug,
            Message = "Sending email message {MessageId} to {RecipientEmail}")]
        internal static partial void LogSendingEmail(this ILogger logger, string messageId, string? recipientEmail);

        [LoggerMessage(
            EventId = 3202,
            Level = LogLevel.Information,
            Message = "Email sent successfully. MessageId: {MessageId}, RemoteMessageId: {RemoteMessageId}")]
        internal static partial void LogEmailSent(this ILogger logger, string messageId, string remoteMessageId);

        [LoggerMessage(
            EventId = 3203,
            Level = LogLevel.Error,
            Message = "Failed to send email message {MessageId}")]
        internal static partial void LogEmailSendFailed(this ILogger logger, string messageId, Exception exception);

        [LoggerMessage(
            EventId = 3204,
            Level = LogLevel.Warning,
            Message = "SendGrid API returned non-success status: {StatusCode} for message {MessageId}")]
        internal static partial void LogApiNonSuccessStatus(this ILogger logger, int statusCode, string messageId);

        #endregion

        #region Batch Sending Logging

        [LoggerMessage(
            EventId = 3301,
            Level = LogLevel.Debug,
            Message = "Sending batch of {MessageCount} email messages")]
        internal static partial void LogSendingBatch(this ILogger logger, int messageCount);

        [LoggerMessage(
            EventId = 3302,
            Level = LogLevel.Information,
            Message = "Email batch sent successfully. BatchId: {BatchId}, SuccessCount: {SuccessCount}, FailureCount: {FailureCount}")]
        internal static partial void LogBatchSent(this ILogger logger, string batchId, int successCount, int failureCount);

        [LoggerMessage(
            EventId = 3303,
            Level = LogLevel.Error,
            Message = "Failed to send email batch")]
        internal static partial void LogBatchSendFailed(this ILogger logger, Exception exception);

        #endregion

        #region Configuration Logging

        [LoggerMessage(
            EventId = 3601,
            Level = LogLevel.Debug,
            Message = "Sandbox mode is {SandboxMode}")]
        internal static partial void LogSandboxMode(this ILogger logger, bool sandboxMode);

        [LoggerMessage(
            EventId = 3602,
            Level = LogLevel.Debug,
            Message = "Tracking settings enabled: {TrackingEnabled}")]
        internal static partial void LogTrackingSettings(this ILogger logger, bool trackingEnabled);

        [LoggerMessage(
            EventId = 3603,
            Level = LogLevel.Debug,
            Message = "Webhook URL configured: {WebhookUrl}")]
        internal static partial void LogWebhookConfigured(this ILogger logger, string webhookUrl);

        [LoggerMessage(
            EventId = 3604,
            Level = LogLevel.Debug,
            Message = "Default from name set: {DefaultFromName}")]
        internal static partial void LogDefaultFromName(this ILogger logger, string defaultFromName);

        [LoggerMessage(
            EventId = 3605,
            Level = LogLevel.Debug,
            Message = "Default reply-to address set: {DefaultReplyTo}")]
        internal static partial void LogDefaultReplyTo(this ILogger logger, string defaultReplyTo);

        #endregion
    }
}