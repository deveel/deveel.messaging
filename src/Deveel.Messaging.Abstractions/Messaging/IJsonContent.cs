//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a content of a message that is encoded in JSON format.
	/// </summary>
	public interface IJsonContent : IMessageContent
	{
		/// <summary>
		/// Gets the JSON content of the message.
		/// </summary>
		string Json { get; }
	}
}
