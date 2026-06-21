using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Ratatosk.XUnit.Fixtures;

namespace Ratatosk.Extensions.HealthChecks.XUnit.Integration
{
    public class ConnectorHealthCheckIntegrationTests
    {
        [Fact]
        public async Task Should_ProbeConnector_When_RegisteredViaMessagingBuilder()
        {
            var services = new ServiceCollection();

            services.AddMessaging()
                .AddConnector<MockConnector>(c => c.WithSetting("key", "value"));

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var connector = provider.GetRequiredService<MockConnector>();
            await connector.InitializeAsync(CancellationToken.None);

            var check = provider.GetRequiredService<ConnectorHealthCheck>();
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains(result.Data, kvp => kvp.Key == "Mock");
        }

        [Fact]
        public async Task Should_ProbeNamedConnector_When_RegisteredViaMessagingBuilder()
        {
            var services = new ServiceCollection();

            services.AddMessaging()
                .AddConnector<MockConnector>("my-connector", c => c.WithSetting("key", "value"));

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("my-connector");
            await connector.InitializeAsync(CancellationToken.None);

            var check = provider.GetRequiredService<ConnectorHealthCheck>();
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains(result.Data, kvp => kvp.Key == "my-connector");
        }

        [Fact]
        public async Task Should_ReportUnhealthy_When_ConnectorFailsToInitialize()
        {
            var services = new ServiceCollection();

            var connector = new MockConnector(new MockSchema
            {
                Capabilities = ChannelCapability.SendMessages | ChannelCapability.HealthCheck
            })
            {
                FailOnInitialize = true
            };

            services.AddSingleton<IChannelConnector>(connector);
            services.AddSingleton(connector);

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var check = provider.GetRequiredService<ConnectorHealthCheck>();

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_ProbeMultipleConnectors_And_ReportWorstStatus()
        {
            var services = new ServiceCollection();

            var healthy = new MockConnector(new MockSchema
            {
                Capabilities = ChannelCapability.SendMessages | ChannelCapability.HealthCheck
            });

            var unhealthy = new MockConnector(new MockSchema
            {
                Capabilities = ChannelCapability.SendMessages | ChannelCapability.HealthCheck
            })
            {
                FailOnInitialize = true
            };

            services.AddSingleton<IChannelConnector>(healthy);
            services.AddSingleton(healthy);
            services.AddSingleton<IChannelConnector>(unhealthy);
            services.AddSingleton(unhealthy);

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            await healthy.InitializeAsync(CancellationToken.None);

            var check = provider.GetRequiredService<ConnectorHealthCheck>();
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_WorkWithHealthCheckService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMessaging()
                .AddConnector<MockConnector>(c => c.WithSetting("key", "value"));

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var connector = provider.GetRequiredService<MockConnector>();
            await connector.InitializeAsync(CancellationToken.None);

            var healthCheckService = provider.GetRequiredService<HealthCheckService>();
            var result = await healthCheckService.CheckHealthAsync();

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains(result.Entries, kvp => kvp.Key == "ratatosk");
        }

        [Fact]
        public async Task Should_IncludeConnectorData_When_ProbedViaHealthCheckService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMessaging()
                .AddConnector<MockConnector>(c => c.WithSetting("key", "value"));

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var connector = provider.GetRequiredService<MockConnector>();
            await connector.InitializeAsync(CancellationToken.None);

            var healthCheckService = provider.GetRequiredService<HealthCheckService>();
            var result = await healthCheckService.CheckHealthAsync();
            var report = result.Entries["ratatosk"];

            Assert.NotNull(report.Data);
        }

        [Fact]
        public async Task Should_ReturnHealthy_When_NoConnectorsRegistered()
        {
            var services = new ServiceCollection();

            services.AddHealthChecks()
                .AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var check = provider.GetRequiredService<ConnectorHealthCheck>();

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
    }
}