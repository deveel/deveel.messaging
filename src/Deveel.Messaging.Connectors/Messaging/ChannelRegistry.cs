//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides a thread-safe implementation of <see cref="IChannelRegistry"/> that manages
	/// channel connector types and their associated master schemas discovered through metadata attributes.
	/// </summary>
	/// <remarks>
	/// This registry automatically discovers master schemas from <see cref="ChannelSchemaAttribute"/>
	/// decorating connector classes. Each connector type can only be registered once, ensuring
	/// consistent schema definitions across the application.
	/// </remarks>
	public class ChannelRegistry : IChannelRegistry
	{
		private readonly ConcurrentDictionary<Type, ConnectorRegistration> _registrations = new();

		/// <inheritdoc/>
		public void RegisterConnector<TConnector>(Func<IChannelSchema, TConnector>? connectorFactory = null)
			where TConnector : class, IChannelConnector
		{
			RegisterConnector(typeof(TConnector), connectorFactory != null
				? schema => connectorFactory(schema)
				: null);
		}

		/// <inheritdoc/>
		public void RegisterConnector(Type connectorType, Func<IChannelSchema, IChannelConnector>? connectorFactory = null)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));

			if (!typeof(IChannelConnector).IsAssignableFrom(connectorType))
			{
				throw new ArgumentException($"Type '{connectorType.Name}' must implement {nameof(IChannelConnector)}.", nameof(connectorType));
			}

			// Discover the connector schema from the attribute
			var connectorSchema = DiscoverConnectorSchema(connectorType);

			// Create the registration
			var registration = new ConnectorRegistration(connectorType, connectorSchema, connectorFactory);

			// Attempt to add the registration
			if (!_registrations.TryAdd(connectorType, registration))
			{
				throw new InvalidOperationException($"Connector type '{connectorType.Name}' is already registered.");
			}
		}

		/// <inheritdoc/>
		public async Task<TConnector> CreateConnectorAsync<TConnector>(CancellationToken cancellationToken = default)
			where TConnector : class, IChannelConnector
		{
			var connector = await CreateConnectorAsync(typeof(TConnector), cancellationToken);
			return (TConnector)connector;
		}

		/// <inheritdoc/>
		public async Task<TConnector> CreateConnectorAsync<TConnector>(IChannelSchema runtimeSchema, CancellationToken cancellationToken = default)
			where TConnector : class, IChannelConnector
		{
			var connector = await CreateConnectorAsync(typeof(TConnector), runtimeSchema, cancellationToken);
			return (TConnector)connector;
		}

		/// <inheritdoc/>
		public async Task<IChannelConnector> CreateConnectorAsync(Type connectorType, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));

			if (!_registrations.TryGetValue(connectorType, out var registration))
			{
				throw new InvalidOperationException($"Connector type '{connectorType.Name}' is not registered.");
			}

			// Use the master schema
			return await CreateConnectorInstanceAsync(registration, registration.Schema, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task<IChannelConnector> CreateConnectorAsync(Type connectorType, IChannelSchema runtimeSchema, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));
			ArgumentNullException.ThrowIfNull(runtimeSchema, nameof(runtimeSchema));

			if (!_registrations.TryGetValue(connectorType, out var registration))
			{
				throw new InvalidOperationException($"Connector type '{connectorType.Name}' is not registered.");
			}

			// Validate the runtime schema against the master schema
			var validationResults = ValidateRuntimeSchemaInternal(registration, runtimeSchema);
			if (validationResults.Any())
			{
				var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
				throw new InvalidOperationException($"Runtime schema validation failed: {errors}");
			}

			return await CreateConnectorInstanceAsync(registration, runtimeSchema, cancellationToken);
		}

		/// <inheritdoc/>
		public IChannelSchema GetConnectorSchema<TConnector>()
			where TConnector : class, IChannelConnector
		{
			return GetConnectorSchema(typeof(TConnector));
		}

		/// <inheritdoc/>
		public IChannelSchema GetConnectorSchema(Type connectorType)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));

			if (!_registrations.TryGetValue(connectorType, out var registration))
			{
				throw new InvalidOperationException($"Connector type '{connectorType.Name}' is not registered.");
			}

			return registration.Schema;
		}

		/// <inheritdoc/>
		public IChannelSchema? FindSchema(string channelProvider, string channelType)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelProvider, nameof(channelProvider));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelType, nameof(channelType));

			var registration = _registrations.Values
				.FirstOrDefault(r => r.Schema.ChannelProvider.Equals(channelProvider, StringComparison.OrdinalIgnoreCase) &&
									r.Schema.ChannelType.Equals(channelType, StringComparison.OrdinalIgnoreCase));

			return registration?.Schema;
		}

		/// <inheritdoc/>
		public Type? FindConnector(string channelProvider, string channelType)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelProvider, nameof(channelProvider));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(channelType, nameof(channelType));

			var registration = _registrations.Values
				.FirstOrDefault(r => r.Schema.ChannelProvider.Equals(channelProvider, StringComparison.OrdinalIgnoreCase) &&
									r.Schema.ChannelType.Equals(channelType, StringComparison.OrdinalIgnoreCase));

			return registration?.ConnectorType;
		}

		/// <inheritdoc/>
		public IEnumerable<ValidationResult> ValidateSchema<TConnector>(IChannelSchema runtimeSchema)
			where TConnector : class, IChannelConnector
		{
			return ValidateSchema(typeof(TConnector), runtimeSchema);
		}

		/// <inheritdoc/>
		public IEnumerable<ValidationResult> ValidateSchema(Type connectorType, IChannelSchema runtimeSchema)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));
			ArgumentNullException.ThrowIfNull(runtimeSchema, nameof(runtimeSchema));

			if (!_registrations.TryGetValue(connectorType, out var registration))
			{
				throw new InvalidOperationException($"Connector type '{connectorType.Name}' is not registered.");
			}

			return ValidateRuntimeSchemaInternal(registration, runtimeSchema);
		}

		/// <inheritdoc/>
		public IEnumerable<Type> GetConnectorTypes()
		{
			return _registrations.Keys.ToList();
		}

		/// <inheritdoc/>
		public IEnumerable<ConnectorDescriptor> GetConnectorDescriptors(Func<ConnectorDescriptor, bool>? predicate = null)
		{
			var descriptors = _registrations.Values.Select(r => new ConnectorDescriptor(r.ConnectorType, r.Schema));
			return predicate != null ? descriptors.Where(predicate) : descriptors;
		}

		/// <inheritdoc/>
		public IEnumerable<IChannelSchema> QuerySchemas(Func<IChannelSchema, bool> predicate)
		{
			ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
			return _registrations.Values.Select(r => r.Schema).Where(predicate);
		}

		/// <inheritdoc/>
		public bool IsConnectorRegistered<TConnector>()
			where TConnector : class, IChannelConnector
		{
			return IsConnectorRegistered(typeof(TConnector));
		}

		/// <inheritdoc/>
		public bool IsConnectorRegistered(Type connectorType)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));
			return _registrations.ContainsKey(connectorType);
		}

		/// <inheritdoc/>
		public bool UnregisterConnector<TConnector>()
			where TConnector : class, IChannelConnector
		{
			return UnregisterConnector(typeof(TConnector));
		}

		/// <inheritdoc/>
		public bool UnregisterConnector(Type connectorType)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));
			return _registrations.TryRemove(connectorType, out _);
		}

		private static IChannelSchema DiscoverConnectorSchema(Type connectorType)
		{
			var attribute = connectorType.GetCustomAttribute<ChannelSchemaAttribute>();
			if (attribute == null)
			{
				throw new ArgumentException($"Connector type '{connectorType.Name}' must be decorated with {nameof(ChannelSchemaAttribute)}.", nameof(connectorType));
			}

			try
			{
				return attribute.CreateSchema();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to create schema for connector type '{connectorType.Name}': {ex.Message}", ex);
			}
		}

		private static async Task<IChannelConnector> CreateConnectorInstanceAsync(ConnectorRegistration registration, IChannelSchema schema, CancellationToken cancellationToken)
		{
			IChannelConnector connector;

			if (registration.ConnectorFactory != null)
			{
				connector = registration.ConnectorFactory(schema);
			}
			else
			{
				connector = CreateConnectorInstance(registration.ConnectorType, schema);
			}

			// Initialize the connector
			await connector.InitializeAsync(cancellationToken);

			return connector;
		}

		private static IChannelConnector CreateConnectorInstance(Type connectorType, IChannelSchema schema)
		{
			try
			{
				var connector = Activator.CreateInstance(connectorType, schema) as IChannelConnector;
				
				if (connector == null)
				{
					throw new InvalidOperationException($"Failed to create instance of '{connectorType.Name}'. " +
						"Ensure the connector has a constructor that accepts IChannelSchema.");
				}

				return connector;
			}
			catch (Exception ex) when (!(ex is InvalidOperationException))
			{
				throw new InvalidOperationException($"Failed to create instance of '{connectorType.Name}'. " +
					"Ensure the connector has a public constructor that accepts IChannelSchema.", ex);
			}
		}

		private static IEnumerable<ValidationResult> ValidateRuntimeSchemaInternal(ConnectorRegistration registration, IChannelSchema runtimeSchema)
		{
			// First check logical compatibility (same provider/type/version)
			if (!registration.Schema.IsCompatibleWith(runtimeSchema))
			{
				yield return new ValidationResult(
					$"Runtime schema logical identity '{runtimeSchema.GetLogicalIdentity()}' " +
					$"is not compatible with schema '{registration.Schema.GetLogicalIdentity()}'.");
				yield break; // No point in further validation if not compatible
			}

			// Validate that runtime schema is a valid restriction of schema
			var restrictionValidationResults = runtimeSchema.ValidateAsRestrictionOf(registration.Schema);
			foreach (var result in restrictionValidationResults)
			{
				yield return result;
			}
		}

		/// <summary>
		/// Represents a connector registration in the registry.
		/// </summary>
		private class ConnectorRegistration
		{
			public ConnectorRegistration(Type connectorType, IChannelSchema schema, Func<IChannelSchema, IChannelConnector>? connectorFactory)
			{
				ConnectorType = connectorType;
				Schema = schema;
				ConnectorFactory = connectorFactory;
			}

			public Type ConnectorType { get; }
			public IChannelSchema Schema { get; }
			public Func<IChannelSchema, IChannelConnector>? ConnectorFactory { get; }
		}
	}
}