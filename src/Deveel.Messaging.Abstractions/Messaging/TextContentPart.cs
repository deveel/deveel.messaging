//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a content part that is formatted as plain text.
	/// </summary>
	public class TextContentPart : MessageContentPart, ITextContentPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TextContentPart"/> class.
		/// </summary>
		public TextContentPart()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextContentPart"/> class 
		/// with the specified text and encoding.
		/// </summary>
		/// <param name="text">
		/// The text content of this part.
		/// </param>
		/// <param name="encoding">
		/// The encoding type of the text content.
		/// </param>
		public TextContentPart(string? text, string? encoding = null)
		{
			Text = text;
			Encoding = encoding;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextContentPart"/> class by 
		/// copying the text and encoding from the specified content part.
		/// </summary>
		/// <param name="contentPart">
		/// The content part from which to copy the text and encoding.
		/// </param>
		public TextContentPart(ITextContentPart contentPart)
		{
			Text = contentPart?.Text;
			Encoding = contentPart?.Encoding;
		}

		/// <inheritdoc />
		public override MessageContentType ContentType => MessageContentType.PlainText;

		/// <inheritdoc/>
		public string? Encoding { get; set; }

		/// <inheritdoc/>
		public string? Text { get; set; }
	}
}
