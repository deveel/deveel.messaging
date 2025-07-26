//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides a message content that consists of binary data,
	/// and includes the MIME type of the content.
	/// </summary>
	public class BinaryContent : MessageContent, IBinaryContent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BinaryContent"/> 
		/// class with the specified raw data and MIME type.
		/// </summary>
		/// <param name="rawData">The binary data representing the content. Cannot be null.</param>
		/// <param name="mimeType">The MIME type of the content. Cannot be null or empty.</param>
		public BinaryContent(byte[] rawData, string mimeType)
		{
			RawData = rawData;
			MimeType = mimeType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BinaryContent"/> 
		/// class using the specified binary content.
		/// </summary>
		/// <param name="content">The binary content from which to initialize 
		/// the new instance.</param>
		public BinaryContent(IBinaryContent content)
		{
			RawData = content?.RawData ?? Array.Empty<byte>();
			MimeType = content?.MimeType ?? string.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BinaryContent"/> 
		/// class with default values.
		/// </summary>
		/// <remarks>This constructor sets the <see cref="RawData"/> property 
		/// to an empty byte array and the <see cref="MimeType"/> property to 
		/// an empty string.
		/// </remarks>
		public BinaryContent()
		{
			RawData = Array.Empty<byte>();
			MimeType = string.Empty;
		}

		/// <inheritdoc/>
		public override MessageContentType ContentType => MessageContentType.Binary;

		/// <inheritdoc/>
		public byte[] RawData { get; set; }

		/// <inheritdoc/>
		public string MimeType { get; set; }
	}
}
