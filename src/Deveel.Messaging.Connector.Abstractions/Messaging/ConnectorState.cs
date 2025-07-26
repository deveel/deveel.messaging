//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Enumerates the various states of a connector 
	/// during its lifecycle.
	/// </summary>
	/// <remarks>
	/// The <see cref="ConnectorState"/> enumeration defines the 
	/// possible states a connector can be in, from the initial 
	/// uninitialized state to the final shutdown state.
	/// This can be used to track and manage the connector's status 
	/// in an application.
	/// </remarks>
	public enum ConnectorState
	{
		/// <summary>
		/// The connector has not been initialized yet.
		/// </summary>
		Uninitialized,

		/// <summary>
		/// The connector is in the process of initializing.
		/// </summary>
		Initializing,

		/// <summary>
		/// Represents the readiness state of an connector
		/// to send or receive messages.
		/// </summary>
		Ready,

		/// <summary>
		/// Indicates that the connector is currently unhealthy
		/// and may not be able to process messages correctly.
		/// </summary>
		Error,

		/// <summary>
		/// Represents the state of being disconnected from the remote 
		/// network or service.
		/// </summary>
		Disconnected,

		/// <summary>
		/// The connector is in the process of shutting down and it
		/// might not be able to process new messages.
		/// </summary>
		ShuttingDown,

		/// <summary>
		/// Indicates that the connector has been shut down and is no 
		/// longer operational.
		/// </summary>
		Shutdown
	}
}
