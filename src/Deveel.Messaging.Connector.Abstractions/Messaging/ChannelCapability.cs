//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Specifies the capabilities that a channel can support 
	/// in a messaging system.
	/// </summary>
	/// <remarks>
	/// This enumeration defines various features that a channel
	/// may implement, such as sending and receiving messages, querying
	/// message status, handling message states, supporting media attachments,
	/// using message templates, and enabling bulk messaging.
	/// Each capability allows the channel to perform specific operations within
	/// the messaging infrastructure.
	/// </remarks>
	[Flags]
	public enum ChannelCapability
	{
		/// <summary>
		/// Represents the ability to send messages.
		/// </summary>
		SendMessages = 1,

		/// <summary>
		/// Represents the ability to receive messages.
		/// </summary>
		ReceiveMessages = 2,

		/// <summary>
		/// The connector can query the status of messages.
		/// </summary>
		MessageStatusQuery = 4,

		/// <summary>
		/// The connector can handle the state of messages
		/// from the provider.
		/// </summary>
		HandlerMessageState = 8,

		/// <summary>
		/// Indicates that the connector can handle
		/// media attachments to messages.
		/// </summary>
		MediaAttachments = 16,

		/// <summary>
		/// The connector supports templates for messages,
		/// allowing for dynamic content generation.
		/// </summary>
		Templates = 32,

		/// <summary>
		/// Provides functionality for sending messages in bulk 
		/// to multiple recipients.
		/// </summary>
		BulkMessaging = 64,

		/// <summary>
		/// The connector supports checking the health of the 
		/// messaging system.
		/// </summary>
		HealthCheck = 128,
	}
}
