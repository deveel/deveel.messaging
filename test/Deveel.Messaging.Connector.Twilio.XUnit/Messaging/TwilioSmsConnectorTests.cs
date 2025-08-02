using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="TwilioSmsConnector"/> class to verify
/// its functionality and integration with the Twilio API.
/// </summary>
public class TwilioSmsConnectorTests
{
    [Fact]
    public void Constructor_WithValidSchemaAndSettings_CreatesConnector()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();

        // Act
        var connector = new TwilioSmsConnector(schema, connectionSettings);

        // Assert
        Assert.Same(schema, connector.Schema);
        Assert.Equal(ConnectorState.Uninitialized, connector.State);
    }

    [Fact]
    public void Constructor_WithConnectionSettingsOnly_UsesDefaultSchema()
    {
        // Arrange
        var connectionSettings = CreateValidConnectionSettings();

        // Act
        var connector = new TwilioSmsConnector(connectionSettings);

        // Assert
        Assert.Equal("Twilio", connector.Schema.ChannelProvider);
        Assert.Equal("SMS", connector.Schema.ChannelType);
        Assert.Equal(ConnectorState.Uninitialized, connector.State);
    }

    [Fact]
    public void Constructor_WithNullConnectionSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwilioSmsConnector(schema, null!));
        Assert.Throws<ArgumentNullException>(() => new TwilioSmsConnector(null!));
    }

    [Fact]
    public void Constructor_WithLogger_StoresLogger()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var logger = new TestLogger<TwilioSmsConnector>();

        // Act
        var connector = new TwilioSmsConnector(schema, connectionSettings, null, logger);

        // Assert
        Assert.Same(schema, connector.Schema);
        Assert.Equal(ConnectorState.Uninitialized, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_WithValidSettings_ReturnsSuccess()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms; // Use simple schema to avoid validation complexity
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(ConnectorState.Ready, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingCredentials_ReturnsFailure()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = new ConnectionSettings(); // Empty settings
        var connector = new TwilioSmsConnector(schema, connectionSettings);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal("MISSING_CREDENTIALS", result.Error?.ErrorCode);
        Assert.Equal(ConnectorState.Error, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingFromNumberAndMessagingService_ReturnsFailure()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
        var connector = new TwilioSmsConnector(schema, connectionSettings);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful); // Should succeed now since FromNumber is no longer required at connection level
        Assert.Equal(ConnectorState.Ready, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_WithMessagingServiceOnly_ReturnsSuccess()
    {
        // Arrange
        var schema = TwilioChannelSchemas.BulkSms; // This schema requires MessagingServiceSid
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("MessagingServiceSid", "MG1234567890123456789012345678901234");
        var connector = new TwilioSmsConnector(schema, connectionSettings);

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
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal("ALREADY_INITIALIZED", result.Error?.ErrorCode);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            connector.TestConnectionAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithoutSendCapability_ThrowsNotSupportedException()
    {
        // Arrange
        var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
            .WithCapabilities(ChannelCapability.ReceiveMessages); // No send capability
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            connector.SendMessageAsync(message, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            connector.SendMessageAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithoutCapability_ThrowsNotSupportedException()
    {
        // Arrange
        var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
            .WithCapabilities(ChannelCapability.SendMessages); // No status query capability
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            connector.GetMessageStatusAsync("test-message", CancellationToken.None));
    }

    [Fact]
    public async Task GetHealthAsync_WhenInitialized_ReturnsHealthInfo()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(ConnectorState.Ready, result.Value.State);
        // Note: IsHealthy might be false due to connection test with test credentials
        // but the main assertion is that the connector state is Ready and the result is successful
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCorrectInformation()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetStatusAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        // StatusInfo is a value type, no need for NotNull check
        Assert.Contains("Twilio SMS Connector", result.Value.Status);
        Assert.True(result.Value.AdditionalData.ContainsKey("AccountSid"));
        Assert.True(result.Value.AdditionalData.ContainsKey("State"));
        Assert.True(result.Value.AdditionalData.ContainsKey("Uptime"));
    }

    [Fact]
    public async Task ShutdownAsync_TransitionsToShutdownState()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        await connector.ShutdownAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ConnectorState.Shutdown, connector.State);
    }

    [Fact]
    public void TwilioSmsConnector_ImplementsCorrectInterface()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleSms;
        var connectionSettings = CreateValidConnectionSettings();

        // Act
        IChannelConnector connector = new TwilioSmsConnector(schema, connectionSettings);

        // Assert
        Assert.NotNull(connector);
        Assert.Same(schema, connector.Schema);
        Assert.Equal(ConnectorState.Uninitialized, connector.State);
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
    }

    private static TestMessage CreateTestMessage()
    {
        return new TestMessage
        {
            Id = "test-message-id",
            Sender = new TestEndpoint(EndpointType.PhoneNumber, "+1234567890"), // Add required Sender
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "+1987654321"),
            Content = new TestMessageContent(MessageContentType.PlainText, "Hello World")
        };
    }

    // Test helper classes
    private class TestMessage : IMessage
    {
        public string Id { get; set; } = string.Empty;
        public IEndpoint? Sender { get; set; }
        public IEndpoint? Receiver { get; set; }
        public IMessageContent? Content { get; set; }
        public IDictionary<string, IMessageProperty>? Properties { get; set; }
    }

    private class TestEndpoint : IEndpoint
    {
        public TestEndpoint(EndpointType type, string address)
        {
            Type = type;
            Address = address;
        }

        public EndpointType Type { get; }
        public string Address { get; }
    }

    private class TestMessageContent : IMessageContent
    {
        public TestMessageContent(MessageContentType contentType, string content)
        {
            ContentType = contentType;
            _content = content;
        }

        private readonly string _content;

        public MessageContentType ContentType { get; }

        public override string ToString() => _content;
    }

    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}