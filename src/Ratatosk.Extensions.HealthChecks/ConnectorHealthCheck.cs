using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk
{
    /// <summary>
    /// An ASP.NET Core <see cref="IHealthCheck"/> that probes all registered
    /// Ratatosk connectors and reports their aggregate health status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check resolves all unnamed connectors via
    /// <see cref="IEnumerable{IChannelConnector}"/> and all named connectors
    /// via <see cref="NamedConnectorDescriptor"/> at check time. Each connector
    /// is probed through its <see cref="IChannelConnector.GetHealthAsync"/>
    /// method and the results are aggregated into a single
    /// <see cref="HealthCheckResult"/>.
    /// </para>
    /// <para>
    /// The overall status is the worst status across all connectors:
    /// <see cref="HealthStatus.Unhealthy"/> if any connector is unhealthy,
    /// <see cref="HealthStatus.Degraded"/> if any connector has issues,
    /// <see cref="HealthStatus.Healthy"/> if all connectors are healthy.
    /// </para>
    /// <para>
    /// Per-connector detail (status, state, issues, uptime, metrics) is
    /// included in the <see cref="HealthCheckResult.Data"/> dictionary,
    /// keyed by the connector type name (with the "Connector" suffix removed)
    /// or the named connector's registration name.
    /// </para>
    /// </remarks>
    public sealed class ConnectorHealthCheck : IHealthCheck
    {
        private readonly IEnumerable<IChannelConnector> _connectors;
        private readonly IEnumerable<NamedConnectorDescriptor> _namedDescriptors;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorHealthCheck"/> class.
        /// </summary>
        /// <param name="connectors">
        /// The collection of unnamed connectors registered in the application.
        /// </param>
        /// <param name="namedDescriptors">
        /// The collection of named connector descriptors registered in the application.
        /// </param>
        /// <param name="serviceProvider">
        /// The service provider used to resolve named connector instances.
        /// Can be <c>null</c> if no named connectors are registered.
        /// </param>
        public ConnectorHealthCheck(
            IEnumerable<IChannelConnector> connectors,
            IEnumerable<NamedConnectorDescriptor> namedDescriptors,
            IServiceProvider? serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(connectors);
            ArgumentNullException.ThrowIfNull(namedDescriptors);

            _connectors = connectors;
            _namedDescriptors = namedDescriptors;
            _serviceProvider = serviceProvider!;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var overall = HealthStatus.Healthy;

            foreach (var connector in _connectors)
            {
                var key = GetConnectorKey(connector);
                var (status, detail) = await ProbeAsync(connector, cancellationToken);
                data[key] = detail;
                if (status < overall)
                    overall = status;
            }

            foreach (var descriptor in _namedDescriptors)
            {
                var connector = _serviceProvider.GetKeyedService<IChannelConnector>(descriptor.Name);
                if (connector == null)
                {
                    data[descriptor.Name] = new Dictionary<string, object>
                    {
                        ["status"] = "Unhealthy",
                        ["error"] = $"Named connector '{descriptor.Name}' is not resolvable from the service provider"
                    };
                    overall = HealthStatus.Unhealthy;
                    continue;
                }

                var (status, detail) = await ProbeAsync(connector, cancellationToken);
                data[descriptor.Name] = detail;
                if (status < overall)
                    overall = status;
            }

            return new HealthCheckResult(overall, data: data);
        }

        private static async Task<(HealthStatus, Dictionary<string, object>)> ProbeAsync(
            IChannelConnector connector,
            CancellationToken cancellationToken)
        {
            var detail = new Dictionary<string, object>();

            try
            {
                var result = await connector.GetHealthAsync(cancellationToken);

                if (!result.IsSuccess())
                {
                    detail["status"] = "Unhealthy";
                    detail["error"] = result.Error?.Message ?? "Unknown error";
                    return (HealthStatus.Unhealthy, detail);
                }

                var health = result.Value!;
                var status = MapHealthStatus(health);

                detail["status"] = status.ToString();
                detail["state"] = health.State.ToString();
                detail["isHealthy"] = health.IsHealthy;
                detail["issues"] = health.Issues;
                detail["uptime"] = health.Uptime.ToString();
                detail["lastHealthCheck"] = health.LastHealthCheck;

                if (health.Metrics.Count > 0)
                    detail["metrics"] = new Dictionary<string, object>(health.Metrics);

                return (status, detail);
            }
            catch (Exception ex)
            {
                detail["status"] = "Unhealthy";
                detail["error"] = ex.Message;
                return (HealthStatus.Unhealthy, detail);
            }
        }

        private static string GetConnectorKey(IChannelConnector connector)
        {
            var name = connector.GetType().Name;
            return name.EndsWith("Connector", StringComparison.Ordinal)
                ? name[..^"Connector".Length]
                : name;
        }

        /// <summary>
        /// Maps a <see cref="ConnectorHealth"/> to the corresponding
        /// <see cref="HealthStatus"/>.
        /// </summary>
        /// <param name="health">The connector health to evaluate.</param>
        /// <returns>
        /// <see cref="HealthStatus.Healthy"/> if the connector is healthy
        /// and has no issues; <see cref="HealthStatus.Degraded"/> if the
        /// connector is healthy but has issues; <see cref="HealthStatus.Unhealthy"/>
        /// if the connector is not healthy.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="health"/> is <c>null</c>.
        /// </exception>
        public static HealthStatus MapHealthStatus(ConnectorHealth health)
        {
            ArgumentNullException.ThrowIfNull(health);

            if (!health.IsHealthy)
                return HealthStatus.Unhealthy;

            return health.Issues.Count > 0
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;
        }
    }
}