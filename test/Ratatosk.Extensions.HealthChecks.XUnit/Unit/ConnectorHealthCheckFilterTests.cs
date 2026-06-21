using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk.Extensions.HealthChecks.XUnit.Unit
{
    public class ConnectorHealthCheckFilterTests
    {
        [Fact]
        public async Task Should_ProbeAll_When_NoFilters()
        {
            var connectors = new IChannelConnector[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = true, State = ConnectorState.Ready, LastHealthCheck = DateTime.UtcNow }),
                new Fixtures.FakeFailingConnector("Failing")
            };

            var check = new ConnectorHealthCheck(connectors, [], null);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task Should_FilterByType_When_ConnectorTypesProvided()
        {
            var connectors = new IChannelConnector[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = true, State = ConnectorState.Ready, LastHealthCheck = DateTime.UtcNow }),
                new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = false, State = ConnectorState.Error, LastHealthCheck = DateTime.UtcNow })
            };

            var types = new HashSet<Type> { typeof(Fixtures.FakeConnector) };
            var check = new ConnectorHealthCheck(connectors, [], null, types, null);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_FilterByType_When_TypeDoesNotMatch()
        {
            var connectors = new IChannelConnector[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = false, State = ConnectorState.Error, LastHealthCheck = DateTime.UtcNow })
            };

            var types = new HashSet<Type> { typeof(string) };
            var check = new ConnectorHealthCheck(connectors, [], null, types, null);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task Should_FilterByName_When_ConnectorNamesProvided()
        {
            var services = new ServiceCollection();
            var connector = new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = true, State = ConnectorState.Ready, LastHealthCheck = DateTime.UtcNow });
            services.AddKeyedSingleton<IChannelConnector>("my-connector", connector);
            var provider = services.BuildServiceProvider();

            var descriptors = new[] { new NamedConnectorDescriptor("my-connector", typeof(Fixtures.FakeConnector), new Fixtures.FakeSchema()) };
            var names = new HashSet<string> { "my-connector" };

            var check = new ConnectorHealthCheck([], descriptors, provider, null, names);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains(result.Data, kvp => kvp.Key == "my-connector");
        }

        [Fact]
        public async Task Should_FilterByName_When_NameDoesNotMatch()
        {
            var services = new ServiceCollection();
            var connector = new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = true, State = ConnectorState.Ready, LastHealthCheck = DateTime.UtcNow });
            services.AddKeyedSingleton<IChannelConnector>("my-connector", connector);
            var provider = services.BuildServiceProvider();

            var descriptors = new[] { new NamedConnectorDescriptor("my-connector", typeof(Fixtures.FakeConnector), new Fixtures.FakeSchema()) };
            var names = new HashSet<string> { "other-connector" };

            var check = new ConnectorHealthCheck([], descriptors, provider, null, names);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task Should_CombineTypeAndNameFilters()
        {
            var services = new ServiceCollection();
            var namedConnector = new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = true, State = ConnectorState.Ready, LastHealthCheck = DateTime.UtcNow });
            services.AddKeyedSingleton<IChannelConnector>("named-one", namedConnector);
            var provider = services.BuildServiceProvider();

            var unnamedConnector = new Fixtures.FakeConnector(() => new ConnectorHealth { IsHealthy = true, State = ConnectorState.Ready, LastHealthCheck = DateTime.UtcNow });
            var descriptors = new[] { new NamedConnectorDescriptor("named-one", typeof(Fixtures.FakeConnector), new Fixtures.FakeSchema()) };

            var types = new HashSet<Type> { typeof(Fixtures.FakeConnector) };
            var names = new HashSet<string> { "named-one" };

            var check = new ConnectorHealthCheck([unnamedConnector], descriptors, provider, types, names);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal(2, result.Data.Count);
        }
    }
}