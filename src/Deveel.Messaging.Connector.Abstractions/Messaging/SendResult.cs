//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// The result of sending a message through a channel.
	/// </summary>
	public class SendResult
	{
		/// <summary>
		/// Gets the unique identifier for the message being sent.
		/// </summary>
		public string MessageId { get; }

		/// <summary>
		/// Represents the result of a message send operation, 
		/// including identifiers for tracking.
		/// </summary>
		/// <param name="messageId">
		/// The unique identifier for the message within the local 
		/// system.Cannot be null or empty.</param>
		/// <param name="remoteMessageId">
		/// The unique identifier assigned to the message by the 
		/// remote system. Cannot be null or empty.</param>
		public SendResult(string messageId, string remoteMessageId)
		{
			ArgumentNullException.ThrowIfNull(messageId, nameof(messageId));
			ArgumentNullException.ThrowIfNull(remoteMessageId, nameof(remoteMessageId));

			MessageId = messageId;
			RemoteMessageId = remoteMessageId;
		}

		/// <summary>
		/// Gets the unique identifier for the message on the 
		/// remote system.
		/// </summary>
		public string RemoteMessageId { get; }

		/// <summary>
		/// Gets the current status of the operation.
		/// </summary>
		public MessageStatus? Status { get; set; }

		/// <summary>
		/// Gets the timestamp when the message was sent or processed.
		/// </summary>
		public DateTimeOffset? Timestamp { get; set; }

		/// <summary>
		/// Gets a collection of additional data associated with the result.
		/// </summary>
		/// <remarks>
		/// This property is used to store supplementary information that is 
		/// not defined by the standard properties of the result.
		/// </remarks>
		public IDictionary<string, object> AdditionalData { get; } = new Dictionary<string, object>();
	}
}
