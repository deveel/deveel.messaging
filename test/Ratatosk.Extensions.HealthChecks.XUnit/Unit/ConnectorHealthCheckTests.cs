using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk.Extensions.HealthChecks.XUnit.Unit
{
    public class ConnectorHealthCheckTests
    {
        [Fact]
        public async Task Should_ReturnHealthy_When_AllConnectorsHealthy()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = true,
                    State = ConnectorState.Ready,
                    LastHealthCheck = DateTime.UtcNow
                })
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains(result.Data, kvp => kvp.Key == "Fake");
        }

        [Fact]
        public async Task Should_ReturnDegraded_When_ConnectorHasIssues()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = true,
                    State = ConnectorState.Ready,
                    Issues = ["High latency detected"],
                    LastHealthCheck = DateTime.UtcNow
                })
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task Should_ReturnUnhealthy_When_ConnectorIsNotHealthy()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = false,
                    State = ConnectorState.Error,
                    Issues = ["Connection failed"],
                    LastHealthCheck = DateTime.UtcNow
                })
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_ReturnUnhealthy_When_GetHealthAsyncFails()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => throw new InvalidOperationException("Unexpected error"))
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_ReturnUnhealthy_When_OperationResultIsFailure()
        {
            var connectors = new[]
            {
                new Fixtures.FakeFailingConnector("FailingConnector")
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_IncludeMetrics_When_ConnectorProvidesMetrics()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = true,
                    State = ConnectorState.Ready,
                    Metrics = new Dictionary<string, object>
                    {
                        ["ProjectId"] = "test-project",
                        ["IsInitialized"] = true
                    },
                    LastHealthCheck = DateTime.UtcNow
                })
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            var data = Assert.IsType<Dictionary<string, object>>(result.Data["Fake"]);
            var metrics = Assert.IsType<Dictionary<string, object>>(data["metrics"]);
            Assert.Equal("test-project", metrics["ProjectId"]);
        }

        [Fact]
        public async Task Should_ReportWorstStatus_When_MultipleConnectors()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = true,
                    State = ConnectorState.Ready,
                    LastHealthCheck = DateTime.UtcNow
                }),
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = false,
                    State = ConnectorState.Error,
                    Issues = ["Down"],
                    LastHealthCheck = DateTime.UtcNow
                })
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Should_ReportDegraded_When_MixedHealthyAndDegraded()
        {
            var connectors = new[]
            {
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = true,
                    State = ConnectorState.Ready,
                    LastHealthCheck = DateTime.UtcNow
                }),
                new Fixtures.FakeConnector(() => new ConnectorHealth
                {
                    IsHealthy = true,
                    State = ConnectorState.Ready,
                    Issues = ["Slow response"],
                    LastHealthCheck = DateTime.UtcNow
                })
            };

            var check = new ConnectorHealthCheck(connectors, [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Degraded, result.Status);
        }

        [Fact]
        public async Task Should_HandleEmptyConnectors()
        {
            var check = new ConnectorHealthCheck([], [], null!);
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public void MapHealthStatus_ShouldReturnHealthy_WhenIsHealthyAndNoIssues()
        {
            var health = new ConnectorHealth
            {
                IsHealthy = true,
                Issues = []
            };

            var status = ConnectorHealthCheck.MapHealthStatus(health);
            Assert.Equal(HealthStatus.Healthy, status);
        }

        [Fact]
        public void MapHealthStatus_ShouldReturnDegraded_WhenIsHealthyButHasIssues()
        {
            var health = new ConnectorHealth
            {
                IsHealthy = true,
                Issues = ["Warning"]
            };

            var status = ConnectorHealthCheck.MapHealthStatus(health);
            Assert.Equal(HealthStatus.Degraded, status);
        }

        [Fact]
        public void MapHealthStatus_ShouldReturnUnhealthy_WhenNotHealthy()
        {
            var health = new ConnectorHealth
            {
                IsHealthy = false,
                Issues = []
            };

            var status = ConnectorHealthCheck.MapHealthStatus(health);
            Assert.Equal(HealthStatus.Unhealthy, status);
        }
    }
}