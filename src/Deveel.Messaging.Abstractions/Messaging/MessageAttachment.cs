//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// A default implementation of a message attachment.
	/// </summary>
	public class MessageAttachment : IAttachment {
		/// <summary>
		/// Constructs the message attachment with the specified identifier,
		/// the file name, the MIME type and the content.
		/// </summary>
		/// <param name="id">
		/// A unique identifier for the attachment within the scope
		/// of the message that contains it.
		/// </param>
		/// <param name="fileName">
		/// The name of the file that the attachment represents.
		/// </param>
		/// <param name="mimeType">
		/// The MIME type of the content of the attachment.
		/// </param>
		/// <param name="content">
		/// The base64-encoded content of the attachment.
		/// </param>
		public MessageAttachment(string id, string fileName, string mimeType, string content) {
			Id = id;
			FileName = fileName;
			MimeType = mimeType;
			Content = content;
		}

		/// <summary>
		/// Constructs a new instance of the message attachment.
		/// </summary>
		public MessageAttachment() {
		}

		/// <summary>
		/// Constructs a new instance of the message attachment
		/// from the given attachment.
		/// </summary>
		/// <param name="attachment"></param>
		public MessageAttachment(IAttachment attachment) {
			Id = attachment.Id;
			FileName = attachment.FileName;
			MimeType = attachment.MimeType;
			Content = attachment.Content;
		}

		/// <inheritdoc/>
		public string Id { get; set; } = "";

		/// <inheritdoc/>
		public string FileName { get; set; } = "";

		/// <inheritdoc/>
		public string MimeType { get; set; } = "";

		/// <inheritdoc/>
		[JsonConverter(typeof(Base64JsonConverter))]
		public string Content { get; set; } = "";
	}
}
