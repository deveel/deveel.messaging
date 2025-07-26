//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a source of message that can be used to parse
	/// messages from a remote service.
	/// </summary>
	/// <remarks>
	/// The <see cref="MessageSource"/> struct provides a way to encapsulate 
	/// message content along with its metadata, such as content type and 
	/// encoding. 
	/// It supports various content types, including binary, JSON, XML, and
	/// plain text.
	/// </remarks>
	public readonly ref struct MessageSource
	{
		/// <summary>
		/// Constructs a new instance of <see cref="MessageSource"/> with the specified 
		/// content type, raw data, and optional content encoding.
		/// </summary>
		/// <param name="contentType">
		/// The type of content provided by the message source.
		/// </param>
		/// <param name="rawData">
		/// The raw data of the message content, represented as a byte array.
		/// </param>
		/// <param name="contentEncoding">
		/// The encoding used for the message content, if applicable.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// </exception>
		public MessageSource(string contentType, ReadOnlySpan<byte> rawData, string? contentEncoding = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(contentType, nameof(contentType));

			ContentType = contentType;
			Content = rawData;
			ContentEncoding = contentEncoding;
		}

		/// <summary>
		/// Gets the MIME type of the content.
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Gets the content encoding used to interpret the 
		/// message content.
		/// </summary>
		public string? ContentEncoding { get; }

		/// <summary>
		/// Gets the raw content of the message as a byte array.
		/// </summary>
		public ReadOnlySpan<byte> Content { get; }

		/// <summary>
		/// Represents the MIME type for binary data.
		/// </summary>
		/// <remarks>
		/// This constant is typically used to specify the content 
		/// type of HTTP requests or responses that contain binary 
		/// data, such as files or streams.
		/// </remarks>
		public const string BinaryContentType = "application/octet-stream";

		/// <summary>
		/// Represents the MIME type for JSON content.
		/// </summary>
		public const string JsonContentType = "application/json";

		/// <summary>
		/// Represents the MIME type for XML content.
		/// </summary>
		/// <remarks>
		/// This constant can be used to specify the content type for the message
		/// source involves XML data.</remarks>
		public const string XmlContentType = "application/xml";

		/// <summary>
		/// Represents the MIME type for plain text content.
		/// </summary>
		public const string TextContentType = "text/plain";

		/// <summary>
		/// Represents the content type for URL-encoded form data.
		/// </summary>
		/// <remarks>
		/// This constant is typically used to specify the content type of HTTP 
		/// requests that submit form data encoded as key-value pairs.
		/// </remarks>
		public const string UrlPostContentType = "application/x-www-form-urlencoded";

		private string AsEncodedString()
		{
			if (ContentEncoding is null)
				return Encoding.UTF8.GetString(Content);

			var encoding = Encoding.GetEncoding(ContentEncoding);
			return encoding.GetString(Content);
		}

		/// <summary>
		/// Converts the content to a text representation 
		/// if the content type is text.
		/// </summary>
		/// <remarks>
		/// This method interprets the content as text only if 
		/// the <see cref="ContentType"/> is set to a text-compatible type.
		/// </remarks>
		/// <returns>
		/// Returns a string containing the text representation of the content, 
		/// or <see langword="null"/> if the content is empty.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the <see cref="ContentType"/> is not compatible with text.
		/// </exception>
		public string? AsText()
		{
			if (ContentType != TextContentType)
				throw new InvalidOperationException($"Cannot interpret content of type '{ContentType}' as text.");

			return AsEncodedString();
		}

		/// <summary>
		/// Deserializes the content to an object of the specified type using JSON format.
		/// </summary>
		/// <typeparam name="T">
		/// The type to which the content is deserialized.
		/// </typeparam>
		/// <param name="options">
		/// Optional JSON serializer options to customize the deserialization process.
		/// </param>
		/// <returns>
		/// An object of type <typeparamref name="T"/> representing the deserialized 
		/// content, or <see langword="null"/> if the content is empty.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown if the content type is not JSON.</exception>
		public T? AsJson<T>(JsonSerializerOptions? options = null)
		{
			if (ContentType != JsonContentType)
				throw new InvalidOperationException($"Cannot deserialize content of type '{ContentType}' as JSON.");

			var jsonString = AsEncodedString();
			return JsonSerializer.Deserialize<T>(jsonString, options);
		}

		// TODO: Support for XML deserialization

		/// <summary>
		/// Converts the source content to a key/value dictionary
		/// that represents URL-encoded form data.
		/// </summary>
		/// <returns>
		/// Returns a dictionary containing the key/value pairs
		/// of the URL-encoded form data.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the content type is not <see cref="UrlPostContentType"/>.
		/// </exception>
		public IDictionary<string, string> AsUrlPostData()
		{
			if (ContentType != UrlPostContentType)
				throw new InvalidOperationException($"Cannot parse content of type '{ContentType}' as URL-encoded form data.");

			var postData = AsEncodedString();
			var pairs = postData.Split('&', StringSplitOptions.RemoveEmptyEntries);
			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in pairs)
			{
				var keyValue = pair.Split('=', 2);
				if (keyValue.Length == 2)
				{
					result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
				}
			}

			return result;
		}

		private static MessageSource FromString(string contentType, string content, Encoding? encoding = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(contentType, nameof(contentType));
			ArgumentNullException.ThrowIfNull(content, nameof(content));

			return new MessageSource(contentType, (encoding ?? Encoding.UTF8).GetBytes(content), (encoding ?? Encoding.UTF8).WebName);
		}

		/// <summary>
		/// Constucts a new <see cref="MessageSource"/> that is containing
		/// binary data.
		/// </summary>
		/// <param name="content">
		/// The binary content of the source.
		/// </param>
		/// <returns>
		/// Returns a new <see cref="MessageSource"/> that represents
		/// a binary content.
		/// </returns>
		public static MessageSource Binary(byte[] content)
			=> new MessageSource(BinaryContentType, content);

		/// <summary>
		/// Constructs a new <see cref="MessageSource"/> that is containing
		/// a JSON string.
		/// </summary>
		/// <param name="json">
		/// The JSON string content of the source.
		/// </param>
		/// <param name="encoding">
		/// A character encoding to use for the content.
		/// </param>
		/// <returns>
		/// Returns a new <see cref="MessageSource"/> that represents
		/// a JSON content.
		/// </returns>
		public static MessageSource Json(string json, Encoding? encoding = null)
			=> FromString(JsonContentType, json, encoding);

		/// <summary>
		/// Creates a <see cref="MessageSource"/> from an XML string.
		/// </summary>
		/// <param name="xml">The XML content to be used as the message source.</param>
		/// <param name="encoding">The character encoding to use. If <see langword="null"/>, the default encoding is applied.</param>
		/// <returns>
		/// Returns a <see cref="MessageSource"/> representing the provided XML content.
		/// </returns>
		public static MessageSource Xml(string xml, Encoding? encoding = null)
			=> FromString(XmlContentType, xml, encoding);

		/// <summary>
		/// Creates a <see cref="MessageSource"/> from a plain text string.
		/// </summary>
		/// <param name="text">The plain text content to be used as the message source.</param>
		/// <param name="encoding">The character encoding to use. If <see langword="null"/>, the default encoding is applied.</param>
		/// <returns>
		/// Returns a <see cref="MessageSource"/> representing the provided plain text content.
		/// </returns>
		public static MessageSource Text(string text, Encoding? encoding = null)
			=> FromString(TextContentType, text, encoding);

		/// <summary>
		/// Creates a <see cref="MessageSource"/> from URL-encoded form data.
		/// </summary>
		/// <param name="postData">The URL-encoded form data to be used as the message source.</param>
		/// <param name="encoding">The character encoding to use. If <see langword="null"/>, the default encoding is applied.</param>
		/// <returns>
		/// Returns a <see cref="MessageSource"/> representing the provided URL-encoded form data.
		/// </returns>
		public static MessageSource UrlPost(string postData, Encoding? encoding = null)
			=> FromString(UrlPostContentType, postData, encoding);
	}
}
