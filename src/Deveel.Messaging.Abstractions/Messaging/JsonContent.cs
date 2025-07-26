//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a content of a message that is encoded in JSON format.
	/// </summary>
	public class JsonContent : MessageContent, IJsonContent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonContent"/> class.
		/// </summary>
		public JsonContent() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonContent"/> 
		/// class with the specified JSON string.
		/// </summary>
		/// <param name="json">
		/// The JSON string to be encapsulated by this instance.
		/// </param>
		public JsonContent(string json)
		{
			Json = json;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonContent"/> class 
		/// with the specified JSON content.
		/// </summary>
		/// <param name="jsonContent">
		/// An object implementing <see cref="IJsonContent"/> that provides the 
		/// JSON string.
		/// </param>
		public JsonContent(IJsonContent jsonContent)
		{
			Json = jsonContent?.Json ?? "";
		}

		/// <inheritdoc />
		public override MessageContentType ContentType => MessageContentType.Json;

		/// <inheritdoc/>
		public string Json { get; set; } = "";
	}
}
