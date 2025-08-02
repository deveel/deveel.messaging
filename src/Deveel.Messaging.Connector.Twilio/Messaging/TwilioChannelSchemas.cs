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
            .AddParameter(new ChannelParameter("AccountSid", ParameterType.String)
            {
                IsRequired = true,
                Description = "Twilio Account SID - found in your Twilio Console Dashboard"
            })
            .AddParameter(new ChannelParameter("AuthToken", ParameterType.String)
            {
                IsRequired = true,
                IsSensitive = true,
                Description = "Twilio Auth Token - found in your Twilio Console Dashboard"
            })
            .AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
            {
                IsRequired = false, // Changed to false since MessagingServiceSid can replace it
                Description = "Sender phone number in E.164 format (e.g., +1234567890) - must be a Twilio phone number. Required unless MessagingServiceSid is provided."
            })
            .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL to receive webhook notifications for message status updates and incoming messages"
            })
            .AddParameter(new ChannelParameter("StatusCallback", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL to receive delivery status callbacks for sent messages"
            })
            .AddParameter(new ChannelParameter("ValidityPeriod", ParameterType.Integer)
            {
                IsRequired = false,
                DefaultValue = 14400, // 4 hours in seconds
                Description = "The number of seconds that the message can remain in Twilio's outgoing message queue"
            })
            .AddParameter(new ChannelParameter("MaxPrice", ParameterType.Number)
            {
                IsRequired = false,
                Description = "The maximum price in US dollars that you are willing to pay for the message"
            })
            .AddParameter(new ChannelParameter("MessagingServiceSid", ParameterType.String)
            {
                IsRequired = false,
                Description = "The SID of the Messaging Service to use for the message. Can replace FromNumber for sending."
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Media)
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
            {
                CanSend = true,
                CanReceive = true
            })
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
            {
                CanSend = false,
                CanReceive = true
            })
            .AddAuthenticationType(AuthenticationType.Basic)
            .AddMessageProperty(new MessagePropertyConfiguration("To", ParameterType.String)
            {
                IsRequired = true,
                Description = "Destination phone number in E.164 format"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("Body", ParameterType.String)
            {
                IsRequired = false,
                Description = "The text content of the message (up to 1,600 characters)"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("MediaUrl", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL of media to send with the message"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("ValidityPeriod", ParameterType.Integer)
            {
                IsRequired = false,
                Description = "Message-specific validity period override"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("MaxPrice", ParameterType.Number)
            {
                IsRequired = false,
                Description = "Message-specific maximum price override"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("ProvideCallback", ParameterType.Boolean)
            {
                IsRequired = false,
                Description = "Whether to provide delivery status callbacks for this message"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("AttemptLimits", ParameterType.Integer)
            {
                IsRequired = false,
                Description = "Total number of attempts made by Twilio to deliver the message"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("SmartEncoded", ParameterType.Boolean)
            {
                IsRequired = false,
                Description = "Whether Twilio will automatically optimize the message encoding"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("PersistentAction", ParameterType.String)
            {
                IsRequired = false,
                Description = "Rich Communication Services (RCS) specific action"
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
            .AddParameter(new ChannelParameter("AccountSid", ParameterType.String)
            {
                IsRequired = true,
                Description = "Twilio Account SID - found in your Twilio Console Dashboard"
            })
            .AddParameter(new ChannelParameter("AuthToken", ParameterType.String)
            {
                IsRequired = true,
                IsSensitive = true,
                Description = "Twilio Auth Token - found in your Twilio Console Dashboard"
            })
            .AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
            {
                IsRequired = true,
                Description = "WhatsApp Business phone number in E.164 format (e.g., whatsapp:+1234567890) - must be a verified WhatsApp Business number"
            })
            .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL to receive webhook notifications for message status updates and incoming WhatsApp messages"
            })
            .AddParameter(new ChannelParameter("StatusCallback", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL to receive delivery status callbacks for sent WhatsApp messages"
            })
            .AddParameter(new ChannelParameter("ContentSid", ParameterType.String)
            {
                IsRequired = false,
                Description = "Twilio Content SID for approved WhatsApp template messages"
            })
            .AddParameter(new ChannelParameter("ContentVariables", ParameterType.String)
            {
                IsRequired = false,
                Description = "JSON string containing variables for WhatsApp template messages"
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Media)
            .AddContentType(MessageContentType.Template)
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
            {
                CanSend = true,
                CanReceive = true
            })
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
            {
                CanSend = false,
                CanReceive = true
            })
            .AddAuthenticationType(AuthenticationType.Basic)
            .AddMessageProperty(new MessagePropertyConfiguration("To", ParameterType.String)
            {
                IsRequired = true,
                Description = "Destination WhatsApp phone number in E.164 format (e.g., whatsapp:+1234567890)"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("Body", ParameterType.String)
            {
                IsRequired = false,
                Description = "The text content of the WhatsApp message"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("MediaUrl", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL of media to send with the WhatsApp message (images, documents, audio, video)"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("ContentSid", ParameterType.String)
            {
                IsRequired = false,
                Description = "Twilio Content SID for WhatsApp template messages"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("ContentVariables", ParameterType.String)
            {
                IsRequired = false,
                Description = "JSON string containing variables for WhatsApp template messages"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("ProvideCallback", ParameterType.Boolean)
            {
                IsRequired = false,
                Description = "Whether to provide delivery status callbacks for this WhatsApp message"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("PersistentAction", ParameterType.String)
            {
                IsRequired = false,
                Description = "Rich Communication Services (RCS) specific action for WhatsApp"
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
            .UpdateParameter("FromNumber", param => param.IsRequired = true) // Make FromNumber required since MessagingServiceSid is removed
            .RemoveContentType(MessageContentType.Media)
            .RemoveMessageProperty("MediaUrl")
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
            .RemoveMessageProperty("MediaUrl")
            .RemoveMessageProperty("PersistentAction");

        /// <summary>
        /// Gets a bulk messaging schema optimized for high-volume SMS campaigns.
        /// This schema includes messaging service support and advanced delivery options.
        /// </summary>
        public static ChannelSchema BulkSms => new ChannelSchema(TwilioSms, "Twilio Bulk SMS")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .UpdateParameter("MessagingServiceSid", param => param.IsRequired = true)
            .RemoveParameter("FromNumber") // Messaging Service handles sender selection
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
            .RemoveParameter("ContentSid")
            .RemoveParameter("ContentVariables")
            .RemoveContentType(MessageContentType.Template)
            .RemoveMessageProperty("ContentSid")
            .RemoveMessageProperty("ContentVariables")
            .RemoveMessageProperty("ProvideCallback")
            .RemoveMessageProperty("PersistentAction");

        /// <summary>
        /// Gets a template-focused WhatsApp schema optimized for business notifications using approved templates.
        /// This schema focuses on template messaging capabilities with webhook support for status tracking.
        /// </summary>
        public static ChannelSchema WhatsAppTemplates => new ChannelSchema(TwilioWhatsApp, "Twilio WhatsApp Templates")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveCapability(ChannelCapability.MediaAttachments)
            .UpdateParameter("ContentSid", param => param.IsRequired = true) // Template messaging requires Content SID
            .RemoveContentType(MessageContentType.Media)
            .RemoveMessageProperty("MediaUrl");
    }
}