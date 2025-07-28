//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines standard error codes used by channel connectors.
    /// </summary>
    /// <remarks>
    /// This class provides a centralized location for all standard error codes
    /// that can be returned by connector operations. These codes provide
    /// consistent error identification across different connector implementations.
    /// </remarks>
    public static class ConnectorErrorCodes
    {
        #region Initialization Errors

        /// <summary>
        /// Indicates that the connector has already been initialized and cannot be initialized again.
        /// </summary>
        /// <remarks>
        /// This error occurs when <see cref="IChannelConnector.InitializeAsync"/> is called
        /// on a connector that is not in the <see cref="ConnectorState.Uninitialized"/> state.
        /// </remarks>
        public const string AlreadyInitialized = "ALREADY_INITIALIZED";

        /// <summary>
        /// Indicates that an error occurred during connector initialization.
        /// </summary>
        /// <remarks>
        /// This error is typically thrown when an exception occurs during the
        /// initialization process, preventing the connector from reaching the
        /// <see cref="ConnectorState.Ready"/> state.
        /// </remarks>
        public const string InitializationError = "INITIALIZATION_ERROR";

        #endregion

        #region Connection Errors

        /// <summary>
        /// Indicates that an error occurred while testing the connection to the external service.
        /// </summary>
        /// <remarks>
        /// This error is returned when <see cref="IChannelConnector.TestConnectionAsync"/>
        /// fails due to an exception or connectivity issue with the remote service.
        /// </remarks>
        public const string ConnectionTestError = "CONNECTION_TEST_ERROR";

        #endregion

        #region Message Sending Errors

        /// <summary>
        /// Indicates that message validation failed before sending.
        /// </summary>
        /// <remarks>
        /// This error occurs when a message fails validation checks performed
        /// by <see cref="IChannelConnector.ValidateMessageAsync"/> before the
        /// actual send operation is attempted.
        /// </remarks>
        public const string MessageValidationFailed = "MESSAGE_VALIDATION_FAILED";

        /// <summary>
        /// Indicates that an error occurred while sending a single message.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the message
        /// sending process in <see cref="IChannelConnector.SendMessageAsync"/>.
        /// </remarks>
        public const string SendMessageError = "SEND_MESSAGE_ERROR";

        /// <summary>
        /// Indicates that batch validation failed before sending.
        /// </summary>
        /// <remarks>
        /// This error occurs when one or more messages in a batch fail validation
        /// checks before the batch send operation is attempted.
        /// </remarks>
        public const string BatchValidationFailed = "BATCH_VALIDATION_FAILED";

        /// <summary>
        /// Indicates that an error occurred while sending a batch of messages.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the batch
        /// sending process in <see cref="IChannelConnector.SendBatchAsync"/>.
        /// </remarks>
        public const string SendBatchError = "SEND_BATCH_ERROR";

        #endregion

        #region Status and Health Errors

        /// <summary>
        /// Indicates that an error occurred while retrieving connector status information.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the status
        /// retrieval process in <see cref="IChannelConnector.GetStatusAsync"/>.
        /// </remarks>
        public const string GetStatusError = "GET_STATUS_ERROR";

        /// <summary>
        /// Indicates that an error occurred while retrieving message status information.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the message status
        /// retrieval process in <see cref="IChannelConnector.GetMessageStatusAsync"/>.
        /// </remarks>
        public const string GetMessageStatusError = "GET_MESSAGE_STATUS_ERROR";

        /// <summary>
        /// Indicates that an error occurred while performing a health check.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the health check
        /// process in <see cref="IChannelConnector.GetHealthAsync"/>.
        /// </remarks>
        public const string GetHealthError = "GET_HEALTH_ERROR";

        #endregion

        #region Message Receiving Errors

        /// <summary>
        /// Indicates that an error occurred while receiving status updates.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the status
        /// receiving process in <see cref="IChannelConnector.ReceiveMessageStatusAsync"/>.
        /// </remarks>
        public const string ReceiveStatusError = "RECEIVE_STATUS_ERROR";

        /// <summary>
        /// Indicates that an error occurred while receiving messages.
        /// </summary>
        /// <remarks>
        /// This error is returned when an exception occurs during the message
        /// receiving process in <see cref="IChannelConnector.ReceiveMessagesAsync"/>.
        /// </remarks>
        public const string ReceiveMessagesError = "RECEIVE_MESSAGES_ERROR";

        #endregion
    }
}