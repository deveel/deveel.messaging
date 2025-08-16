//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a Facebook quick reply button.
	/// </summary>
	public class FacebookQuickReply
    {
        public string ContentType { get; set; } = "text";
        public string Title { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}