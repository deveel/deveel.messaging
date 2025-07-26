//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides information about the result of a message 
	/// receive operation.
	/// </summary>
	public sealed class ReceiveResult
	{
		/// <summary>
		/// Represents the result of receiving one ore more messages.
		/// </summary>
		/// <param name="batchId">
		/// The unique identifier for the batch of messages received, that
		/// is provided by the remote service or by the local application.</param>
		/// <param name="messages">
		/// The list of messages received in the batch.
		/// </param>
		public ReceiveResult(string batchId, IReadOnlyList<IMessage> messages)
		{
			BatchId = batchId;
			Messages = messages;
		}

		/// <summary>
		/// Gets the list of messages received in the batch.
		/// </summary>
		public IReadOnlyList<IMessage> Messages { get; }

		/// <summary>
		/// Gets the unique identifier for the batch, provided
		/// by the remote service or by the local application.
		/// </summary>
		public string BatchId { get; }
	}
}
