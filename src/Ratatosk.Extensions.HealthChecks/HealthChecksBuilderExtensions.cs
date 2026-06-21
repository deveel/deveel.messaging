using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk
{
    /// <summary>
    /// Provides extension methods for registering Ratatosk health checks
    /// with the ASP.NET Core health check infrastructure.
    /// </summary>
    public static class HealthChecksBuilderExtensions
    {
        /// <summary>
        /// Registers a <see cref="ConnectorHealthCheck"/> that probes all
        /// registered Ratatosk connectors.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IHealthChecksBuilder"/> to configure.
        /// </param>
        /// <param name="name">
        /// The name of the health check registration. Defaults to <c>"ratatosk"</c>.
        /// </param>
        /// <returns>
        /// The builder instance for chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This overload probes all unnamed and named connectors registered
        /// via the <see cref="MessagingBuilder"/>.
        /// </para>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddRatatoskHealthChecks();
        /// </code>
        /// </remarks>
        public static IHealthChecksBuilder AddRatatoskHealthChecks(
            this IHealthChecksBuilder builder,
            string name = "ratatosk")
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<ConnectorHealthCheck>();
            builder.AddCheck<ConnectorHealthCheck>(name, tags: ["messaging"]);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="ConnectorHealthCheck"/> with a fluent
        /// configuration delegate to restrict which connectors are probed.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IHealthChecksBuilder"/> to configure.
        /// </param>
        /// <param name="name">
        /// The name of the health check registration.
        /// </param>
        /// <param name="configure">
        /// A delegate that configures the <see cref="RatatoskHealthCheckBuilder"/>
        /// to specify which connectors to probe.
        /// </param>
        /// <returns>
        /// The builder instance for chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> or <paramref name="configure"/>
        /// is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddRatatoskHealthChecks("sms", h => h
        ///         .ForConnector("twilio-sms")
        ///         .ForConnector("twilio-whatsapp"));
        /// </code>
        /// </remarks>
        public static IHealthChecksBuilder AddRatatoskHealthChecks(
            this IHealthChecksBuilder builder,
            string name,
            Action<RatatoskHealthCheckBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var filter = new RatatoskHealthCheckBuilder();
            configure(filter);

            builder.Services.AddSingleton<ConnectorHealthCheck>(sp =>
            {
                var connectors = sp.GetRequiredService<IEnumerable<IChannelConnector>>();
                var namedDescriptors = sp.GetRequiredService<IEnumerable<NamedConnectorDescriptor>>();
                return new ConnectorHealthCheck(
                    connectors,
                    namedDescriptors,
                    sp,
                    filter.IncludeAll ? null : new HashSet<Type>(filter.ConnectorTypes),
                    filter.IncludeAll ? null : new HashSet<string>(filter.ConnectorNames));
            });

            builder.AddCheck<ConnectorHealthCheck>(name, tags: ["messaging"]);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="ConnectorHealthCheck"/> that probes only
        /// connectors of the specified type.
        /// </summary>
        /// <typeparam name="TConnector">
        /// The type of connector to probe.
        /// </typeparam>
        /// <param name="builder">
        /// The <see cref="IHealthChecksBuilder"/> to configure.
        /// </param>
        /// <param name="name">
        /// The name of the health check registration.
        /// </param>
        /// <returns>
        /// The builder instance for chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddRatatoskHealthChecks&lt;TwilioSmsConnector&gt;("twilio");
        /// </code>
        /// </remarks>
        public static IHealthChecksBuilder AddRatatoskHealthChecks<TConnector>(
            this IHealthChecksBuilder builder,
            string name)
            where TConnector : class, IChannelConnector
        {
            ArgumentNullException.ThrowIfNull(builder);

            var connectorType = typeof(TConnector);

            builder.Services.AddSingleton<ConnectorHealthCheck>(sp =>
            {
                var connectors = sp.GetRequiredService<IEnumerable<IChannelConnector>>();
                var namedDescriptors = sp.GetRequiredService<IEnumerable<NamedConnectorDescriptor>>();
                return new ConnectorHealthCheck(
                    connectors,
                    namedDescriptors,
                    sp,
                    new HashSet<Type> { connectorType },
                    null);
            });

            builder.AddCheck<ConnectorHealthCheck>(name, tags: ["messaging"]);

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="ConnectorHealthCheck"/> that probes only
        /// the named connector with the specified name.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IHealthChecksBuilder"/> to configure.
        /// </param>
        /// <param name="name">
        /// The name of the health check registration.
        /// </param>
        /// <param name="connectorName">
        /// The name of the connector to probe (as registered via
        /// <c>AddConnector&lt;T&gt;(connectorName, ...)</c>).
        /// </param>
        /// <returns>
        /// The builder instance for chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="connectorName"/> is <c>null</c> or empty.
        /// </exception>
        /// <remarks>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddRatatoskHealthChecks("sms", "twilio-sms");
        /// </code>
        /// </remarks>
        public static IHealthChecksBuilder AddRatatoskHealthChecks(
            this IHealthChecksBuilder builder,
            string name,
            string connectorName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectorName);

            builder.Services.AddSingleton<ConnectorHealthCheck>(sp =>
            {
                var connectors = sp.GetRequiredService<IEnumerable<IChannelConnector>>();
                var namedDescriptors = sp.GetRequiredService<IEnumerable<NamedConnectorDescriptor>>();
                return new ConnectorHealthCheck(
                    connectors,
                    namedDescriptors,
                    sp,
                    null,
                    new HashSet<string> { connectorName });
            });

            builder.AddCheck<ConnectorHealthCheck>(name, tags: ["messaging"]);

            return builder;
        }
    }
}