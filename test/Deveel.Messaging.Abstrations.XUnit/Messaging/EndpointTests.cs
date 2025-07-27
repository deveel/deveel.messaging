namespace Deveel.Messaging;

public class EndpointTests
{
    [Fact]
    public void Endpoint_DefaultConstructor_CreatesEmptyEndpoint()
    {
        // Arrange & Act
        var endpoint = new Endpoint();

        // Assert
        Assert.Equal(new EndpointType(), endpoint.Type);
        Assert.Equal("", endpoint.Address);
    }

    [Fact]
    public void Endpoint_ConstructorWithTypeAndAddress_SetsProperties()
    {
        // Arrange
        var type = EndpointType.EmailAddress;
        var address = "test@example.com";

        // Act
        var endpoint = new Endpoint(type, address);

        // Assert
        Assert.Equal(type, endpoint.Type);
        Assert.Equal(address, endpoint.Address);
    }

    [Fact]
    public void Endpoint_ConstructorWithIEndpoint_CopiesProperties()
    {
        // Arrange
        var sourceEndpoint = new Endpoint(EndpointType.PhoneNumber, "+1234567890");

        // Act
        var endpoint = new Endpoint(sourceEndpoint);

        // Assert
        Assert.Equal(EndpointType.PhoneNumber, endpoint.Type);
        Assert.Equal("+1234567890", endpoint.Address);
    }

    [Fact]
    public void Create_ValidTypeAndAddress_ReturnsEndpoint()
    {
        // Arrange
        var type = EndpointType.EmailAddress;
        var address = "test@example.com";

        // Act
        var endpoint = Endpoint.Create(type, address);

        // Assert
        Assert.Equal(type, endpoint.Type);
        Assert.Equal(address, endpoint.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_InvalidEmptyType_ThrowsArgumentException(string? type)
    {
        // Arrange
        var address = "test@example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Endpoint.Create(type!, address));
    }

	[Fact]
	public void Create_InvalidNullType_ThrowsArgumentNullException()
	{
		// Arrange
		var address = "test@example.com";

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => Endpoint.Create((string)null!, address));
	}


	[Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_InvalidEmptyAddress_ThrowsArgumentException(string? address)
    {
        // Arrange
        var type = EndpointType.EmailAddress;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Endpoint.Create(type, address!));
    }

    [Fact]
	public void Create_InvalidNullAddress_ThrowsArgumentException()
	{
		// Arrange
		var type = EndpointType.EmailAddress;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => Endpoint.Create(type, null!));
	}


	[Fact]
    public void Id_ValidEndpointId_ReturnsEndpointWithIdType()
    {
        // Arrange
        var endpointId = "endpoint-123";

        // Act
        var endpoint = Endpoint.Id(endpointId);

        // Assert
        Assert.Equal(EndpointType.Id, endpoint.Type);
        Assert.Equal(endpointId, endpoint.Address);
    }

    [Fact]
    public void EmailAddress_ValidEmail_ReturnsEmailEndpoint()
    {
        // Arrange
        var email = "user@example.com";

        // Act
        var endpoint = Endpoint.EmailAddress(email);

        // Assert
        Assert.Equal(EndpointType.EmailAddress, endpoint.Type);
        Assert.Equal(email, endpoint.Address);
    }

    [Fact]
    public void PhoneNumber_ValidPhone_ReturnsPhoneEndpoint()
    {
        // Arrange
        var phone = "+1234567890";

        // Act
        var endpoint = Endpoint.PhoneNumber(phone);

        // Assert
        Assert.Equal(EndpointType.PhoneNumber, endpoint.Type);
        Assert.Equal(phone, endpoint.Address);
    }

    [Fact]
    public void Url_ValidUrl_ReturnsUrlEndpoint()
    {
        // Arrange
        var url = "https://example.com/webhook";

        // Act
        var endpoint = Endpoint.Url(url);

        // Assert
        Assert.Equal(EndpointType.Url, endpoint.Type);
        Assert.Equal(url, endpoint.Address);
    }

    [Fact]
    public void Application_ValidAppId_ReturnsApplicationEndpoint()
    {
        // Arrange
        var appId = "app-123";

        // Act
        var endpoint = Endpoint.Application(appId);

        // Assert
        Assert.Equal(EndpointType.ApplicationId, endpoint.Type);
        Assert.Equal(appId, endpoint.Address);
    }

