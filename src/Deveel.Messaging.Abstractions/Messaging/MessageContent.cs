//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// A default implementation of a message content.
	/// </summary>
	[JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(ContentType))]
	[JsonDerivedType(typeof(TextContent), nameof(MessageContentType.PlainText))]
	[JsonDerivedType(typeof(HtmlContent), nameof(MessageContentType.Html))]
	[JsonDerivedType(typeof(TemplateContent), nameof(MessageContentType.Template))]
	[JsonDerivedType(typeof(MultipartContent), nameof(MessageContentType.Multipart))]
	[JsonDerivedType(typeof(JsonContent), nameof(MessageContentType.Json))]
	[JsonDerivedType(typeof(BinaryContent), nameof(MessageContentType.Binary))]
	// TODO: [JsonDerivedType(typeof(XmlContent), nameof(MessageContentType.Xml))]
	[JsonDerivedType(typeof(MediaContent), nameof(MessageContentType.Media))]
	public abstract class MessageContent : IMessageContent {
		/// <summary>
		/// Gets the type of the content.
		/// </summary>
		public abstract MessageContentType ContentType { get; }

		/// <summary>
		/// A factory method to create a message content from a given content.
		/// </summary>
		/// <param name="content">
		/// The content to create the message content from.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="MessageContent"/> that represents
		/// an implementation of the given content.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the given content is not supported.
		/// </exception>
		public static MessageContent? Create(IMessageContent? content) {
			if (content == null)
				return null;
			if (content is MessageContent messageContent)
				return messageContent;

			if (content is ITextContent textContent)
				return new TextContent(textContent);
			if (content is IHtmlContent htmlContent)
				return new HtmlContent(htmlContent);
			if (content is ITemplateContent templateContent)
				return new TemplateContent(templateContent);
			if (content is IMultipartContent multipartContent)
				return new MultipartContent(multipartContent);
			if (content is IJsonContent jsonContent)
				return new JsonContent(jsonContent);
			if (content is IBinaryContent binaryContent)
				return new BinaryContent(binaryContent);
			if (content is IMediaContent mediaContent)
				return new MediaContent(mediaContent);

			throw new NotSupportedException($"The content of type '{content.ContentType}' is not supported");
		}
	}
}
