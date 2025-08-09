//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides high-performance logging extensions for Twilio SMS Connector operations using source-generated logging methods.
    /// </summary>
    internal static partial class TwilioSmsConnectorLoggerExtensions
    {
        #region Initialization Logging

        [LoggerMessage(
            EventId = 4001,
            Level = LogLevel.Information,
            Message = "Initializing Twilio SMS connector")]
        internal static partial void LogInitializingConnector(this ILogger logger);

        [LoggerMessage(
            EventId = 4002,
            Level = LogLevel.Information,
            Message = "Twilio SMS connector initialized successfully")]
        internal static partial void LogConnectorInitialized(this ILogger logger);

        [LoggerMessage(
            EventId = 4003,
            Level = LogLevel.Error,
            Message = "Connection settings validation failed: {Errors}")]
        internal static partial void LogConnectionSettingsValidationFailed(this ILogger logger, string errors);

        [LoggerMessage(
            EventId = 4004,
            Level = LogLevel.Error,
            Message = "Failed to initialize Twilio SMS connector")]
        internal static partial void LogInitializationFailed(this ILogger logger, Exception exception);

        #endregion

        #region Connection Testing Logging

        [LoggerMessage(
            EventId = 4101,
            Level = LogLevel.Debug,
            Message = "Testing Twilio connection...")]
        internal static partial void LogTestingConnection(this ILogger logger);

        [LoggerMessage(
            EventId = 4102,
            Level = LogLevel.Debug,
            Message = "Connection test successful. Account: {AccountFriendlyName}")]
        internal static partial void LogConnectionTestSuccessful(this ILogger logger, string? accountFriendlyName);

        [LoggerMessage(
            EventId = 4103,
            Level = LogLevel.Error,
            Message = "Connection test failed")]
        internal static partial void LogConnectionTestFailed(this ILogger logger, Exception exception);

        #endregion

        #region SMS Sending Logging

        [LoggerMessage(
            EventId = 4201,
            Level = LogLevel.Debug,
            Message = "Sending SMS message {MessageId}")]
        internal static partial void LogSendingSms(this ILogger logger, string messageId);

        [LoggerMessage(
            EventId = 4202,
            Level = LogLevel.Information,
            Message = "SMS sent successfully. MessageId: {MessageId}, TwilioSid: {TwilioSid}, Status: {Status}")]
        internal static partial void LogSmsSent(this ILogger logger, string messageId, string twilioSid, string status);

        [LoggerMessage(
            EventId = 4203,
            Level = LogLevel.Error,
            Message = "Failed to send SMS message {MessageId}")]
        internal static partial void LogSmsSendFailed(this ILogger logger, string messageId, Exception exception);

        [LoggerMessage(
            EventId = 4204,
            Level = LogLevel.Debug,
            Message = "Using messaging service SID: {MessagingServiceSid}")]
        internal static partial void LogUsingMessagingServiceSid(this ILogger logger, string messagingServiceSid);

        [LoggerMessage(
            EventId = 4205,
            Level = LogLevel.Debug,
            Message = "Using from number: {FromNumber}")]
        internal static partial void LogUsingFromNumber(this ILogger logger, string fromNumber);

        #endregion

        #region Batch Sending Logging

        [LoggerMessage(
            EventId = 4301,
            Level = LogLevel.Debug,
            Message = "Sending batch of {MessageCount} SMS messages")]
        internal static partial void LogSendingBatch(this ILogger logger, int messageCount);

        [LoggerMessage(
            EventId = 4302,
            Level = LogLevel.Information,
            Message = "SMS batch sent successfully. BatchId: {BatchId}, SuccessCount: {SuccessCount}, FailureCount: {FailureCount}")]
        internal static partial void LogBatchSent(this ILogger logger, string batchId, int successCount, int failureCount);

        [LoggerMessage(
            EventId = 4303,
            Level = LogLevel.Error,
            Message = "Failed to send SMS batch")]
        internal static partial void LogBatchSendFailed(this ILogger logger, Exception exception);

        #endregion

        #region Configuration Logging

        [LoggerMessage(
            EventId = 4701,
            Level = LogLevel.Debug,
            Message = "Using Twilio Account SID: {AccountSid}")]
        internal static partial void LogUsingAccountSid(this ILogger logger, string accountSid);

        [LoggerMessage(
            EventId = 4702,
            Level = LogLevel.Debug,
            Message = "Status callback URL configured: {StatusCallbackUrl}")]
        internal static partial void LogStatusCallbackConfigured(this ILogger logger, string statusCallbackUrl);

        [LoggerMessage(
            EventId = 4703,
            Level = LogLevel.Debug,
            Message = "Default messaging service SID: {MessagingServiceSid}")]
        internal static partial void LogDefaultMessagingServiceSid(this ILogger logger, string messagingServiceSid);

        [LoggerMessage(
            EventId = 4704,
            Level = LogLevel.Debug,
            Message = "Default from number: {FromNumber}")]
        internal static partial void LogDefaultFromNumber(this ILogger logger, string fromNumber);

        #endregion
    }
}