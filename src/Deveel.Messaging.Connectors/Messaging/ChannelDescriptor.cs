//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides descriptive information about a registered channel.
	/// </summary>
	/// <remarks>
	/// This class encapsulates metadata about a channel registration, including
	/// its identifier, connector type, master schema, and capabilities.
	/// </remarks>
	public sealed class ChannelDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelDescriptor"/> class.
		/// </summary>
		/// <param name="channelId">The unique identifier for the channel.</param>
		/// <param name="connectorType">The type of connector associated with the channel.</param>
		/// <param name="masterSchema">The master schema that defines the channel's capabilities.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when any of the required parameters is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when connectorType does not implement IChannelConnector.
		/// </exception>
		public ChannelDescriptor(string channelId, Type connectorType, IChannelSchema masterSchema)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId, nameof(channelId));
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));
			ArgumentNullException.ThrowIfNull(masterSchema, nameof(masterSchema));

			if (!typeof(IChannelConnector).IsAssignableFrom(connectorType))
			{
				throw new ArgumentException($"Type '{connectorType.Name}' must implement IChannelConnector.", nameof(connectorType));
			}

			ChannelId = channelId;
			ConnectorType = connectorType;
			MasterSchema = masterSchema;
		}

		/// <summary>
		/// Gets the unique identifier for this channel.
		/// </summary>
		public string ChannelId { get; }

		/// <summary>
		/// Gets the type of connector associated with this channel.
		/// </summary>
		public Type ConnectorType { get; }

		/// <summary>
		/// Gets the master schema that defines this channel's capabilities and requirements.
		/// </summary>
		public IChannelSchema MasterSchema { get; }

		/// <summary>
		/// Gets the logical identity of the master schema.
		/// </summary>
		public string LogicalIdentity => MasterSchema.GetLogicalIdentity();

		/// <summary>
		/// Gets the channel provider from the master schema.
		/// </summary>
		public string ChannelProvider => MasterSchema.ChannelProvider;

		/// <summary>
		/// Gets the channel type from the master schema.
		/// </summary>
		public string ChannelType => MasterSchema.ChannelType;

		/// <summary>
		/// Gets the version from the master schema.
		/// </summary>
		public string Version => MasterSchema.Version;

		/// <summary>
		/// Gets the display name from the master schema.
		/// </summary>
		public string? DisplayName => MasterSchema.DisplayName;

		/// <summary>
		/// Gets the capabilities from the master schema.
		/// </summary>
		public ChannelCapability Capabilities => MasterSchema.Capabilities;

		/// <summary>
		/// Determines whether this channel supports the specified capability.
		/// </summary>
		/// <param name="capability">The capability to check.</param>
		/// <returns>True if the channel supports the capability; otherwise, false.</returns>
		public bool SupportsCapability(ChannelCapability capability)
		{
			return MasterSchema.Capabilities.HasFlag(capability);
		}

		/// <summary>
		/// Determines whether this channel supports the specified content type.
		/// </summary>
		/// <param name="contentType">The content type to check.</param>
		/// <returns>True if the channel supports the content type; otherwise, false.</returns>
		public bool SupportsContentType(MessageContentType contentType)
		{
			return MasterSchema.ContentTypes.Contains(contentType);
		}

		/// <summary>
		/// Determines whether this channel supports the specified endpoint type.
		/// </summary>
		/// <param name="endpointType">The endpoint type to check.</param>
		/// <returns>True if the channel supports the endpoint type; otherwise, false.</returns>
		public bool SupportsEndpointType(EndpointType endpointType)
		{
			return MasterSchema.Endpoints.Any(e => e.Type == endpointType || e.Type == EndpointType.Any);
		}

		/// <summary>
		/// Determines whether this channel supports the specified authentication type.
		/// </summary>
		/// <param name="authenticationType">The authentication type to check.</param>
		/// <returns>True if the channel supports the authentication type; otherwise, false.</returns>
		public bool SupportsAuthenticationType(AuthenticationType authenticationType)
		{
			return MasterSchema.AuthenticationTypes.Contains(authenticationType);
		}

		/// <summary>
		/// Returns a string representation of this channel descriptor.
		/// </summary>
		/// <returns>A string containing the channel ID and logical identity.</returns>
		public override string ToString()
		{
			return $"{ChannelId} ({LogicalIdentity})";
		}

		/// <summary>
		/// Determines whether the specified object is equal to this channel descriptor.
		/// </summary>
		/// <param name="obj">The object to compare.</param>
		/// <returns>True if the objects are equal; otherwise, false.</returns>
		public override bool Equals(object? obj)
		{
			return obj is ChannelDescriptor other && string.Equals(ChannelId, other.ChannelId, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Gets the hash code for this channel descriptor.
		/// </summary>
		/// <returns>The hash code based on the channel ID.</returns>
		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(ChannelId);
		}
	}
}