//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a Facebook message attachment.
	/// </summary>
	public class FacebookAttachment
    {
        public string Type { get; set; } = string.Empty;
        public FacebookPayload Payload { get; set; } = new();
    }
}