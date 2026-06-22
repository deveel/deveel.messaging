namespace Ratatosk
{
    /// <summary>
    /// Provides a fluent API for configuring which connectors a health check
    /// should probe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default the builder probes all registered connectors. Call
    /// <see cref="ForAllConnectors"/> to reset to the default, or use
    /// <see cref="ForConnectorType{TConnector}"/> and
    /// <see cref="ForConnector"/> to restrict the scope.
    /// </para>
    /// </remarks>
    public sealed class RatatoskHealthCheckBuilder
    {
        internal bool IncludeAll { get; private set; } = true;
        internal List<Type> ConnectorTypes { get; } = new();
        internal List<string> ConnectorNames { get; } = new();

        /// <summary>
        /// Configures the health check to probe all registered connectors.
        /// </summary>
        /// <returns>The builder instance for chaining.</returns>
        public RatatoskHealthCheckBuilder ForAllConnectors()
        {
            IncludeAll = true;
            ConnectorTypes.Clear();
            ConnectorNames.Clear();
            return this;
        }

        /// <summary>
        /// Adds a connector type to the set of connectors to probe.
        /// </summary>
        /// <typeparam name="TConnector">
        /// The type of the connector to include.
        /// </typeparam>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>
        /// When at least one type or name is added, the health check only
        /// probes the specified connectors instead of all connectors.
        /// </remarks>
        public RatatoskHealthCheckBuilder ForConnectorType<TConnector>()
            where TConnector : class, IChannelConnector
        {
            IncludeAll = false;
            ConnectorTypes.Add(typeof(TConnector));
            return this;
        }

        /// <summary>
        /// Adds a named connector to the set of connectors to probe.
        /// </summary>
        /// <param name="name">
        /// The name of the connector (as registered via
        /// <c>AddConnector&lt;T&gt;(name, ...)</c>).
        /// </param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="name"/> is <c>null</c> or empty.
        /// </exception>
        /// <remarks>
        /// When at least one type or name is added, the health check only
        /// probes the specified connectors instead of all connectors.
        /// </remarks>
        public RatatoskHealthCheckBuilder ForConnector(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            IncludeAll = false;
            ConnectorNames.Add(name);
            return this;
        }
    }
}