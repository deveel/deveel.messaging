using Moq;
using System.Reflection;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Api.V2010;
using Twilio.Types;

namespace Deveel.Messaging;

/// <summary>
/// Factory class for creating mock Twilio services and resources for testing.
/// </summary>
public static class TwilioMockFactory
{
    /// <summary>
    /// Creates a mock Twilio service with default successful behaviors.
    /// </summary>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioService()
    {
        var mock = new Mock<ITwilioService>();
        
        // Setup default successful initialization
        mock.Setup(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>()));
        
        return mock;
    }

    /// <summary>
    /// Creates a mock Twilio service configured for successful message sending.
    /// </summary>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioServiceForSending()
    {
        return CreateMockTwilioServiceForSending("SM123456789", MessageResource.StatusEnum.Queued);
    }

    /// <summary>
    /// Creates a mock Twilio service configured for successful message sending.
    /// </summary>
    /// <param name="messageSid">The message SID to return.</param>
    /// <param name="status">The message status to return.</param>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioServiceForSending(string messageSid, MessageResource.StatusEnum status)
    {
        var mock = CreateMockTwilioService();
        
        var messageResource = CreateMockMessageResource(messageSid, status);
        mock.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageResource);
            
        return mock;
    }

    /// <summary>
    /// Creates a mock Twilio service configured for successful connection testing.
    /// </summary>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioServiceForConnectionTest()
    {
        return CreateMockTwilioServiceForConnectionTest("AC1234567890123456789012345678901234", "Test Account");
    }

    /// <summary>
    /// Creates a mock Twilio service configured for successful connection testing.
    /// </summary>
    /// <param name="accountSid">The account SID.</param>
    /// <param name="friendlyName">The account friendly name.</param>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioServiceForConnectionTest(string accountSid, string friendlyName)
    {
        var mock = CreateMockTwilioService();
        
        var accountResource = CreateMockAccountResource(accountSid, friendlyName);
        mock.Setup(x => x.FetchAccountAsync(accountSid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResource);
            
        return mock;
    }

    /// <summary>
    /// Creates a mock Twilio service configured for successful status queries.
    /// </summary>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioServiceForStatusQuery()
    {
        return CreateMockTwilioServiceForStatusQuery("SM123456789", MessageResource.StatusEnum.Delivered);
    }

    /// <summary>
    /// Creates a mock Twilio service configured for successful status queries.
    /// </summary>
    /// <param name="messageSid">The message SID to query.</param>
    /// <param name="status">The status to return.</param>
    /// <returns>A configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateMockTwilioServiceForStatusQuery(string messageSid, MessageResource.StatusEnum status)
    {
        var mock = CreateMockTwilioService();
        
        var messageResource = CreateMockMessageResource(messageSid, status);
        mock.Setup(x => x.FetchMessageAsync(messageSid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageResource);
            
        return mock;
    }

    /// <summary>
    /// Creates a fully configured mock Twilio service for comprehensive testing.
    /// </summary>
    /// <returns>A fully configured mock Twilio service.</returns>
    public static Mock<ITwilioService> CreateFullyConfiguredMockTwilioService()
    {
        var mock = CreateMockTwilioService();
        
        // Setup successful message sending
        var sendMessageResource = CreateMockMessageResource();
        mock.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sendMessageResource);
        
        // Setup successful connection testing
        var accountResource = CreateMockAccountResource();
        mock.Setup(x => x.FetchAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResource);
        
        // Setup successful status queries
        var statusMessageResource = CreateMockMessageResource("SM123456789", MessageResource.StatusEnum.Delivered);
        mock.Setup(x => x.FetchMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusMessageResource);
        
        return mock;
    }

    /// <summary>
    /// Creates a mock MessageResource with default properties.
    /// </summary>
    /// <returns>A mock MessageResource.</returns>
    public static MessageResource CreateMockMessageResource()
    {
        return CreateMockMessageResource("SM123456789", MessageResource.StatusEnum.Queued, "+1987654321", "+1234567890", "Hello World");
    }

    /// <summary>
    /// Creates a mock MessageResource with specified properties.
    /// </summary>
    /// <param name="sid">The message SID.</param>
    /// <param name="status">The message status.</param>
    /// <returns>A mock MessageResource.</returns>
    public static MessageResource CreateMockMessageResource(string sid, MessageResource.StatusEnum status)
    {
        return CreateMockMessageResource(sid, status, "+1987654321", "+1234567890", "Hello World");
    }

    /// <summary>
    /// Creates a MessageResource with specified properties using careful reflection.
    /// </summary>
    /// <param name="sid">The message SID.</param>
    /// <param name="status">The message status.</param>
    /// <param name="to">The recipient phone number.</param>
    /// <param name="from">The sender phone number.</param>
    /// <param name="body">The message body.</param>
    /// <returns>A MessageResource.</returns>
    public static MessageResource CreateMockMessageResource(string sid, MessageResource.StatusEnum status, string to, string from, string body)
    {
        var messageResource = (MessageResource)Activator.CreateInstance(typeof(MessageResource), true)!;
        
        // Use reflection to set backing fields directly using the correct compiler-generated field names
        // Skip To and From fields for now since they seem to have complex type requirements
        SetPrivateField(messageResource, "<Sid>k__BackingField", sid);
        SetPrivateField(messageResource, "<Status>k__BackingField", status);
        SetPrivateField(messageResource, "<Body>k__BackingField", body);
        SetPrivateField(messageResource, "<DateCreated>k__BackingField", DateTime.UtcNow);
        SetPrivateField(messageResource, "<DateUpdated>k__BackingField", DateTime.UtcNow);
        SetPrivateField(messageResource, "<NumSegments>k__BackingField", "1");
        SetPrivateField(messageResource, "<Price>k__BackingField", "0.0075");
        SetPrivateField(messageResource, "<PriceUnit>k__BackingField", "USD");
        SetPrivateField(messageResource, "<ErrorCode>k__BackingField", (int?)null);
        SetPrivateField(messageResource, "<ErrorMessage>k__BackingField", (string?)null);

        return messageResource;
    }

    /// <summary>
    /// Creates a mock AccountResource with default properties.
    /// </summary>
    /// <returns>A mock AccountResource.</returns>
    public static AccountResource CreateMockAccountResource()
    {
        return CreateMockAccountResource("AC1234567890123456789012345678901234", "Test Account");
    }

    /// <summary>
    /// Creates an AccountResource with specified properties using careful reflection.
    /// </summary>
    /// <param name="sid">The account SID.</param>
    /// <param name="friendlyName">The account friendly name.</param>
    /// <returns>An AccountResource.</returns>
    public static AccountResource CreateMockAccountResource(string sid, string friendlyName)
    {
        var accountResource = (AccountResource)Activator.CreateInstance(typeof(AccountResource), true)!;
        
        // Use reflection to set backing fields directly using the correct compiler-generated field names
        SetPrivateField(accountResource, "<Sid>k__BackingField", sid);
        SetPrivateField(accountResource, "<FriendlyName>k__BackingField", friendlyName);
        SetPrivateField(accountResource, "<Status>k__BackingField", AccountResource.StatusEnum.Active);

        return accountResource;
    }

    /// <summary>
    /// Creates a mock MessageResource representing a failed message.
    /// </summary>
    /// <param name="sid">The message SID.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A mock MessageResource with error information.</returns>
    public static MessageResource CreateMockFailedMessageResource(string sid, int errorCode, string errorMessage)
    {
        var messageResource = (MessageResource)Activator.CreateInstance(typeof(MessageResource), true)!;
        
        SetPrivateField(messageResource, "<Sid>k__BackingField", sid);
        SetPrivateField(messageResource, "<Status>k__BackingField", MessageResource.StatusEnum.Failed);
        SetPrivateField(messageResource, "<Body>k__BackingField", "Hello World");
        SetPrivateField(messageResource, "<DateCreated>k__BackingField", DateTime.UtcNow);
        SetPrivateField(messageResource, "<DateUpdated>k__BackingField", DateTime.UtcNow);
        SetPrivateField(messageResource, "<NumSegments>k__BackingField", "1");
        SetPrivateField(messageResource, "<Price>k__BackingField", "0.0075");
        SetPrivateField(messageResource, "<PriceUnit>k__BackingField", "USD");
        SetPrivateField(messageResource, "<ErrorCode>k__BackingField", errorCode);
        SetPrivateField(messageResource, "<ErrorMessage>k__BackingField", errorMessage);

        return messageResource;
    }

    /// <summary>
    /// Configures a mock Twilio service to throw exceptions for testing error scenarios.
    /// </summary>
    /// <param name="mock">The mock to configure.</param>
    /// <param name="exception">The exception to throw.</param>
    public static void ConfigureForException(Mock<ITwilioService> mock, Exception exception)
    {
        mock.Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        
        mock.Setup(x => x.FetchMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        
        mock.Setup(x => x.FetchAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
    }

    /// <summary>
    /// Sets a private field value using reflection.
    /// </summary>
    /// <param name="obj">The object instance.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="value">The value to set.</param>
    private static void SetPrivateField(object obj, string fieldName, object? value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}