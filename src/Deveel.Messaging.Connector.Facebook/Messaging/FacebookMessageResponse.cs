//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a Facebook message response.
	/// </summary>
	public class FacebookMessageResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
    }
}