//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides pre-configured channel schemas for Facebook Messenger messaging services.
    /// </summary>
    public static class FacebookChannelSchemas
    {
        /// <summary>
        /// Gets the comprehensive base schema for Facebook Messenger that supports
        /// all available capabilities and configurations.
        /// </summary>
        /// <remarks>
        /// This schema includes all Facebook Messenger capabilities including sending, receiving,
        /// media attachments, and webhook support. It can be used as-is
        /// or derived to create more restrictive configurations for specific use cases.
        /// </remarks>
        public static ChannelSchema FacebookMessenger => new ChannelSchema(FacebookConnectorConstants.Provider, FacebookConnectorConstants.MessengerChannel, "1.0.0")
            .WithDisplayName("Facebook Messenger Connector")
            .WithCapabilities(
                ChannelCapability.SendMessages | 
                ChannelCapability.ReceiveMessages |
                ChannelCapability.MediaAttachments |
                ChannelCapability.HealthCheck)
            .AddParameter(new ChannelParameter("PageAccessToken", DataType.String)
            {
                IsRequired = true,
                IsSensitive = true,
                Description = "Facebook Page Access Token - obtained from Facebook App settings"
            })
            .AddParameter(new ChannelParameter("PageId", DataType.String)
            {
                IsRequired = true,
                Description = "Facebook Page ID - the ID of the Facebook Page to send messages from"
            })
            .AddParameter(new ChannelParameter("WebhookUrl", DataType.String)
            {
                IsRequired = false,
                Description = "URL to receive webhook notifications for incoming messages"
            })
            .AddParameter(new ChannelParameter("VerifyToken", DataType.String)
            {
                IsRequired = false,
                IsSensitive = true,
                Description = "Webhook verification token for Facebook webhook validation"
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Media)
            .HandlesMessageEndpoint(EndpointType.UserId, e =>
            {
                e.CanSend = true;
                e.CanReceive = true;
                e.IsRequired = true; // Facebook User ID (PSID) is required for both sending and receiving
            })
            .HandlesMessageEndpoint(EndpointType.EmailAddress, e =>
            {
                e.CanSend = true; // Allow EmailAddress so it passes schema validation
                e.CanReceive = false;
                e.IsRequired = false;
            })
            .HandlesMessageEndpoint(EndpointType.Url, e =>
            {
                e.CanSend = false;
                e.CanReceive = true;
            })
            .AddAuthenticationType(AuthenticationType.Token)
            .AddMessageProperty("QuickReplies", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "JSON array of quick reply options for the message";
            })
            .AddMessageProperty("NotificationType", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Push notification type: REGULAR, SILENT_PUSH, or NO_PUSH";
            })
            .AddMessageProperty("MessagingType", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Message type: RESPONSE, UPDATE, MESSAGE_TAG, or NON_PROMOTIONAL_SUBSCRIPTION";
            })
            .AddMessageProperty("Tag", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Message tag for sending outside 24-hour window";
            });

        /// <summary>
        /// Gets a simplified Messenger schema for basic messaging use cases.
        /// This schema removes webhook capabilities and advanced features.
        /// </summary>
        public static ChannelSchema SimpleMessenger => new ChannelSchema(FacebookMessenger, "Facebook Simple Messenger")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveParameter("WebhookUrl")
            .RemoveParameter("VerifyToken")
            .RemoveContentType(MessageContentType.Media)
            .RemoveMessageProperty("QuickReplies")
            .RemoveMessageProperty("Tag");

        /// <summary>
        /// Gets a send-only schema optimized for notifications and alerts.
        /// This schema includes media support but removes receiving capabilities.
        /// </summary>
        public static ChannelSchema NotificationMessenger => new ChannelSchema(FacebookMessenger, "Facebook Notification Messenger")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .RemoveParameter("WebhookUrl")
            .RemoveParameter("VerifyToken")
            .RemoveMessageProperty("QuickReplies");

        /// <summary>
        /// Gets a media-focused schema optimized for rich content messaging.
        /// This schema includes all media capabilities and interactive features.
        /// </summary>
        public static ChannelSchema MediaMessenger => new ChannelSchema(FacebookMessenger, "Facebook Media Messenger")
            .RemoveCapability(ChannelCapability.ReceiveMessages)
            .AddMessageProperty("Attachment", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "JSON object defining attachment (image, audio, video, file)";
            })
            .AddMessageProperty("Template", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "JSON object defining structured message template";
            });
    }
}