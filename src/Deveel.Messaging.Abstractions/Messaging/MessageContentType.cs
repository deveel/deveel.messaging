//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Specifies the content type of a message.
	/// </summary>
	/// <remarks>
	/// This enumeration is used to define the format of the message 
	/// content, which can be plain text, HTML, a template, JSON, 
	/// binary data, or XML.</remarks>
	public enum MessageContentType
	{
		/// <summary>
		/// Represents plain text content without any formatting.
		/// </summary>
		PlainText = 1,

		/// <summary>
		/// Represents the HTML format type for content processing.
		/// </summary>
		/// <remarks>
		/// This enumeration value is used to specify that the content 
		/// should be processed as HTML.
		/// </remarks>
		Html = 2,

		/// <summary>
		/// Represents a multipart content type in a message or data stream.
		/// </summary>
		/// <remarks>
		/// This typically indicates that the content consists of multiple parts,
		/// such as in a multipart MIME message. 
		/// It is useful in scenarios where data is divided into discrete sections, 
		/// each with its own content type and headers.
		/// </remarks>
		Multipart = 3,

		/// <summary>
		/// The message references a template on the server side for
		/// remote rendering.
		/// </summary>
		/// <remarks>
		/// This identifies a template within a set of predefined types
		/// as provided remotely by the server.
		/// </remarks>
		Template = 4,

		/// <summary>
		/// The message content is a media object
		/// </summary>
		Media = 5,

		/// <summary>
		/// The content of the message is a JSON object.
		/// </summary>
		Json = 6,

		/// <summary>
		/// The content of the message is a binary stream.
		/// </summary>
		Binary = 7
	}
}
