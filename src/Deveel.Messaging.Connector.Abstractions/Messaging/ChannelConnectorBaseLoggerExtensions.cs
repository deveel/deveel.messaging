//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides high-performance logging extensions for Channel Connector Base operations using source-generated logging methods.
    /// </summary>
    internal static partial class ChannelConnectorBaseLoggerExtensions
    {
        #region Authentication Logging

        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Debug,
            Message = "Starting authentication process")]
        internal static partial void LogStartingAuthentication(this ILogger logger);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "No suitable authentication configuration found for the provided connection settings")]
        internal static partial void LogNoAuthenticationConfigurationFound(this ILogger logger);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Debug,
            Message = "Using authentication configuration: {AuthenticationType}")]
        internal static partial void LogUsingAuthenticationConfiguration(this ILogger logger, string authenticationType);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Information,
            Message = "Authentication successful using {AuthenticationType}")]
        internal static partial void LogAuthenticationSuccessful(this ILogger logger, string authenticationType);

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Error,
            Message = "Authentication failed: {ErrorMessage}")]
        internal static partial void LogAuthenticationFailed(this ILogger logger, string errorMessage);

        [LoggerMessage(
            EventId = 2006,
            Level = LogLevel.Error,
            Message = "Unexpected error during authentication")]
        internal static partial void LogAuthenticationException(this ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 2007,
            Level = LogLevel.Warning,
            Message = "No authentication credential to refresh")]
        internal static partial void LogNoCredentialToRefresh(this ILogger logger);

        [LoggerMessage(
            EventId = 2008,
            Level = LogLevel.Debug,
            Message = "Refreshing authentication credential")]
        internal static partial void LogRefreshingAuthenticationCredential(this ILogger logger);

        [LoggerMessage(
            EventId = 2009,
            Level = LogLevel.Warning,
            Message = "Authentication configuration not found for credential type: {AuthenticationType}")]
        internal static partial void LogAuthenticationConfigurationNotFound(this ILogger logger, string authenticationType);

        [LoggerMessage(
            EventId = 2010,
            Level = LogLevel.Information,
            Message = "Authentication credential refreshed successfully")]
        internal static partial void LogAuthenticationCredentialRefreshed(this ILogger logger);

        [LoggerMessage(
            EventId = 2011,
            Level = LogLevel.Error,
            Message = "Authentication refresh failed: {ErrorMessage}")]
        internal static partial void LogAuthenticationRefreshFailed(this ILogger logger, string errorMessage);

        [LoggerMessage(
            EventId = 2012,
            Level = LogLevel.Error,
            Message = "Unexpected error during authentication refresh")]
        internal static partial void LogAuthenticationRefreshException(this ILogger logger, Exception exception);

        #endregion

        #region State Management Logging

        [LoggerMessage(
            EventId = 2101,
            Level = LogLevel.Debug,
            Message = "Connector state changed from {OldState} to {NewState}")]
        internal static partial void LogStateChanged(this ILogger logger, ConnectorState oldState, ConnectorState newState);

        [LoggerMessage(
            EventId = 2102,
            Level = LogLevel.Information,
            Message = "Connector initialized successfully")]
        internal static partial void LogConnectorInitialized(this ILogger logger);

        [LoggerMessage(
            EventId = 2103,
            Level = LogLevel.Error,
            Message = "Connector initialization failed")]
        internal static partial void LogConnectorInitializationFailed(this ILogger logger, Exception exception);

        #endregion

        #region Validation Logging

        [LoggerMessage(
            EventId = 2201,
            Level = LogLevel.Debug,
            Message = "Validating message {MessageId} for sending")]
        internal static partial void LogValidatingMessage(this ILogger logger, string messageId);

        [LoggerMessage(
            EventId = 2202,
            Level = LogLevel.Warning,
            Message = "Message {MessageId} validation failed with {ErrorCount} error(s)")]
        internal static partial void LogMessageValidationFailed(this ILogger logger, string messageId, int errorCount);

        [LoggerMessage(
            EventId = 2203,
            Level = LogLevel.Debug,
            Message = "Message {MessageId} validation passed")]
        internal static partial void LogMessageValidationPassed(this ILogger logger, string messageId);

        [LoggerMessage(
            EventId = 2204,
            Level = LogLevel.Warning,
            Message = "Batch validation failed for {MessageCount} message(s) in the batch")]
        internal static partial void LogBatchValidationFailed(this ILogger logger, int messageCount);

        #endregion

        #region Capability Validation Logging

        [LoggerMessage(
            EventId = 2301,
            Level = LogLevel.Debug,
            Message = "Validating capability: {Capability}")]
        internal static partial void LogValidatingCapability(this ILogger logger, string capability);

        [LoggerMessage(
            EventId = 2302,
            Level = LogLevel.Error,
            Message = "Capability {Capability} is not supported by this connector")]
        internal static partial void LogCapabilityNotSupported(this ILogger logger, string capability);

        [LoggerMessage(
            EventId = 2303,
            Level = LogLevel.Debug,
            Message = "Validating operational state. Current state: {State}")]
        internal static partial void LogValidatingOperationalState(this ILogger logger, ConnectorState state);

        [LoggerMessage(
            EventId = 2304,
            Level = LogLevel.Error,
            Message = "Connector is not in an operational state. Current state: {State}")]
        internal static partial void LogNotInOperationalState(this ILogger logger, ConnectorState state);

        #endregion

        #region Generic Operation Logging

        [LoggerMessage(
            EventId = 2401,
            Level = LogLevel.Debug,
            Message = "Starting operation: {OperationName}")]
        internal static partial void LogStartingOperation(this ILogger logger, string operationName);

        [LoggerMessage(
            EventId = 2402,
            Level = LogLevel.Debug,
            Message = "Operation completed successfully: {OperationName}")]
        internal static partial void LogOperationCompleted(this ILogger logger, string operationName);

        [LoggerMessage(
            EventId = 2403,
            Level = LogLevel.Error,
            Message = "Operation failed: {OperationName}")]
        internal static partial void LogOperationFailed(this ILogger logger, string operationName, Exception exception);

        #endregion
    }
}