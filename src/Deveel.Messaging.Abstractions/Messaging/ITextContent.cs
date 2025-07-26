//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A type of message that contains plain-text.
	/// </summary>
	public interface ITextContent : IMessageContent {
		/// <summary>
		/// Gets the code of the encoding used to 
		/// encode the text.
		/// </summary>
		/// <remarks>
		/// When this is not specified, the default
		/// channel encoding is used.
		/// </remarks>
		string? Encoding { get; }

		/// <summary>
		/// Gets the text of the message.
		/// </summary>
		string? Text { get; }
	}
}
