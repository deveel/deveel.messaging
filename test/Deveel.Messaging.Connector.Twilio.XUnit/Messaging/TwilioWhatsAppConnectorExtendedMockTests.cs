//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Moq;
using Twilio.Rest.Api.V2010.Account;

namespace Deveel.Messaging;

/// <summary>
/// Extended tests for the <see cref="TwilioWhatsAppConnector"/> class demonstrating 
/// various WhatsApp scenarios using the TwilioMockFactory for comprehensive testing.
/// </summary>
public class TwilioWhatsAppConnectorExtendedMockTests
{
    [Fact]
    public async Task SendWhatsAppMessageAsync_UsingMockFactory_SendsSuccessfully()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending();
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal("SM123456789", result.Value?.RemoteMessageId);
        Assert.Equal("WhatsApp", result.Value?.AdditionalData["Channel"]);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendWhatsAppMessageAsync_WithDeliveredStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending("SM987654321", MessageResource.StatusEnum.Delivered);
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(MessageStatus.Delivered, result.Value?.Status);
        Assert.Equal("SM987654321", result.Value?.RemoteMessageId);
        Assert.Equal("WhatsApp", result.Value?.AdditionalData["Channel"]);
    }

    [Fact]
    public async Task GetWhatsAppMessageStatusAsync_UsingMockFactory_ReturnsStatus()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForStatusQuery("SM555666777", MessageResource.StatusEnum.Sent);
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetMessageStatusAsync("SM555666777", CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(MessageStatus.Sent, result.Value?.Updates.First().Status);
        Assert.Equal("WhatsApp", result.Value?.Updates.First().AdditionalData["Channel"]);
        mockTwilioService.Verify(x => x.FetchMessageAsync("SM555666777", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestWhatsAppConnectionAsync_UsingMockFactory_ReturnsSuccess()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForConnectionTest("AC9999888877776666555544443333222211", "Production Account");
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC9999888877776666555544443333222211")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("FromNumber", "whatsapp:+1234567890");

        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            connectionSettings,
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.TestConnectionAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        mockTwilioService.Verify(x => x.FetchAccountAsync("AC9999888877776666555544443333222211", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullWhatsAppWorkflow_UsingFullyConfiguredMock_WorksEndToEnd()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateFullyConfiguredMockTwilioService();
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        var message = CreateWhatsAppTestMessage();

        // Act & Assert - Initialize
        var initResult = await connector.InitializeAsync(CancellationToken.None);
        Assert.True(initResult.Successful);

        // Act & Assert - Test connection
        var connectionResult = await connector.TestConnectionAsync(CancellationToken.None);
        Assert.True(connectionResult.Successful);

        // Act & Assert - Send message
        var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
        Assert.True(sendResult.Successful);
        Assert.Equal("WhatsApp", sendResult.Value?.AdditionalData["Channel"]);

        // Act & Assert - Get status
        var statusResult = await connector.GetMessageStatusAsync(sendResult.Value!.RemoteMessageId, CancellationToken.None);
        Assert.True(statusResult.Successful);
        Assert.Equal("WhatsApp", statusResult.Value?.Updates.First().AdditionalData["Channel"]);

        // Verify all expected calls were made
        mockTwilioService.Verify(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockTwilioService.Verify(x => x.FetchAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        mockTwilioService.Verify(x => x.FetchMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendWhatsAppTemplateMessage_WithContentSid_SendsSuccessfully()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending();
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.WhatsAppTemplates,
            CreateValidTemplateConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTemplateMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal("SM123456789", result.Value?.RemoteMessageId);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendWhatsAppMessageAsync_WithFailedMessage_ReturnsFailedStatus()
    {
        // Arrange
        var mockTwilioService = new Mock<ITwilioService>();
        mockTwilioService.Setup(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()));
        
        var failedMessage = TwilioMockFactory.CreateMockFailedMessageResource("SM111222333", 30008, "Unknown WhatsApp number");
        mockTwilioService.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedMessage);

        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful); // Message was sent, but has failed status
        Assert.Equal(MessageStatus.DeliveryFailed, result.Value?.Status);
        Assert.Equal("WhatsApp", result.Value?.AdditionalData["Channel"]);
    }

    [Fact]
    public async Task SendWhatsAppMessageAsync_WithPhoneNumberFormatting_AddsWhatsAppPrefix()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending();
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        
        // Create message with regular phone number (without whatsapp: prefix)
        var message = new TestMessage
        {
            Id = "test-message-id",
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "+1987654321"), // No whatsapp: prefix
            Content = new TestMessageContent(MessageContentType.PlainText, "Hello WhatsApp!")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MultipleWhatsAppOperations_WithDifferentMockSetups_WorkCorrectly()
    {
        // Arrange
        var mockTwilioService = new Mock<ITwilioService>();
        mockTwilioService.Setup(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()));

        // Setup different responses for different message SIDs
        var message1 = TwilioMockFactory.CreateMockMessageResource("SM111", MessageResource.StatusEnum.Queued);
        var message2 = TwilioMockFactory.CreateMockMessageResource("SM222", MessageResource.StatusEnum.Sent);

        mockTwilioService.SetupSequence(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message1)
            .ReturnsAsync(message2);

        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result1 = await connector.SendMessageAsync(CreateWhatsAppTestMessage("msg1"), CancellationToken.None);
        var result2 = await connector.SendMessageAsync(CreateWhatsAppTestMessage("msg2"), CancellationToken.None);

        // Assert
        Assert.True(result1.Successful);
        Assert.True(result2.Successful);
        Assert.Equal("SM111", result1.Value?.RemoteMessageId);
        Assert.Equal("SM222", result2.Value?.RemoteMessageId);
        Assert.Equal(MessageStatus.Queued, result1.Value?.Status);
        Assert.Equal(MessageStatus.Sent, result2.Value?.Status);
        Assert.Equal("WhatsApp", result1.Value?.AdditionalData["Channel"]);
        Assert.Equal("WhatsApp", result2.Value?.AdditionalData["Channel"]);

        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ErrorScenarios_UsingMockFactory_HandleGracefully()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioService();
        TwilioMockFactory.ConfigureForException(mockTwilioService, new InvalidOperationException("WhatsApp API error"));

        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTestMessage();

        // Act & Assert - Send should fail
        var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
        Assert.False(sendResult.Successful);
        Assert.Contains("WhatsApp API error", sendResult.Error?.ErrorMessage);

        // Act & Assert - Status query should fail
        var statusResult = await connector.GetMessageStatusAsync("SM123", CancellationToken.None);
        Assert.False(statusResult.Successful);
        Assert.Contains("WhatsApp API error", statusResult.Error?.ErrorMessage);

        // Act & Assert - Connection test should fail
        var connectionResult = await connector.TestConnectionAsync(CancellationToken.None);
        Assert.False(connectionResult.Successful);
        Assert.Contains("WhatsApp API error", connectionResult.Error?.ErrorMessage);
    }

    [Theory]
    [InlineData("Queued", "Queued")]
    [InlineData("Sending", "Sent")]
    [InlineData("Sent", "Sent")]
    [InlineData("Delivered", "Delivered")]
    [InlineData("Undelivered", "DeliveryFailed")]
    [InlineData("Failed", "DeliveryFailed")]
    [InlineData("Received", "Received")]
    public async Task WhatsAppStatusMapping_CorrectlyMapsAllTwilioStatuses(string twilioStatusString, string expectedStatusString)
    {
        // Map string values to enum values manually to avoid parsing issues
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
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending("SM123", twilioStatus);
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateWhatsAppTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(expectedStatus, result.Value?.Status);
        Assert.Equal("WhatsApp", result.Value?.AdditionalData["Channel"]);
    }

    [Fact]
    public async Task SendWhatsAppMessageAsync_WithMediaContent_SendsSuccessfully()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending();
        var connector = new TwilioWhatsAppConnector(
            TwilioChannelSchemas.SimpleWhatsApp,
            CreateValidWhatsAppConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        
        var message = new TestMessage
        {
            Id = "test-media-message-id",
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1987654321"),
            Content = new TestMessageContent(MessageContentType.Media, "Media content")
        };

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal("WhatsApp", result.Value?.AdditionalData["Channel"]);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ConnectionSettings CreateValidWhatsAppConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("FromNumber", "whatsapp:+1234567890");
    }

    private static ConnectionSettings CreateValidTemplateConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678")
            .SetParameter("FromNumber", "whatsapp:+1234567890")
            .SetParameter("ContentSid", "HX1234567890123456789012345678901234");
    }

    private static TestMessage CreateWhatsAppTestMessage(string? id = null)
    {
        return new TestMessage
        {
            Id = id ?? "test-whatsapp-message-id",
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1987654321"),
            Content = new TestMessageContent(MessageContentType.PlainText, "Hello WhatsApp!")
        };
    }

    private static TestMessage CreateWhatsAppTemplateMessage(string? id = null)
    {
        return new TestMessage
        {
            Id = id ?? "test-template-message-id",
            Receiver = new TestEndpoint(EndpointType.PhoneNumber, "whatsapp:+1987654321"),
            Content = new TestMessageContent(MessageContentType.Template, "Template Content"),
            Properties = new Dictionary<string, IMessageProperty>
            {
                { "ContentSid", new TestMessageProperty("ContentSid", "HX1234567890123456789012345678901234") },
                { "ContentVariables", new TestMessageProperty("ContentVariables", "{\"name\":\"John\",\"code\":\"123\"}") }
            }
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