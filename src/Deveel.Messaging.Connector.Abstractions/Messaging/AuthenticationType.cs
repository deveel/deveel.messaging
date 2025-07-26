//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System;

namespace Deveel.Messaging
{
	/// <summary>
	/// Specifies the types of authentication that can be used
	/// to connect to a messaging service.
	/// </summary>
	/// <remarks>
	/// This enumeration provides various authentication methods, 
	/// including basic, token-based, client credentials, certificate, 
	/// and custom authentication.
	/// Each type represents a different  mechanism for verifying identity 
	/// and granting access, suitable for different security requirements  
	/// and scenarios.
	/// </remarks>
	public enum AuthenticationType
	{
		/// <summary>
		/// No authentication is required.
		/// </summary>
		None,

		/// <summary>
		/// A key that is provided in the header of the request to APIs.
		/// </summary>
		ApiKey,

		/// <summary>
		/// Basic authentication using username and password.
		/// </summary>
		Basic,

		/// <summary>
		/// Token-based authentication, such as OAuth or JWT.
		/// </summary>
		/// <remarks>
		/// The difference between <see cref="Token"/> and <see cref="ClientCredentials"/> 
		/// is that <see cref="Token"/> typically refers to a token that is
		/// already obtained and used for subsequent requests, while
		/// the <see cref="ClientCredentials"/> method is used to obtain a token
		/// after providing client credentials (like client ID and secret).
		/// It might support various token types, including JWT or OAuth tokens,
		/// </remarks>
		Token,

		/// <summary>
		/// Client credentials authentication.
		/// </summary>
		/// <remarks>
		/// This method is often used in OAuth 2.0 flows where the client
		/// dentifies itself using a client ID and secret to obtain an access token.
		/// </remarks>
		ClientCredentials,

		/// <summary>
		/// Certificate-based authentication.
		/// </summary>
		Certificate,

		/// <summary>
		/// Custom authentication method defined by the connector.
		/// </summary>
		Custom
	}
}
