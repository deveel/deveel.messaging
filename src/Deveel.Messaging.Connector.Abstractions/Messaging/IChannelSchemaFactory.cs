//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Defines the contract for creating channel schemas.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface are used by the <see cref="ChannelSchemaAttribute"/>
	/// to provide master schemas for channel connectors.
	/// </remarks>
	public interface IChannelSchemaFactory
	{
		/// <summary>
		/// Creates and returns the master schema for a channel connector.
		/// </summary>
		/// <returns>The master schema instance.</returns>
		IChannelSchema CreateSchema();
	}
}
