//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents content that consists of binary data, 
	/// providing access to the raw data and its MIME type.
	/// </summary>
	public interface IBinaryContent : IMessageContent
	{
		/// <summary>
		/// Gets the binary data of the content.
		/// </summary>
		byte[] RawData { get; }

		/// <summary>
		/// Gets the MIME type of the binary content.
		/// </summary>
		string MimeType { get; }
	}
}
