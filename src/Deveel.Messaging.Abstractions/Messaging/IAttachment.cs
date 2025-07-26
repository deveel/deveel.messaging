//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// An attachment to a message that has
	/// another type of content.
	/// </summary>
	public interface IAttachment {
		/// <summary>
		/// Gets the unique identifier of the attachment
		/// within the scope of the message.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets the name of the file that is attached.
		/// </summary>
		string FileName { get; }

		/// <summary>
		/// Gets the MIME type of the file that is attached.
		/// </summary>
		string MimeType { get; }

		/// <summary>
		/// Gets a base64-encoded string that represents
		/// the content of the attachment.
		/// </summary>
		string Content { get; }
	}
}
