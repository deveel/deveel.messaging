namespace Deveel.Messaging
{
	class FacebookMessengerSchemaFactory : IChannelSchemaFactory
	{
		public IChannelSchema CreateSchema() => FacebookChannelSchemas.FacebookMessenger;
	}
}