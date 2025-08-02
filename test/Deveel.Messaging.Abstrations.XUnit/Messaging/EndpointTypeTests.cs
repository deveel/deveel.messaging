namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="EndpointType"/> enum to ensure all values are properly defined and behave correctly.
/// </summary>
public class EndpointTypeTests
{
    [Fact]
    public void EndpointType_AllValues_AreUnique()
    {
        // Arrange
        var allValues = Enum.GetValues<EndpointType>();

        // Act
        var uniqueValues = allValues.Distinct().ToArray();

        // Assert
        Assert.Equal(allValues.Length, uniqueValues.Length);
    }

    [Fact]
    public void EndpointType_AllValues_HaveValidNames()
    {
        // Arrange
        var allValues = Enum.GetValues<EndpointType>();

        // Act & Assert
        foreach (var value in allValues)
        {
            var name = Enum.GetName(value);
            Assert.NotNull(name);
            Assert.NotEmpty(name);
            Assert.False(string.IsNullOrWhiteSpace(name));
        }
    }

    [Theory]
    [InlineData(EndpointType.PhoneNumber)]
    [InlineData(EndpointType.EmailAddress)]
    [InlineData(EndpointType.Url)]
    [InlineData(EndpointType.Topic)]
    [InlineData(EndpointType.Id)]
    [InlineData(EndpointType.UserId)]
    [InlineData(EndpointType.ApplicationId)]
    [InlineData(EndpointType.DeviceId)]
    [InlineData(EndpointType.Label)]
    [InlineData(EndpointType.Any)]
    public void EndpointType_SpecificValues_AreValid(EndpointType endpointType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(EndpointType), endpointType));
    }

    [Fact]
    public void EndpointType_CanBeUsedInEndpointConstructor()
    {
        // Arrange
        var testCases = new[]
        {
            (EndpointType.EmailAddress, "test@example.com"),
            (EndpointType.PhoneNumber, "+1234567890"),
            (EndpointType.Url, "https://example.com"),
            (EndpointType.Topic, "topic-name"),
            (EndpointType.UserId, "user123"),
            (EndpointType.ApplicationId, "app456"),
            (EndpointType.Id, "endpoint789"),
            (EndpointType.DeviceId, "device012"),
            (EndpointType.Label, "LABEL")
        };

        // Act & Assert
        foreach (var (type, address) in testCases)
        {
            var endpoint = new Endpoint(type, address);
            Assert.Equal(type, endpoint.Type);
            Assert.Equal(address, endpoint.Address);
        }
    }

    [Fact]
    public void EndpointType_WithEndpointStaticMethods_CreatesCorrectTypes()
    {
        // Act
        var emailEndpoint = Endpoint.EmailAddress("test@example.com");
        var phoneEndpoint = Endpoint.PhoneNumber("+1234567890");
        var urlEndpoint = Endpoint.Url("https://example.com");
        var userEndpoint = Endpoint.User("user123");
        var appEndpoint = Endpoint.Application("app456");
        var idEndpoint = Endpoint.Id("endpoint789");
        var deviceEndpoint = Endpoint.Device("device012");
        var labelEndpoint = Endpoint.AlphaNumeric("LABEL");

        // Assert
        Assert.Equal(EndpointType.EmailAddress, emailEndpoint.Type);
        Assert.Equal(EndpointType.PhoneNumber, phoneEndpoint.Type);
        Assert.Equal(EndpointType.Url, urlEndpoint.Type);
        Assert.Equal(EndpointType.UserId, userEndpoint.Type);
        Assert.Equal(EndpointType.ApplicationId, appEndpoint.Type);
        Assert.Equal(EndpointType.Id, idEndpoint.Type);
        Assert.Equal(EndpointType.DeviceId, deviceEndpoint.Type);
        Assert.Equal(EndpointType.Label, labelEndpoint.Type);
    }

    [Fact]
    public void EndpointType_ToString_ReturnsExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.Equal("PhoneNumber", EndpointType.PhoneNumber.ToString());
        Assert.Equal("EmailAddress", EndpointType.EmailAddress.ToString());
        Assert.Equal("Url", EndpointType.Url.ToString());
        Assert.Equal("Id", EndpointType.Id.ToString());
        Assert.Equal("UserId", EndpointType.UserId.ToString());
        Assert.Equal("ApplicationId", EndpointType.ApplicationId.ToString());
        Assert.Equal("DeviceId", EndpointType.DeviceId.ToString());
        Assert.Equal("Label", EndpointType.Label.ToString());
    }

    [Fact]
    public void EndpointType_Comparison_WorksCorrectly()
    {
        // Arrange
        var email1 = EndpointType.EmailAddress;
        var email2 = EndpointType.EmailAddress;
        var phone = EndpointType.PhoneNumber;

        // Act & Assert
        Assert.Equal(email1, email2);
        Assert.NotEqual(email1, phone);
        Assert.True(email1 == email2);
        Assert.False(email1 == phone);
        Assert.False(email1 != email2);
        Assert.True(email1 != phone);
    }


    [Fact]
    public void EndpointType_CanBeUsedInSwitchExpressions()
    {
        // Arrange
        var testTypes = new[]
        {
            EndpointType.EmailAddress,
            EndpointType.PhoneNumber,
            EndpointType.Url,
            EndpointType.UserId
        };

        // Act & Assert
        foreach (var type in testTypes)
        {
            var description = type switch
            {
                EndpointType.EmailAddress => "Email",
                EndpointType.PhoneNumber => "Phone",
                EndpointType.Url => "Web URL",
                EndpointType.UserId => "User",
                EndpointType.ApplicationId => "Application",
                EndpointType.Id => "Identifier",
                EndpointType.DeviceId => "Device",
                EndpointType.Label => "Label",
                _ => "Unknown"
            };

            Assert.NotEqual("Unknown", description);
        }
    }

    [Fact]
    public void EndpointType_EnumParsing_WorksCorrectly()
    {
        // Arrange
        var typeNames = new[]
        {
            "EmailAddress",
            "PhoneNumber", 
            "Url",
            "UserId",
            "ApplicationId",
            "Id",
            "DeviceId",
            "Label"
        };

        // Act & Assert
        foreach (var typeName in typeNames)
        {
            var success = Enum.TryParse<EndpointType>(typeName, out var parsedType);
            Assert.True(success, $"Failed to parse '{typeName}'");
            Assert.Equal(typeName, parsedType.ToString());
        }
    }

    [Fact]
    public void EndpointType_CaseInsensitiveParsing_WorksCorrectly()
    {
        // Act & Assert
        Assert.True(Enum.TryParse<EndpointType>("emailaddress", true, out var emailType));
        Assert.Equal(EndpointType.EmailAddress, emailType);

        Assert.True(Enum.TryParse<EndpointType>("PHONENUMBER", true, out var phoneType));
        Assert.Equal(EndpointType.PhoneNumber, phoneType);

        Assert.True(Enum.TryParse<EndpointType>("url", true, out var urlType));
        Assert.Equal(EndpointType.Url, urlType);
    }
}