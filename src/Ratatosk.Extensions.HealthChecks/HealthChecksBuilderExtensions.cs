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
        /// Registers the <see cref="ConnectorHealthCheck"/> into the health
        /// check pipeline, enabling automatic probing of all registered
        /// Ratatosk connectors.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IHealthChecksBuilder"/> to configure.
        /// </param>
        /// <returns>
        /// The builder instance for chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method registers a single <see cref="ConnectorHealthCheck"/>
        /// named <c>"ratatosk"</c> with the <c>"messaging"</c> tag. The check
        /// discovers all connectors registered via the
        /// <see cref="MessagingBuilder"/> at check time and probes each one.
        /// </para>
        /// <para>
        /// Call this method after all connectors have been registered:
        /// <code>
        /// services.AddMessaging()
        ///     .AddTwilioSms(...)
        ///     .AddSendGridEmail(...);
        /// services.AddHealthChecks()
        ///     .AddRatatoskHealthChecks();
        /// </code>
        /// </para>
        /// </remarks>
        public static IHealthChecksBuilder AddRatatoskHealthChecks(
            this IHealthChecksBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<ConnectorHealthCheck>();
            builder.AddCheck<ConnectorHealthCheck>("ratatosk", tags: ["messaging"]);

            return builder;
        }
    }
}