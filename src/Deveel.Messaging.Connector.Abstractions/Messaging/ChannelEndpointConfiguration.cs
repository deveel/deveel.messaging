//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents the configuration settings for a type of endpoint in messages 
	/// processed by a channel, including its capabilities and roles.
	/// </summary>
	/// <remarks>
	/// This class is used to define the characteristics of a type of endpoint 
	/// used by messages processed by a channel, such as whether it is required 
	/// and its ability to send or receive data.
	/// </remarks>
	public sealed class ChannelEndpointConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelEndpointConfiguration"/> 
		/// class with the specified type.
		/// </summary>
		/// <param name="type">
		/// The type of the endpoint to be handled by the channel.
		/// </param>
		public ChannelEndpointConfiguration(EndpointType type)
		{
			Type = type;
		}

		/// <summary>
		/// Gets the type of the endpoint that this configuration applies to.
		/// </summary>
		public EndpointType Type { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the endpoint is required
		/// to be present in messages processed by the channel.
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this type of endpoint
		/// can be used to send messages from.
		/// </summary>
		public bool CanSend { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether this type of endpoint
		/// can be used to receive messages.
		/// </summary>
		public bool CanReceive { get; set; } = true;

		/// <summary>
		/// Gets or sets a description of this endpoint configuration.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Represents a configuration for a channel endpoint that 
		/// matches any endpoint.
		/// </summary>
		/// <remarks>
		/// This static field uses the EndpointType.Any enumeration value, 
		/// indicating that it can be used to configure a channel to accept 
		/// messages being sent from or received to any endpoint. 
		/// This is useful in scenarios where the specific endpoint is not 
		/// predetermined or when a channel should be open to all incoming connections.
		/// </remarks>
		public static ChannelEndpointConfiguration AnyEndpoint = new ChannelEndpointConfiguration(EndpointType.Any);

		/// <summary>
		/// Gets a value indicating whether this configuration represents a wildcard endpoint.
		/// </summary>
		public bool IsWildcard => Type == EndpointType.Any;

		/// <summary>
		/// Determines whether this endpoint configuration matches the specified endpoint type.
		/// </summary>
		/// <param name="endpointType">The endpoint type to match against.</param>
		/// <returns>True if the configuration matches the specified type; otherwise, false.</returns>
		public bool Matches(EndpointType endpointType)
		{
			if (IsWildcard) return true;
			return Type == endpointType;
		}

		/// <summary>
		/// Returns the string representation of this endpoint configuration's type.
		/// </summary>
		/// <returns>The string representation of the endpoint type.</returns>
		public override string ToString()
		{
			return Type.ToString();
		}
	}
}
