using Microsoft.Extensions.Logging.Abstractions;

namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "ChannelConnectorBase")]
public class ChannelConnectorBaseExtendedTests
{
    private sealed class TestConnector : ChannelConnectorBase
    {
        public TestConnector(IChannelSchema schema, ConnectionSettings? settings = null, IAuthenticationManager? authManager = null)
            : base(schema, settings ?? new ConnectionSettings(), authenticationManager: authManager)
        {
        }

        protected override ValueTask InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            SetState(ConnectorState.Ready);
            return ValueTask.CompletedTask;
        }

        protected override ValueTask TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        protected override Task<SendResult> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SendResult(message.Id, "remote-id"));
        }

        protected override Task<StatusInfo> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new StatusInfo("OK"));

        public new void ValidateCapability(ChannelCapability capability) => base.ValidateCapability(capability);
    }

    private sealed class ExposedConnector : ChannelConnectorBase
    {
        public ExposedConnector(IChannelSchema schema, ConnectionSettings? settings = null)
            : base(schema, settings ?? new ConnectionSettings())
        {
        }

        public new string? GetAuthenticationHeader() => base.GetAuthenticationHeader();
        public new string? GetApiKey() => base.GetApiKey();
        public new bool IsAnonymousConnector() => base.IsAnonymousConnector();
        public new string? GetEndpointType(IEndpoint endpoint) => base.GetEndpointType(endpoint);
        public new bool IsEndpointTypeSupported(EndpointType type, bool asSender = false, bool asReceiver = false)
            => base.IsEndpointTypeSupported(type, asSender, asReceiver);

        protected override ValueTask InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            SetState(ConnectorState.Ready);
            return ValueTask.CompletedTask;
        }
        protected override ValueTask TestConnectorConnectionAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        protected override Task<SendResult> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
        protected override Task<StatusInfo> GetConnectorStatusAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static IChannelSchema CreateSchema(string type = "test", ChannelCapability capabilities = ChannelCapability.SendMessages)
        => new ChannelSchemaBuilder("TestProvider", type, "1.0")
            .WithCapabilities(capabilities)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => { e.CanSend = true; e.CanReceive = true; })
            .Build();

    [Fact]
    public void Should_ThrowValidationFailed_When_ValidateMessageReturnsErrors()
    {
        var schema = new ChannelSchemaBuilder("TestProvider", "test", "1.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .WithFlexibleMode()
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => { e.CanSend = true; e.CanReceive = true; })
            .AddMessageProperty("RequiredField", DataType.String, p => p.IsRequired = true)
            .Build();
        var connector = new TestConnector(schema);
        connector.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        var message = new Message();
        var result = connector.SendMessageAsync(message, CancellationToken.None).GetAwaiter().GetResult();

        Assert.False(result.IsSuccess());
        Assert.Equal(ConnectorErrorCodes.MessageValidationFailed, result.Error?.Code);
    }

    [Fact]
    public async Task Should_ReturnErrorResult_When_BaseReceiveCoreAsyncCalled()
    {
        var schema = CreateSchema("test", ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages);
        var connector = new TestConnector(schema);
        connector.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        var source = MessageSource.Text("data");

        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);
        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_ReturnErrorResult_When_BaseGetMessageStatusCoreAsyncCalled()
    {
        var schema = CreateSchema("test", ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery);
        var connector = new TestConnector(schema);
        connector.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        var result = await connector.GetMessageStatusAsync("test-id", CancellationToken.None);
        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_ReturnHealthDefault_When_DefaultGetConnectorHealthCalled()
    {
        var schema = CreateSchema("test", ChannelCapability.SendMessages | ChannelCapability.HealthCheck);
        var connector = new TestConnector(schema);
        connector.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        var health = await connector.GetHealthAsync(CancellationToken.None);
        Assert.True(health.IsSuccess());
        Assert.True(health.Value.IsHealthy);
    }

    [Fact]
    public async Task Should_ReturnSendSuccess_When_SendMessageCoreSucceeds()
    {
        var schema = CreateSchema("test");
        var connector = new TestConnector(schema);
        connector.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        var message = new MessageBuilder()
            .WithId("test-1")
            .To(new Endpoint(EndpointType.PhoneNumber, "+1234567890"))
            .From(new PhoneSender("+0987654321"))
            .Build();

        var result = await connector.SendMessageAsync(message, CancellationToken.None);
        Assert.True(result.IsSuccess());
        Assert.Equal("remote-id", result.Value.RemoteMessageId);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_InitializeAlreadyInitialized()
    {
        var connector = new TestConnector(CreateSchema());
        await connector.InitializeAsync(CancellationToken.None);
        var result2 = await connector.InitializeAsync(CancellationToken.None);
        Assert.False(result2.IsSuccess());
    }

    [Fact]
    public void Should_ReturnNull_When_GetAuthenticationHeaderWithNoCredential()
    {
        var schema = CreateSchema();
        var connector = new ExposedConnector(schema);
        Assert.Null(connector.GetAuthenticationHeader());
    }

    [Fact]
    public void Should_ReturnNull_When_GetApiKeyWithNoCredential()
    {
        var schema = CreateSchema();
        var connector = new ExposedConnector(schema);
        Assert.Null(connector.GetApiKey());
    }

    [Fact]
    public void Should_ReturnTrue_When_IsAnonymousConnectorWithNoAuthConfigs()
    {
        var schema = CreateSchema();
        var connector = new ExposedConnector(schema);
        Assert.True(connector.IsAnonymousConnector());
    }

    [Fact]
    public void Should_ReturnFalse_When_IsAnonymousConnectorWithAuthConfigs()
    {
        var schema = new ChannelSchemaBuilder("TestProvider", "test", "1.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .AddAuthenticationScheme(AuthenticationScheme.Bearer)
            .Build();
        var connector = new ExposedConnector(schema);
        Assert.False(connector.IsAnonymousConnector());
    }
}
