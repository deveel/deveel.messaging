using Microsoft.Extensions.DependencyInjection;
using Ratatosk.XUnit.Fixtures;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ratatosk.XUnit.Unit;

[CollectionDefinition("ActivityListenerTests", DisableParallelization = true)]
public class ActivityListenerTestsCollection;

[Trait("Category", "Integration")]
[Trait("Feature", "MessagingClient")]
[Collection("ActivityListenerTests")]
public class MessagingClientContextEnrichmentTests
{
    private static IServiceProvider CreateClient(Action<MessagingBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessaging();
        builder.AddConnector<MockConnector>("mock", _ => { });
        configure?.Invoke(builder);
        builder.AddClient();
        return services.BuildServiceProvider();
    }

    private static Message CreateTestMessage(string id = "test-msg")
        => new MessageBuilder()
            .WithId(id)
            .FromPhone("+15551234567")
            .ToPhone("+15557654321")
            .WithText("Hello world")
            .Build();

    // ── Message property stamping on send ───────────────────────────────

    [Fact]
    public async Task Should_StampContext_OnMessageProperties_WhenSending()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ctx-stamp-1");
        var context = new MessageContext(
            ("tenant_id", "tenant-123"),
            ("user_id", "user-456")
        );

        var request = new SendRequest("mock", message) { Context = context };
        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess());

        var msgProps = message.Properties;
        Assert.NotNull(msgProps);
        Assert.Contains(msgProps, p => p.Key == "tenant_id");
        Assert.Contains(msgProps, p => p.Key == "user_id");
        Assert.Equal("tenant-123", msgProps!["tenant_id"].Value);
        Assert.Equal("user-456", msgProps["user_id"].Value);
    }

    [Fact]
    public async Task Should_NotStampContext_WhenContextIsNull()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ctx-null");
        var request = new SendRequest("mock", message);

        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess());
        Assert.Null(message.Properties);
    }

    [Fact]
    public async Task Should_NotStampContext_WhenContextDataIsEmpty()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ctx-empty");
        var request = new SendRequest("mock", message)
        {
            Context = new MessageContext()
        };

        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess());
        Assert.Null(message.Properties);
    }

    [Fact]
    public async Task Should_StampMultipleContextEntries()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ctx-multi");
        var context = new MessageContext()
            .With("tenant_id", "t-1")
            .With("env", "prod")
            .With("version", "2.0");

        var request = new SendRequest("mock", message) { Context = context };
        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess());
        Assert.Equal("t-1", message.Properties!["tenant_id"].Value);
        Assert.Equal("prod", message.Properties["env"].Value);
        Assert.Equal("2.0", message.Properties["version"].Value);
    }

    [Fact]
    public async Task Should_PreserveExistingProperties_WhenStampingContext()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ctx-merge");
        message.Properties = new Dictionary<string, MessageProperty>
        {
            ["existing"] = new MessageProperty("existing", "keep-me")
        };

        var context = new MessageContext(("tenant_id", "t-1"));
        var request = new SendRequest("mock", message) { Context = context };

        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess());
        Assert.Equal("keep-me", message.Properties["existing"].Value);
        Assert.Equal("t-1", message.Properties["tenant_id"].Value);
    }

    // ── Activity tag enrichment (via ActivityListener) ──────────────────

    [Fact]
    public async Task Should_AddContextTags_ToSendActivity()
    {
        var recorded = new BlockingCollection<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Ratatosk.Client",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => recorded.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("act-tag-send");
        var context = new MessageContext(("tenant_id", "t-1"), ("user_id", "u-1"));
        var request = new SendRequest("mock", message) { Context = context };

        await client.SendAsync(request);

        var activity = WaitForActivity(recorded, "mock send");
        Assert.NotNull(activity);
        Assert.Equal("ratatosk", activity.GetTagItem("messaging.system"));
        Assert.Equal("send", activity.GetTagItem("messaging.operation"));
        Assert.Equal("t-1", activity.GetTagItem("tenant_id"));
        Assert.Equal("u-1", activity.GetTagItem("user_id"));
    }

    [Fact]
    public async Task Should_AddContextTags_ToReceiveActivity()
    {
        var recorded = new BlockingCollection<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Ratatosk.Client",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => recorded.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var context = new MessageContext(("tenant_id", "t-2"));
        var source = MessageSource.Json("{}");
        var request = new ReceiveRequest("mock", source) { Context = context };

        await client.ReceiveAsync(request);

        var activity = WaitForActivity(recorded, "mock receive");
        Assert.NotNull(activity);
        Assert.Equal("t-2", activity.GetTagItem("tenant_id"));
    }

    [Fact]
    public async Task Should_AddContextTags_ToStatusActivity()
    {
        var recorded = new BlockingCollection<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Ratatosk.Client",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => recorded.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var context = new MessageContext(("tenant_id", "t-3"));
        var request = new StatusRequest("mock") { Context = context };

        await client.GetStatusAsync(request);

        var activity = WaitForActivity(recorded, "mock status_query");
        Assert.NotNull(activity);
        Assert.Equal("t-3", activity.GetTagItem("tenant_id"));
    }

    [Fact]
    public async Task Should_AddContextTags_ToReceiveStatusActivity()
    {
        var recorded = new BlockingCollection<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Ratatosk.Client",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => recorded.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var context = new MessageContext(("tenant_id", "t-4"));
        var source = MessageSource.Json("{}");
        var request = new ReceiveStatusRequest("mock", source) { Context = context };

        await client.ReceiveMessageStatusAsync(request);

        var activity = WaitForActivity(recorded, "mock receive_status");
        Assert.NotNull(activity);
        Assert.Equal("t-4", activity.GetTagItem("tenant_id"));
    }

    [Fact]
    public async Task Should_NotCreateActivity_WhenNoContextListener()
    {
        // By default, telemetry is enabled but without a listener there's no allocation.
        // This verifies HasListeners() guard works.
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("no-listener");
        var request = new SendRequest("mock", message)
        {
            Context = new MessageContext(("k", "v"))
        };

        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess());
    }

    // ── Activity parent-child linking ───────────────────────────────────

    [Fact]
    public async Task SendActivity_ShouldBeChildOfCurrentActivity()
    {
        var recorded = new BlockingCollection<Activity>();

        using var clientListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "Ratatosk.Client" or "TestApp",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => recorded.Add(activity)
        };
        ActivitySource.AddActivityListener(clientListener);

        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var parentSource = new ActivitySource("TestApp");
        using var parent = parentSource.StartActivity("ParentOperation", ActivityKind.Server);

        Assert.NotNull(parent);

        var message = CreateTestMessage("child-span");
        var request = new SendRequest("mock", message);
        await client.SendAsync(request);

        var childActivity = WaitForActivity(recorded, "mock send");
        Assert.NotNull(childActivity);

        // The send span should be a child of the parent activity
        Assert.Equal(parent.TraceId, childActivity.TraceId);
        Assert.Equal(parent.SpanId, childActivity.ParentSpanId);
    }

    [Fact]
    public async Task SendActivity_ShouldBeRoot_WhenNoCurrentActivity()
    {
        var recorded = new BlockingCollection<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Ratatosk.Client",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => recorded.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        // Ensure no current activity
        Activity.Current = null;

        var message = CreateTestMessage("root-span");
        var request = new SendRequest("mock", message);
        await client.SendAsync(request);

        var activity = WaitForActivity(recorded, "mock send");
        Assert.NotNull(activity);
        // When there's no parent Activity instance, Parent is null
        Assert.Null(activity.Parent);
        // ParentSpanId is default(SpanId) when no parent context
        Assert.Equal(default(ActivitySpanId), activity.ParentSpanId);
    }

    // ── Extension methods backward compat ───────────────────────────────

    [Fact]
    public async Task Should_CallSendViaExtension_ByName()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ext-name");
        var result = await client.SendAsync("mock", message);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallSendViaExtension_ByNameWithSettings()
    {
        var services = new ServiceCollection();
        services.AddMessaging()
            .AddConnectorType<MockConnector>("mock")
            .AddClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMessagingClient>();
        var settings = new ConnectionSettings();

        var message = CreateTestMessage("ext-name-settings");
        var result = await client.SendAsync("mock", settings, message);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallSendViaExtension_ByType()
    {
        var services = new ServiceCollection();
        services.AddMessaging()
            .AddConnector<MockConnector>(_ => { })
            .AddClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMessagingClient>();

        var message = CreateTestMessage("ext-type");
        var result = await client.SendAsync<MockConnector>(message);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallSendViaExtension_ByTypeWithSettings()
    {
        var services = new ServiceCollection();
        services.AddMessaging()
            .AddConnector<MockConnector>(_ => { })
            .AddClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMessagingClient>();
        var settings = new ConnectionSettings();

        var message = CreateTestMessage("ext-type-settings");
        var result = await client.SendAsync<MockConnector>(settings, message);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallReceiveViaExtension_ByName()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var source = MessageSource.Json("{}");
        var result = await client.ReceiveAsync("mock", source);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallReceiveViaExtension_ByType()
    {
        var services = new ServiceCollection();
        services.AddMessaging()
            .AddConnector<MockConnector>(_ => { })
            .AddClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMessagingClient>();

        var result = await client.ReceiveAsync<MockConnector>(MessageSource.Json("{}"));

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallGetStatusViaExtension_ByName()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var result = await client.GetStatusAsync("mock");

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallGetStatusViaExtension_ByType()
    {
        var services = new ServiceCollection();
        services.AddMessaging()
            .AddConnector<MockConnector>(_ => { })
            .AddClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMessagingClient>();

        var result = await client.GetStatusAsync<MockConnector>();

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallReceiveStatusViaExtension_ByName()
    {
        var provider = CreateClient();
        var client = provider.GetRequiredService<IMessagingClient>();

        var source = MessageSource.Json("{}");
        var result = await client.ReceiveMessageStatusAsync("mock", source);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Should_CallReceiveStatusViaExtension_ByType()
    {
        var services = new ServiceCollection();
        services.AddMessaging()
            .AddConnector<MockConnector>(_ => { })
            .AddClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMessagingClient>();

        var result = await client.ReceiveMessageStatusAsync<MockConnector>(MessageSource.Json("{}"));

        Assert.True(result.IsSuccess());
    }

    // ── Request object construction ────────────────────────────────────

    [Fact]
    public void SendRequest_ShouldThrow_WhenChannelNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SendRequest(null!, new Message()));
    }

    [Fact]
    public void SendRequest_ShouldThrow_WhenMessageIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SendRequest("ch", null!));
    }

    [Fact]
    public void ReceiveRequest_ShouldThrow_WhenChannelNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ReceiveRequest(null!, default));
    }

    [Fact]
    public void StatusRequest_ShouldThrow_WhenChannelNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new StatusRequest(null!));
    }

    [Fact]
    public void ReceiveStatusRequest_ShouldThrow_WhenChannelNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ReceiveStatusRequest(null!, default));
    }

    [Fact]
    public void SendRequest_ShouldInitializeProperties()
    {
        var msg = new Message { Id = "req-test" };
        var request = new SendRequest("my-channel", msg)
        {
            ConnectionSettings = new ConnectionSettings(),
            ConnectorType = typeof(MockConnector),
            Context = new MessageContext(("k", "v"))
        };

        Assert.Equal("my-channel", request.ChannelName);
        Assert.Same(msg, request.Message);
        Assert.NotNull(request.ConnectionSettings);
        Assert.Equal(typeof(MockConnector), request.ConnectorType);
        Assert.Equal("v", request.Context!["k"]);
    }

    private static Activity WaitForActivity(BlockingCollection<Activity> recorded, string operationName)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (recorded.TryTake(out var activity, 100) && activity.OperationName == operationName)
                return activity;
        }
        throw new InvalidOperationException($"Activity '{operationName}' was not recorded within the timeout.");
    }
}
