//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Defines the schema for a communication channel, 
	/// including its properties, capabilities, and supported
	/// configurations.
	/// </summary>
	/// <remarks>
	/// This interface provides a standardized way to describe 
	/// the characteristics and capabilities of a channel used 
	/// for communication.
	/// It includes properties for identifying the channel provider, 
	/// type, version, and other descriptive information. Additionally, 
	/// it specifies the capabilities, parameters, content types, and 
	/// authentication types supported by the channel.</remarks>
	public interface IChannelSchema
	{
		/// <summary>
		/// Gets the channel provider identifier.
		/// </summary>
		string ChannelProvider { get; }

		/// <summary>
		/// Gets the type of communication channel.
		/// </summary>
		string ChannelType { get; }

		/// <summary>
		/// Gets the version of the schema or channel.
		/// </summary>
		string Version { get; }

		/// <summary>
		/// Gets the display name of the configuration schema.
		/// </summary>
		string? DisplayName { get; }

		/// <summary>
		/// Gets the list of capabilities supported by the channel.
		/// </summary>
		ChannelCapability Capabilities { get; }

		/// <summary>
		/// Gets the collection of channel endpoint configurations
		/// for the channel.
		/// </summary>
		IList<ChannelEndpointConfiguration> Endpoints { get; }

		/// <summary>
		/// Gets the collection of parameters that define the 
		/// configuration for the channel.
		/// </summary>
		IList<ChannelParameter> Parameters { get; }

		/// <summary>
		/// Gets the collection of configurations for message properties
		/// that are handled by the channel.
		/// </summary>
		IList<MessagePropertyConfiguration> MessageProperties { get; }

		/// <summary>
		/// Gets the list of content types supported by the channel.
		/// </summary>
		IList<MessageContentType> ContentTypes { get; }

		/// <summary>
		/// Gets the list of authentication types supported or required
		/// by the channel.
		/// </summary>
		IList<AuthenticationType> AuthenticationTypes { get; }
	}
}