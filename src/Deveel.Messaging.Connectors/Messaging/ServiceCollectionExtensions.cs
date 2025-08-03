//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides extension methods for <see cref="IServiceCollection"/> to configure
	/// channel registry services with attribute-driven connector registration.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds the channel registry services to the service collection and returns a builder
		/// for registering channel connectors with automatic schema discovery.
		/// </summary>
		/// <param name="services">The service collection to add the registry to.</param>
		/// <returns>A <see cref="ChannelRegistryBuilder"/> for configuring connector registrations.</returns>
		/// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
		/// <remarks>
		/// This method registers the <see cref="IChannelRegistry"/> as a singleton service
		/// and returns a builder that allows for fluent configuration of connector registrations.
		/// Connectors registered through this builder will have their master schemas automatically
		/// discovered from <see cref="ChannelSchemaAttribute"/> metadata.
		/// </remarks>
		/// <example>
		/// <code>
		/// services.AddChannelRegistry()
		///     .RegisterConnector&lt;TwilioSmsConnector&gt;()
		///     .RegisterConnector&lt;SendGridEmailConnector&gt;();
		/// </code>
		/// </example>
		public static ChannelRegistryBuilder AddChannelRegistry(this IServiceCollection services)
		{
			ArgumentNullException.ThrowIfNull(services, nameof(services));

			// Register the channel registry as a singleton
			services.AddSingleton<IChannelRegistry, ChannelRegistry>();

			// Return a builder for fluent configuration
			return new ChannelRegistryBuilder(services);
		}

		/// <summary>
		/// Registers a single channel connector type with automatic schema discovery.
		/// </summary>
		/// <typeparam name="TConnector">The type of connector to register.</typeparam>
		/// <param name="services">The service collection to add the registry to.</param>
		/// <param name="connectorFactory">An optional factory function to create connector instances.</param>
		/// <returns>The service collection for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the connector type does not have a ChannelSchemaAttribute.</exception>
		/// <remarks>
		/// This is a convenience method for registering a single connector. For multiple
		/// connectors, use <see cref="AddChannelRegistry(IServiceCollection)"/> and the builder pattern.
		/// </remarks>
		/// <example>
		/// <code>
		/// services.AddChannelConnector&lt;TwilioSmsConnector&gt;();
		/// </code>
		/// </example>
		public static IServiceCollection AddChannelConnector<TConnector>(this IServiceCollection services, Func<IServiceProvider, IChannelSchema, TConnector>? connectorFactory = null)
			where TConnector : class, IChannelConnector
		{
			services.AddChannelRegistry()
				.RegisterConnector(connectorFactory);

			return services;
		}

		/// <summary>
		/// Registers a single channel connector type with automatic schema discovery.
		/// </summary>
		/// <param name="services">The service collection to add the registry to.</param>
		/// <param name="connectorType">The type of connector to register.</param>
		/// <param name="connectorFactory">An optional factory function to create connector instances.</param>
		/// <returns>The service collection for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when services or connectorType is null.</exception>
		/// <exception cref="ArgumentException">Thrown when connectorType does not implement IChannelConnector or does not have a ChannelSchemaAttribute.</exception>
		/// <remarks>
		/// This is a convenience method for registering a single connector by type. For multiple
		/// connectors, use <see cref="AddChannelRegistry(IServiceCollection)"/> and the builder pattern.
		/// </remarks>
		public static IServiceCollection AddChannelConnector(this IServiceCollection services, Type connectorType, Func<IServiceProvider, IChannelSchema, IChannelConnector>? connectorFactory = null)
		{
			services.AddChannelRegistry()
				.RegisterConnector(connectorType, connectorFactory);

			return services;
		}
	}
}