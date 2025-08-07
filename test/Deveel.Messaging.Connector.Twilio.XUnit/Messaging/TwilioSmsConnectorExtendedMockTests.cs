using Microsoft.Extensions.Logging;
using Moq;
using Twilio.Rest.Api.V2010.Account;

namespace Deveel.Messaging;

/// <summary>
/// Extended tests for the <see cref="TwilioSmsConnector"/> class demonstrating 
/// various scenarios using the TwilioMockFactory for comprehensive testing.
/// </summary>
public class TwilioSmsConnectorExtendedMockTests
{
    [Fact]
    public async Task SendMessageAsync_UsingMockFactory_SendsSuccessfully()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending();
        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal("SM123456789", result.Value?.RemoteMessageId);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithDeliveredStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForSending("SM987654321", MessageResource.StatusEnum.Delivered);
        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(MessageStatus.Delivered, result.Value?.Status);
        Assert.Equal("SM987654321", result.Value?.RemoteMessageId);
    }

    [Fact]
    public async Task GetMessageStatusAsync_UsingMockFactory_ReturnsStatus()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForStatusQuery("SM555666777", MessageResource.StatusEnum.Sent);
        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.GetMessageStatusAsync("SM555666777", CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(MessageStatus.Sent, result.Value?.Updates.First().Status);
        mockTwilioService.Verify(x => x.FetchMessageAsync("SM555666777", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_UsingMockFactory_ReturnsSuccess()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioServiceForConnectionTest("AC9999888877776666555544443333222211", "Production Account");
        var connectionSettings = new ConnectionSettings()
            .SetParameter("AccountSid", "AC9999888877776666555544443333222211")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");

        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
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
    public async Task FullWorkflow_UsingFullyConfiguredMock_WorksEndToEnd()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateFullyConfiguredMockTwilioService();
        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        var message = CreateTestMessage();

        // Act & Assert - Initialize
        var initResult = await connector.InitializeAsync(CancellationToken.None);
        Assert.True(initResult.Successful);

        // Act & Assert - Test connection
        var connectionResult = await connector.TestConnectionAsync(CancellationToken.None);
        Assert.True(connectionResult.Successful);

        // Act & Assert - Send message
        var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
        Assert.True(sendResult.Successful);

        // Act & Assert - Get status
        var statusResult = await connector.GetMessageStatusAsync(sendResult.Value!.RemoteMessageId, CancellationToken.None);
        Assert.True(statusResult.Successful);

        // Verify all expected calls were made
        mockTwilioService.Verify(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockTwilioService.Verify(x => x.FetchAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        mockTwilioService.Verify(x => x.FetchMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithFailedMessage_ReturnsFailedStatus()
    {
        // Arrange
        var mockTwilioService = new Mock<ITwilioService>();
        mockTwilioService.Setup(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()));
        
        var failedMessage = TwilioMockFactory.CreateMockFailedMessageResource("SM111222333", 30008, "Unknown destination handset");
        mockTwilioService.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedMessage);

        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful); // Message was sent, but has failed status
        Assert.Equal(MessageStatus.DeliveryFailed, result.Value?.Status);
        
        // Check if the error information is available in additional data
        if (result.Value?.AdditionalData?.ContainsKey("ErrorCode") == true)
        {
            Assert.Equal("30008", result.Value.AdditionalData["ErrorCode"]);
        }
        
        if (result.Value?.AdditionalData?.ContainsKey("ErrorMessage") == true)
        {
            Assert.Equal("Unknown destination handset", result.Value.AdditionalData["ErrorMessage"]);
        }
    }

    [Fact]
    public async Task MultipleOperations_WithDifferentMockSetups_WorkCorrectly()
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

        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result1 = await connector.SendMessageAsync(CreateTestMessage("msg1"), CancellationToken.None);
        var result2 = await connector.SendMessageAsync(CreateTestMessage("msg2"), CancellationToken.None);

        // Assert
        Assert.True(result1.Successful);
        Assert.True(result2.Successful);
        Assert.Equal("SM111", result1.Value?.RemoteMessageId);
        Assert.Equal("SM222", result2.Value?.RemoteMessageId);
        Assert.Equal(MessageStatus.Queued, result1.Value?.Status);
        Assert.Equal(MessageStatus.Sent, result2.Value?.Status);

        mockTwilioService.Verify(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ErrorScenarios_UsingMockFactory_HandleGracefully()
    {
        // Arrange
        var mockTwilioService = TwilioMockFactory.CreateMockTwilioService();
        TwilioMockFactory.ConfigureForException(mockTwilioService, new InvalidOperationException("Network error"));

        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act & Assert - Send should fail
        var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
        Assert.False(sendResult.Successful);
        Assert.Contains("Network error", sendResult.Error?.ErrorMessage);

        // Act & Assert - Status query should fail
        var statusResult = await connector.GetMessageStatusAsync("SM123", CancellationToken.None);
        Assert.False(statusResult.Successful);
        Assert.Contains("Network error", statusResult.Error?.ErrorMessage);

        // Act & Assert - Connection test should fail
        var connectionResult = await connector.TestConnectionAsync(CancellationToken.None);
        Assert.False(connectionResult.Successful);
        Assert.Contains("Network error", connectionResult.Error?.ErrorMessage);
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
        var connector = new TwilioSmsConnector(
            TwilioChannelSchemas.SimpleSms,
            CreateValidConnectionSettings(),
            mockTwilioService.Object);

        await connector.InitializeAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(expectedStatus, result.Value?.Status);
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
    }

    private static Message CreateTestMessage(string? id = null)
    {
        return new Message
        {
            Id = id ?? "test-message-id",
            Sender = new Endpoint(EndpointType.PhoneNumber, "+1234567890"),
            Receiver = new Endpoint(EndpointType.PhoneNumber, "+1987654321"),
            Content = new TextContent("Hello World")
        };
    }
}