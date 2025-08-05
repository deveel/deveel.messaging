//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Defines the contract for a registry that manages channel schemas and their associated connectors.
	/// </summary>
	/// <remarks>
	/// The channel registry provides a centralized way to register channel connector types with their 
	/// master schemas discovered through metadata attributes. Each connector type can only be registered 
	/// once, and its master schema is automatically discovered from the <see cref="ChannelSchemaAttribute"/>
	/// </remarks>
	public interface IChannelRegistry : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Registers a channel connector type, discovering its master schema from metadata attributes.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector to register.</typeparam>
		/// <param name="connectorFactory">An optional factory function to create connector instances. If not provided, Activator.CreateInstance will be used.</param>
		/// <exception cref="ArgumentException">Thrown when the connector type does not have a ChannelSchemaAttribute.</exception>
		/// <exception cref="InvalidOperationException">Thrown when a connector of the same type is already registered.</exception>
		void RegisterConnector<TConnector>(Func<IChannelSchema, TConnector>? connectorFactory = null)
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Registers a channel connector type, discovering its master schema from metadata attributes.
		/// </summary>
		/// <param name="connectorType">The type of connector to register.</param>
		/// <param name="connectorFactory">An optional factory function to create connector instances. If not provided, Activator.CreateInstance will be used.</param>
		/// <exception cref="ArgumentNullException">Thrown when connectorType is null.</exception>
		/// <exception cref="ArgumentException">Thrown when connectorType does not implement IChannelConnector or does not have a ChannelSchemaAttribute.</exception>
		/// <exception cref="InvalidOperationException">Thrown when a connector of the same type is already registered.</exception>
		void RegisterConnector(Type connectorType, Func<IChannelSchema, IChannelConnector>? connectorFactory = null);

		/// <summary>
		/// Creates a connector instance using the master schema for the specified connector type.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector to create.</typeparam>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A new connector instance configured with the master schema.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered.</exception>
		Task<TConnector> CreateConnectorAsync<TConnector>(CancellationToken cancellationToken = default)
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Creates a connector instance using the provided runtime schema for the specified connector type.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector to create.</typeparam>
		/// <param name="schema">The schema to use for the connector instance. Must be compatible with the connector schema.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A new connector instance configured with the runtime schema.</returns>
		/// <exception cref="ArgumentNullException">Thrown when runtimeSchema is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered or the runtime schema is not compatible with the master schema.</exception>
		Task<TConnector> CreateConnectorAsync<TConnector>(IChannelSchema schema, CancellationToken cancellationToken = default)
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Creates a connector instance using the connector schema for the specified connector type.
		/// </summary>
		/// <param name="connectorType">The type of connector to create.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A new connector instance configured with the connector schema.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered.</exception>
		Task<IChannelConnector> CreateConnectorAsync(Type connectorType, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a connector instance using the provided runtime schema for the specified connector type.
		/// </summary>
		/// <param name="connectorType">The type of connector to create.</param>
		/// <param name="schema">The schema to use for the connector instance. Must be compatible with the connector schema.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A new connector instance configured with the runtime schema.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType or schema is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered or the runtime schema is not compatible with the master schema.</exception>
		Task<IChannelConnector> CreateConnectorAsync(Type connectorType, IChannelSchema schema, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the schema for the specified connector type.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector.</typeparam>
		/// <returns>The master schema for the connector type.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered.</exception>
		IChannelSchema GetConnectorSchema<TConnector>()
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Gets the schema for the specified connector type.
		/// </summary>
		/// <param name="connectorType">The type of connector.</param>
		/// <returns>The master schema for the connector type.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered.</exception>
		IChannelSchema GetConnectorSchema(Type connectorType);

		/// <summary>
		/// Finds the schema by channel provider and type.
		/// </summary>
		/// <param name="channelProvider">The channel provider to search for.</param>
		/// <param name="channelType">The channel type to search for.</param>
		/// <returns>The schema if found; otherwise, null.</returns>
		/// <exception cref="ArgumentNullException">Thrown when channelProvider or channelType is null or whitespace.</exception>
		IChannelSchema? FindSchema(string channelProvider, string channelType);

		/// <summary>
		/// Finds the connector type by channel provider and type.
		/// </summary>
		/// <param name="channelProvider">The channel provider to search for.</param>
		/// <param name="channelType">The channel type to search for.</param>
		/// <returns>The connector type if found; otherwise, null.</returns>
		/// <exception cref="ArgumentNullException">Thrown when channelProvider or channelType is null or whitespace.</exception>
		Type? FindConnector(string channelProvider, string channelType);

		/// <summary>
		/// Validates whether a runtime schema is compatible with the schema for the specified connector type.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector.</typeparam>
		/// <param name="runtimeSchema">The runtime schema to validate.</param>
		/// <returns>An enumerable of validation results. Empty if the schema is compatible.</returns>
		/// <exception cref="ArgumentNullException">Thrown when runtimeSchema is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered.</exception>
		IEnumerable<ValidationResult> ValidateSchema<TConnector>(IChannelSchema runtimeSchema)
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Validates whether a runtime schema is compatible with the schema for the specified connector type.
		/// </summary>
		/// <param name="connectorType">The type of connector.</param>
		/// <param name="schema">The runtime schema to validate.</param>
		/// <returns>An enumerable of validation results. Empty if the schema is compatible.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType or schema is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the connector type is not registered.</exception>
		IEnumerable<ValidationResult> ValidateSchema(Type connectorType, IChannelSchema schema);

		/// <summary>
		/// Gets all registered connector types.
		/// </summary>
		/// <returns>An enumerable of all registered connector types.</returns>
		IEnumerable<Type> GetConnectorTypes();

		/// <summary>
		/// Gets descriptive information about registered connectors.
		/// </summary>
		/// <param name="predicate">An optional predicate to filter connectors.</param>
		/// <returns>An enumerable of connector descriptors.</returns>
		IEnumerable<ConnectorDescriptor> GetConnectorDescriptors(Func<ConnectorDescriptor, bool>? predicate = null);

		/// <summary>
		/// Queries channel schemas by conditions.
		/// </summary>
		/// <param name="predicate">A predicate to filter schemas by various conditions.</param>
		/// <returns>An enumerable of schemas that match the specified conditions.</returns>
		IEnumerable<IChannelSchema> QuerySchemas(Func<IChannelSchema, bool> predicate);

		/// <summary>
		/// Determines whether a connector type is registered.
		/// </summary>
		/// <typeparam name="TConnector">The connector type to check.</typeparam>
		/// <returns>True if the connector type is registered; otherwise, false.</returns>
		bool IsConnectorRegistered<TConnector>()
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Determines whether a connector type is registered.
		/// </summary>
		/// <param name="connectorType">The connector type to check.</param>
		/// <returns>True if the connector type is registered; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType is null.</exception>
		bool IsConnectorRegistered(Type connectorType);

		/// <summary>
		/// Unregisters a connector type from the registry.
		/// </summary>
		/// <typeparam name="TConnector">The connector type to unregister.</typeparam>
		/// <returns>True if the connector type was successfully unregistered; false if it was not registered.</returns>
		bool UnregisterConnector<TConnector>()
			where TConnector : class, IChannelConnector;

		/// <summary>
		/// Unregisters a connector type from the registry.
		/// </summary>
		/// <param name="connectorType">The connector type to unregister.</param>
		/// <returns>True if the connector type was successfully unregistered; false if it was not registered.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType is null.</exception>
		bool UnregisterConnector(Type connectorType);
	}
}