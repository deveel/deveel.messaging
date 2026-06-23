using System.ComponentModel.DataAnnotations;

namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "ChannelSchemaExtensions")]
public class ChannelSchemaExtensionsTests
{
    private static IChannelSchema CreateSchema(string provider = "TestProvider", string type = "TestType", string version = "1.0")
        => new ChannelSchemaBuilder(provider, type, version).Build();

    [Fact]
    public void Should_GetLogicalIdentity()
    {
        var schema = CreateSchema("MyProvider", "MyType", "2.0");
        var identity = schema.GetLogicalIdentity();
        Assert.Equal("MyProvider/MyType/2.0", identity);
    }

    [Fact]
    public void Should_Throw_When_GetLogicalIdentityWithNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IChannelSchema)null!).GetLogicalIdentity());
    }

    [Fact]
    public void Should_BeCompatible_When_SameProviderTypeVersion()
    {
        var schema1 = CreateSchema("P", "T", "1.0");
        var schema2 = CreateSchema("P", "T", "1.0");
        Assert.True(schema1.IsCompatibleWith(schema2));
    }

    [Fact]
    public void Should_NotBeCompatible_When_DifferentProvider()
    {
        var schema1 = CreateSchema("P1", "T", "1.0");
        var schema2 = CreateSchema("P2", "T", "1.0");
        Assert.False(schema1.IsCompatibleWith(schema2));
    }

    [Fact]
    public void Should_NotBeCompatible_When_DifferentVersion()
    {
        var schema1 = CreateSchema("P", "T", "1.0");
        var schema2 = CreateSchema("P", "T", "2.0");
        Assert.False(schema1.IsCompatibleWith(schema2));
    }

    [Fact]
    public void Should_Throw_When_IsCompatibleWithNull()
    {
        var schema = CreateSchema();
        Assert.Throws<ArgumentNullException>(() => schema.IsCompatibleWith(null!));
    }

    [Fact]
    public void Should_GetAuthenticationSchemes_When_ConfigsExist()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .AddAuthenticationScheme(AuthenticationScheme.Bearer)
            .AddAuthenticationScheme(AuthenticationScheme.ApiKey)
            .Build();

        var schemes = schema.GetAuthenticationSchemes().ToList();

        Assert.Contains(AuthenticationScheme.Bearer, schemes);
        Assert.Contains(AuthenticationScheme.ApiKey, schemes);
    }

    [Fact]
    public void Should_ReturnEmpty_When_GetAuthenticationSchemesWithNoConfigs()
    {
        var schema = CreateSchema();
        var schemes = schema.GetAuthenticationSchemes().ToList();
        Assert.Empty(schemes);
    }

    [Fact]
    public void Should_SupportAuthenticationScheme_When_ConfigExists()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .AddAuthenticationScheme(AuthenticationScheme.Basic)
            .Build();

        Assert.True(schema.SupportsAuthenticationScheme(AuthenticationScheme.Basic));
        Assert.False(schema.SupportsAuthenticationScheme(AuthenticationScheme.Bearer));
    }

    [Fact]
    public void Should_Throw_When_SupportsAuthenticationSchemeWithNull()
    {
        var schema = CreateSchema();
        Assert.Throws<ArgumentNullException>(() => schema.SupportsAuthenticationScheme(null!));
    }

    [Fact]
    public void Should_ValidateConnectionSettings_When_MissingRequiredParam()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .AddParameter("ApiKey", DataType.String, p => p.IsRequired = true)
            .Build();
        var settings = new ConnectionSettings();

        var errors = schema.ValidateConnectionSettings(settings).ToList();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains("ApiKey"));
    }

    [Fact]
    public void Should_PassValidation_When_RequiredParamProvided()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .AddParameter("ApiKey", DataType.String, p => p.IsRequired = true)
            .Build();
        var settings = new ConnectionSettings();
        settings.SetParameter("ApiKey", "my-key");

        var errors = schema.ValidateConnectionSettings(settings).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Should_ValidateConnectionSettings_When_UnknownParamInStrictMode()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .WithStrictMode()
            .Build();
        var settings = new ConnectionSettings();
        settings.SetParameter("UnknownParam", "value");

        var errors = schema.ValidateConnectionSettings(settings).ToList();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains("UnknownParam"));
    }

    [Fact]
    public void Should_ValidateMessage_When_MissingId()
    {
        var schema = CreateSchema();
        var message = new Message();

        var errors = schema.ValidateMessage(message).ToList();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains("Id"));
    }

    [Fact]
    public void Should_PassMessageValidation_When_ValidMessage()
    {
        var schema = CreateSchema();
        var message = new Message { Id = "msg-1" };

        var errors = schema.ValidateMessage(message).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Should_ValidateMessage_When_MissingRequiredProperty()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .AddMessageProperty("Subject", DataType.String, p => p.IsRequired = true)
            .Build();
        var message = new Message { Id = "msg-1" };

        var errors = schema.ValidateMessage(message).ToList();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains("Subject"));
    }

    [Fact]
    public void Should_ValidateMessage_When_UnknownPropertyInStrictMode()
    {
        var schema = new ChannelSchemaBuilder("P", "T", "1.0")
            .WithStrictMode()
            .Build();
        var message = new Message
        {
            Id = "msg-1",
            Properties = new Dictionary<string, MessageProperty>
            {
                ["UnknownProp"] = new MessageProperty("UnknownProp", "value")
            }
        };

        var errors = schema.ValidateMessage(message).ToList();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains("UnknownProp"));
    }

    [Fact]
    public void Should_ValidateAsRestrictionOf_When_Incompatible()
    {
        var schema1 = CreateSchema("P1", "T", "1.0");
        var schema2 = CreateSchema("P2", "T", "1.0");

        var errors = schema1.ValidateAsRestrictionOf(schema2).ToList();

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Should_PassRestrictionValidation_When_Compatible()
    {
        var schema1 = CreateSchema("P", "T", "1.0");
        var schema2 = CreateSchema("P", "T", "1.0");

        var errors = schema1.ValidateAsRestrictionOf(schema2).ToList();

        Assert.Empty(errors);
    }
}
