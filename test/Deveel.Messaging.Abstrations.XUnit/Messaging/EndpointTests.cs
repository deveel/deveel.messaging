namespace Deveel.Messaging;

public class EndpointTests
{
    [Fact]
    public void Endpoint_DefaultConstructor_CreatesEmptyEndpoint()
    {
        // Arrange & Act
        var endpoint = new Endpoint();

        // Assert
        Assert.Equal("", endpoint.Type);
        Assert.Equal("", endpoint.Address);
    }

    [Fact]
    public void Endpoint_ConstructorWithTypeAndAddress_SetsProperties()
    {
        // Arrange
        var type = KnownEndpointTypes.Email;
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
        var sourceEndpoint = new Endpoint(KnownEndpointTypes.Phone, "+1234567890");

        // Act
        var endpoint = new Endpoint(sourceEndpoint);

        // Assert
        Assert.Equal(KnownEndpointTypes.Phone, endpoint.Type);
        Assert.Equal("+1234567890", endpoint.Address);
    }

    [Fact]
    public void Create_ValidTypeAndAddress_ReturnsEndpoint()
    {
        // Arrange
        var type = KnownEndpointTypes.Email;
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
	public void Create_InvalidNullType_ThrowsArgumentException()
	{
		// Arrange
		var address = "test@example.com";

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => Endpoint.Create(null, address));
	}


	[Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_InvalidEmptyAddress_ThrowsArgumentException(string? address)
    {
        // Arrange
        var type = KnownEndpointTypes.Email;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Endpoint.Create(type, address!));
    }

    [Fact]
	public void Create_InvalidNullAddress_ThrowsArgumentException()
	{
		// Arrange
		var type = KnownEndpointTypes.Email;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => Endpoint.Create(type, null));
	}


	[Fact]
    public void Id_ValidEndpointId_ReturnsEndpointWithIdType()
    {
        // Arrange
        var endpointId = "endpoint-123";

        // Act
        var endpoint = Endpoint.Id(endpointId);

        // Assert
        Assert.Equal(KnownEndpointTypes.EndpointId, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.Email, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.Phone, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.Url, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.Application, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.UserId, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.DeviceId, endpoint.Type);
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
        Assert.Equal(KnownEndpointTypes.Label, endpoint.Type);
        Assert.Equal(label, endpoint.Address);
    }

    [Fact]
    public void IEndpoint_Implementation_ExposesCorrectProperties()
    {
        // Arrange
        var endpoint = new Endpoint(KnownEndpointTypes.Email, "test@example.com");

        // Act & Assert
        IEndpoint iEndpoint = endpoint;
        Assert.Equal(KnownEndpointTypes.Email, iEndpoint.Type);
        Assert.Equal("test@example.com", iEndpoint.Address);
    }

    [Fact]
    public void PropertySetters_SetValues_UpdatesProperties()
    {
        // Arrange
        var endpoint = new Endpoint();

        // Act
        endpoint.Type = KnownEndpointTypes.Phone;
        endpoint.Address = "+9876543210";

        // Assert
        Assert.Equal(KnownEndpointTypes.Phone, endpoint.Type);
        Assert.Equal("+9876543210", endpoint.Address);
    }
}