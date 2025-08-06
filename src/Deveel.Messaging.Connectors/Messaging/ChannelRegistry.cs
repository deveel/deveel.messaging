//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.DependencyInjection;

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
		private readonly ConcurrentBag<IChannelConnector> _connectors = new();
		private readonly IServiceProvider _services;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelRegistry"/> class.
		/// </summary>
		/// <param name="services">The service provider used to resolve dependencies 
		/// for channel registration and connector instantiation.</param>
		public ChannelRegistry(IServiceProvider services)
		{
			ArgumentNullException.ThrowIfNull(services, nameof(services));

			_services = services;
		}

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
			var connectorSchema = DiscoverConnectorSchema(_services, connectorType);

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

			// Use the reference schema
			var connector = await CreateConnectorInstanceAsync(_services, registration, registration.Schema, cancellationToken);

			// Only add to tracking if initialization was successful
			_connectors.Add(connector);

			return connector;
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

			var connector = await CreateConnectorInstanceAsync(_services, registration, runtimeSchema, cancellationToken);

			// Only add to tracking if initialization was successful
			_connectors.Add(connector);

			return connector;
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

		private static IChannelSchema DiscoverConnectorSchema(IServiceProvider services, Type connectorType)
		{
			var attribute = connectorType.GetCustomAttribute<ChannelSchemaAttribute>();
			if (attribute == null)
				throw new ArgumentException($"Connector type '{connectorType.Name}' must be decorated with {nameof(ChannelSchemaAttribute)}.", nameof(connectorType));

			try
			{
				return CreateSchema(services, attribute.SchemaType);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to create schema for connector type '{connectorType.Name}': {ex.Message}", ex);
			}
		}

		private static IChannelSchema CreateSchema(IServiceProvider services, Type schemaType)
		{
			try
			{
				IChannelSchema? schema = null;
				if (typeof(IChannelSchemaFactory).IsAssignableFrom(schemaType))
				{
					var factory = ActivatorUtilities.CreateInstance(services, schemaType) as IChannelSchemaFactory;
					if (factory == null)
						throw new InvalidOperationException($"Failed to create instance of schema factory '{schemaType.Name}'.");

					schema = factory.CreateSchema();
				} else if (typeof(IChannelSchema).IsAssignableFrom(schemaType))
				{
					schema = ActivatorUtilities.CreateInstance(services, schemaType) as IChannelSchema;
					if (schema == null)
						throw new InvalidOperationException($"Failed to create instance of schema '{schemaType.Name}'.");
				}
				else
				{
					throw new InvalidOperationException($"Type '{schemaType.Name}' is not a valid schema factory or schema type.");
				}

				return schema;
			} catch (Exception ex) when (!(ex is InvalidOperationException))
			{
				throw new InvalidOperationException($"Failed to create schema using factory '{schemaType.Name}': {ex.Message}", ex);
			}
		}


		private static async Task<IChannelConnector> CreateConnectorInstanceAsync(IServiceProvider services, ConnectorRegistration registration, IChannelSchema schema, CancellationToken cancellationToken)
		{
			IChannelConnector? connector = null;

			try
			{
				if (registration.ConnectorFactory != null)
				{
					connector = registration.ConnectorFactory(schema);
				}
				else
				{
					connector = CreateConnectorInstance(services, registration.ConnectorType, schema);
				}

				// Initialize the connector
				var initResult = await connector.InitializeAsync(cancellationToken);
				
				// Check if initialization was successful
				if (!initResult.Successful)
				{
					// If connector implements IDisposable, dispose it before throwing
					if (connector is IDisposable disposable)
					{
						try
						{
							disposable.Dispose();
						}
						catch
						{
							// Ignore disposal errors
						}
					}
					
					throw new InvalidOperationException(
						$"Failed to initialize connector of type '{registration.ConnectorType.Name}': {initResult.Error?.ErrorMessage ?? "Unknown error"}");
				}

				return connector;
			}
			catch (Exception ex)
			{
				// If we have a connector instance and initialization failed, try to dispose it
				if (connector != null && connector is IDisposable disposable)
				{
					try
					{
						disposable.Dispose();
					}
					catch
					{
						// Ignore disposal errors
					}
				}
				
				// Re-throw the original exception or wrap it
				if (ex is InvalidOperationException)
					throw;
					
				throw new InvalidOperationException(
					$"Failed to create and initialize connector of type '{registration.ConnectorType.Name}': {ex.Message}", ex);
			}
		}

		private static IChannelConnector CreateConnectorInstance(IServiceProvider services, Type connectorType, IChannelSchema schema)
		{
			try
			{
				var connector = ActivatorUtilities.CreateInstance(services, connectorType, new object[] { schema }) as IChannelConnector;
				
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
		/// Releases the unmanaged resources used by the <see cref="ChannelRegistry"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">
		/// true to release both managed and unmanaged resources; false to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose of connectors - sync version
				DisposeConnectorsSync();
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// This will dispose all tracked connectors synchronously.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously.
		/// This will dispose all tracked connectors asynchronously, allowing for graceful shutdown.
		/// </summary>
		/// <returns>A task that represents the asynchronous dispose operation.</returns>
		public async ValueTask DisposeAsync()
		{
			// Dispose connectors asynchronously
			await DisposeConnectorsAsync();
			
			// Suppress finalization since we've already disposed
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes all tracked connectors synchronously.
		/// </summary>
		private void DisposeConnectorsSync()
		{
			foreach (var connector in _connectors)
			{
				try
				{
					// Try to shutdown the connector gracefully with a reasonable timeout
					var shutdownTask = connector.ShutdownAsync(CancellationToken.None);
					if (shutdownTask.Wait(TimeSpan.FromSeconds(5)))
					{
						// Shutdown completed within timeout
					}
					else
					{
						// Timeout occurred, force disposal
					}
				}
				catch
				{
					// Ignore shutdown errors during disposal
				}

				try
				{
					// Dispose the connector if it implements IDisposable
					if (connector is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
				catch
				{
					// Ignore disposal errors to prevent exceptions during cleanup
				}
			}

			// Clear the collection
			while (_connectors.TryTake(out _))
			{
				// Remove all items from the bag
			}
		}

		/// <summary>
		/// Disposes all tracked connectors asynchronously.
		/// </summary>
		private async Task DisposeConnectorsAsync()
		{
			var shutdownTasks = new List<Task>();

			// Collect all connectors into a list to avoid concurrent modification issues
			var connectorsToDispose = _connectors.ToList();

			// Start shutdown for all connectors concurrently
			foreach (var connector in connectorsToDispose)
			{
				try
				{
					var shutdownTask = connector.ShutdownAsync(CancellationToken.None);
					shutdownTasks.Add(shutdownTask);
				}
				catch
				{
					// Ignore shutdown initialization errors
				}
			}

			// Wait for all shutdowns to complete with a reasonable timeout
			try
			{
				using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
				await Task.WhenAll(shutdownTasks).WaitAsync(timeoutCts.Token);
			}
			catch (OperationCanceledException)
			{
				// Timeout occurred, proceed with disposal anyway
			}
			catch
			{
				// Ignore other shutdown errors
			}

			// Dispose connectors that implement IDisposable or IAsyncDisposable
			foreach (var connector in connectorsToDispose)
			{
				try
				{
					if (connector is IAsyncDisposable asyncDisposable)
					{
						await asyncDisposable.DisposeAsync();
					}
					else if (connector is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
				catch
				{
					// Ignore disposal errors to prevent exceptions during cleanup
				}
			}

			// Clear the collection
			while (_connectors.TryTake(out _))
			{
				// Remove all items from the bag
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