//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a message content that is composed of multiple parts.
	/// </summary>
	public class MultipartContent : MessageContent, IMultipartContent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MultipartContent"/> class.
		/// </summary>
		public MultipartContent()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipartContent"/> class with 
		/// the specified content parts.
		/// </summary>
		/// <param name="parts">
		/// A collection of <see cref="MessageContentPart"/> objects that make up 
		/// the multipart content.
		/// </param>
		public MultipartContent(IEnumerable<MessageContentPart> parts)
		{
			Parts = new List<MessageContentPart>(parts);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipartContent"/> class
		/// copying the parts from an existing <see cref="IMultipartContent"/>.
		/// </summary>
		/// <param name="multipartContent"></param>
		public MultipartContent(IMultipartContent multipartContent)
		{
			Parts = multipartContent.Parts?
				.Select(p => MessageContentPart.Create(p)).ToList() ?? new List<MessageContentPart>();
		}

		IEnumerable<IMessageContentPart> IMultipartContent.Parts => Parts;

		/// <summary>
		/// Gets or sets the collection of message content parts.
		/// </summary>
		public IList<MessageContentPart> Parts { get; set; } = new List<MessageContentPart>();

		/// <inheritdoc/>
		public override MessageContentType ContentType => MessageContentType.Multipart;
	}
}
