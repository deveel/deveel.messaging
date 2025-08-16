//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a Facebook message.
	/// </summary>
	public class FacebookMessage
    {
        public string? Text { get; set; }
        public FacebookAttachment? Attachment { get; set; }
        public List<FacebookQuickReply>? QuickReplies { get; set; }
    }
}