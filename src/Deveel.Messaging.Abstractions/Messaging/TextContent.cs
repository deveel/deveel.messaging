//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// Represents a plain-text content of a message.
	/// </summary>
	public class TextContent : MessageContent, ITextContent {
		/// <summary>
		/// Constructs the content with the given text and encoding.
		/// </summary>
		/// <param name="text">
		/// The text content of the message.
		/// </param>
		/// <param name="encoding">
		/// An optional code that specifies the encoding of the text.
		/// </param>
		public TextContent(string? text, string? encoding = null) {
			Encoding = encoding;
			Text = text;
		}

		/// <inheritdoc/>
		public override MessageContentType ContentType => MessageContentType.PlainText;

		/// <summary>
		/// Constructs the content from the given instance.
		/// </summary>
		/// <param name="content">
		/// The source instance of <see cref="ITextContent"/> that is used
		/// to initialize the properties of this instance.
		/// </param>
		public TextContent(ITextContent content)
			: this(content?.Text, content?.Encoding) { 
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextContent"/> class.
		/// </summary>
		public TextContent() {
		}

		/// <inheritdoc/>
		public string? Encoding { get; set; }

		/// <inheritdoc/>
		public string? Text { get; set; }
	}
}
