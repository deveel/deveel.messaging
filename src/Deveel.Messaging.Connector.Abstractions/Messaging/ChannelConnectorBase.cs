//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides a base implementation of the <see cref="IChannelConnector"/> interface
	/// with common functionality for state management and capability validation.
	/// </summary>
	/// <remarks>
	/// This abstract class handles the common concerns of connector implementations such as
	/// state transitions, capability checking, and providing sensible default implementations
	/// for operations that may not be supported by all connectors.
	/// 
	/// Derived classes need to implement the abstract methods to provide connector-specific
	/// functionality while benefiting from the state management and validation logic
	/// provided by this base class.
	/// </remarks>
	public abstract class ChannelConnectorBase : IChannelConnector
	{
		private ConnectorState _state = ConnectorState.Uninitialized;
		private readonly object _stateLock = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelConnectorBase"/> class
		/// with the specified schema.
		/// </summary>
		/// <param name="schema">The schema describing the connector's capabilities and configuration.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="schema"/> is null.</exception>
		protected ChannelConnectorBase(IChannelSchema schema)
		{
			Schema = schema ?? throw new ArgumentNullException(nameof(schema));
		}

		/// <inheritdoc/>
		public IChannelSchema Schema { get; }

		/// <inheritdoc/>
		public ConnectorState State
		{
			get
			{
				lock (_stateLock)
				{
					return _state;
				}
			}
		}

		/// <summary>
		/// Sets the current state of the connector.
		/// </summary>
		/// <param name="newState">The new state to transition to.</param>
		protected void SetState(ConnectorState newState)
		{
			lock (_stateLock)
			{
				_state = newState;
			}
		}

		/// <summary>
		/// Validates that the connector supports the specified capability.
		/// </summary>
		/// <param name="capability">The capability to validate.</param>
		/// <exception cref="NotSupportedException">
		/// Thrown when the connector does not support the specified capability.
		/// </exception>
		protected void ValidateCapability(ChannelCapability capability)
		{
			if (!Schema.Capabilities.HasFlag(capability))
			{
				throw new NotSupportedException($"The connector does not support the '{capability}' capability.");
			}
		}

		/// <summary>
		/// Validates that the connector is in an operational state (Ready or Error).
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the connector is not in an operational state.
		/// </exception>
		protected void ValidateOperationalState()
		{
			var currentState = State;
			if (currentState == ConnectorState.Uninitialized || 
			    currentState == ConnectorState.Initializing ||
			    currentState == ConnectorState.ShuttingDown || 
			    currentState == ConnectorState.Shutdown)
			{
				throw new InvalidOperationException($"The connector is not in an operational state. Current state: {currentState}");
			}
		}

		/// <inheritdoc/>
		public async Task<ConnectorResult<bool>> InitializeAsync(CancellationToken cancellationToken)
		{
			if (State != ConnectorState.Uninitialized)
			{
				return ConnectorResult<bool>.Fail("ALREADY_INITIALIZED", 
					"The connector has already been initialized.");
			}

			SetState(ConnectorState.Initializing);

			try
			{
				var result = await InitializeConnectorAsync(cancellationToken);
				
				if (result.Successful)
				{
					SetState(ConnectorState.Ready);
				}
				else
				{
					SetState(ConnectorState.Error);
				}

				return result;
			}
			catch (Exception ex)
			{
				SetState(ConnectorState.Error);
				return ConnectorResult<bool>.Fail("INITIALIZATION_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, performs the actual connector initialization logic.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous initialization operation.</returns>
		protected abstract Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken);

		/// <inheritdoc/>
		public async Task<ConnectorResult<bool>> TestConnectionAsync(CancellationToken cancellationToken)
		{
			ValidateOperationalState();

			try
			{
				return await TestConnectorConnectionAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				return ConnectorResult<bool>.Fail("CONNECTION_TEST_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, performs the actual connection testing logic.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous connection test operation.</returns>
		protected abstract Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken);

		/// <inheritdoc/>
		public async Task<ConnectorResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(message);
			ValidateCapability(ChannelCapability.SendMessages);
			ValidateOperationalState();

			try
			{
				// Validate the message before sending
				var validationErrors = new List<ValidationResult>();
				await foreach (var validationResult in ValidateMessageAsync(message, cancellationToken))
				{
					if (validationResult != ValidationResult.Success)
					{
						validationErrors.Add(validationResult);
					}
				}

				// If there are validation errors, return a failure result
				if (validationErrors.Count > 0)
				{
					return ConnectorResult<SendResult>.ValidationFailed("MESSAGE_VALIDATION_FAILED", 
						"The message failed validation", validationErrors);
				}

				return await SendMessageCoreAsync(message, cancellationToken);
			}
			catch (Exception ex)
			{
				return ConnectorResult<SendResult>.Fail("SEND_MESSAGE_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, performs the actual message sending logic.
		/// </summary>
		/// <param name="message">The message to send.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous send operation.</returns>
		protected abstract Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken);

		/// <inheritdoc/>
		public virtual async Task<ConnectorResult<BatchSendResult>> SendBatchAsync(IMessageBatch batch, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(batch);
			ValidateCapability(ChannelCapability.BulkMessaging);
			ValidateOperationalState();

			try
			{
				// Validate all messages in the batch before sending
				var allValidationErrors = new List<ValidationResult>();
				var messageValidationResults = new Dictionary<string, List<ValidationResult>>();

				foreach (var message in batch.Messages)
				{
					var messageErrors = new List<ValidationResult>();
					await foreach (var validationResult in ValidateMessageAsync(message, cancellationToken))
					{
						if (validationResult != ValidationResult.Success)
						{
							messageErrors.Add(validationResult);
							allValidationErrors.Add(validationResult);
						}
					}

					if (messageErrors.Count > 0)
					{
						messageValidationResults[message.Id] = messageErrors;
					}
				}

				// If there are validation errors, return a failure result with details
				if (allValidationErrors.Count > 0)
				{
					var errorData = new Dictionary<string, object>
					{
						["MessageValidationResults"] = messageValidationResults
					};
					
					return ConnectorResult<BatchSendResult>.ValidationFailed("BATCH_VALIDATION_FAILED", 
						$"Validation failed for {messageValidationResults.Count} message(s) in the batch", 
						allValidationErrors);
				}

				return await SendBatchCoreAsync(batch, cancellationToken);
			}
			catch (Exception ex)
			{
				return ConnectorResult<BatchSendResult>.Fail("SEND_BATCH_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, performs the actual batch sending logic.
		/// The default implementation throws <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="batch">The batch of messages to send.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous batch send operation.</returns>
		protected virtual Task<ConnectorResult<BatchSendResult>> SendBatchCoreAsync(IMessageBatch batch, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("Batch sending is not supported by this connector.");
		}

		/// <inheritdoc/>
		public async Task<ConnectorResult<StatusInfo>> GetStatusAsync(CancellationToken cancellationToken)
		{
			try
			{
				return await GetConnectorStatusAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				return ConnectorResult<StatusInfo>.Fail("GET_STATUS_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, retrieves the current status of the connector.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous status retrieval operation.</returns>
		protected abstract Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken);

		/// <inheritdoc/>
		public virtual async Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(messageId);
			ValidateCapability(ChannelCapability.MessageStatusQuery);
			ValidateOperationalState();

			try
			{
				return await GetMessageStatusCoreAsync(messageId, cancellationToken);
			}
			catch (Exception ex)
			{
				return ConnectorResult<StatusUpdatesResult>.Fail("GET_MESSAGE_STATUS_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, retrieves the status updates for a specific message.
		/// The default implementation throws <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="messageId">The unique identifier of the message.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous message status retrieval operation.</returns>
		protected virtual Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusCoreAsync(string messageId, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("Message status querying is not supported by this connector.");
		}

		/// <inheritdoc/>
		public virtual async IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(message);

			await foreach (var result in ValidateMessageCoreAsync(message, cancellationToken))
			{
				yield return result;
			}
		}

		/// <summary>
		/// When overridden in a derived class, validates a message for sending through the connector.
		/// The default implementation performs basic validation.
		/// </summary>
		/// <param name="message">The message to validate.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>An async enumerable of validation results.</returns>
		protected virtual async IAsyncEnumerable<ValidationResult> ValidateMessageCoreAsync(IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			// Basic validation - check message ID
			if (string.IsNullOrWhiteSpace(message.Id))
			{
				yield return new ValidationResult("Message ID is required", new[] { "Id" });
			}

			// Validate content type compatibility if schema specifies supported content types
			if (message.Content != null && Schema.ContentTypes.Any())
			{
				var contentType = message.Content.ContentType;
				if (!Schema.ContentTypes.Contains(contentType))
				{
					yield return new ValidationResult(
						$"Content type '{contentType}' is not supported by this connector. Supported types: {string.Join(", ", Schema.ContentTypes)}", 
						new[] { "Content.ContentType" });
				}
			}

			// Validate endpoints if message has sender/receiver
			if (message.Sender != null)
			{
				var senderEndpointType = GetEndpointType(message.Sender);
				if (!string.IsNullOrEmpty(senderEndpointType) && !IsEndpointTypeSupported(senderEndpointType, asSender: true))
				{
					yield return new ValidationResult(
						$"Sender endpoint type '{senderEndpointType}' is not supported by this connector", 
						new[] { "Sender" });
				}
			}

			if (message.Receiver != null)
			{
				var receiverEndpointType = GetEndpointType(message.Receiver);
				if (!string.IsNullOrEmpty(receiverEndpointType) && !IsEndpointTypeSupported(receiverEndpointType, asReceiver: true))
				{
					yield return new ValidationResult(
						$"Receiver endpoint type '{receiverEndpointType}' is not supported by this connector", 
						new[] { "Receiver" });
				}
			}

			// If no validation errors found, return success
			if (message.Content != null && Schema.ContentTypes.Any() && 
			    Schema.ContentTypes.Contains(message.Content.ContentType))
			{
				yield return ValidationResult.Success!;
			}

			await Task.CompletedTask; // Suppress compiler warning about not being async
		}

		/// <summary>
		/// Gets the endpoint type from an endpoint. Override this method to provide custom endpoint type extraction logic.
		/// </summary>
		/// <param name="endpoint">The endpoint to extract the type from.</param>
		/// <returns>The endpoint type string, or null if it cannot be determined.</returns>
		protected virtual string? GetEndpointType(IEndpoint endpoint)
		{
			// Return the Type property directly from the endpoint
			return endpoint.Type;
		}

		/// <summary>
		/// Checks if an endpoint type is supported by this connector.
		/// </summary>
		/// <param name="endpointType">The endpoint type to check.</param>
		/// <param name="asSender">Whether to check as a sender.</param>
		/// <param name="asReceiver">Whether to check as a receiver.</param>
		/// <returns>True if the endpoint type is supported in the specified role.</returns>
		protected virtual bool IsEndpointTypeSupported(string endpointType, bool asSender = false, bool asReceiver = false)
		{
			if (string.IsNullOrWhiteSpace(endpointType))
				return false;

			// Check if any endpoint configuration matches
			return Schema.Endpoints.Any(e => 
				(e.Type == "*" || string.Equals(e.Type, endpointType, StringComparison.OrdinalIgnoreCase)) &&
				(!asSender || e.CanSend) &&
				(!asReceiver || e.CanReceive));
		}

		/// <inheritdoc/>
		public virtual Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusAsync(MessageSource source, CancellationToken cancellationToken)
		{
			ValidateCapability(ChannelCapability.HandlerMessageState);
			ValidateOperationalState();

			try
			{
				return ReceiveMessageStatusCoreAsync(source, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromResult(ConnectorResult<StatusUpdateResult>.Fail("RECEIVE_STATUS_ERROR", ex.Message));
			}
		}

		/// <summary>
		/// When overridden in a derived class, receives status updates from the specified source.
		/// The default implementation throws <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="source">The source from which to receive status updates.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous status receiving operation.</returns>
		protected virtual Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusCoreAsync(MessageSource source, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("Status receiving is not supported by this connector.");
		}

		/// <inheritdoc/>
		public virtual Task<ConnectorResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource source, CancellationToken cancellationToken)
		{
			ValidateCapability(ChannelCapability.ReceiveMessages);
			ValidateOperationalState();

			try
			{
				return ReceiveMessagesCoreAsync(source, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromResult(ConnectorResult<ReceiveResult>.Fail("RECEIVE_MESSAGES_ERROR", ex.Message));
			}
		}

		/// <summary>
		/// When overridden in a derived class, receives messages from the specified source.
		/// The default implementation throws <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="source">The source from which to receive messages.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous message receiving operation.</returns>
		protected virtual Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("Message receiving is not supported by this connector.");
		}

		/// <inheritdoc/>
		public virtual async Task<ConnectorResult<ConnectorHealth>> GetHealthAsync(CancellationToken cancellationToken)
		{
			ValidateCapability(ChannelCapability.HealthCheck);

			try
			{
				return await GetConnectorHealthAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				return ConnectorResult<ConnectorHealth>.Fail("GET_HEALTH_ERROR", ex.Message);
			}
		}

		/// <summary>
		/// When overridden in a derived class, retrieves the health information of the connector.
		/// The default implementation returns basic health information based on the current state.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous health check operation.</returns>
		protected virtual Task<ConnectorResult<ConnectorHealth>> GetConnectorHealthAsync(CancellationToken cancellationToken)
		{
			var health = new ConnectorHealth
			{
				State = State,
				IsHealthy = State == ConnectorState.Ready,
				LastHealthCheck = DateTime.UtcNow,
				Uptime = TimeSpan.Zero // Derived classes should track actual uptime
			};

			if (!health.IsHealthy)
			{
				health.Issues.Add($"Connector is in {State} state");
			}

			return Task.FromResult(ConnectorResult<ConnectorHealth>.Success(health));
		}

		/// <inheritdoc/>
		public async Task ShutdownAsync(CancellationToken cancellationToken)
		{
			if (State == ConnectorState.Shutdown || State == ConnectorState.ShuttingDown)
			{
				return;
			}

			SetState(ConnectorState.ShuttingDown);

			try
			{
				await ShutdownConnectorAsync(cancellationToken);
			}
			finally
			{
				SetState(ConnectorState.Shutdown);
			}
		}

		/// <summary>
		/// When overridden in a derived class, performs the actual connector shutdown logic.
		/// The default implementation does nothing.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous shutdown operation.</returns>
		protected virtual Task ShutdownConnectorAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}