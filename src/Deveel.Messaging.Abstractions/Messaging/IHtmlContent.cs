//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A content that is encoded in HTML.
	/// </summary>
	public interface IHtmlContent : IMessageContent {
		/// <summary>
		/// Gets the base64 encoded HTML content.
		/// </summary>
		string Html { get; }

		/// <summary>
		/// Gets a set of optional attachments that are
		/// included to the message content.
		/// </summary>
		IEnumerable<IAttachment> Attachments { get; }
	}
}
