//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="ChannelConnectorBase"/> abstract class to verify
/// its state management, capability validation, and default implementations.
/// </summary>
public class ChannelConnectorBaseTests
{
	[Fact]
	public void Constructor_WithValidSchema_SetsSchemaCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");

		// Act
		var connector = new TestConnector(schema);

		// Assert
		Assert.Same(schema, connector.Schema);
		Assert.Equal(ConnectorState.Uninitialized, connector.State);
	}

	[Fact]
	public void Constructor_WithNullSchema_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new TestConnector(null!));
	}

	[Fact]
	public async Task InitializeAsync_WhenUninitialized_TransitionsToInitializingThenReady()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);

		// Act
		var result = await connector.InitializeAsync(CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.Equal(ConnectorState.Ready, connector.State);
	}

	[Fact]
	public async Task InitializeAsync_WhenAlreadyInitialized_ReturnsFailure()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act
		var result = await connector.InitializeAsync(CancellationToken.None);

		// Assert
		Assert.False(result.Successful);
		Assert.Equal("ALREADY_INITIALIZED", result.Error?.ErrorCode);
	}

	[Fact]
	public async Task InitializeAsync_WhenInitializationFails_TransitionsToErrorState()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema) { ShouldFailInitialization = true };

		// Act
		var result = await connector.InitializeAsync(CancellationToken.None);

		// Assert
		Assert.False(result.Successful);
		Assert.Equal(ConnectorState.Error, connector.State);
	}

	[Fact]
	public async Task InitializeAsync_WhenInitializationThrows_TransitionsToErrorState()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema) { ShouldThrowOnInitialization = true };

		// Act
		var result = await connector.InitializeAsync(CancellationToken.None);

		// Assert
		Assert.False(result.Successful);
		Assert.Equal(ConnectorState.Error, connector.State);
		Assert.Equal("INITIALIZATION_ERROR", result.Error?.ErrorCode);
	}

	[Fact]
	public async Task TestConnectionAsync_WhenNotOperational_ThrowsInvalidOperationException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => 
			connector.TestConnectionAsync(CancellationToken.None));
	}

	[Fact]
	public async Task TestConnectionAsync_WhenOperational_ReturnsResult()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act
		var result = await connector.TestConnectionAsync(CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.True(result.Value);
	}

	[Fact]
	public async Task SendMessageAsync_WithoutSendCapability_ThrowsNotSupportedException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.WithCapabilities(ChannelCapability.ReceiveMessages); // No send capability
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		var message = new MockMessage();

		// Act & Assert
		await Assert.ThrowsAsync<NotSupportedException>(() => 
			connector.SendMessageAsync(message, CancellationToken.None));
	}

	[Fact]
	public async Task SendMessageAsync_WithNullMessage_ThrowsArgumentNullException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(() => 
			connector.SendMessageAsync(null!, CancellationToken.None));
	}

	[Fact]
	public async Task SendMessageAsync_WhenSupported_ReturnsResult()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		var message = new MockMessage();

		// Act
		var result = await connector.SendMessageAsync(message, CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.NotNull(result.Value);
		Assert.Equal(message.Id, result.Value.MessageId);
	}

	[Fact]
	public async Task SendBatchAsync_WithoutBulkCapability_ThrowsNotSupportedException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		var batch = new MockMessageBatch();

		// Act & Assert
		await Assert.ThrowsAsync<NotSupportedException>(() => 
			connector.SendBatchAsync(batch, CancellationToken.None));
	}

	[Fact]
	public async Task SendBatchAsync_WithCapability_CallsImplementation()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.WithCapability(ChannelCapability.BulkMessaging);
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		var batch = new MockMessageBatch();

		// Act
		var result = await connector.SendBatchAsync(batch, CancellationToken.None);

		// Assert
		Assert.False(result.Successful);
		Assert.Contains("Batch sending is not supported", result.Error!.ErrorMessage!);
	}

	[Fact]
	public async Task GetMessageStatusAsync_WithoutCapability_ThrowsNotSupportedException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act & Assert
		await Assert.ThrowsAsync<NotSupportedException>(() => 
			connector.GetMessageStatusAsync("test-message", CancellationToken.None));
	}

	[Fact]
	public async Task GetMessageStatusAsync_WithNullMessageId_ThrowsArgumentNullException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.WithCapability(ChannelCapability.MessageStatusQuery);
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(() => 
			connector.GetMessageStatusAsync(null!, CancellationToken.None));
	}

	[Fact]
	public async Task ValidateMessageAsync_WithMessage_ReturnsValidationResults()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AddContentType(MessageContentType.PlainText);
		var connector = new TestConnector(schema);
		var message = new MockMessage();

		// Act
		var results = new List<ValidationResult>();
		await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
		{
			results.Add(result);
		}

		// Assert
		Assert.Single(results);
		Assert.Equal(ValidationResult.Success, results[0]);
	}

	[Fact]
	public async Task ReceiveStatusAsync_WithoutCapability_ThrowsNotSupportedException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act & Assert - Create source inside the lambda to avoid ref struct issues
		await Assert.ThrowsAsync<NotSupportedException>(() => 
			connector.ReceiveMessageStatusAsync(MessageSource.Text("test content"), CancellationToken.None));
	}

	[Fact]
	public async Task ReceiveMessagesAsync_WithoutCapability_ThrowsNotSupportedException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act & Assert - Create source inside the lambda to avoid ref struct issues
		await Assert.ThrowsAsync<NotSupportedException>(() => 
			connector.ReceiveMessagesAsync(MessageSource.Text("test content"), CancellationToken.None));
	}

	[Fact]
	public async Task GetHealthAsync_WithoutCapability_ThrowsNotSupportedException()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);

		// Act & Assert
		await Assert.ThrowsAsync<NotSupportedException>(() => 
			connector.GetHealthAsync(CancellationToken.None));
	}

	[Fact]
	public async Task GetHealthAsync_WithCapability_ReturnsHealthInfo()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.WithCapability(ChannelCapability.HealthCheck);
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act
		var result = await connector.GetHealthAsync(CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.NotNull(result.Value);
		Assert.Equal(ConnectorState.Ready, result.Value.State);
		Assert.True(result.Value.IsHealthy);
	}

	[Fact]
	public async Task GetHealthAsync_WhenNotReady_ReturnsUnhealthyStatus()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.WithCapability(ChannelCapability.HealthCheck);
		var connector = new TestConnector(schema) { ShouldFailInitialization = true };
		await connector.InitializeAsync(CancellationToken.None); // This will put it in Error state

		// Act
		var result = await connector.GetHealthAsync(CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.NotNull(result.Value);
		Assert.Equal(ConnectorState.Error, result.Value.State);
		Assert.False(result.Value.IsHealthy);
		Assert.Contains("Connector is in Error state", result.Value.Issues);
	}

	[Fact]
	public async Task ShutdownAsync_TransitionsToShutdownState()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);

		// Act
		await connector.ShutdownAsync(CancellationToken.None);

		// Assert
		Assert.Equal(ConnectorState.Shutdown, connector.State);
	}

	[Fact]
	public async Task ShutdownAsync_WhenAlreadyShutdown_DoesNothing()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		await connector.ShutdownAsync(CancellationToken.None);

		// Act
		await connector.ShutdownAsync(CancellationToken.None);

		// Assert
		Assert.Equal(ConnectorState.Shutdown, connector.State);
	}

	[Theory]
	[InlineData(ConnectorState.Uninitialized)]
	[InlineData(ConnectorState.Initializing)]
	[InlineData(ConnectorState.ShuttingDown)]
	[InlineData(ConnectorState.Shutdown)]
	public async Task OperationalMethods_WithNonOperationalState_ThrowInvalidOperationException(ConnectorState state)
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		connector.SetStatePublic(state);
		var message = new MockMessage();

		// Act & Assert
		if (state != ConnectorState.Shutdown && state != ConnectorState.ShuttingDown)
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => 
				connector.TestConnectionAsync(CancellationToken.None));
		}

		if (state != ConnectorState.Shutdown && state != ConnectorState.ShuttingDown)
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => 
				connector.SendMessageAsync(message, CancellationToken.None));
		}
	}

	[Fact]
	public async Task GetStatusAsync_ReturnsStatus()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);

		// Act
		var result = await connector.GetStatusAsync(CancellationToken.None);

		// Assert
		Assert.True(result.Successful);
		Assert.Equal("Test Status", result.Value.Status);
	}

	[Fact]
	public async Task SendMessageAsync_WithValidationErrors_ReturnsValidationFailure()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AddContentType(MessageContentType.Html); // Only supports HTML, not PlainText
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		var message = new MockMessage(); // This has PlainText content

		// Act
		var result = await connector.SendMessageAsync(message, CancellationToken.None);

		// Assert
		Assert.False(result.Successful);
		Assert.Equal("MESSAGE_VALIDATION_FAILED", result.Error?.ErrorCode);
		Assert.Contains("validation", result.Error?.ErrorMessage?.ToLowerInvariant());
	}

	[Fact]
	public async Task SendBatchAsync_WithValidationErrors_ReturnsValidationFailure()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.WithCapability(ChannelCapability.BulkMessaging)
			.AddContentType(MessageContentType.Html); // Only supports HTML
		var connector = new TestConnector(schema);
		await connector.InitializeAsync(CancellationToken.None);
		
		var batch = new MockMessageBatch(); // Contains messages with PlainText content

		// Act
		var result = await connector.SendBatchAsync(batch, CancellationToken.None);

		// Assert
		Assert.False(result.Successful);
		Assert.Equal("BATCH_VALIDATION_FAILED", result.Error?.ErrorCode);
		Assert.Contains("validation failed", result.Error?.ErrorMessage?.ToLowerInvariant());
	}

	[Fact]
	public async Task ValidateMessageAsync_WithInvalidContentType_ReturnsValidationError()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AddContentType(MessageContentType.Html); // Only supports HTML
		var connector = new TestConnector(schema);
		var message = new MockMessage(); // Has PlainText content

		// Act
		var results = new List<ValidationResult>();
		await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
		{
			results.Add(result);
		}

		// Assert
		Assert.NotEmpty(results);
		var errorResult = results.FirstOrDefault(r => r != ValidationResult.Success);
		Assert.NotNull(errorResult);
		Assert.Contains("not supported", errorResult.ErrorMessage);
		Assert.Contains("PlainText", errorResult.ErrorMessage);
	}

	[Fact]
	public async Task ValidateMessageAsync_WithMissingMessageId_ReturnsValidationError()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		var message = new MockMessage { Id = "" }; // Empty ID

		// Act
		var results = new List<ValidationResult>();
		await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
		{
			results.Add(result);
		}

		// Assert
		Assert.NotEmpty(results);
		var errorResult = results.FirstOrDefault(r => r != ValidationResult.Success);
		Assert.NotNull(errorResult);
		Assert.Contains("Message ID is required", errorResult.ErrorMessage);
		Assert.Contains("Id", errorResult.MemberNames);
	}

	[Fact]
	public async Task ValidateMessageAsync_WithValidMessage_ReturnsSuccess()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AddContentType(MessageContentType.PlainText); // Supports PlainText
		var connector = new TestConnector(schema);
		var message = new MockMessage(); // Has PlainText content and valid ID

		// Act
		var results = new List<ValidationResult>();
		await foreach (var result in connector.ValidateMessageAsync(message, CancellationToken.None))
		{
			results.Add(result);
		}

		// Assert
		Assert.Single(results);
		Assert.Equal(ValidationResult.Success, results[0]);
	}

	[Fact]
	public void GetEndpointType_ReturnsEndpointTypeProperty()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0");
		var connector = new TestConnector(schema);
		var mockEndpoint = new MockEndpoint("email", "test@example.com");

		// Act
		var endpointType = connector.GetEndpointTypePublic(mockEndpoint);

		// Assert
		Assert.Equal("email", endpointType);
	}

	[Fact]
	public void IsEndpointTypeSupported_WithMatchingEndpointType_ReturnsTrue()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: false);
		var connector = new TestConnector(schema);

		// Act
		var isSupported = connector.IsEndpointTypeSupportedPublic(EndpointType.EmailAddress, asSender: true);

		// Assert
		Assert.True(isSupported);
	}

	[Fact]
	public void IsEndpointTypeSupported_WithNonMatchingEndpointType_ReturnsFalse()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: false);
		var connector = new TestConnector(schema);

		// Act
		var isSupported = connector.IsEndpointTypeSupportedPublic(EndpointType.PhoneNumber, asSender: true);

		// Assert
		Assert.False(isSupported);
	}

	[Fact]
	public async Task ValidateMessageAsync_WithEndpointTypeValidation_WorksCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("TestProvider", "Email", "1.0.0")
			.AddContentType(MessageContentType.PlainText)
			.AllowsMessageEndpoint(EndpointType.EmailAddress, asSender: true, asReceiver: false);
		
		var connector = new TestConnector(schema);
		var validMessage = new MockMessage
		{
			Sender = new MockEndpoint("email", "sender@test.com"),
			Receiver = new MockEndpoint("email", "receiver@test.com")  // This should fail validation
		};

		// Act
		var results = new List<ValidationResult>();
		await foreach (var result in connector.ValidateMessageAsync(validMessage, CancellationToken.None))
		{
			results.Add(result);
		}

		// Assert - Should have one error for receiver not being supported
		var errorResults = results.Where(r => r != ValidationResult.Success).ToList();
		Assert.Single(errorResults);
		var errorResult = errorResults.First();
		Assert.Contains("Receiver endpoint type", errorResult.ErrorMessage);
		Assert.Contains("not supported", errorResult.ErrorMessage);
	}

	// Test connector implementation for testing
	private class TestConnector : ChannelConnectorBase
	{
		public bool ShouldFailInitialization { get; set; }
		public bool ShouldThrowOnInitialization { get; set; }

		public TestConnector(IChannelSchema schema) : base(schema)
		{
		}

		public void SetStatePublic(ConnectorState state) => SetState(state);

		// Expose protected methods for testing
		public string? GetEndpointTypePublic(IEndpoint endpoint) => GetEndpointType(endpoint);

		public bool IsEndpointTypeSupportedPublic(EndpointType endpointType, bool asSender = false, bool asReceiver = false) =>
			IsEndpointTypeSupported(endpointType, asSender, asReceiver);

		protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
		{
			if (ShouldThrowOnInitialization)
				throw new InvalidOperationException("Test initialization failure");

			if (ShouldFailInitialization)
				return Task.FromResult(ConnectorResult<bool>.Fail("INIT_FAILED", "Initialization failed"));

			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(ConnectorResult<bool>.Success(true));
		}

		protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
		{
			var result = new SendResult(message.Id, $"remote-{message.Id}");
			return Task.FromResult(ConnectorResult<SendResult>.Success(result));
		}

		protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
		{
			var status = new StatusInfo("Test Status");
			return Task.FromResult(ConnectorResult<StatusInfo>.Success(status));
		}
	}

	// Mock message implementation
	private class MockMessage : IMessage
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public IEndpoint? Sender { get; set; }
		public IEndpoint? Receiver { get; set; }
		public IMessageContent? Content { get; set; } = new MockMessageContent();
		public IDictionary<string, IMessageProperty>? Properties { get; set; }
	}

	// Mock message content implementation
	private class MockMessageContent : IMessageContent
	{
		public MessageContentType ContentType { get; } = MessageContentType.PlainText;
	}

	// Mock message batch implementation
	private class MockMessageBatch : IMessageBatch
	{
		public string Id { get; } = Guid.NewGuid().ToString();
		public IDictionary<string, object>? Properties { get; }
		public IEnumerable<IMessage> Messages { get; } = new[] { new MockMessage() };
	}

	// Mock endpoint implementation
	private class MockEndpoint : IEndpoint
	{
		public MockEndpoint(string typeString, string address)
		{
			// Convert string type to EndpointType enum for compatibility
			Type = typeString.ToLowerInvariant() switch
			{
				"email" => EndpointType.EmailAddress,
				"phone" => EndpointType.PhoneNumber,
				"url" => EndpointType.Url,
				"user-id" => EndpointType.UserId,
				"app-id" => EndpointType.ApplicationId,
				"endpoint-id" => EndpointType.Id,
				"device-id" => EndpointType.DeviceId,
				"label" => EndpointType.Label,
				"topic" => EndpointType.Topic,
				"sms" => EndpointType.PhoneNumber, // Map sms to phone for testing
				"webhook" => EndpointType.Url, // Map webhook to URL for testing
				_ => EndpointType.Id // Default fallback
			};
			Address = address;
		}

		public EndpointType Type { get; }
		public string Address { get; }
	}
}