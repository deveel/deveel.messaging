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
		/// Gets a value indicating whether the schema operates in strict mode.
		/// </summary>
		/// <remarks>
		/// When <c>true</c>, validation will reject unknown parameters in connection settings 
		/// and unknown properties in message properties. When <c>false</c>, unknown parameters 
		/// and properties are allowed and will not cause validation errors.
		/// </remarks>
		bool IsStrict { get; }

		/// <summary>
		/// Gets the list of capabilities supported by the channel.
		/// </summary>
		ChannelCapability Capabilities { get; }

		/// <summary>
		/// Gets the collection of channel endpoint configurations
		/// for the channel.
		/// </summary>
		IReadOnlyList<ChannelEndpointConfiguration> Endpoints { get; }

		/// <summary>
		/// Gets the collection of parameters that define the 
		/// configuration for the channel.
		/// </summary>
		IReadOnlyList<ChannelParameter> Parameters { get; }

		/// <summary>
		/// Gets the collection of configurations for message properties
		/// that are handled by the channel.
		/// </summary>
		IReadOnlyList<MessagePropertyConfiguration> MessageProperties { get; }

		/// <summary>
		/// Gets the list of content types supported by the channel.
		/// </summary>
		IReadOnlyList<MessageContentType> ContentTypes { get; }

		/// <summary>
		/// Gets the collection of authentication configurations that define 
		/// the detailed authentication requirements for the channel.
		/// </summary>
		/// <remarks>
		/// Each authentication configuration specifies the exact connection settings
		/// fields required for a particular authentication method, providing more
		/// precise control than generic authentication type validation.
		/// </remarks>
		IReadOnlyList<AuthenticationConfiguration> AuthenticationConfigurations { get; }
	}
}