//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents the result of a batch send operation, 
	/// including results of the single messages
	/// </summary>
	public class BatchSendResult
	{
		/// <summary>
		/// Constructs a new instance of <see cref="BatchSendResult"/> with
		/// the specified local batch ID, the batch ID assigned by the provider, 
		/// and optional message results.
		/// </summary>
		/// <param name="batchId"></param>
		/// <param name="remoteBatchId"></param>
		/// <param name="messageResults"></param>
		public BatchSendResult(string batchId, string? remoteBatchId, IDictionary<string, SendResult>? messageResults = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(batchId, nameof(batchId));

			BatchId = batchId;
			RemoteBatchId = remoteBatchId;
			MessageResults = messageResults ?? new Dictionary<string, SendResult>();
		}

		/// <summary>
		/// Gets the unique  identifier of the batch being sent.
		/// </summary>
		public string BatchId { get; }

		/// <summary>
		/// Gets the identifier for the remote batch associated with this instance.
		/// </summary>
		/// <remarks>
		/// It is not assured that every provider will assign an identifier 
		/// to the batch.
		/// </remarks>
		public string? RemoteBatchId { get; }

		/// <summary>
		/// Gets a dictionary containing the results of message 
		/// sending operations.
		/// </summary>
		public IDictionary<string, SendResult> MessageResults { get; } = new Dictionary<string, SendResult>();
	}
}
