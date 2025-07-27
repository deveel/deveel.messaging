//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// An endpoint is a node in a network that is able 
	/// to send or receive messages
	/// </summary>
	/// <remarks>
	/// <para>
	/// A endpoint is specialized to a specific protocol
	/// for sending and receiving messages: this can be
	/// an HTTP endpoint (eg. a URL), an e-mail address,
	/// a TCP/IP address, etc.
	/// </para>
	/// <para>
	/// A type of endpoint can be used by more than one
	/// protocol.
	/// </para>
	/// </remarks>
	public interface IEndpoint {
		/// <summary>
		/// Gets the type of the endpoint that is used.
		/// </summary>
		EndpointType Type { get; }

		/// <summary>
		/// Gets the address of the endpoint.
		/// </summary>
		string Address { get; }
	}
}