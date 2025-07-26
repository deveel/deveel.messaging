//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Integration tests demonstrating how to use the <see cref="ChannelConnectorBase"/>
/// abstract class to create concrete connector implementations.
/// </summary>
public class ChannelConnectorUsageExamples
{
	[Fact]
	public async Task EmailConnector_Example_CanSendMessage()
	{
		// Arrange
		var schema = new ChannelSchema("SMTP", "Email", "1.0.0")
			.WithDisplayName("Email Connector")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.HealthCheck)
			.AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddAuthenticationType(AuthenticationType.Basic);

		var connector = new ExampleEmailConnector(schema);

		// Act
		await connector.InitializeAsync(CancellationToken.None);
		var message = new ExampleMessage("test@example.com", "Hello World");
		var result = await connector.SendMessageAsync(message, CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.NotNull(result.Value);
		Assert.Equal(message.Id, result.Value.MessageId);
		Assert.StartsWith("email-", result.Value.RemoteMessageId);
	}

	[Fact]
	public async Task SmsConnector_Example_SupportsStatusQueries()
	{
		// Arrange
		var schema = new ChannelSchema("Twilio", "SMS", "2.0.0")
			.WithDisplayName("SMS Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.MessageStatusQuery |
				ChannelCapability.HealthCheck)
			.AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true, IsSensitive = true })
			.AddContentType(MessageContentType.PlainText)
			.AddAuthenticationType(AuthenticationType.Token);

		var connector = new ExampleSmsConnector(schema);

		// Act
		await connector.InitializeAsync(CancellationToken.None);
		var message = new ExampleMessage("+1234567890", "Hello SMS");
		var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
		var statusResult = await connector.GetMessageStatusAsync(sendResult.Value!.MessageId, CancellationToken.None);

		// Assert
		Assert.True(sendResult.Successful);
		Assert.True(statusResult.Successful);
		Assert.Equal(message.Id, statusResult.Value!.MessageId);
		Assert.Single(statusResult.Value.Updates);
	}

	[Fact]
	public async Task ConnectorWithHealthCheck_Example_ReturnsHealthStatus()
	{
		// Arrange
		var schema = new ChannelSchema("Custom", "Health", "1.0.0")
			.WithCapabilities(ChannelCapability.HealthCheck);

		var connector = new ExampleHealthConnector(schema);

		// Act
		await connector.InitializeAsync(CancellationToken.None);
		var healthResult = await connector.GetHealthAsync(CancellationToken.None);

		// Assert
		Assert.True(healthResult.Successful);
		Assert.NotNull(healthResult.Value);
		Assert.True(healthResult.Value.IsHealthy);
		Assert.Equal(ConnectorState.Ready, healthResult.Value.State);
		Assert.Contains("connections", healthResult.Value.Metrics.Keys);
	}

	// Example Email Connector Implementation
	private class ExampleEmailConnector : ChannelConnectorBase
	{
		public ExampleEmailConnector(IChannelSchema schema) : base(schema) { }

		protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
		{
			// Simulate email server configuration
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
		{
			// Simulate connection test to email server
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
		{
			// Simulate sending email
			var result = new SendResult(message.Id, $"email-{Guid.NewGuid()}");
			result.Status = "sent";
			return Task.FromResult(ConnectorResult<SendResult>.Success(result));
		}

		protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
		{
			var status = new StatusInfo("Connected");
			return Task.FromResult(ConnectorResult<StatusInfo>.Success(status));
		}
	}

	// Example SMS Connector Implementation with Status Query Support
	private class ExampleSmsConnector : ChannelConnectorBase
	{
		public ExampleSmsConnector(IChannelSchema schema) : base(schema) { }

		protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
		{
			var result = new SendResult(message.Id, $"sms-{Guid.NewGuid()}");
			result.Status = "queued";
			return Task.FromResult(ConnectorResult<SendResult>.Success(result));
		}

		protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
		{
			var status = new StatusInfo("Active");
			return Task.FromResult(ConnectorResult<StatusInfo>.Success(status));
		}

		// Override to provide status query capability
		protected override Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusCoreAsync(string messageId, CancellationToken cancellationToken)
		{
			var statusUpdate = new StatusUpdateResult(MessageStatus.Delivered);
			var result = new StatusUpdatesResult(messageId, new[] { statusUpdate });
			return Task.FromResult(ConnectorResult<StatusUpdatesResult>.Success(result));
		}
	}

	// Example Health-focused Connector Implementation
	private class ExampleHealthConnector : ChannelConnectorBase
	{
		public ExampleHealthConnector(IChannelSchema schema) : base(schema) { }

		protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
		{
			// This connector doesn't support sending
			throw new NotSupportedException("This connector is for health monitoring only");
		}

		protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
		{
			var status = new StatusInfo("Monitoring");
			return Task.FromResult(ConnectorResult<StatusInfo>.Success(status));
		}

		// Override to provide custom health information
		protected override Task<ConnectorResult<ConnectorHealth>> GetConnectorHealthAsync(CancellationToken cancellationToken)
		{
			var health = new ConnectorHealth
			{
				State = State,
				IsHealthy = State == ConnectorState.Ready,
				LastHealthCheck = DateTime.UtcNow,
				Uptime = TimeSpan.FromHours(1), // Simulate 1 hour uptime
			};

			// Add custom metrics
			health.Metrics["connections"] = 5;
			health.Metrics["memory_usage"] = "120MB";
			health.Metrics["cpu_usage"] = "15%";

			return Task.FromResult(ConnectorResult<ConnectorHealth>.Success(health));
		}
	}

	// Example Message Implementation
	private class ExampleMessage : IMessage
	{
		public ExampleMessage(string recipient, string content)
		{
			Id = Guid.NewGuid().ToString();
			Recipient = recipient;
			Content = new ExampleContent(content);
		}

		public string Id { get; }
		public string Recipient { get; }
		public IEndpoint? Sender { get; }
		public IEndpoint? Receiver { get; }
		public IMessageContent? Content { get; }
		public IDictionary<string, IMessageProperty>? Properties { get; }
	}

	// Example Content Implementation
	private class ExampleContent : IMessageContent
	{
		public ExampleContent(string text)
		{
			Text = text;
		}

		public string Text { get; }
		public MessageContentType ContentType => MessageContentType.PlainText;
	}
}