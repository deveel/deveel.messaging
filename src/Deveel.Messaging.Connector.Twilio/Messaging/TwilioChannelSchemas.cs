//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides pre-configured channel schemas for Twilio messaging services.
    /// </summary>
    public static class TwilioChannelSchemas
    {
        /// <summary>
        /// Gets the comprehensive base schema for Twilio SMS messaging that supports
        /// all available capabilities and configurations.
        /// </summary>
        /// <remarks>
        /// This schema includes all Twilio SMS capabilities including sending, receiving,
        /// status queries, bulk messaging, and webhook support. It can be used as-is
        /// or derived to create more restrictive configurations for specific use cases.
        /// </remarks>
        public static ChannelSchema TwilioSms => new ChannelSchema("Twilio", "SMS", "1.0.0")
            .WithDisplayName("Twilio SMS Connector")
            .WithCapabilities(
                ChannelCapability.SendMessages | 
                ChannelCapability.ReceiveMessages |
                ChannelCapability.MessageStatusQuery |
                ChannelCapability.BulkMessaging |
                ChannelCapability.HealthCheck)
            .AddParameter(new ChannelParameter("AccountSid", DataType.String)
            {
                IsRequired = true,
                Description = "Twilio Account SID - found in your Twilio Console Dashboard"
            })
            .AddParameter(new ChannelParameter("AuthToken", DataType.String)
            {
                IsRequired = true,
                IsSensitive = true,
                Description = "Twilio Auth Token - found in your Twilio Console Dashboard"
            })
            .AddParameter(new ChannelParameter("WebhookUrl", DataType.String)
            {
                IsRequired = false,
                Description = "URL to receive webhook notifications for message status updates and incoming messages"
            })
            .AddParameter(new ChannelParameter("StatusCallback", DataType.String)
            {
                IsRequired = false,
                Description = "URL to receive delivery status callbacks for sent messages"
            })
            .AddParameter(new ChannelParameter("ValidityPeriod", DataType.Integer)
            {
                IsRequired = false,
                DefaultValue = 14400, // 4 hours in seconds
                Description = "The number of seconds that the message can remain in Twilio's outgoing message queue"
            })
            .AddParameter(new ChannelParameter("MaxPrice", DataType.Number)
            {
                IsRequired = false,
                Description = "The maximum price in US dollars that you are willing to pay for the message"
            })
            .AddParameter(new ChannelParameter("MessagingServiceSid", DataType.String)
            {
                IsRequired = false,
                Description = "The SID of the Messaging Service to use for the message. Can replace Sender for sending."
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Media)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
                e.IsRequired = true; // Phone number is required for both sending and receiving
            })
            .HandlesMessageEndpoint(EndpointType.Url, e =>
            {
                e.CanSend = false;
                e.CanReceive = true;
            })
            .AddAuthenticationType(AuthenticationType.Basic)
            // Body and MediaUrl are derived from message content, not separate message properties
            // Body comes from TextContent.Text when ContentType = PlainText
            // MediaUrl comes from MediaContent.FileUrl when ContentType = Media
            .AddMessageProperty("ValidityPeriod", DataType.Integer, p =>
            {
                p.IsRequired = false;
                p.Description = "Message-specific validity period override";
            })
            .AddMessageProperty("MaxPrice", DataType.Number, p =>
            {
                p.IsRequired = false;
                p.Description = "Message-specific maximum price override";
            })
            .AddMessageProperty("ProvideCallback", DataType.Boolean, p =>
            {
                p.IsRequired = false;
                p.Description = "Whether to provide delivery status callbacks for this message";
            })
            .AddMessageProperty("AttemptLimits", DataType.Integer, p =>
            {
                p.IsRequired = false;
                p.Description = "Total number of attempts made by Twilio to deliver the message";
            })
            .AddMessageProperty("SmartEncoded", DataType.Boolean, p =>
            {
                p.IsRequired = false;
                p.Description = "Whether Twilio will automatically optimize the message encoding";
            })
            .AddMessageProperty("PersistentAction", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Rich Communication Services (RCS) specific action";
            });

        /// <summary>
        /// Gets the comprehensive base schema for Twilio WhatsApp messaging that supports
        /// all available capabilities and configurations.
        /// </summary>
        /// <remarks>
        /// This schema includes all Twilio WhatsApp Business API capabilities including sending, receiving,
        /// status queries, template messages, media attachments, and webhook support. It can be used as-is
        /// or derived to create more restrictive configurations for specific use cases.
        /// </remarks>
        public static ChannelSchema TwilioWhatsApp => new ChannelSchema("Twilio", "WhatsApp", "1.0.0")
            .WithDisplayName("Twilio WhatsApp Business API Connector")
            .WithCapabilities(
                ChannelCapability.SendMessages | 
                ChannelCapability.ReceiveMessages |
                ChannelCapability.MessageStatusQuery |
                ChannelCapability.Templates |
                ChannelCapability.MediaAttachments |
                ChannelCapability.HealthCheck)
            .AddParameter("AccountSid", DataType.String, p =>
            {
                p.IsRequired = true;
                p.Description = "Twilio Account SID - found in your Twilio Console Dashboard";
            })
            .AddParameter("AuthToken", DataType.String, p =>
            {
                p.IsRequired = true;
                p.IsSensitive = true;
                p.Description = "Twilio Auth Token - found in your Twilio Console Dashboard";
            })
            .AddParameter("WebhookUrl", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "URL to receive webhook notifications for message status updates and incoming WhatsApp messages";
            })
            .AddParameter("StatusCallback", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "URL to receive delivery status callbacks for sent WhatsApp messages";
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Media)
            .AddContentType(MessageContentType.Template)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
                e.IsRequired = true; // WhatsApp phone number is required for both sending and receiving
            })
            .HandlesMessageEndpoint(EndpointType.Url, e =>
            {
                e.CanSend = false;
                e.CanReceive = true;
            })
            .AddAuthenticationType(AuthenticationType.Basic)
            .AddMessageProperty("ProvideCallback", DataType.Boolean, p =>
            {
                p.IsRequired = false;
                p.Description = "Whether to provide delivery status callbacks for this WhatsApp message";
            })
            .AddMessageProperty("PersistentAction", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Rich Communication Services (RCS) specific action for WhatsApp";
            });

        /// <summary>
        /// Gets a simplified SMS-only schema for basic messaging use cases.
        /// This schema removes webhook capabilities and advanced features.
        /// </summary>
        public static ChannelSchema SimpleSms => new ChannelSchema(TwilioSms, "Twilio Simple SMS")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveCapability(ChannelCapability.BulkMessaging)
            .RemoveParameter("WebhookUrl")
            .RemoveParameter("StatusCallback")
            .RemoveParameter("MessagingServiceSid")
            .RemoveContentType(MessageContentType.Media)
            .RemoveMessageProperty("ProvideCallback")
            .RemoveMessageProperty("PersistentAction")
            .RemoveMessageProperty("SmartEncoded");

        /// <summary>
        /// Gets a send-only schema optimized for notifications and alerts.
        /// This schema includes status tracking but removes receiving capabilities.
        /// </summary>
        public static ChannelSchema NotificationSms => new ChannelSchema(TwilioSms, "Twilio Notification SMS")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveParameter("WebhookUrl")
            .RemoveContentType(MessageContentType.Media)
            .RemoveMessageProperty("PersistentAction");

        /// <summary>
        /// Gets a bulk messaging schema optimized for high-volume SMS campaigns.
        /// This schema includes messaging service support and advanced delivery options.
        /// </summary>
        public static ChannelSchema BulkSms => new ChannelSchema(TwilioSms, "Twilio Bulk SMS")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .UpdateParameter("MessagingServiceSid", param => param.IsRequired = true)
            .UpdateEndpoint(EndpointType.PhoneNumber, endpoint => 
            {
                endpoint.IsRequired = false; // Not required when MessagingServiceSid is used
                endpoint.CanReceive = false; // Send-only
            })
            .RemoveMessageProperty("PersistentAction");

        /// <summary>
        /// Gets a simplified WhatsApp schema for basic messaging use cases.
        /// This schema removes webhook capabilities and template features for simple text/media messaging.
        /// </summary>
        public static ChannelSchema SimpleWhatsApp => new ChannelSchema(TwilioWhatsApp, "Twilio Simple WhatsApp")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveCapability(ChannelCapability.Templates)
            .RemoveParameter("WebhookUrl")
            .RemoveParameter("StatusCallback")
            // ContentSid and ContentVariables are no longer parameters or message properties
            .RemoveContentType(MessageContentType.Template)
            .RemoveMessageProperty("ProvideCallback")
            .RemoveMessageProperty("PersistentAction");

        /// <summary>
        /// Gets a template-focused WhatsApp schema optimized for business notifications using approved templates.
        /// This schema focuses on template messaging capabilities with webhook support for status tracking.
        /// </summary>
        public static ChannelSchema WhatsAppTemplates => new ChannelSchema(TwilioWhatsApp, "Twilio WhatsApp Templates")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveCapability(ChannelCapability.MediaAttachments)
            // ContentSid is now derived from TemplateContent.TemplateId, not a separate parameter
            .RemoveContentType(MessageContentType.Media);
    }
}