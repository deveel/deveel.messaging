//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a Facebook attachment payload.
	/// </summary>
	public class FacebookPayload
    {
        public string Url { get; set; } = string.Empty;
        public bool IsReusable { get; set; } = true;
    }
}