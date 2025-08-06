using System.Text.Json;
using Xunit;

namespace Deveel.Messaging;

/// <summary>
/// Additional edge case tests for TwilioSmsConnector JSON message source handling,
/// covering various error scenarios, malformed data, and special cases.
/// </summary>
public class TwilioSmsConnectorJsonEdgeCaseTests
{
    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_EmptyJsonObject_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        var emptyJson = "{}";
        var source = MessageSource.Json(emptyJson);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_NullStringValues_HandlesGracefully()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // JSON with null string values (which JSON.NET might deserialize as null)
        var webhookJson = new
        {
            MessageSid = "SM1234567890",
            From = "+1234567890",
            To = "+1987654321",
            Body = (string?)null,
            MessageStatus = "received"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.Equal("", ((ITextContent)message.Content!).Text); // null body should become empty string
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_BatchWithSomeInvalidMessages_ReturnsValidOnes()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Batch with some invalid messages (missing required fields)
        var webhookJson = new
        {
            Messages = new object[]
            {
                new { MessageSid = "SM1111111111", From = "+1111111111", To = "+1987654321", Body = "Valid message 1" },
                new { MessageSid = "", From = "+2222222222", To = "+1987654321", Body = "Invalid - empty SID" },
                new { MessageSid = "SM3333333333", From = "", To = "+1987654321", Body = "Invalid - empty From" },
                new { MessageSid = "SM4444444444", From = "+4444444444", To = "+1987654321", Body = "Valid message 2" }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Messages.Count); // Only 2 valid messages
        
        var messages = result.Value.Messages.ToList();
        Assert.Equal("SM1111111111", messages[0].Id);
        Assert.Equal("SM4444444444", messages[1].Id);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_VeryLargeJson_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a JSON with many additional properties to test large payload handling
        var largePayload = new Dictionary<string, object>
        {
            ["MessageSid"] = "SM1234567890",
            ["From"] = "+1234567890",
            ["To"] = "+1987654321",
            ["Body"] = "Message with many extra fields",
            ["MessageStatus"] = "received"
        };

        // Add 100 additional properties
        for (int i = 0; i < 100; i++)
        {
            largePayload[$"ExtraField{i}"] = $"ExtraValue{i}";
        }

        var jsonPayload = JsonSerializer.Serialize(largePayload);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.Equal("Message with many extra fields", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_MissingMessageSid_UsesDefault()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Status callback missing MessageSid
        var statusJson = new
        {
            MessageStatus = "delivered",
            To = "+1987654321",
            From = "+1234567890"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("unknown", result.Value.MessageId); // Should default to "unknown"
        Assert.Equal(MessageStatus.Delivered, result.Value.Status);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_MissingMessageStatus_UsesDefault()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Status callback missing MessageStatus
        var statusJson = new
        {
            MessageSid = "SM1234567890",
            To = "+1987654321",
            From = "+1234567890"
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("SM1234567890", result.Value.MessageId);
        Assert.Equal(MessageStatus.Unknown, result.Value.Status); // Should default to Unknown
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_DeepNestedJson_ExtractsCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Complex nested JSON structure
        var nestedJson = new
        {
            webhook = new
            {
                data = new
                {
                    MessageSid = "SM1234567890",
                    From = "+1234567890",
                    To = "+1987654321",
                    Body = "Deeply nested message",
                    MessageStatus = "received",
                    metadata = new
                    {
                        timestamp = "2023-12-01T10:30:00Z",
                        source = "test"
                    }
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(nestedJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        // Should fail because the structure doesn't match expected Twilio format
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_CaseSensitiveFields_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Test case sensitivity - these should work (Twilio uses PascalCase)
        var jsonPayload = """
        {
            "MessageSid": "SM1234567890",
            "From": "+1234567890",
            "To": "+1987654321",
            "Body": "Case sensitive test",
            "MessageStatus": "received"
        }
        """;

        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_WrongCaseFields_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Test wrong case (camelCase instead of PascalCase)
        var jsonPayload = """
        {
            "messageSid": "SM1234567890",
            "from": "+1234567890",
            "to": "+1987654321",
            "body": "Wrong case test",
            "messageStatus": "received"
        }
        """;

        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_NumericFields_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Test with numeric fields that should be strings
        var jsonPayload = """
        {
            "MessageSid": "SM1234567890",
            "From": "+1234567890",
            "To": "+1987654321",
            "Body": "Numeric fields test",
            "MessageStatus": "received",
            "NumSegments": 1,
            "ErrorCode": 0,
            "Price": 0.0075
        }
        """;

        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM1234567890", message.Id);
        Assert.Equal("Numeric fields test", ((ITextContent)message.Content!).Text);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_SpecialCharactersInSid_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Test with SID containing special characters
        var webhookJson = new
        {
            MessageSid = "SM123-456_789.abc",
            From = "+1234567890",
            To = "+1987654321",
            Body = "Special SID test",
            MessageStatus = "received"
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Messages);
        
        var message = result.Value.Messages.First();
        Assert.Equal("SM123-456_789.abc", message.Id);
    }

    [Fact]
    public async Task ReceiveMessageStatusAsync_WithTwilioJsonStatusCallback_ExtremelyLongProperties_HandlesCorrectly()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // Status callback with extremely long property values
        var longString = new string('A', 10000);
        var statusJson = new
        {
            MessageSid = "SM1234567890",
            MessageStatus = "failed",
            ErrorMessage = longString,
            ExtraData = longString
        };

        var jsonPayload = JsonSerializer.Serialize(statusJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessageStatusAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.NotNull(result.Value);
        Assert.Equal("SM1234567890", result.Value.MessageId);
        Assert.Equal(MessageStatus.DeliveryFailed, result.Value.Status);
        
        // Verify long property is preserved
        Assert.True(result.Value.AdditionalData.ContainsKey("ErrorMessage"));
        Assert.Equal(longString, result.Value.AdditionalData["ErrorMessage"]);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_EmptyArrayInMessages_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // JSON with empty Messages array
        var webhookJson = new
        {
            Messages = new object[] { }
        };

        var jsonPayload = JsonSerializer.Serialize(webhookJson);
        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.InvalidWebhookData, result.Error.ErrorCode);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_WithTwilioJsonWebhook_NonArrayMessages_ReturnsError()
    {
        // Arrange
        var schema = TwilioChannelSchemas.TwilioSms;
        var connectionSettings = CreateValidConnectionSettings();
        var connector = new TwilioSmsConnector(schema, connectionSettings);
        await connector.InitializeAsync(CancellationToken.None);

        // JSON with Messages as a string instead of array
        var jsonPayload = """
        {
            "Messages": "not an array"
        }
        """;

        var source = MessageSource.Json(jsonPayload);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.False(result.Successful);
        Assert.NotNull(result.Error);
        Assert.Equal(TwilioErrorCodes.ReceiveMessageFailed, result.Error.ErrorCode);
    }

    private static ConnectionSettings CreateValidConnectionSettings()
    {
        return new ConnectionSettings()
            .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
            .SetParameter("AuthToken", "auth_token_1234567890123456789012345678");
    }
}