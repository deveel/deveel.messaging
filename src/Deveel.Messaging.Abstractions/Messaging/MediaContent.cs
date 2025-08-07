//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a content of a message that is a media object,
	/// like an image, audio, or video file.
	/// </summary>
	public class MediaContent : MessageContent, IMediaContent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MediaContent"/> class.
		/// </summary>
		public MediaContent()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MediaContent"/> class with
		/// the specified media type, file name and data.
		/// </summary>
		/// <param name="mediaType">
		/// The type of media that is represented by the content.
		/// </param>
		/// <param name="fileName">
		/// The name of the file that is attached to the content.
		/// </param>
		/// <param name="data">
		/// The content of the media file, encoded in base64.
		/// </param>
		public MediaContent(MediaType mediaType, string? fileName, byte[]? data)
		{
			MediaType = mediaType;
			FileName = fileName;
			Data = data;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MediaContent"/> class with 
		/// the specified media type, file name, and file URL.
		/// </summary>
		/// <param name="mediaType">
		/// The type of media content, such as image, video, or audio.
		/// </param>
		/// <param name="fileName">
		/// The name of the file, including its extension.
		/// </param>
		/// <param name="fileUrl">
		/// The URL where the media file is located.
		/// </param>
		public MediaContent(MediaType mediaType, string? fileName, string? fileUrl)
		{
			MediaType = mediaType;
			FileName = fileName;
			FileUrl = fileUrl;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MediaContent"/> class 
		/// using the specified media content.
		/// </summary>
		/// <param name="content">
		/// The media content to initialize the instance with.
		/// </param>
		public MediaContent(IMediaContent content)
		{
			MediaType = content?.MediaType ?? new MediaType();
			FileName = content?.FileName ?? "";
			FileUrl = content?.FileUrl ?? "";
			Data = content?.Data ?? Array.Empty<byte>();
		}

		/// <inheritdoc />
		public MediaType MediaType { get; set; }

		/// <inheritdoc />
		public string? FileName { get; set; }

		/// <inheritdoc />
		public string? FileUrl { get; set; }

		/// <inheritdoc />
		public byte[]? Data { get; set; }

		/// <inheritdoc/>
		public override MessageContentType ContentType => MessageContentType.Media;
	}
}
