//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides high-performance logging extensions for Firebase Push Connector operations using source-generated logging methods.
    /// </summary>
    internal static partial class FirebasePushConnectorLoggerExtensions
    {
        #region Initialization Logging

        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Information,
            Message = "Initializing Firebase push connector")]
        internal static partial void LogInitializingConnector(this ILogger logger);

        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Information,
            Message = "Firebase push connector initialized successfully for project {ProjectId}")]
        internal static partial void LogConnectorInitialized(this ILogger logger, string projectId);

        [LoggerMessage(
            EventId = 1003,
            Level = LogLevel.Error,
            Message = "Failed to initialize Firebase push connector")]
        internal static partial void LogInitializationFailed(this ILogger logger, Exception exception);

        #endregion

        #region Connection Testing Logging

        [LoggerMessage(
            EventId = 1101,
            Level = LogLevel.Debug,
            Message = "Testing Firebase connection")]
        internal static partial void LogTestingConnection(this ILogger logger);

        [LoggerMessage(
            EventId = 1102,
            Level = LogLevel.Debug,
            Message = "Firebase connection test successful")]
        internal static partial void LogConnectionTestSuccessful(this ILogger logger);

        [LoggerMessage(
            EventId = 1103,
            Level = LogLevel.Warning,
            Message = "Firebase connection test failed")]
        internal static partial void LogConnectionTestFailed(this ILogger logger);

        [LoggerMessage(
            EventId = 1104,
            Level = LogLevel.Error,
            Message = "Firebase connection test threw an exception")]
        internal static partial void LogConnectionTestException(this ILogger logger, Exception exception);

        #endregion

        #region Message Sending Logging

        [LoggerMessage(
            EventId = 1201,
            Level = LogLevel.Debug,
            Message = "Sending push notification to {ReceiverAddress}")]
        internal static partial void LogSendingPushNotification(this ILogger logger, string? receiverAddress);

        [LoggerMessage(
            EventId = 1202,
            Level = LogLevel.Information,
            Message = "Push notification sent successfully. MessageId: {MessageId}")]
        internal static partial void LogPushNotificationSent(this ILogger logger, string messageId);

        [LoggerMessage(
            EventId = 1203,
            Level = LogLevel.Error,
            Message = "Failed to send push notification")]
        internal static partial void LogPushNotificationSendFailed(this ILogger logger, Exception exception);

        #endregion

        #region Batch Sending Logging

        [LoggerMessage(
            EventId = 1301,
            Level = LogLevel.Debug,
            Message = "Sending batch of {MessageCount} push notifications")]
        internal static partial void LogSendingBatch(this ILogger logger, int messageCount);

        [LoggerMessage(
            EventId = 1302,
            Level = LogLevel.Information,
            Message = "Batch push notification sent successfully. BatchId: {BatchId}, MessageCount: {MessageCount}")]
        internal static partial void LogBatchSent(this ILogger logger, string batchId, int messageCount);

        [LoggerMessage(
            EventId = 1303,
            Level = LogLevel.Error,
            Message = "Failed to send batch push notifications")]
        internal static partial void LogBatchSendFailed(this ILogger logger, Exception exception);

        #endregion
    }
}