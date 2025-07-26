//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A static class that contains the known properties of a message.
	/// </summary>
	public static class KnownMessageProperties {
		/// <summary>
		/// The subject of a message.
		/// </summary>
		public const string Subject = "subject";

		/// <summary>
		/// The identifier of the message that is being handled
		/// from a remote source.
		/// </summary>
		public const string RemoteMessageId = "remoteMessageId";

		/// <summary>
		/// The identifier of the message that is being
		/// replied to from the message.
		/// </summary>
		public const string ReplyTo = "replyTo";

		/// <summary>
		/// Represents the key used to store or retrieve the 
		/// correlation identifier property of a message.
		/// </summary>
		/// <remarks>
		/// The correlation identifier is typically used to track 
		/// and associate related messaging operations across different 
		/// systems or components. 
		/// This constant can be used as a key in logging, tracing, or other
		/// diagnostic scenarios.
		/// It can also be used to correlate messages in a conversation 
		/// or workflow, allowing systems to maintain context.
		/// </remarks>
		public const string CorrelationId = "correlationId";
	}
}
