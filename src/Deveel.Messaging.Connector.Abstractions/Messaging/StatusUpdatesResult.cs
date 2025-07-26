//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents the result of a status updates query for 
	/// a specific message.
	/// </summary>
	public sealed class StatusUpdatesResult
	{
		/// <summary>
		/// Constructs a new instance of <see cref="StatusUpdatesResult"/> for
		/// the specified message ID and optional status list.
		/// </summary>
		/// <param name="messageId">
		/// The identifier of the message for which the status updates are retrieved.
		/// </param>
		/// <param name="results">
		/// An optional collection of status update results.
		/// </param>
		public StatusUpdatesResult(string messageId, IEnumerable<StatusUpdateResult>? results = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(messageId, nameof(messageId));
			MessageId = messageId;
			Updates = results != null ? new List<StatusUpdateResult>(results) : new List<StatusUpdateResult>();
		}

		/// <summary>
		/// Gets the unique identifier for the message.
		/// </summary>
		public string MessageId { get; }

		/// <summary>
		/// Gets the collection of status update results.
		/// </summary>
		public IList<StatusUpdateResult> Updates { get; } = new List<StatusUpdateResult>();
	}
}
