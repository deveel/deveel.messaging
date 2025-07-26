//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// Enumerates all the known endpoint types that are used
	/// in the messaging system.
	/// </summary>
	public static class KnownEndpointTypes {
		/// <summary>
		/// The endpoint type for an email address.
		/// </summary>
		public const string Email = "email";

		/// <summary>
		/// The endpoint type for a phone number.
		/// </summary>
		public const string Phone = "phone";

		/// <summary>
		/// The endpoint type for an Uniform Resource Locator (URL).
		/// </summary>
		public const string Url = "url";

		/// <summary>
		/// The endpoint type for a user identifier within a system.
		/// </summary>
		public const string UserId = "user-id";

		/// <summary>
		/// The endpoint type for an application identifier.
		/// </summary>
		public const string Application = "app-id";

		/// <summary>
		/// The type of a endpoint that is registered
		/// within a system.
		/// </summary>
		public const string EndpointId = "endpoint-id";

		/// <summary>
		/// The endpoint type for a device identifier.
		/// </summary>
		public const string DeviceId = "device-id";

		/// <summary>
		/// Represents an endpoint identified by an alpha-numeric label
		/// (e.g., a username or a custom identifier).
		/// </summary>
		public const string Label = "label";
	}
}
