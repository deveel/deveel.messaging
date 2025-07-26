//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A content that is composed by multiple parts.
	/// </summary>
	public interface IMultipartContent : IMessageContent {
		/// <summary>
		/// Gets the parts that compose the content of
		/// the message.
		/// </summary>
		IEnumerable<IMessageContentPart> Parts { get; }
	}
}
