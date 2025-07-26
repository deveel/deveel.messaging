//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json.Serialization;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides an abstraction for different types of message content parts.
	/// </summary>
	/// <remarks>
	/// This class serves as a base for specific content types 
	/// such as plain text and HTML. 
	/// It provides a factory method to create instances of derived types based 
	/// on the provided content part.
	/// </remarks>
	[JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(ContentType))]
	[JsonDerivedType(typeof(TextContentPart), nameof(MessageContentType.PlainText))]
	[JsonDerivedType(typeof(HtmlContentPart), nameof(MessageContentType.Html))]
	public abstract class MessageContentPart : MessageContent, IMessageContentPart
	{
		/// <summary>
		/// Creates a new instance of a <see cref="MessageContentPart"/> based on 
		/// the specified content part.
		/// </summary>
		/// <param name="part">
		/// The content part to be used as a template for creating a new instance. 
		/// Must not be null or whitespace.
		/// </param>
		/// <returns>
		/// Returns a new instance of <see cref="MessageContentPart"/> that corresponds 
		/// to the type of the specified content part.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the type of <paramref name="part"/> is not supported.
		/// </exception>
		public static MessageContentPart Create(IMessageContentPart part)
		{
			ArgumentNullException.ThrowIfNull(part, nameof(part));

			return part switch
			{
				TextContentPart textPart => new TextContentPart(textPart),
				HtmlContentPart htmlPart => new HtmlContentPart(htmlPart),
				_ => throw new ArgumentException("The content part type is not supported.", nameof(part))
			};
		}
	}
}
