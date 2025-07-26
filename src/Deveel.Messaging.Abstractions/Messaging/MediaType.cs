//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Lists the types of media that can be represented
	/// in a message content.
	/// </summary>
	public enum MediaType
	{
		/// <summary>
		/// Represents an image file.
		/// </summary>
		Image,

		/// <summary>
		/// Represents an audio file.
		/// </summary>
		Audio,

		/// <summary>
		/// Represents a video file.
		/// </summary>
		Video,

		/// <summary>
		/// Represents a document file.
		/// </summary>
		Document,

		/// <summary>
		/// Represents a generic file type.
		/// </summary>
		File
	}
}