    [Fact]
    public void User_ValidUserId_ReturnsUserEndpoint()
    {
        // Arrange
        var userId = "user-456";

        // Act
        var endpoint = Endpoint.User(userId);

        // Assert
        Assert.Equal(EndpointType.UserId, endpoint.Type);
        Assert.Equal(userId, endpoint.Address);
    }

    [Fact]
    public void Device_ValidDeviceId_ReturnsDeviceEndpoint()
    {
        // Arrange
        var deviceId = "device-789";

        // Act
        var endpoint = Endpoint.Device(deviceId);

        // Assert
        Assert.Equal(EndpointType.DeviceId, endpoint.Type);
        Assert.Equal(deviceId, endpoint.Address);
    }

    [Fact]
    public void AlphaNumeric_ValidLabel_ReturnsLabelEndpoint()
    {
        // Arrange
        var label = "TEST123";

        // Act
        var endpoint = Endpoint.AlphaNumeric(label);

        // Assert
        Assert.Equal(EndpointType.Label, endpoint.Type);
        Assert.Equal(label, endpoint.Address);
    }

    [Fact]
    public void IEndpoint_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var endpoint = new Endpoint(EndpointType.EmailAddress, "test@example.com");

        // Act & Assert
        IEndpoint iEndpoint = endpoint;
        Assert.Equal(EndpointType.EmailAddress, iEndpoint.Type);
        Assert.Equal("test@example.com", iEndpoint.Address);
    }

    [Fact]
    public void PropertySetters_SetValues_UpdatesProperties()
    {
        // Arrange
        var endpoint = new Endpoint();

        // Act
        endpoint.Type = EndpointType.PhoneNumber;
        endpoint.Address = "+9876543210";

        // Assert
        Assert.Equal(EndpointType.PhoneNumber, endpoint.Type);
        Assert.Equal("+9876543210", endpoint.Address);
    }

    [Fact]
    public void Endpoint_ConstructorWithStringType_ConvertsToEnum()
    {
        // Arrange & Act
        var endpoint = new Endpoint("email", "test@example.com");

        // Assert
        Assert.Equal(EndpointType.EmailAddress, endpoint.Type);
        Assert.Equal("test@example.com", endpoint.Address);
    }

    [Theory]
    [InlineData("email", EndpointType.EmailAddress)]
    [InlineData("phone", EndpointType.PhoneNumber)]
    [InlineData("url", EndpointType.Url)]
    [InlineData("user-id", EndpointType.UserId)]
    [InlineData("app-id", EndpointType.ApplicationId)]
    [InlineData("endpoint-id", EndpointType.Id)]
    [InlineData("device-id", EndpointType.DeviceId)]
    [InlineData("label", EndpointType.Label)]
    public void Endpoint_ConstructorWithStringTypes_ConvertsCorrectly(string stringType, EndpointType expectedType)
    {
        // Arrange
        var address = "test-address";

        // Act
        var endpoint = new Endpoint(stringType, address);

        // Assert
        Assert.Equal(expectedType, endpoint.Type);
        Assert.Equal(address, endpoint.Address);
    }

    [Fact]
    public void Endpoint_ConstructorWithUnknownStringType_ThrowsArgumentException()
    {
        // Arrange
        var unknownType = "unknown-type";
        var address = "test-address";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Endpoint(unknownType, address));
        Assert.Contains("Unknown endpoint type: unknown-type", exception.Message);
    }

    [Fact]
    public void Create_WithStringType_ConvertsToEnum()
    {
        // Arrange
        var type = "email";
        var address = "test@example.com";

        // Act
        var endpoint = Endpoint.Create(type, address);

        // Assert
        Assert.Equal(EndpointType.EmailAddress, endpoint.Type);
        Assert.Equal(address, endpoint.Address);
    }

    [Fact]
    public void Create_WithUnknownStringType_ThrowsArgumentException()
    {
        // Arrange
        var unknownType = "unknown";
        var address = "test-address";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Endpoint.Create(unknownType, address));
        Assert.Contains("Unknown endpoint type: unknown", exception.Message);
    }

    [Theory]
    [InlineData("EMAIL")]
    [InlineData("Email")]
    [InlineData("PHONE")]
    [InlineData("Phone")]
    public void Endpoint_StringTypeConversion_IsCaseInsensitive(string stringType)
    {
        // Arrange
        var address = "test-address";

        // Act
        var endpoint = new Endpoint(stringType, address);

        // Assert
        Assert.True(endpoint.Type == EndpointType.EmailAddress || endpoint.Type == EndpointType.PhoneNumber);
        Assert.Equal(address, endpoint.Address);
    }
}