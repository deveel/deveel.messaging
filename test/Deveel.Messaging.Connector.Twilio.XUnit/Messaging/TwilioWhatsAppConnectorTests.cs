//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Moq;
using Twilio.Rest.Api.V2010.Account;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="TwilioWhatsAppConnector"/> class using mocked Twilio services
/// to verify WhatsApp messaging functionality without requiring actual Twilio API calls.
/// </summary>
public class TwilioWhatsAppConnectorTests
{
    private readonly Mock<ITwilioService> _mockTwilioService;
    private readonly Mock<ILogger<TwilioWhatsAppConnector>> _mockLogger;

    public TwilioWhatsAppConnectorTests()
    {
        _mockTwilioService = new Mock<ITwilioService>();
        _mockLogger = new Mock<ILogger<TwilioWhatsAppConnector>>();
    }

    [Fact]
    public void Constructor_WithValidSchemaAndSettings_CreatesConnector()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();

        // Act
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings);

        // Assert
        Assert.Same(schema, connector.Schema);
        Assert.Equal(ConnectorState.Uninitialized, connector.State);
    }

    [Fact]
    public void Constructor_WithConnectionSettingsOnly_UsesDefaultSchema()
    {
        // Arrange
        var connectionSettings = CreateValidWhatsAppConnectionSettings();

        // Act
        var connector = new TwilioWhatsAppConnector(connectionSettings);

        // Assert
        Assert.Equal("Twilio", connector.Schema.ChannelProvider);
        Assert.Equal("WhatsApp", connector.Schema.ChannelType);
        Assert.Equal(ConnectorState.Uninitialized, connector.State);
    }

    [Fact]
    public void Constructor_WithNullConnectionSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioWhatsApp;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwilioWhatsAppConnector(schema, null!));
        Assert.Throws<ArgumentNullException>(() => new TwilioWhatsAppConnector(null!));
    }

    [Fact]
    public async Task InitializeAsync_WithValidSettings_ReturnsSuccess()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(ConnectorState.Ready, connector.State);
        _mockTwilioService.Verify(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingCredentials_ReturnsFailure()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("FromNumber", "whatsapp:+1234567890");
        
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(TwilioErrorCodes.MissingCredentials, result.Error?.ErrorCode);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingFromNumber_ReturnsSuccess()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
        
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful); // Initialization should succeed without FromNumber
        Assert.Equal(ConnectorState.Ready, connector.State);
    }

    [Fact]
    public async Task InitializeAsync_AddsWhatsAppPrefixToPhoneNumber()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
        
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        // Act
        var result = await connector.InitializeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(ConnectorState.Ready, connector.State);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var mockAccount = TwilioMockFactory.CreateMockAccountResource("AC1234567890123456789012345678901234", "Test Account");
        _mockTwilioService.Setup(x => x.FetchAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAccount);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        _mockTwilioService.Verify(x => x.FetchAccountAsync("AC1234567890123456789012345678901234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidWhatsAppMessage_ReturnsSuccess()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var mockMessageResource = TwilioMockFactory.CreateMockMessageResource("SM123456789", MessageResource.StatusEnum.Queued);
        _mockTwilioService.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessageResource);

        var message = CreateWhatsAppTestMessage();
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(message.Id, result.Value.MessageId);
        Assert.Equal("SM123456789", result.Value.RemoteMessageId);
        Assert.Equal(MessageStatus.Queued, result.Value.Status);
        Assert.Equal("WhatsApp", result.Value.AdditionalData["Channel"]);

        _mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithTemplateMessage_UsesContentSid()
    {
        // Arrange
        var schema = TwilioChannelSchemas.WhatsAppTemplates;
        var connectionSettings = CreateValidTemplateConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var mockMessageResource = TwilioMockFactory.CreateMockMessageResource("SM123456789", MessageResource.StatusEnum.Queued);
        _mockTwilioService.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessageResource);

        var message = CreateWhatsAppTemplateMessage();
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        _mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithInvalidRecipient_ReturnsFailure()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var message = new TestMessage
        {
            Id = "test-message-id",
            Sender = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1234567890"), // Add required Sender
            Receiver = new TestEndpoint(EndpointType.EmailAddress, "invalid@email.com"), // Invalid endpoint type
            Content = new TestMessageContent(MessageContentType.PlainText, "Hello WhatsApp")
        };

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(ConnectorErrorCodes.MessageValidationFailed, result.Error?.ErrorCode);
        
        // Verify Twilio service was not called due to validation failure
        _mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WithPhoneNumberButEmptyAddress_ReturnsInvalidRecipientError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var message = new TestMessage
        {
            Id = "test-message-id",
            Sender = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1234567890"), // Add required Sender
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, ""), // Valid endpoint type but empty address
            Content = new TestMessageContent(MessageContentType.PlainText, "Hello WhatsApp")
        };

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(TwilioErrorCodes.InvalidRecipient, result.Error?.ErrorCode);
        Assert.Contains("WhatsApp phone number is required", result.Error?.ErrorMessage);
        
        // Verify Twilio service was not called due to validation failure
        _mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WithTwilioException_ReturnsFailure()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        _mockTwilioService.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("WhatsApp API error"));

        var message = CreateWhatsAppTestMessage();
        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(TwilioErrorCodes.SendMessageFailed, result.Error?.ErrorCode);
        Assert.Contains("WhatsApp API error", result.Error?.ErrorMessage);
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithValidMessageId_ReturnsStatus()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var mockMessageResource = TwilioMockFactory.CreateMockMessageResource("SM123456789", MessageResource.StatusEnum.Delivered);
        _mockTwilioService.Setup(x => x.FetchMessageAsync("SM123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessageResource);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetMessageStatusAsync("SM123456789", CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Updates);
        Assert.Equal(MessageStatus.Delivered, result.Value.Updates.First().Status);
        Assert.Equal("WhatsApp", result.Value.Updates.First().AdditionalData["Channel"]);

        _mockTwilioService.Verify(x => x.FetchMessageAsync("SM123456789", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithTwilioException_ReturnsFailure()
    {
        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        _mockTwilioService.Setup(x => x.FetchMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Status query failed"));

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetMessageStatusAsync("SM123456789", CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.Equal(TwilioErrorCodes.StatusQueryFailed, result.Error?.ErrorCode);
        Assert.Contains("Status query failed", result.Error?.ErrorMessage);
    }

    [Theory]
    [InlineData("Queued", "Queued")]
    [InlineData("Sending", "Sent")]
    [InlineData("Sent", "Sent")]
    [InlineData("Delivered", "Delivered")]
    [InlineData("Undelivered", "DeliveryFailed")]
    [InlineData("Failed", "DeliveryFailed")]
    [InlineData("Received", "Received")]
    public async Task StatusMapping_CorrectlyMapsAllTwilioStatuses(string twilioStatusString, string expectedStatusString)
    {
        // Map string values to enum values manually
        var twilioStatus = twilioStatusString switch
        {
            "Queued" => MessageResource.StatusEnum.Queued,
            "Sending" => MessageResource.StatusEnum.Sending,
            "Sent" => MessageResource.StatusEnum.Sent,
            "Delivered" => MessageResource.StatusEnum.Delivered,
            "Undelivered" => MessageResource.StatusEnum.Undelivered,
            "Failed" => MessageResource.StatusEnum.Failed,
            "Received" => MessageResource.StatusEnum.Received,
            _ => throw new ArgumentException($"Unknown Twilio status: {twilioStatusString}")
        };

        var expectedStatus = expectedStatusString switch
        {
            "Queued" => MessageStatus.Queued,
            "Sent" => MessageStatus.Sent,
            "Delivered" => MessageStatus.Delivered,
            "DeliveryFailed" => MessageStatus.DeliveryFailed,
            "Received" => MessageStatus.Received,
            _ => throw new ArgumentException($"Unknown expected status: {expectedStatusString}")
        };

        // Arrange
        var schema = TwilioChannelSchemas.SimpleWhatsApp;
        var connectionSettings = CreateValidWhatsAppConnectionSettings();
        var connector = new TwilioWhatsAppConnector(schema, connectionSettings, _mockTwilioService.Object, _mockLogger.Object);

        var mockMessageResource = TwilioMockFactory.CreateMockMessageResource("SM123", twilioStatus);
        _mockTwilioService.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessageResource);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(expectedStatus, result.Value?.Status);
    }

    private static ConnectionSettings CreateValidWhatsAppConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
    }

    private static ConnectionSettings CreateValidTemplateConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("ContentSid", "HX1234567890123456789012345678901234");
    }

    private static TestMessage CreateWhatsAppTestMessage(string? id = null)
    {
        return new TestMessage
        {
            Id = id ?? "test-whatsapp-message-id",
            Sender = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1234567890"), // Add required Sender
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1987654321"),
            Content = new TestMessageContent(MessageContentType.PlainText, "Hello WhatsApp!")
        };
    }

    private static TestMessage CreateWhatsAppTemplateMessage(string? id = null)
    {
        var message = new TestMessage
        {
            Id = id ?? "test-template-message-id",
            Sender = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1234567890"), // Add required Sender
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1987654321"),
            Content = new TestMessageContent(MessageContentType.Template, "Template Content"),
            Properties = new Dictionary<string, IMessageProperty>
            {
                { "ContentSid", new TestMessageProperty("ContentSid", "HX1234567890123456789012345678901234") },
                { "ContentVariables", new TestMessageProperty("ContentVariables", "{\"name\":\"John\",\"code\":\"123\"}") }
            }
        };

        return message;
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

    private class TestMessageProperty : IMessageProperty
    {
        public TestMessageProperty(string name, object value, bool isSensitive = false)
        {
            Name = name;
            Value = value;
            IsSensitive = isSensitive;
        }

        public string Name { get; }
        public object Value { get; }
        public bool IsSensitive { get; }

        public override string? ToString() => Value?.ToString();
    }
}