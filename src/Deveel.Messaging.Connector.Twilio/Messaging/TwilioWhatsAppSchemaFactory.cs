namespace Deveel.Messaging
{
	class TwilioWhatsAppSchemaFactory : IChannelSchemaFactory
	{
		public IChannelSchema CreateSchema() => TwilioChannelSchemas.TwilioWhatsApp;
	}
}
