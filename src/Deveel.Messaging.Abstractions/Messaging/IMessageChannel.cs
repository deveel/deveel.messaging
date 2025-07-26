//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// The reference to the instance of the channel
	/// that is used to transport the message.
	/// </summary>
	public interface IMessageChannel {
		/// <summary>
		/// Gets the unique identifier of the channel
		/// in the scope of the network.
		/// </summary>
		string? Id { get; }

		/// <summary>
		/// Gets the type of the channel.
		/// </summary>
		string Type { get; }

		/// <summary>
		/// Gets the identifier of the provider of the channel.
		/// </summary>
		string Provider { get; }

		/// <summary>
		/// Gets the descriptive name of the channel.
		/// </summary>
		string? Name { get; }
	}
}
