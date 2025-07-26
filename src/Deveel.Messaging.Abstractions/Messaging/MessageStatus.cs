//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Runtime.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// Enumerates the possible status of a message
	/// at a given time in its handling lifecycle
	/// </summary>
	public enum MessageStatus {
		/// <summary>
		/// The status is unknown. Note: messages should
		/// never be in this status, but it is provided
		/// for completeness.
		/// </summary>
		[EnumMember(Value = "unknown")]
		Unknown = 0,

		/// <summary>
		/// The message has been received by the system.
		/// </summary>
		[EnumMember(Value = "received")]
		Received = 1,

		/// <summary>
		/// The message has been queued for delivery.
		/// </summary>
		[EnumMember(Value = "queued")]
		Queued = 2,

		/// <summary>
		/// The message was routed to the next node
		/// of the delivery chain.
		/// </summary>
		[EnumMember(Value = "routed")]
		Routed = 3,

		/// <summary>
		///	The message routing failed to reach the
		///	next node in the delivery chain.
		///	</summary>
		[EnumMember(Value = "routeFailed")]
		RouteFailed = 4,

		/// <summary>
		/// The message has been sent through the channel
		/// to the recipient.
		/// </summary>
		[EnumMember(Value = "sent")]
		Sent = 5,

		/// <summary>
		/// The message has been delivered to the recipient.
		/// </summary>
		[EnumMember(Value = "delivered")]
		Delivered = 6,

		/// <summary>
		/// The message has failed to be delivered to the recipient.
		/// </summary>
		[EnumMember(Value = "deliveryFailed")]
		DeliveryFailed = 7,

		/// <summary>
		/// The message has been read by the recipient.
		/// </summary>
		[EnumMember(Value = "read")]
		Read = 8,

		/// <summary>
		/// The message has been deleted by the recipient.
		/// </summary>
		[EnumMember(Value = "deleted")]
		Deleted = 9
	}
}
