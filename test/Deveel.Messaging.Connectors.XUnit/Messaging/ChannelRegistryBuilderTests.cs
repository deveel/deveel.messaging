using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Deveel.Messaging.XUnit {
	public class ChannelRegistryBuilderTests {
		private IServiceCollection CreateServices() => new ServiceCollection();

		[Fact]
		public void Builder_Registers_Connector_Descriptor() {
			var services = CreateServices();
			var builder = services.AddChannelRegistry();

			builder.RegisterConnector<TestConnector>();

			// The hosted service should be registered
			Assert.Contains(services, d => d.ServiceType == typeof(IHostedService));
		}

		[Fact]
		public void RegisterConnector_Throws_On_NonConnector_Type() {
			var services = CreateServices();
			var builder = services.AddChannelRegistry();
			Assert.Throws<ArgumentException>(() => builder.RegisterConnector(typeof(string)));
		}

		[Fact]
		public void RegisterConnector_Type_And_Factory() {
			var services = CreateServices();
			var builder = services.AddChannelRegistry();
			bool factoryCalled = false;
			builder.RegisterConnector(typeof(TestConnector), (sp, schema) => {
				factoryCalled = true;
				return new TestConnector(schema);
			});
			Assert.Contains(services, d => d.ServiceType == typeof(IHostedService));
		}

		[Fact]
		public void RegisterConnector_Generic_And_Factory() {
			var services = CreateServices();
			var builder = services.AddChannelRegistry();
			bool factoryCalled = false;
			builder.RegisterConnector<TestConnector>((sp, schema) => {
				factoryCalled = true;
				return new TestConnector(schema);
			});
			Assert.Contains(services, d => d.ServiceType == typeof(IHostedService));
		}

		[Fact]
		public async Task HostedService_Performs_Registrations() {
			var services = CreateServices();
			
			var builder = services.AddChannelRegistry();
			services.AddSingleton<IChannelRegistry, DummyRegistry>();

			bool factoryCalled = false;
			builder.RegisterConnector<TestConnector>((sp, schema) => {
				factoryCalled = true;
				return new TestConnector(schema);
			});
			var provider = services.BuildServiceProvider();
			var hosted = provider.GetServices<IHostedService>().OfType<object>().FirstOrDefault(x => x.GetType().Name.Contains("ConnectorRegistrationService"));
			Assert.NotNull(hosted);
			var startAsync = hosted!.GetType().GetMethod("StartAsync");
			var registry = provider.GetRequiredService<IChannelRegistry>() as DummyRegistry;
			Assert.NotNull(startAsync);
			await (Task)startAsync.Invoke(hosted, new object[] { CancellationToken.None })!;
			Assert.True(registry!.Registered);
		}

		private class DummyRegistry : IChannelRegistry, IDisposable, IAsyncDisposable {
			public bool Registered { get; private set; }
			public void RegisterConnector<TConnector>(Func<IChannelSchema, TConnector>? connectorFactory = null) where TConnector : class, IChannelConnector {
				Registered = true;
			}
			public void RegisterConnector(Type connectorType, Func<IChannelSchema, IChannelConnector>? connectorFactory = null) {
				Registered = true;
			}
			public Task<TConnector> CreateConnectorAsync<TConnector>(CancellationToken cancellationToken = default) where TConnector : class, IChannelConnector => throw new NotImplementedException();
			public Task<TConnector> CreateConnectorAsync<TConnector>(IChannelSchema runtimeSchema, CancellationToken cancellationToken = default) where TConnector : class, IChannelConnector => throw new NotImplementedException();
			public Task<IChannelConnector> CreateConnectorAsync(Type connectorType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
			public Task<IChannelConnector> CreateConnectorAsync(Type connectorType, IChannelSchema runtimeSchema, CancellationToken cancellationToken = default) => throw new NotImplementedException();
			public IChannelSchema GetConnectorSchema<TConnector>() where TConnector : class, IChannelConnector => throw new NotImplementedException();
			public IChannelSchema GetConnectorSchema(Type connectorType) => throw new NotImplementedException();
			public IChannelSchema? FindSchema(string channelProvider, string channelType) => throw new NotImplementedException();
			public Type? FindConnector(string channelProvider, string channelType) => throw new NotImplementedException();
			public IEnumerable<ValidationResult> ValidateSchema<TConnector>(IChannelSchema runtimeSchema) where TConnector : class, IChannelConnector => throw new NotImplementedException();
			public IEnumerable<ValidationResult> ValidateSchema(Type connectorType, IChannelSchema runtimeSchema) => throw new NotImplementedException();
			public IEnumerable<Type> GetConnectorTypes() => throw new NotImplementedException();
			public IEnumerable<ConnectorDescriptor> GetConnectorDescriptors(Func<ConnectorDescriptor, bool>? predicate = null) => throw new NotImplementedException();
			public IEnumerable<IChannelSchema> QuerySchemas(Func<IChannelSchema, bool> predicate) => throw new NotImplementedException();
			public bool IsConnectorRegistered<TConnector>() where TConnector : class, IChannelConnector => throw new NotImplementedException();
			public bool IsConnectorRegistered(Type connectorType) => throw new NotImplementedException();
			public bool UnregisterConnector<TConnector>() where TConnector : class, IChannelConnector => throw new NotImplementedException();
			public bool UnregisterConnector(Type connectorType) => throw new NotImplementedException();
			
			public void Dispose()
			{
				// Empty implementation for test dummy
			}
			
			public ValueTask DisposeAsync()
			{
				return ValueTask.CompletedTask;
			}
		}
		
		[ChannelSchema(typeof(TestSchemaFactory))]
		private class TestConnector : IChannelConnector {
			public TestConnector(IChannelSchema schema) { Schema = schema; }
			public IChannelSchema Schema { get; }
			public ConnectorState State => ConnectorState.Ready;
			public Task<ConnectorResult<bool>> InitializeAsync(CancellationToken cancellationToken) => Task.FromResult(ConnectorResult<bool>.Success(true));
			public Task<ConnectorResult<bool>> TestConnectionAsync(CancellationToken cancellationToken) => Task.FromResult(ConnectorResult<bool>.Success(true));
			public Task<ConnectorResult<SendResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task<ConnectorResult<BatchSendResult>> SendBatchAsync(IMessageBatch batch, CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task<ConnectorResult<StatusInfo>> GetStatusAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken) => throw new NotImplementedException();
			public IAsyncEnumerable<ValidationResult> ValidateMessageAsync(IMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusAsync(MessageSource source, CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task<ConnectorResult<ReceiveResult>> ReceiveMessagesAsync(MessageSource source, CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task<ConnectorResult<ConnectorHealth>> GetHealthAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
			public Task ShutdownAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
		}
		private class TestSchemaFactory : IChannelSchemaFactory {
			public IChannelSchema CreateSchema() => new DummySchema();
		}
		private class DummySchema : IChannelSchema {
			public string ChannelProvider => "Test";
			public string ChannelType => "Test";
			public string Version => "1.0";
			public string? DisplayName => "Test";
			public bool IsStrict => false;
			public ChannelCapability Capabilities => ChannelCapability.SendMessages;
			public IReadOnlyList<ChannelEndpointConfiguration> Endpoints => new List<ChannelEndpointConfiguration>();
			public IReadOnlyList<ChannelParameter> Parameters => new List<ChannelParameter>();
			public IReadOnlyList<MessagePropertyConfiguration> MessageProperties => new List<MessagePropertyConfiguration>();
			public IReadOnlyList<MessageContentType> ContentTypes => new List<MessageContentType>();
			public IReadOnlyList<AuthenticationConfiguration> AuthenticationConfigurations => new List<AuthenticationConfiguration>();
		}
	}
}
