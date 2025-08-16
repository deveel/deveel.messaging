//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// A set of constants used in the Facebook Messenger Connector scope
	/// </summary>
	public static class FacebookConnectorConstants
	{
		/// <summary>
		/// The name of the provider (facebook)
		/// </summary>
		public const string Provider = "facebook";

		/// <summary>
		/// The Messenger channel type
		/// </summary>
		public const string MessengerChannel = "messenger";

		/// <summary>
		/// The Facebook Graph API version to use
		/// </summary>
		public const string GraphApiVersion = "v21.0";

		/// <summary>
		/// The base URL for Facebook Graph API
		/// </summary>
		public const string GraphApiBaseUrl = "https://graph.facebook.com";
	}
}