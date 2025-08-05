//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// An implementation of a message batch, that is 
	/// a collection of messages that can be processed together.
	/// </summary>
	public class MessageBatch : IMessageBatch
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageBatch"/> class.
		/// </summary>
		public MessageBatch()
		{
		}

		/// <inheritdoc/>
		public string Id { get; set; } = "";

		/// <inheritdoc />
		public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

		/// <inheritdoc />
		public IList<IMessage> Messages { get; set; } = new List<IMessage>();

		IEnumerable<IMessage> IMessageBatch.Messages => Messages;
	}
}
