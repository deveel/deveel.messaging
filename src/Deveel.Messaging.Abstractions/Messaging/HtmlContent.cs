//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// An implementation of <see cref="IHtmlContent"/> that
	/// provides a message content that is HTML.
	/// </summary>
	public class HtmlContent : MessageContent, IHtmlContent {
		/// <summary>
		/// Constructs the content with the given HTML content,
		/// formatted as base64, and optionally a list of attachments.
		/// </summary>
		/// <param name="html">
		/// The base64 encoded HTML content of the message.
		/// </param>
		/// <param name="attachments">
		/// An optional list of attachments to the message.
		/// </param>
		public HtmlContent(string html, IEnumerable<MessageAttachment>? attachments = null) {
			Html = html;
			Attachments = attachments?.Select(x => new MessageAttachment(x))?.ToList() ?? new List<MessageAttachment>();
		}

		/// <summary>
		/// Constructs the content from an existing <see cref="IHtmlContent"/>
		/// that is used as a template.
		/// </summary>
		/// <param name="content">
		/// The content to use as a template.
		/// </param>
		public HtmlContent(IHtmlContent content) {
			Html = content.Html;
			Attachments = content.Attachments?.Select(x => new MessageAttachment(x))?.ToList() ?? new List<MessageAttachment>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlContent"/> class.
		/// </summary>
		public HtmlContent() {
		}

		/// <inheritdoc/>
		public override MessageContentType ContentType => MessageContentType.Html;

		/// <summary>
		/// Gets the HTML content of the message, as
		/// a base64 encoded string.
		/// </summary>
		[JsonConverter(typeof(Base64JsonConverter))]
		public string Html { get; set; } = string.Empty;

		[ExcludeFromCodeCoverage]
		IEnumerable<IAttachment> IHtmlContent.Attachments => Attachments;

		/// <summary>
		/// Gets or sets the list of attachments to the message.
		/// </summary>
		public List<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
	}
}
