namespace Deveel.Messaging;

/// <summary>
/// Integration tests to ensure the EndpointType change works correctly across all components.
/// </summary>
public class EndpointTypeIntegrationTests
{
    [Fact]
    public void EndpointType_ChangesIntegration_WorksCorrectly()
    {
        // Test 1: Direct enum usage
        var emailEndpoint = new Endpoint(EndpointType.EmailAddress, "test@example.com");
        Assert.Equal(EndpointType.EmailAddress, emailEndpoint.Type);
        Assert.Equal("test@example.com", emailEndpoint.Address);

        // Test 2: String compatibility via constructor
        var phoneEndpoint = new Endpoint("phone", "+1234567890");
        Assert.Equal(EndpointType.PhoneNumber, phoneEndpoint.Type);
        Assert.Equal("+1234567890", phoneEndpoint.Address);

        // Test 3: Static factory methods
        var urlEndpoint = Endpoint.Url("https://example.com");
        Assert.Equal(EndpointType.Url, urlEndpoint.Type);

        // Test 4: Message creation with endpoints
        var message = new Message
        {
            Id = "test-msg",
            Sender = emailEndpoint,
            Receiver = phoneEndpoint,
            Content = new TextContent("Test message")
        };
        Assert.Equal(EndpointType.EmailAddress, message.Sender!.Type);
        Assert.Equal(EndpointType.PhoneNumber, message.Receiver!.Type);

        // Test 5: Message fluent interface integration
        var builtMessage = new Message()
            .WithId("builder-msg")
            .WithEmailSender("builder@test.com")
            .WithPhoneReceiver("+9876543210")
            .WithTextContent("Built message");
        
        Assert.Equal(EndpointType.EmailAddress, builtMessage.Sender!.Type);
        Assert.Equal(EndpointType.PhoneNumber, builtMessage.Receiver!.Type);
        Assert.Equal("builder@test.com", builtMessage.Sender.Address);
        Assert.Equal("+9876543210", builtMessage.Receiver.Address);

        // Test 6: IEndpoint interface compatibility
        IEndpoint iEndpoint = emailEndpoint;
        Assert.Equal(EndpointType.EmailAddress, iEndpoint.Type);
        Assert.Equal("test@example.com", iEndpoint.Address);

        // Test 7: All known types mapping
        var typeMapping = new Dictionary<string, EndpointType>
        {
            { "email", EndpointType.EmailAddress },
            { "phone", EndpointType.PhoneNumber },
            { "url", EndpointType.Url },
            { "user-id", EndpointType.UserId },
            { "app-id", EndpointType.ApplicationId },
            { "endpoint-id", EndpointType.Id },
            { "device-id", EndpointType.DeviceId },
            { "label", EndpointType.Label }
        };

        foreach (var (stringType, enumType) in typeMapping)
        {
            var testEndpoint = new Endpoint(stringType, "test-address");
            Assert.Equal(enumType, testEndpoint.Type);
        }
    }

    //[Fact]
    //public void ChannelSchema_WithEndpointTypes_WorksCorrectly()
    //{
    //    // Test that channel schemas work with endpoint type strings (for backward compatibility)
    //    var schema = new ChannelSchema("TestProvider", "TestChannel", "1.0.0")
    //        .AllowsMessageEndpoint("email", asSender: true, asReceiver: true)
    //        .AllowsMessageEndpoint("sms", asSender: false, asReceiver: true);

    //    Assert.Equal(2, schema.Endpoints.Count);
    //    Assert.Contains(schema.Endpoints, e => e.Type == "email");
    //    Assert.Contains(schema.Endpoints, e => e.Type == "sms");
    //}

    [Fact]
    public void EndpointTypeConversion_AllKnownTypes_ConvertCorrectly()
    {
        // Test conversion from all known string types to enum
        var testCases = new[]
        {
            ("email", EndpointType.EmailAddress),
            ("EMAIL", EndpointType.EmailAddress), // Case insensitive
            ("phone", EndpointType.PhoneNumber),
            ("url", EndpointType.Url),
            ("user-id", EndpointType.UserId),
            ("app-id", EndpointType.ApplicationId),
            ("endpoint-id", EndpointType.Id),
            ("device-id", EndpointType.DeviceId),
            ("label", EndpointType.Label)
        };

        foreach (var (stringType, expectedEnum) in testCases)
        {
            var endpoint = new Endpoint(stringType, "test-address");
            Assert.Equal(expectedEnum, endpoint.Type);
        }
    }

    [Fact]
    public void EndpointTypeConversion_UnknownType_ThrowsException()
    {
        // Test that unknown types throw appropriate exceptions
        Assert.Throws<ArgumentException>(() => new Endpoint("unknown-type", "test-address"));
        Assert.Throws<ArgumentException>(() => Endpoint.Create("invalid-type", "test-address"));
    }

    [Theory]
    [InlineData(EndpointType.EmailAddress, "test@example.com")]
    [InlineData(EndpointType.PhoneNumber, "+1234567890")]
    [InlineData(EndpointType.Url, "https://example.com")]
    [InlineData(EndpointType.UserId, "user123")]
    [InlineData(EndpointType.ApplicationId, "app456")]
    [InlineData(EndpointType.Id, "id789")]
    [InlineData(EndpointType.DeviceId, "device012")]
    [InlineData(EndpointType.Label, "LABEL")]
    public void EndpointType_EnumValues_CanBeUsedDirectly(EndpointType endpointType, string address)
    {
        // Test that all enum values can be used directly
        var endpoint = new Endpoint(endpointType, address);
        Assert.Equal(endpointType, endpoint.Type);
        Assert.Equal(address, endpoint.Address);

        // Test with Create method
        var createdEndpoint = Endpoint.Create(endpointType, address);
        Assert.Equal(endpointType, createdEndpoint.Type);
        Assert.Equal(address, createdEndpoint.Address);
    }

    [Fact]
    public void BackwardCompatibility_StringTypes_StillWork()
    {
        // Ensure that existing code using string constants still works
        var endpoints = new[]
        {
            new Endpoint("email", "test@example.com"),
            new Endpoint("phone", "+1234567890"),
            new Endpoint("url", "https://example.com"),
            new Endpoint("user-id", "user123"),
            new Endpoint("app-id", "app456"),
            new Endpoint("endpoint-id", "endpoint789"),
            new Endpoint("device-id", "device012"),
            new Endpoint("label", "LABEL")
        };

        // All endpoints should be successfully created and have correct types
        Assert.All(endpoints, endpoint => 
        {
            Assert.True(Enum.IsDefined(typeof(EndpointType), endpoint.Type));
            Assert.NotEmpty(endpoint.Address);
        });
    }
}