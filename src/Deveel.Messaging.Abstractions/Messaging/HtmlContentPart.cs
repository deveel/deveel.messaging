//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//


namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a content part of a message that is encoded in HTML.
	/// </summary>
	public class HtmlContentPart : MessageContentPart, IHtmlContentPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlContentPart"/> class.
		/// </summary>
		public HtmlContentPart()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlContentPart"/> class 
		/// with the specified HTML content and optional
		/// attachments.
		/// </summary>
		/// <param name="html">
		/// The HTML content to be included in this part.
		/// </param>
		/// <param name="attachments">
		/// An optional collection of <see cref="MessageAttachment"/> objects to 
		/// be associated with this part.
		/// </param>
		public HtmlContentPart(string? html, IEnumerable<MessageAttachment>? attachments = null)
		{
			Html = html;
			Attachments = attachments?.ToList() ?? new List<MessageAttachment>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlContentPart"/> class using 
		/// the specified content part.
		/// </summary>
		/// <remarks>
		/// This constructor copies the HTML content and attachments from 
		/// the provided <paramref name="contentPart"/>.
		/// </remarks>
		/// <param name="contentPart">The content part from which to initialize the HTML 
		/// content and attachments.</param>
		public HtmlContentPart(IHtmlContentPart contentPart)
		{
			Html = contentPart?.Html ?? "";
			Attachments = contentPart?.Attachments.Select(a => new MessageAttachment(a)).ToList() 
				?? new List<MessageAttachment>();
		}

		/// <inheritdoc />
		public override MessageContentType ContentType => MessageContentType.Html;

		/// <inheritdoc/>
		public string Html { get; set; } = "";

		IEnumerable<IAttachment> IHtmlContent.Attachments => Attachments;

		/// <summary>
		/// Gets or sets the collection of attachments associated with the message.
		/// </summary>
		public IList<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
	}
}
