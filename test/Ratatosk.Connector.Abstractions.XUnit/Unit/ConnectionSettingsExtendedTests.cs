namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "ConnectionSettings")]
public class ConnectionSettingsExtendedTests
{
    [Fact]
    public void Should_GetTypedParameter_When_Called()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("Active", true);
        var result = settings.GetParameter<bool>("Active");
        Assert.True(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ParameterNotFound()
    {
        var settings = new ConnectionSettings();
        var result = settings.GetParameter<string>("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void Should_ConvertTo_When_TypeIsCompatible()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("count", 42);
        var result = settings.GetParameter<int>("count");
        Assert.Equal(42, result);
    }

    [Fact]
    public void Should_Throw_When_TypeIncompatible()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("value", "not-a-number");
        Assert.Throws<InvalidCastException>(() => settings.GetParameter<int>("value"));
    }

    [Fact]
    public void Should_GetDefaultSender_When_DefaultSenderNameProvided()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("DefaultSenderName", "MySender");
        settings.SetParameter("DefaultSenderAddress", "+1234567890");
        settings.SetParameter("DefaultSenderType", "PhoneNumber");

        var sender = settings.GetDefaultSender();

        Assert.NotNull(sender);
        Assert.Equal("MySender", sender.Name);
        Assert.Equal("+1234567890", sender.Address);
        Assert.Equal(EndpointType.PhoneNumber, sender.Type);
    }

    [Fact]
    public void Should_GetDefaultSender_When_FromProvided()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("From", "sender@example.com");

        var sender = settings.GetDefaultSender();

        Assert.NotNull(sender);
        Assert.Equal("sender@example.com", sender.Address);
        Assert.Equal(EndpointType.Any, sender.Type);
    }

    [Fact]
    public void Should_ReturnNull_When_NoDefaultSenderConfigured()
    {
        var settings = new ConnectionSettings();
        var sender = settings.GetDefaultSender();
        Assert.Null(sender);
    }

    [Fact]
    public void Should_CopySettings_When_CopyConstructorUsed()
    {
        var original = new ConnectionSettings();
        original.SetParameter("Key1", "value1");
        original.SetParameter("Key2", 42);

        var copy = new ConnectionSettings(original);

        Assert.Equal("value1", copy.GetParameter("Key1"));
        Assert.Equal(42, copy.GetParameter<int>("Key2"));
    }

    [Fact]
    public void Should_Throw_When_SetParameterWithUnsupportedKey()
    {
        var schema = new ChannelSchemaBuilder("Test", "channel", "1.0")
            .AddParameter("AllowedParam", DataType.String, p => { })
            .Build();
        var settings = new ConnectionSettings(schema);

        Assert.Throws<ArgumentException>(() => settings.SetParameter("UnknownParam", "value"));
    }

    [Fact]
    public void Should_Throw_When_SetParameterWithIncompatibleType()
    {
        var schema = new ChannelSchemaBuilder("Test", "channel", "1.0")
            .AddParameter("Count", DataType.Integer, p => { })
            .Build();
        var settings = new ConnectionSettings(schema);

        Assert.Throws<ArgumentException>(() => settings.SetParameter("Count", "not-a-number"));
    }

    [Fact]
    public void Should_Throw_When_SetParameterWithDisallowedValue()
    {
        var schema = new ChannelSchemaBuilder("Test", "channel", "1.0")
            .AddParameter("Mode", DataType.String, p => p.AllowedValues = new object[] { "active", "passive" })
            .Build();
        var settings = new ConnectionSettings(schema);

        Assert.Throws<ArgumentException>(() => settings.SetParameter("Mode", "invalid"));
    }

    [Fact]
    public void Should_Throw_When_SetRequiredParameterToNull()
    {
        var schema = new ChannelSchemaBuilder("Test", "channel", "1.0")
            .AddParameter("RequiredKey", DataType.String, p => p.IsRequired = true)
            .Build();
        var settings = new ConnectionSettings(schema);

        Assert.Throws<ArgumentException>(() => settings.SetParameter("RequiredKey", null));
    }

    [Fact]
    public void Should_ReturnDefaultFromSchema_When_ParameterNotFound()
    {
        var schema = new ChannelSchemaBuilder("Test", "channel", "1.0")
            .AddParameter("Timeout", DataType.Integer, p => p.DefaultValue = 30)
            .Build();
        var settings = new ConnectionSettings(schema);

        var value = settings.GetParameter("Timeout");

        Assert.Equal(30, value);
    }

    [Fact]
    public void Should_ThrowInvalidCast_When_GetParameterWithIncompatibleType()
    {
        var schema = new ChannelSchemaBuilder("Test", "channel", "1.0")
            .AddParameter("Flag", DataType.Boolean, p => { })
            .Build();
        var settings = new ConnectionSettings(schema);
        settings.SetParameter("Flag", true);

        Assert.Throws<InvalidCastException>(() => settings.GetParameter<int>("Flag"));
    }

    [Fact]
    public void Should_ParseConnectionString_WithQuotedValues()
    {
        var settings = ConnectionSettings.Parse("Key1=value1;Key2='quoted;value';Key3=\"double;quoted\"");

        Assert.Equal("value1", settings.GetParameter("Key1"));
        Assert.Equal("quoted;value", settings.GetParameter("Key2"));
        Assert.Equal("double;quoted", settings.GetParameter("Key3"));
    }

    [Fact]
    public void Should_ParseConnectionString_WithKeyOnly()
    {
        var settings = ConnectionSettings.Parse("Key1;Key2=value2");

        Assert.Equal("", settings.GetParameter("Key1"));
        Assert.Equal("value2", settings.GetParameter("Key2"));
    }

    [Fact]
    public void Should_ParseConnectionString_WithWhitespace()
    {
        var settings = ConnectionSettings.Parse("  Key1 = value1  ;  Key2 = value2  ");

        Assert.Equal("value1", settings.GetParameter("Key1"));
        Assert.Equal("value2", settings.GetParameter("Key2"));
    }

    [Fact]
    public void Should_Throw_When_ParseNullConnectionString()
    {
        Assert.Throws<ArgumentException>(() => ConnectionSettings.Parse(null!));
    }

    [Fact]
    public void Should_Throw_When_ParseEmptyConnectionString()
    {
        Assert.Throws<ArgumentException>(() => ConnectionSettings.Parse(""));
    }
}
