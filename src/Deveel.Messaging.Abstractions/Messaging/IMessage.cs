//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A message is a data structure that is sent from a
	/// sender to a receiver.
	/// </summary>
	public interface IMessage {
		/// <summary>
		/// Gets the unique identifier of the message
		/// within the network.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets the endpoint that is the sender of the message.
		/// </summary>
		IEndpoint? Sender { get; }

		/// <summary>
		/// Gets the endpoint that is the receiver of the message.
		/// </summary>
		IEndpoint? Receiver { get; }

		/// <summary>
		/// Gets the content of the message.
		/// </summary>
		IMessageContent? Content { get; }

		/// <summary>
		/// Gets a set of properties that extend the message.
		/// </summary>
		IDictionary<string, IMessageProperty>? Properties { get; }

	}
}
