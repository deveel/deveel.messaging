//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents the result of a status update operation, 
	/// including the status, timestamp, and optional additional data.
	/// </summary>
	/// <remarks>
	/// This construct is used to encapsulate the result of a status update
	/// on a message that was previously sent through a messaging system.
	/// </remarks>
	public class StatusUpdateResult
	{
		/// <summary>
		/// Constructs a new instance of <see cref="StatusUpdateResult"/>
		/// that represents the status of a message update.
		/// </summary>
		/// <param name="status">
		/// The status of the message update.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp of the status update.
		/// </param>
		public StatusUpdateResult(MessageStatus status, DateTimeOffset? timestamp = null)
		{
			Status = status;
			Timestamp = timestamp ?? DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Gets the current status of the message.
		/// </summary>
		public MessageStatus Status { get; }

		/// <summary>
		/// Gets the timestamp indicating when the event occurred.
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Gets a description of the status update, if available.
		/// </summary>
		public string? Description { get; }

		/// <summary>
		/// Gets a collection of additional data associated with the status update.
		/// </summary>
		public IDictionary<string, object>? AdditionalData { get; } = new Dictionary<string, object>();
	}
}
