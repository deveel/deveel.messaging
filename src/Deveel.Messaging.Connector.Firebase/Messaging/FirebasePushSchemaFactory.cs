//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Factory class for creating Firebase push notification channel schemas.
	/// </summary>
	/// <remarks>
	/// This factory is used by the <see cref="ChannelSchemaAttribute"/> to provide
	/// the master schema for Firebase push notification connectors.
	/// </remarks>
	internal class FirebasePushSchemaFactory : IChannelSchemaFactory
	{
		/// <inheritdoc/>
		public IChannelSchema CreateSchema() => FirebaseChannelSchemas.FirebasePush;
	}
}