//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// The content that is transported by a message.
	/// </summary>
	public interface IMessageContent {
		/// <summary>
		/// Gets the identifier of the type of content.
		/// </summary>
		/// <seealso cref="MessageContentType"/>
		MessageContentType ContentType { get; }
	}
}
