using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Feature", "Telemetry")]
public class ConnectorTelemetryTests
{
    private sealed class TelemetryTestConnector : ChannelConnectorBase
    {
        public TelemetryTestConnector(IChannelSchema schema, ConnectionSettings? settings = null)
            : base(schema, settings ?? new ConnectionSettings())
        {
        }

        public bool FailSend { get; set; }
        public bool FailReceive { get; set; }

        protected override ValueTask InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            SetState(ConnectorState.Ready);
            return ValueTask.CompletedTask;
        }

        protected override ValueTask TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        protected override Task<SendResult> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            if (FailSend)
                throw new ConnectorException("SEND_ERROR", "Test", "Send failed");

            return Task.FromResult(new SendResult(message.Id!, "remote-id")
            {
                Status = MessageStatus.Delivered
            });
        }

        protected override Task<StatusInfo> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new StatusInfo("OK"));

        protected override Task<ReceiveResult> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            if (FailReceive)
                throw new ConnectorException("RECV_ERROR", "Test", "Receive failed");

            var messages = new List<IMessage>
            {
                new Message { Id = "recv-1" },
                new Message { Id = "recv-2" }
            };

            return Task.FromResult(new ReceiveResult("batch-1", messages));
        }
    }

    private static IChannelSchema CreateSchema()
        => new ChannelSchemaBuilder("TestProvider", "test", "1.0")
            .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | ChannelCapability.HealthCheck)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => { e.CanSend = true; e.CanReceive = true; })
            .Build();

    private static Message CreateTestMessage()
        => new Message
        {
            Id = "msg-1",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1234"),
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+5678")
        };

    [Fact]
    public async Task SendOperation_CreatesActivity()
    {
        var events = new System.Collections.Concurrent.ConcurrentBag<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Ratatosk.Connector."),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => events.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var message = CreateTestMessage();
        var result = await connector.SendMessageAsync(message, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.Contains(events, a =>
            a.OperationName.Contains("send") &&
            a.Tags.Any(t => t.Key == "messaging.operation" && (string?)t.Value == "send"));
    }

    [Fact]
    public async Task SendOperation_RecordsSentCounter()
    {
        var counterValues = new List<int>();

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == "ratatosk.messages.sent")
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            if (instrument.Name == "ratatosk.messages.sent")
                counterValues.Add(value);
        });
        meterListener.Start();

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var message = CreateTestMessage();
        await connector.SendMessageAsync(message, TestContext.Current.CancellationToken);

        meterListener.RecordObservableInstruments();

        Assert.Contains(counterValues, v => v > 0);
    }

    [Fact]
    public async Task SendOperation_Failure_RecordsFailedCounter()
    {
        var failedValues = new List<int>();

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name is "ratatosk.messages.send_failed")
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            if (instrument.Name == "ratatosk.messages.send_failed")
                failedValues.Add(value);
        });
        meterListener.Start();

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema) { FailSend = true };
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var message = CreateTestMessage();
        await connector.SendMessageAsync(message, TestContext.Current.CancellationToken);

        meterListener.RecordObservableInstruments();

        Assert.Contains(failedValues, v => v > 0);
    }

    [Fact]
    public async Task SendOperation_TagsMessageId()
    {
        Activity? captured = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Ratatosk.Connector."),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if (activity.OperationName.Contains("send"))
                    captured = activity;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var message = CreateTestMessage();
        await connector.SendMessageAsync(message, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Contains(captured.Tags, t => t.Key == "messaging.message.id" && (string?)t.Value == "msg-1");
    }

    [Fact]
    public async Task ReceiveOperation_CreatesActivity()
    {
        var events = new System.Collections.Concurrent.ConcurrentBag<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Ratatosk.Connector."),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => events.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var source = MessageSource.Text("{}");
        var result = await connector.ReceiveMessagesAsync(source, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        
        // Small delay to ensure all Activity callbacks have completed
        await Task.Delay(500, TestContext.Current.CancellationToken);
        
        Assert.Contains(events, a =>
            a.OperationName.Contains("receive") &&
            a.Tags.Any(t => t.Key == "messaging.operation" && (string?)t.Value == "receive"));
    }

    [Fact]
    public async Task ReceiveOperation_RecordsMetrics()
    {
        var recvValues = new List<int>();

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == "ratatosk.messages.received")
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            if (instrument.Name == "ratatosk.messages.received")
                recvValues.Add(value);
        });
        meterListener.Start();

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var source = MessageSource.Text("{}");
        await connector.ReceiveMessagesAsync(source, TestContext.Current.CancellationToken);

        meterListener.RecordObservableInstruments();

        Assert.Contains(recvValues, v => v > 0);
    }

    [Fact]
    public async Task ReceiveOperation_Failure_RecordsFailedCounter()
    {
        var failedValues = new List<int>();

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == "ratatosk.messages.receive_failed")
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            if (instrument.Name == "ratatosk.messages.receive_failed")
                failedValues.Add(value);
        });
        meterListener.Start();

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema) { FailReceive = true };
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        var source = MessageSource.Text("{}");
        await connector.ReceiveMessagesAsync(source, TestContext.Current.CancellationToken);

        meterListener.RecordObservableInstruments();

        Assert.Contains(failedValues, v => v > 0);
    }

    [Fact]
    public async Task InitializeOperation_CreatesActivity()
    {
        Activity? captured = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Ratatosk.Connector."),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if (activity.OperationName.Contains("initialize"))
                    captured = activity;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        var result = await connector.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.NotNull(captured);
        Assert.Contains(captured.Tags, t => t.Key == "messaging.operation" && (string?)t.Value == "initialize");
    }

    [Fact]
    public async Task MultipleConnectors_HaveSeparateSources()
    {
        var sources = new System.Collections.Concurrent.ConcurrentBag<string>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source =>
            {
                sources.Add(source.Name);
                return source.Name.StartsWith("Ratatosk.Connector.");
            },
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var schema1 = new ChannelSchemaBuilder("ProviderA", "type-a", "1.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => { e.CanSend = true; })
            .Build();

        var schema2 = new ChannelSchemaBuilder("ProviderB", "type-b", "1.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .HandlesMessageEndpoint(EndpointType.PhoneNumber, e => { e.CanSend = true; })
            .Build();

        var connector1 = new TelemetryTestConnector(schema1);
        var connector2 = new TelemetryTestConnector(schema2);

        await connector1.InitializeAsync(TestContext.Current.CancellationToken);
        await connector2.InitializeAsync(TestContext.Current.CancellationToken);
        
        // Small delay to ensure all Activity callbacks have completed
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.Contains(sources, s => s.Contains("type-a"));
        Assert.Contains(sources, s => s.Contains("type-b"));
    }

    [Fact]
    public async Task StatusQuery_CreatesActivity()
    {
        Activity? captured = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Ratatosk.Connector."),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if (activity.OperationName.Contains("status_query"))
                    captured = activity;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        await connector.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
    }

    [Fact]
    public async Task HealthCheck_CreatesActivity()
    {
        Activity? captured = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Ratatosk.Connector."),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if (activity.OperationName.Contains("health_check"))
                    captured = activity;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var schema = CreateSchema();
        var connector = new TelemetryTestConnector(schema);
        await connector.InitializeAsync(TestContext.Current.CancellationToken);

        await connector.GetHealthAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
    }
}
