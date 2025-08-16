//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a Facebook message request.
	/// </summary>
	public class FacebookMessageRequest
    {
        public string Recipient { get; set; } = string.Empty;
        public FacebookMessage Message { get; set; } = new();
        public string MessagingType { get; set; } = "RESPONSE";
        public string NotificationType { get; set; } = "REGULAR";
        public string? Tag { get; set; }
        
        /// <summary>
        /// Quick replies for this message (max 13 according to Facebook limits).
        /// </summary>
        public List<FacebookQuickReply>? QuickReplies { get; set; }
    }
}