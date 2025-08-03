//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides a sealed builder for configuring channel connector registrations
	/// with automatic schema discovery through metadata attributes.
	/// </summary>
	/// <remarks>
	/// This builder simplifies the registration of channel connectors by automatically
	/// discovering their master schemas from <see cref="ChannelSchemaAttribute"/> metadata.
	/// Each connector type can only be registered once in the registry.
	/// </remarks>
	public sealed class ChannelRegistryBuilder
	{
		private readonly List<ConnectorRegistrationDescriptor> _registrationDescriptors = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelRegistryBuilder"/> class.
		/// </summary>
		/// <param name="services">The service collection to configure.</param>
		/// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
		internal ChannelRegistryBuilder(IServiceCollection services)
		{
			ArgumentNullException.ThrowIfNull(services, nameof(services));
			
			Services = services;

			// Register a hosted service that will perform the actual registrations
			services.AddSingleton<IHostedService>(serviceProvider =>
				new ConnectorRegistrationService(serviceProvider, _registrationDescriptors));
		}

		/// <summary>
		/// Gets the service collection being configured.
		/// </summary>
		public IServiceCollection Services { get; }

		/// <summary>
		/// Registers a channel connector type, discovering its master schema from metadata attributes.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector to register.</typeparam>
		/// <param name="connectorFactory">An optional factory function to create connector instances.</param>
		/// <returns>The builder instance for method chaining.</returns>
		/// <exception cref="ArgumentException">Thrown when the connector type does not have a ChannelSchemaAttribute.</exception>
		public ChannelRegistryBuilder RegisterConnector<TConnector>(Func<IServiceProvider, IChannelSchema, TConnector>? connectorFactory = null)
			where TConnector : class, IChannelConnector
		{
			_registrationDescriptors.Add(new ConnectorRegistrationDescriptor(typeof(TConnector), 
				connectorFactory != null
					? (serviceProvider, schema) => connectorFactory(serviceProvider, schema)
					: null));

			return this;
		}

		/// <summary>
		/// Registers a channel connector type, discovering its master schema from metadata attributes.
		/// </summary>
		/// <param name="connectorType">The type of connector to register.</param>
		/// <param name="connectorFactory">An optional factory function to create connector instances.</param>
		/// <returns>The builder instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when connectorType is null.</exception>
		/// <exception cref="ArgumentException">Thrown when connectorType does not implement IChannelConnector or does not have a ChannelSchemaAttribute.</exception>
		public ChannelRegistryBuilder RegisterConnector(Type connectorType, Func<IServiceProvider, IChannelSchema, IChannelConnector>? connectorFactory = null)
		{
			ArgumentNullException.ThrowIfNull(connectorType, nameof(connectorType));

			if (!typeof(IChannelConnector).IsAssignableFrom(connectorType))
			{
				throw new ArgumentException($"Type '{connectorType.Name}' must implement {nameof(IChannelConnector)}.", nameof(connectorType));
			}

			_registrationDescriptors.Add(new ConnectorRegistrationDescriptor(connectorType, connectorFactory));

			return this;
		}
	}

	/// <summary>
	/// Describes a connector registration to be applied during startup.
	/// </summary>
	internal class ConnectorRegistrationDescriptor
	{
		public ConnectorRegistrationDescriptor(Type connectorType, Func<IServiceProvider, IChannelSchema, IChannelConnector>? connectorFactory)
		{
			ConnectorType = connectorType;
			ConnectorFactory = connectorFactory;
		}

		public Type ConnectorType { get; }
		public Func<IServiceProvider, IChannelSchema, IChannelConnector>? ConnectorFactory { get; }
	}

	/// <summary>
	/// A hosted service that performs connector registrations during application startup.
	/// </summary>
	/// <remarks>
	/// This service ensures that all connector registrations are applied to the registry
	/// when the application starts, allowing the registry to be properly configured
	/// before it's used by other services.
	/// </remarks>
	internal class ConnectorRegistrationService : IHostedService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly List<ConnectorRegistrationDescriptor> _registrationDescriptors;

		public ConnectorRegistrationService(IServiceProvider serviceProvider, List<ConnectorRegistrationDescriptor> registrationDescriptors)
		{
			_serviceProvider = serviceProvider;
			_registrationDescriptors = registrationDescriptors;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			// Get the registry and apply all registrations
			var registry = _serviceProvider.GetRequiredService<IChannelRegistry>();
			
			foreach (var descriptor in _registrationDescriptors)
			{
				if (descriptor.ConnectorFactory != null)
				{
					// Use the provided factory function
					registry.RegisterConnector(descriptor.ConnectorType, 
						schema => descriptor.ConnectorFactory(_serviceProvider, schema));
				}
				else
				{
					// Use default activator-based factory
					registry.RegisterConnector(descriptor.ConnectorType);
				}
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}