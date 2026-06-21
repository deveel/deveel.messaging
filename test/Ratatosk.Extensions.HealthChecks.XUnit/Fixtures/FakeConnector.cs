using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ratatosk.Extensions.HealthChecks.XUnit.Fixtures
{
    [ChannelSchema(typeof(FakeConnectorSchemaFactory))]
    public class FakeConnector : IChannelConnector
    {
        private readonly Func<ConnectorHealth> _healthFactory;

        public FakeConnector(Func<ConnectorHealth> healthFactory, string name = "Fake")
        {
            _healthFactory = healthFactory;
            Name = name;
        }

        public string Name { get; }
        public IChannelSchema Schema { get; } = new FakeSchema();
        public ConnectionSettings ConnectionSettings { get; } = new();
        public ConnectorState State => ConnectorState.Ready;
        public bool IsReusable => true;

        public ValueTask<OperationResult<bool>> InitializeAsync(CancellationToken cancellationToken)
            => new(OperationResult<bool>.Success(true));

        public ValueTask<OperationResult<bool>> TestConnectionAsync(CancellationToken cancellationToken)
            => new(OperationResult<bool>.Success(true));

        public ValueTask<OperationResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<BatchSendResult>> SendBatchAsync(IMessageBatch batch, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<StatusInfo>> GetStatusAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<StatusUpdatesResult>> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage message, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<OperationResult<StatusUpdateResult>> ReceiveMessageStatusAsync(MessageSource source, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource source, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<ConnectorHealth>> GetHealthAsync(CancellationToken cancellationToken)
        {
            var health = _healthFactory();
            return new(OperationResult<ConnectorHealth>.Success(health));
        }

        public ValueTask ShutdownAsync(CancellationToken cancellationToken) => default;
    }

    public class FakeSchema : IChannelSchema
    {
        public string ChannelProvider { get; set; } = "FakeProvider";
        public string ChannelType { get; set; } = "FakeChannel";
        public string Version { get; set; } = "1.0";
        public string? DisplayName { get; set; } = "Fake Connector";
        public bool IsStrict { get; set; }
        public ChannelCapability Capabilities { get; set; } = ChannelCapability.SendMessages | ChannelCapability.HealthCheck;
        public IReadOnlyList<ChannelEndpointConfiguration> Endpoints { get; set; } = new List<ChannelEndpointConfiguration>();
        public IReadOnlyList<ChannelParameter> Parameters { get; set; } = new List<ChannelParameter>();
        public IReadOnlyList<MessagePropertyConfiguration> MessageProperties { get; set; } = new List<MessagePropertyConfiguration>();
        public IReadOnlyList<MessageContentType> ContentTypes { get; set; } = new List<MessageContentType>();
        public IReadOnlyList<AuthenticationConfiguration> AuthenticationConfigurations { get; set; } = new List<AuthenticationConfiguration>();
    }

    [ChannelSchema(typeof(FakeConnectorSchemaFactory))]
    public class FakeFailingConnector : IChannelConnector
    {
        public FakeFailingConnector(string name = "Failing")
        {
            Name = name;
        }

        public string Name { get; }
        public IChannelSchema Schema { get; } = new FakeSchema();
        public ConnectionSettings ConnectionSettings { get; } = new();
        public ConnectorState State => ConnectorState.Error;
        public bool IsReusable => true;

        public ValueTask<OperationResult<bool>> InitializeAsync(CancellationToken cancellationToken)
            => new(OperationResult<bool>.Fail("ERR", "test", "Failed"));

        public ValueTask<OperationResult<bool>> TestConnectionAsync(CancellationToken cancellationToken)
            => new(OperationResult<bool>.Fail("ERR", "test", "Failed"));

        public ValueTask<OperationResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<BatchSendResult>> SendBatchAsync(IMessageBatch batch, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<StatusInfo>> GetStatusAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<StatusUpdatesResult>> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage message, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<OperationResult<StatusUpdateResult>> ReceiveMessageStatusAsync(MessageSource source, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource source, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<OperationResult<ConnectorHealth>> GetHealthAsync(CancellationToken cancellationToken)
            => new(OperationResult<ConnectorHealth>.Fail("HEALTH_ERR", "test", "Health check failed"));

        public ValueTask ShutdownAsync(CancellationToken cancellationToken) => default;
    }

    public class FakeConnectorSchemaFactory : IChannelSchemaFactory
    {
        public IChannelSchema CreateSchema() => new FakeSchema();
    }
}