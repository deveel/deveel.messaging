namespace Deveel.Messaging;

public class KnownEndpointTypesTests
{
    [Fact]
    public void KnownEndpointTypes_Email_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("email", KnownEndpointTypes.Email);
    }

    [Fact]
    public void KnownEndpointTypes_Phone_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("phone", KnownEndpointTypes.Phone);
    }

    [Fact]
    public void KnownEndpointTypes_Url_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("url", KnownEndpointTypes.Url);
    }

    [Fact]
    public void KnownEndpointTypes_UserId_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("user-id", KnownEndpointTypes.UserId);
    }

    [Fact]
    public void KnownEndpointTypes_Application_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("app-id", KnownEndpointTypes.Application);
    }

    [Fact]
    public void KnownEndpointTypes_EndpointId_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("endpoint-id", KnownEndpointTypes.EndpointId);
    }

    [Fact]
    public void KnownEndpointTypes_DeviceId_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("device-id", KnownEndpointTypes.DeviceId);
    }

    [Fact]
    public void KnownEndpointTypes_Label_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("label", KnownEndpointTypes.Label);
    }

    [Fact]
    public void KnownEndpointTypes_AllValues_AreDistinct()
    {
        // Arrange
        var values = new[]
        {
            KnownEndpointTypes.Email,
            KnownEndpointTypes.Phone,
            KnownEndpointTypes.Url,
            KnownEndpointTypes.UserId,
            KnownEndpointTypes.Application,
            KnownEndpointTypes.EndpointId,
            KnownEndpointTypes.DeviceId,
            KnownEndpointTypes.Label
        };

        // Act
        var distinctValues = values.Distinct().ToList();

        // Assert
        Assert.Equal(values.Length, distinctValues.Count);
    }

    [Fact]
    public void KnownEndpointTypes_AllValues_AreNotNullOrEmpty()
    {
        // Arrange
        var values = new[]
        {
            KnownEndpointTypes.Email,
            KnownEndpointTypes.Phone,
            KnownEndpointTypes.Url,
            KnownEndpointTypes.UserId,
            KnownEndpointTypes.Application,
            KnownEndpointTypes.EndpointId,
            KnownEndpointTypes.DeviceId,
            KnownEndpointTypes.Label
        };

        // Act & Assert
        foreach (var value in values)
        {
            Assert.False(string.IsNullOrEmpty(value), $"Endpoint type value should not be null or empty: {value}");
            Assert.False(string.IsNullOrWhiteSpace(value), $"Endpoint type value should not be whitespace: {value}");
        }
    }
}

public class KnownMessagePropertiesTests
{
    [Fact]
    public void KnownMessageProperties_Subject_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("subject", KnownMessageProperties.Subject);
    }

    [Fact]
    public void KnownMessageProperties_RemoteMessageId_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("remoteMessageId", KnownMessageProperties.RemoteMessageId);
    }

    [Fact]
    public void KnownMessageProperties_ReplyTo_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("replyTo", KnownMessageProperties.ReplyTo);
    }

    [Fact]
    public void KnownMessageProperties_CorrelationId_HasCorrectValue()
    {
        // Act & Assert
        Assert.Equal("correlationId", KnownMessageProperties.CorrelationId);
    }

    [Fact]
    public void KnownMessageProperties_AllValues_AreDistinct()
    {
        // Arrange
        var values = new[]
        {
            KnownMessageProperties.Subject,
            KnownMessageProperties.RemoteMessageId,
            KnownMessageProperties.ReplyTo,
            KnownMessageProperties.CorrelationId
        };

        // Act
        var distinctValues = values.Distinct().ToList();

        // Assert
        Assert.Equal(values.Length, distinctValues.Count);
    }

    [Fact]
    public void KnownMessageProperties_AllValues_AreNotNullOrEmpty()
    {
        // Arrange
        var values = new[]
        {
            KnownMessageProperties.Subject,
            KnownMessageProperties.RemoteMessageId,
            KnownMessageProperties.ReplyTo,
            KnownMessageProperties.CorrelationId
        };

        // Act & Assert
        foreach (var value in values)
        {
            Assert.False(string.IsNullOrEmpty(value), $"Message property value should not be null or empty: {value}");
            Assert.False(string.IsNullOrWhiteSpace(value), $"Message property value should not be whitespace: {value}");
        }
    }

    [Fact]
    public void KnownMessageProperties_CanBeUsedAsKeys_InDictionary()
    {
        // Arrange
        var properties = new Dictionary<string, object>();

        // Act
        properties[KnownMessageProperties.Subject] = "Test Subject";
        properties[KnownMessageProperties.RemoteMessageId] = "remote-123";
        properties[KnownMessageProperties.ReplyTo] = "original-456";
        properties[KnownMessageProperties.CorrelationId] = "correlation-789";

        // Assert
        Assert.Equal("Test Subject", properties[KnownMessageProperties.Subject]);
        Assert.Equal("remote-123", properties[KnownMessageProperties.RemoteMessageId]);
        Assert.Equal("original-456", properties[KnownMessageProperties.ReplyTo]);
        Assert.Equal("correlation-789", properties[KnownMessageProperties.CorrelationId]);
    }
}