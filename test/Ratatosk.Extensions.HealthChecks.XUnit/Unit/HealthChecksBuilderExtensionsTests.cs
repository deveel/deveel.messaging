using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk.Extensions.HealthChecks.XUnit.Unit
{
    public class HealthChecksBuilderExtensionsTests
    {
        [Fact]
        public void Should_RegisterAllConnectors_DefaultName()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks();

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterAllConnectors_CustomName()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks("messaging");

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterSingleType()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks<Fixtures.FakeConnector>("fake");

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterSingleName()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks("sms", "twilio-sms");

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterWithFluentBuilder_AllConnectors()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks("all", h => h.ForAllConnectors());

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterWithFluentBuilder_ByType()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks("fake", h => h.ForConnectorType<Fixtures.FakeConnector>());

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterWithFluentBuilder_ByName()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks("sms", h => h.ForConnector("twilio-sms"));

            var provider = services.BuildServiceProvider();
            var check = provider.GetService<ConnectorHealthCheck>();

            Assert.NotNull(check);
        }

        [Fact]
        public void Should_RegisterWithFluentBuilder_MultipleNames()
        {
            var services = new ServiceCollection();
            var healthBuilder = services.AddHealthChecks();

            healthBuilder.AddRatatoskHealthChecks("sms", h => h
                .ForConnector("twilio-sms")
                .ForConnector("twilio-whatsapp"));

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
        public void Should_Throw_When_ConfigureIsNull()
        {
            var builder = new ServiceCollection().AddHealthChecks();

            Assert.Throws<ArgumentNullException>(() => builder.AddRatatoskHealthChecks("test", (Action<RatatoskHealthCheckBuilder>)null!));
        }

        [Fact]
        public void Should_Throw_When_ConnectorNameIsEmpty()
        {
            var builder = new ServiceCollection().AddHealthChecks();

            Assert.Throws<ArgumentException>(() => builder.AddRatatoskHealthChecks("test", ""));
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