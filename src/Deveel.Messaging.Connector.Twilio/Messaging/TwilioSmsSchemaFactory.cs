namespace Deveel.Messaging
{
	class TwilioSmsSchemaFactory : IChannelSchemaFactory
	{
		public IChannelSchema CreateSchema() => TwilioChannelSchemas.TwilioSms;
	}
}
