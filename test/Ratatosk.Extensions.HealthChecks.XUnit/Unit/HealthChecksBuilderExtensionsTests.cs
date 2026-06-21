using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk.Extensions.HealthChecks.XUnit.Unit
{
    public class HealthChecksBuilderExtensionsTests
    {
        [Fact]
        public void Should_RegisterConnectorHealthCheck()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_Throw_When_BuilderIsNull()
        {
            IHealthChecksBuilder? builder = null;

            Assert.Throws<ArgumentNullException>(() => builder!.AddRatatoskHealthChecks());
        }

        [Fact]
        public async Task Should_ProbeRegisteredConnectors()
        {
            var services = new ServiceCollection();

            var connector = new Fixtures.FakeConnector(() => new ConnectorHealth
            {
                IsHealthy = true,
                State = ConnectorState.Ready,
                LastHealthCheck = DateTime.UtcNow
            });

            services.AddSingleton<IChannelConnector>(connector);
            services.AddSingleton(connector);

            var healthBuilder = services.AddHealthChecks();
            healthBuilder.AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var check = provider.GetRequiredService<ConnectorHealthCheck>();

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
    }
}