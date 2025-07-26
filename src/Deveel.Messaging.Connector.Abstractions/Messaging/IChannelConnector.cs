//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Defines the contract for a connector that facilitates 
	/// communication with a messaging system.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface are responsible for managing 
	/// the connection state and performing operations such as initialization, 
	/// connection testing, message sending, and status retrieval. 
	/// The interface also provides asynchronous validation of messages
	/// that would be sent.
	/// </remarks>
	public interface IChannelConnector
	{
		/// <summary>
		/// Gets the schema describing the configurations, requirements,
		/// constraints and capabilities of connector.
		/// </summary>
		IChannelSchema Schema { get; }

		/// <summary>
		/// Gets the current state of the connector.
		/// </summary>
		ConnectorState State { get; }

		/// <summary>
		/// Initializes the connector to make it ready for messaging operations.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{TValue}"/> indicating whether 
		/// the initialization was successful.
		/// </returns>
		Task<ConnectorResult<bool>> InitializeAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Tests the connection to the external service.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests. Passing 
		/// a cancellation token allows the operation to be cancelled.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{T}"/> indicating whether the 
		/// connection test was successful.
		/// </returns>
		Task<ConnectorResult<bool>> TestConnectionAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Sends a message to the service provided by the connector.
		/// </summary>
		/// <remarks>
		/// This method sends the provided message and returns a result 
		/// indicating success or failure, along with any relevant
		/// information such as message identifiers or status.
		/// In case of failure, the result will contain an error specification.
		/// This method is available for use only if the connector supports the
		/// <see cref="ChannelCapability.SendMessages"/> capability.
		/// </remarks>
		/// <param name="message">The message to be sent. Cannot be null.</param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{SendResult}"/> indicating the outcome o
		/// f the send operation.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the connector does not support sending messages.
		/// </exception>
		Task<ConnectorResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken);

		/// <summary>
		/// Sends a batch of messages asynchronously to the connector.
		/// </summary>
		/// <remarks>
		/// This method sends all messages in the provided batch to the connector.
		/// </remarks>
		/// <param name="batch">The batch of messages to be sent. Cannot be null.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{BatchSendResult}"/> indicating the 
		/// outcome of the batch send operation.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the connector does not support sending batches of messages.
		/// </exception>
		Task<ConnectorResult<BatchSendResult>> SendBatchAsync(IMessageBatch batch, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the current status of the connector.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{StatusInfo}"/> containing the
		/// status information of the connector.
		/// </returns>
		Task<ConnectorResult<StatusInfo>> GetStatusAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Retrieves the status updates for a specific message from
		/// the remote service.
		/// </summary>
		/// <param name="messageId">
		/// The unique identifier of the message for which status updates are requested.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{StatusUpdatesResult}"/> containing the
		/// set of status updates for the specified message.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the connector does not support retrieving status information.
		/// </exception>
		Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken);

		/// <summary>
		/// Validates the specified message to ensure it meets the
		/// requirements for sending through the connector.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage message, CancellationToken cancellationToken);

		/// <summary>
		/// Receives a status update for a specific message from the connector.
		/// </summary>
		/// <param name="source">
		/// The source from which the status update is requested.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{StatusUpdateResult}"/> containing the
		/// status update for the specified message.
		/// </returns>
		Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusAsync(MessageSource source, CancellationToken cancellationToken);

		/// <summary>
		/// Receives one or more messages from the specified source.
		/// </summary>
		/// <param name="source">
		/// The source from which messages are to be received.
		/// </param>
		/// <param name="cancellationToken">
		/// A token that is used to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{ReceiveResult}"/> containing the
		/// received messages.
		/// </returns>
		Task<ConnectorResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource source, CancellationToken cancellationToken);

		/// <summary>
		/// Queries the local health status of the connector.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{ConnectorHealth}"/> containing the
		/// health information of the connector.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the connector does not support health checks.
		/// </exception>
		Task<ConnectorResult<ConnectorHealth>> GetHealthAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Shuts down the connector, releasing any resources it holds,
		/// and ensuring that no further operations can be performed.
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <remarks>
		/// This operation should attempt to deliver any pending messages
		/// but not guarantee their delivery, and should not allow any further
		/// message request to be processed after it has been called.
		/// </remarks>
		/// <returns>
		/// Returns a <see cref="Task"/> that represents the asynchronous operation.
		/// </returns>
		Task ShutdownAsync(CancellationToken cancellationToken);
	}
}
