//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// An implementation of <see cref="IMessage"/> that is used to
	/// represent a message that can be sent or received.
	/// </summary>
	public class Message : IMessage {
		/// <summary>
		/// Constructs an empty message instance.
		/// </summary>
		public Message() {
		}

		/// <summary>
		/// Constructs a message instance from the given <paramref name="message"/>.
		/// </summary>
		/// <param name="message">
		/// The message that is used as source of the new instance.
		/// </param>
		public Message(IMessage message) {
			Id = message.Id;
			Sender = message.Sender != null ? new Endpoint(message.Sender) : null;
			Receiver = message.Receiver != null ? new Endpoint(message.Receiver) : null;
			Content = MessageContent.Create(message.Content);
			Properties = message.Properties?.ToDictionary(x => x.Key, x => new MessageProperty(x.Value));
		}

		/// <inheritdoc/>
		public string Id { get; set; } = "";

		/// <inheritdoc/>
		public Endpoint? Sender { get; set; }

		/// <inheritdoc/>
		public Endpoint? Receiver { get; set; }

		IEndpoint? IMessage.Sender => Sender;

		IEndpoint? IMessage.Receiver => Receiver;


		/// <inheritdoc/>
		public MessageContent? Content { get; set; }

		IMessageContent? IMessage.Content => Content;

		IDictionary<string, IMessageProperty>? IMessage.Properties 
			=> Properties?.ToDictionary(x => x.Key, y => (IMessageProperty)y.Value);

		/// <inheritdoc/>
		public IDictionary<string, MessageProperty>? Properties { get; set; }
	}
}
