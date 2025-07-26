//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A content part that is represented as a plain-text.
	/// </summary>
	/// <seealso cref="IMessageContentPart"/>
	/// <seealso cref="ITextContent"/>
	public interface ITextContentPart : IMessageContentPart, ITextContent {
	}
}
