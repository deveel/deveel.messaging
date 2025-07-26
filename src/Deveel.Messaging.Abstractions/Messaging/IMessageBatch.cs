//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A batch of messages that are exchanged between two endpoints.
	/// </summary>
	public interface IMessageBatch {
		/// <summary>
		/// Gets the unique identifier of the batch.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets a set of properties that are associated with the batch.
		/// </summary>
		IDictionary<string, object>? Properties { get; }

		/// <summary>
		/// Gets the messages included in the batch.
		/// </summary>
		IEnumerable<IMessage> Messages { get; }
	}
}
