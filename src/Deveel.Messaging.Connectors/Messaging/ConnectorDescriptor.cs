//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides descriptive information about a registered connector and its master schema.
	/// </summary>
	/// <remarks>
	/// This class encapsulates metadata about a registered channel connector, including
	/// its type, master schema, and convenient query methods for filtering and discovery.
	/// </remarks>
	public sealed class ConnectorDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectorDescriptor"/> class.
		/// </summary>
		/// <param name="connectorType">The type of the connector.</param>
		/// <param name="schema">The master schema associated with the connector.</param>
		/// <exception cref="ArgumentNullException">Thrown when connectorType or schema is null.</exception>
		public ConnectorDescriptor(Type connectorType, IChannelSchema schema)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));
			ArgumentNullException.ThrowIfNull(schema, nameof(schema));

			ConnectorType = connectorType;
			Schema = schema;
		}

		/// <summary>
		/// Gets the type of the connector.
		/// </summary>
		public Type ConnectorType { get; }

		/// <summary>
		/// Gets the master schema associated with the connector.
		/// </summary>
		public IChannelSchema Schema { get; }

		/// <summary>
		/// Gets the channel provider from the master schema.
		/// </summary>
		public string ChannelProvider => Schema.ChannelProvider;

		/// <summary>
		/// Gets the channel type from the master schema.
		/// </summary>
		public string ChannelType => Schema.ChannelType;

		/// <summary>
		/// Gets the display name from the master schema, or the connector type name if not available.
		/// </summary>
		public string DisplayName => Schema.DisplayName ?? ConnectorType.Name;

		/// <summary>
		/// Gets the capabilities supported by the connector from the master schema.
		/// </summary>
		public ChannelCapability Capabilities => Schema.Capabilities;

		/// <summary>
		/// Determines whether the connector supports the specified capability.
		/// </summary>
		/// <param name="capability">The capability to check.</param>
		/// <returns>True if the connector supports the capability; otherwise, false.</returns>
		public bool SupportsCapability(ChannelCapability capability)
		{
			return Schema.Capabilities.HasFlag(capability);
		}

		/// <summary>
		/// Determines whether the connector supports any of the specified capabilities.
		/// </summary>
		/// <param name="capabilities">The capabilities to check.</param>
		/// <returns>True if the connector supports any of the capabilities; otherwise, false.</returns>
		public bool SupportsAnyCapability(ChannelCapability capabilities)
		{
			return (Schema.Capabilities & capabilities) != 0;
		}

		/// <summary>
		/// Determines whether the connector supports all of the specified capabilities.
		/// </summary>
		/// <param name="capabilities">The capabilities to check.</param>
		/// <returns>True if the connector supports all of the capabilities; otherwise, false.</returns>
		public bool SupportsAllCapabilities(ChannelCapability capabilities)
		{
			return (Schema.Capabilities & capabilities) == capabilities;
		}

		/// <summary>
		/// Determines whether the connector supports the specified content type.
		/// </summary>
		/// <param name="contentType">The content type to check.</param>
		/// <returns>True if the connector supports the content type; otherwise, false.</returns>
		public bool SupportsContentType(MessageContentType contentType)
		{
			return Schema.ContentTypes.Contains(contentType);
		}

		/// <summary>
		/// Determines whether the connector supports the specified endpoint type.
		/// </summary>
		/// <param name="endpointType">The endpoint type to check.</param>
		/// <returns>True if the connector supports the endpoint type; otherwise, false.</returns>
		public bool SupportsEndpointType(EndpointType endpointType)
		{
			return Schema.Endpoints.Any(ep => ep.Matches(endpointType));
		}

		/// <summary>
		/// Determines whether the connector supports the specified authentication type.
		/// </summary>
		/// <param name="authenticationType">The authentication type to check.</param>
		/// <returns>True if the connector supports the authentication type; otherwise, false.</returns>
		public bool SupportsAuthenticationType(AuthenticationType authenticationType)
		{
			return Schema.SupportsAuthenticationType(authenticationType);
		}

		/// <summary>
		/// Gets the logical identity of the master schema (Provider/Type/Version).
		/// </summary>
		/// <returns>The logical identity string.</returns>
		public string GetLogicalIdentity()
		{
			return Schema.GetLogicalIdentity();
		}

		/// <summary>
		/// Returns a string representation of the connector descriptor.
		/// </summary>
		/// <returns>A string containing the connector type name and logical identity.</returns>
		public override string ToString()
		{
			return $"{ConnectorType.Name} ({GetLogicalIdentity()})";
		}

		/// <summary>
		/// Determines whether this descriptor represents the same connector type as another descriptor.
		/// </summary>
		/// <param name="obj">The object to compare with.</param>
		/// <returns>True if both descriptors represent the same connector type; otherwise, false.</returns>
		public override bool Equals(object? obj)
		{
			return obj is ConnectorDescriptor other && ConnectorType == other.ConnectorType;
		}

		/// <summary>
		/// Gets the hash code for this descriptor.
		/// </summary>
		/// <returns>The hash code based on the connector type.</returns>
		public override int GetHashCode()
		{
			return ConnectorType.GetHashCode();
		}
	}
}