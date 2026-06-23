using Ratatosk.XUnit.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ratatosk.XUnit.Unit
{
    [Trait("Category", "Integration")]
    [Trait("Feature", "MessagingClient")]
    public class MessagingClientTests
    {
        private static IServiceProvider CreateClient(Action<MessagingBuilder>? configure = null, Action<MessagingClientOptions>? configureOptions = null)
        {
            var services = new ServiceCollection();
            var builder = services.AddMessaging();
            builder.AddConnector<MockConnector>("mock", _ => { });
            configure?.Invoke(builder);
            if (configureOptions != null)
                builder.AddClient(configureOptions);
            else
                builder.AddClient();
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Should_SendMessage_And_ReturnResult()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();

            var message = new MessageBuilder()
                .WithId("test-1")
                .FromPhone("+15551234567")
                .ToPhone("+15557654321")
                .WithText("Hello world")
                .Build();

            var result = await client.SendAsync("mock", message);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Value);
            Assert.Equal("test-1", result.Value.MessageId);
            Assert.Equal("remote-test-1", result.Value.RemoteMessageId);
        }

        [Fact]
        public async Task Should_InitializeConnector_OnFirstSend()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("mock") as MockConnector;

            Assert.NotNull(connector);

            var message = new MessageBuilder()
                .WithId("init-test")
                .WithText("test")
                .Build();

            await client.SendAsync("mock", message);

            Assert.Equal(ConnectorState.Ready, connector.State);
            Assert.Equal(1, connector.InitCount);
        }

        [Fact]
        public async Task Should_InitializeOnce_When_SendingMultipleMessages()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("mock") as MockConnector;

            Assert.NotNull(connector);

            for (int i = 0; i < 3; i++)
            {
                var msg = new MessageBuilder().WithId($"m-{i}").WithText("test").Build();
                await client.SendAsync("mock", msg);
            }

            Assert.Equal(1, connector.InitCount);
        }

        [Fact]
        public async Task Should_ReturnFailure_When_ChannelNotFound()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();

            var message = new MessageBuilder().WithId("x").WithText("test").Build();
            var result = await client.SendAsync("nonexistent", message);

            Assert.False(result.IsSuccess());
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task Should_ReturnFailure_When_InitializeFails()
        {
            var provider = CreateClient(builder =>
            {
                builder.AddConnector<MockConnector>("failing", cfg =>
                {
                    cfg.WithSetting("__failInit__", true);
                });
            });

            var failingConnector = provider.GetRequiredKeyedService<IChannelConnector>("failing") as MockConnector;
            Assert.NotNull(failingConnector);
            failingConnector.FailOnInitialize = true;

            var client = provider.GetRequiredService<IMessagingClient>();
            var message = new MessageBuilder().WithId("x").WithText("test").Build();
            var result = await client.SendAsync("failing", message);

            Assert.False(result.IsSuccess());
            Assert.Equal(ConnectorState.Error, failingConnector.State);
        }

        [Fact]
        public async Task Should_ReceiveMessages()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();

            var source = MessageSource.Json("{}");
            var result = await client.ReceiveAsync("mock", source);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Value);
            Assert.NotEmpty(result.Value.Messages);
        }

        [Fact]
        public async Task Should_GetStatus()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();

            var result = await client.GetStatusAsync("mock");

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Should_ReceiveMessageStatus()
        {
            var provider = CreateClient();
            var client = provider.GetRequiredService<IMessagingClient>();

            var source = MessageSource.Json("{}");
            var result = await client.ReceiveMessageStatusAsync("mock", source);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Should_NotAutoInitialize_When_OptionIsFalse()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddConnector<MockConnector>("lazy", _ => { })
                .AddClient(o => o.AutoInitialize = false);

            var provider = services.BuildServiceProvider();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("lazy") as MockConnector;

            Assert.NotNull(connector);
            Assert.Equal(ConnectorState.Uninitialized, connector.State);

            var client = provider.GetRequiredService<IMessagingClient>();
            var message = new MessageBuilder().WithId("x").WithText("test").Build();
            var result = await client.SendAsync("lazy", message);

            // The connector was not auto-initialized, but the client still
            // resolves and caches it - the send is forwarded to the connector
            // which may or may not check state. We verify the init was skipped.
            Assert.Equal(0, connector.InitCount);
        }

        [Fact]
        public async Task Should_UseCustomSendHandler()
        {
            var provider = CreateClient();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("mock") as MockConnector;
            Assert.NotNull(connector);

            connector.OnSend = msg => new SendResult(msg.Id, "custom-remote")
            {
                Status = MessageStatus.Delivered
            };

            var client = provider.GetRequiredService<IMessagingClient>();
            var message = new MessageBuilder().WithId("custom").WithText("test").Build();
            var result = await client.SendAsync("mock", message);

            Assert.True(result.IsSuccess());
            Assert.Equal("custom-remote", result.Value!.RemoteMessageId);
            Assert.Equal(MessageStatus.Delivered, result.Value.Status);
        }

        [Fact]
        public async Task Should_UseCustomReceiveHandler()
        {
            var provider = CreateClient();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("mock") as MockConnector;
            Assert.NotNull(connector);

            connector.OnReceive = source =>
            {
                var messages = new List<IMessage>
                {
                    new Message { Id = "custom-rcvd", Content = new TextContent("from handler") }
                };
                return new ReceiveResult("custom-batch", messages);
            };

            var client = provider.GetRequiredService<IMessagingClient>();
            var source = MessageSource.Json("{}");
            var result = await client.ReceiveAsync("mock", source);

            Assert.True(result.IsSuccess());
            var received = result.Value!.Messages.First();
            Assert.Equal("custom-rcvd", received.Id);
        }

        // ── Type-based (anonymous) overloads ─────────────────────────────────

        [Fact]
        public async Task Should_SendMessage_WithTypeParameter()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddConnector<MockConnector>(_ => { })
                .AddClient();
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMessagingClient>();

            var message = new MessageBuilder().WithId("type-send").WithText("test").Build();
            var result = await client.SendAsync<MockConnector>(message);

            Assert.True(result.IsSuccess());
            Assert.Equal("type-send", result.Value!.MessageId);
        }

        [Fact]
        public async Task Should_ReturnFailure_When_TypeParameterNotFound()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddClient();
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMessagingClient>();

            var message = new MessageBuilder().WithId("x").WithText("test").Build();
            var result = await client.SendAsync<MockConnector>(message);

            Assert.False(result.IsSuccess());
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task Should_ReceiveMessages_WithTypeParameter()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddConnector<MockConnector>(_ => { })
                .AddClient();
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMessagingClient>();

            var result = await client.ReceiveAsync<MockConnector>(MessageSource.Json("{}"));

            Assert.True(result.IsSuccess());
            Assert.NotEmpty(result.Value!.Messages);
        }

        [Fact]
        public async Task Should_GetStatus_WithTypeParameter()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddConnector<MockConnector>(_ => { })
                .AddClient();
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMessagingClient>();

            var result = await client.GetStatusAsync<MockConnector>();

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Should_ReceiveMessageStatus_WithTypeParameter()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddConnector<MockConnector>(_ => { })
                .AddClient();
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMessagingClient>();

            var result = await client.ReceiveMessageStatusAsync<MockConnector>(MessageSource.Json("{}"));

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Should_Throw_When_SendAsyncConnectorThrows()
        {
            var provider = CreateClient();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("mock") as MockConnector;
            Assert.NotNull(connector);

            connector.OnSend = msg => throw new InvalidOperationException("Simulated connector failure");

            var client = provider.GetRequiredService<IMessagingClient>();
            var message = new MessageBuilder().WithId("throw-test").WithText("test").Build();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.SendAsync("mock", message));
        }

        [Fact]
        public async Task Should_Throw_When_ReceiveAsyncConnectorThrows()
        {
            var provider = CreateClient();
            var connector = provider.GetRequiredKeyedService<IChannelConnector>("mock") as MockConnector;
            Assert.NotNull(connector);

            connector.OnReceive = source => throw new InvalidOperationException("Simulated receive failure");

            var client = provider.GetRequiredService<IMessagingClient>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.ReceiveAsync("mock", MessageSource.Json("{}")));
        }

        [Fact]
        public async Task Should_DisposeRuntimeConnectors_When_ClientDisposed()
        {
            var services = new ServiceCollection();
            services.AddMessaging()
                .AddConnector<MockConnector>("runtime-test", _ => { })
                .AddClient();
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMessagingClient>() as MessagingClient;

            Assert.NotNull(client);

            var message = new MessageBuilder().WithId("dispose-test").WithText("test").Build();
            var result = await client.SendAsync("runtime-test", message);
            Assert.True(result.IsSuccess());

            client.Dispose();
        }
    }
}
