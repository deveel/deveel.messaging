using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace Ratatosk.XUnit;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "ChannelConnectorBuilder")]
public class ChannelConnectorBuilderAdvancedTests
{
    private static IServiceCollection CreateServices() => new ServiceCollection();

    [Fact]
    public void Should_SetRetryPolicy_When_WithRetryPolicyCalled()
    {
        var services = CreateServices();
        services.AddMessaging()
            .AddConnector<AdvTestConnector>(b => b
                .WithRetryPolicy(r =>
                {
                    r.MaxRetryAttempts = 3;
                    r.BackoffType = RetryBackoffType.Exponential;
                    r.BaseDelay = TimeSpan.FromSeconds(1);
                    r.UseJitter = true;
                    r.EnableCircuitBreaker = true;
                    r.CircuitBreakerFailureRatio = 0.5;
                    r.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);
                    r.CircuitBreakerMinimumThroughput = 10;
                    r.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60);
                }));

        var provider = services.BuildServiceProvider();
        var connector = (AdvTestConnector)provider.GetRequiredService<AdvTestConnector>();

        Assert.Equal(3, connector.ConnectionSettings.GetParameter<int>(RetrySettingsKeys.MaxAttempts));
        Assert.Equal("Exponential", connector.ConnectionSettings.GetParameter(RetrySettingsKeys.BackoffType));
        Assert.True(connector.ConnectionSettings.GetParameter<bool>(RetrySettingsKeys.UseJitter));
        Assert.True(connector.ConnectionSettings.GetParameter<bool>(RetrySettingsKeys.EnableCircuitBreaker));
    }

    [Fact]
    public void Should_SetTelemetry_When_WithTelemetryCalled()
    {
        var services = CreateServices();
        services.AddMessaging()
            .AddConnector<AdvTestConnector>(b => b
                .WithTelemetry(t =>
                {
                    t.EnableTracing = true;
                    t.EnableMetrics = true;
                    t.EnablePayloadSizeMetrics = false;
                }));

        var provider = services.BuildServiceProvider();
        var connector = (AdvTestConnector)provider.GetRequiredService<AdvTestConnector>();

        Assert.True(connector.ConnectionSettings.GetParameter<bool>(TelemetrySettingsKeys.EnableTracing));
        Assert.True(connector.ConnectionSettings.GetParameter<bool>(TelemetrySettingsKeys.EnableMetrics));
        Assert.False(connector.ConnectionSettings.GetParameter<bool>(TelemetrySettingsKeys.EnablePayloadSizeMetrics));
    }

    [Fact]
    public void Should_SetTimeout_When_WithTimeoutCalled()
    {
        var services = CreateServices();
        services.AddMessaging()
            .AddConnector<AdvTestConnector>(b => b
                .WithTimeout(t =>
                {
                    t.SendTimeout = TimeSpan.FromSeconds(30);
                    t.ReceiveTimeout = TimeSpan.FromSeconds(60);
                    t.StatusQueryTimeout = TimeSpan.FromSeconds(15);
                    t.RetryOnTimeout = true;
                }));

        var provider = services.BuildServiceProvider();
        var connector = (AdvTestConnector)provider.GetRequiredService<AdvTestConnector>();

        Assert.Equal("00:00:30", connector.ConnectionSettings.GetParameter(TimeoutSettingsKeys.SendTimeout));
        Assert.Equal("00:01:00", connector.ConnectionSettings.GetParameter(TimeoutSettingsKeys.ReceiveTimeout));
        Assert.Equal("00:00:15", connector.ConnectionSettings.GetParameter(TimeoutSettingsKeys.StatusQueryTimeout));
        Assert.True(connector.ConnectionSettings.GetParameter<bool>(TimeoutSettingsKeys.RetryOnTimeout));
    }

    [Fact]
    public void Should_SetOptions_When_WithOptionsCalled()
    {
        var services = CreateServices();
        services.AddMessaging()
            .AddConnector<AdvTestConnector>(b => b
                .WithOptions(new TestConnectorOptions
                {
                    ApiKey = "test-api-key",
                    Region = "us-east-1"
                }));

        var provider = services.BuildServiceProvider();
        var connector = (AdvTestConnector)provider.GetRequiredService<AdvTestConnector>();

        Assert.Equal("test-api-key", connector.ConnectionSettings.GetParameter("ApiKey"));
        Assert.Equal("us-east-1", connector.ConnectionSettings.GetParameter("Region"));
    }

    [Fact]
    public void Should_UseGenericFactory_When_WithFactoryTypeCalled()
    {
        var services = CreateServices();
        services.AddMessaging()
            .AddConnector<AdvTestConnector>(b => b
                .WithFactory<CustomAdvTestConnectorFactory>());

        var provider = services.BuildServiceProvider();
        var connector = provider.GetRequiredService<AdvTestConnector>();

        Assert.NotNull(connector);
    }

    [Fact]
    public void Should_Throw_When_WithRetryPolicyWithNull()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddMessaging().AddConnector<AdvTestConnector>(b => b.WithRetryPolicy(null!)));
    }

    [Fact]
    public void Should_Throw_When_WithTelemetryWithNull()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddMessaging().AddConnector<AdvTestConnector>(b => b.WithTelemetry(null!)));
    }

    [Fact]
    public void Should_Throw_When_WithTimeoutWithNull()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddMessaging().AddConnector<AdvTestConnector>(b => b.WithTimeout(null!)));
    }

    [Fact]
    public void Should_Throw_When_WithOptionsWithNull()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddMessaging().AddConnector<AdvTestConnector>(b => b.WithOptions<TestConnectorOptions>(null!)));
    }

    private class TestConnectorOptions : IConnectorOptions
    {
        public string? ApiKey { get; set; }
        public string? Region { get; set; }

        public ConnectionSettings ToConnectionSettings()
        {
            var settings = new ConnectionSettings();
            if (ApiKey != null) settings.SetParameter("ApiKey", ApiKey);
            if (Region != null) settings.SetParameter("Region", Region);
            return settings;
        }
    }

    private class CustomAdvTestConnectorFactory : IChannelConnectorFactory<AdvTestConnector>
    {
        public AdvTestConnector Create(ConnectionSettings settings)
            => Create(settings, null);

        public AdvTestConnector Create(ConnectionSettings settings, IChannelSchema? schema)
        {
            var testSchema = schema ?? new AdvDummySchema("CustomFactory", "CustomType");
            return new AdvTestConnector(testSchema, settings);
        }
    }

    [ChannelSchema(typeof(AdvDummySchemaFactory))]
    private class AdvTestConnector : IChannelConnector
    {
        public AdvTestConnector(IChannelSchema schema, ConnectionSettings? settings = null)
        {
            Schema = schema;
            ConnectionSettings = settings ?? new ConnectionSettings();
        }

        public IChannelSchema Schema { get; }
        public ConnectionSettings ConnectionSettings { get; }
        public ConnectorState State => ConnectorState.Uninitialized;

        public ValueTask<OperationResult<bool>> InitializeAsync(CancellationToken ct) => new(OperationResult<bool>.Success(true));
        public ValueTask<OperationResult<bool>> TestConnectionAsync(CancellationToken ct) => new(OperationResult<bool>.Success(true));
        public ValueTask<OperationResult<SendResult>> SendMessageAsync(IMessage m, CancellationToken ct) => throw new NotSupportedException();
        public ValueTask<OperationResult<BatchSendResult>> SendBatchAsync(IMessageBatch b, CancellationToken ct) => throw new NotSupportedException();
        public ValueTask<OperationResult<StatusInfo>> GetStatusAsync(CancellationToken ct) => throw new NotSupportedException();
        public ValueTask<OperationResult<StatusUpdatesResult>> GetMessageStatusAsync(string id, CancellationToken ct) => throw new NotSupportedException();
        public IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage m, CancellationToken ct) => throw new NotSupportedException();
        public ValueTask<OperationResult<StatusUpdateResult>> ReceiveMessageStatusAsync(MessageSource s, CancellationToken ct) => throw new NotSupportedException();
        public ValueTask<OperationResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource s, CancellationToken ct) => throw new NotSupportedException();
        public ValueTask<OperationResult<ConnectorHealth>> GetHealthAsync(CancellationToken ct) => throw new NotSupportedException();
        public ValueTask ShutdownAsync(CancellationToken ct) => default;
    }

    private class AdvDummySchemaFactory : IChannelSchemaFactory
    {
        public IChannelSchema CreateSchema() => new AdvDummySchema("TestProvider", "TestType");
    }

    private class AdvDummySchema : IChannelSchema
    {
        public AdvDummySchema(string provider, string type)
        {
            ChannelProvider = provider;
            ChannelType = type;
        }
        public string ChannelProvider { get; }
        public string ChannelType { get; }
        public string Version => "1.0";
        public string? DisplayName => null;
        public bool IsStrict => false;
        public ChannelCapability Capabilities => ChannelCapability.SendMessages;
        public IReadOnlyList<ChannelEndpointConfiguration> Endpoints => [];
        public IReadOnlyList<ChannelParameter> Parameters => [];
        public IReadOnlyList<MessagePropertyConfiguration> MessageProperties => [];
        public IReadOnlyList<MessageContentType> ContentTypes => [];
        public IReadOnlyList<AuthenticationConfiguration> AuthenticationConfigurations => [];
    }
}
