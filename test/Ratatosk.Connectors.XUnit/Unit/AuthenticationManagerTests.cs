using Moq;

namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "AuthenticationManager")]
public class AuthenticationManagerTests
{
    [Fact]
    public void Should_RegisterDefaultProviders_When_ConstructedWithNoProviders()
    {
        var manager = new AuthenticationManager();

        // Verify by checking that a known scheme can be handled
        var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
            .WithField("ApiKey", DataType.String, f => f.AuthenticationRole = "principal");
        var settings = new ConnectionSettings();
        settings.SetParameter("ApiKey", "test-key");

        var result = manager.AuthenticateAsync(settings, config).GetAwaiter().GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Should_RegisterProvider_When_RegisterProviderCalled()
    {
        var mockProvider = new Mock<IAuthenticationProvider>();
        var customScheme = new AuthenticationScheme("CustomScheme");
        mockProvider.Setup(x => x.Scheme).Returns(customScheme);
        mockProvider.Setup(x => x.DisplayName).Returns("Custom");
        mockProvider.Setup(x => x.CanHandle(It.IsAny<AuthenticationConfiguration>())).Returns(true);
        mockProvider.Setup(x => x.ObtainCredentialAsync(It.IsAny<ConnectionSettings>(), It.IsAny<AuthenticationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Success(AuthenticationCredential.ForBearerToken("test-token")));

        var manager = new AuthenticationManager();
        manager.RegisterProvider(mockProvider.Object);

        var config = new AuthenticationConfiguration(customScheme, "Custom");
        var settings = new ConnectionSettings();
        var result = manager.AuthenticateAsync(settings, config).GetAwaiter().GetResult();

        Assert.True(result.IsSuccessful);
        Assert.Equal("test-token", result.Credential?.Value);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NoMatchingProvider()
    {
        var customScheme = new AuthenticationScheme("UnsupportedScheme");
        var manager = new AuthenticationManager();
        var config = new AuthenticationConfiguration(customScheme, "Unsupported");
        var settings = new ConnectionSettings();

        var result = await manager.AuthenticateAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Equal("NO_PROVIDER", result.ErrorCode);
    }

    [Fact]
    public void Should_ClearCache_When_ClearCacheCalled()
    {
        var mockProvider = new Mock<IAuthenticationProvider>();
        mockProvider.Setup(x => x.Scheme).Returns(AuthenticationScheme.ApiKey);
        mockProvider.Setup(x => x.DisplayName).Returns("Test");
        mockProvider.Setup(x => x.CanHandle(It.IsAny<AuthenticationConfiguration>())).Returns(true);
        mockProvider.Setup(x => x.ObtainCredentialAsync(It.IsAny<ConnectionSettings>(), It.IsAny<AuthenticationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Success(AuthenticationCredential.ForApiKey("key1")));

        var manager = new AuthenticationManager(new[] { mockProvider.Object });
        var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
            .WithField("ApiKey", DataType.String, f => f.AuthenticationRole = "principal");
        var settings = new ConnectionSettings();
        settings.SetParameter("ApiKey", "test-key");

        // First call caches
        manager.AuthenticateAsync(settings, config).GetAwaiter().GetResult();
        manager.ClearCache();

        // Second call should obtain again
        mockProvider.Verify(x => x.ObtainCredentialAsync(It.IsAny<ConnectionSettings>(), It.IsAny<AuthenticationConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Should_InvalidateCredential_When_InvalidateCredentialCalled()
    {
        var mockProvider = new Mock<IAuthenticationProvider>();
        mockProvider.Setup(x => x.Scheme).Returns(AuthenticationScheme.ApiKey);
        mockProvider.Setup(x => x.DisplayName).Returns("Test");
        mockProvider.Setup(x => x.CanHandle(It.IsAny<AuthenticationConfiguration>())).Returns(true);
        mockProvider.Setup(x => x.ObtainCredentialAsync(It.IsAny<ConnectionSettings>(), It.IsAny<AuthenticationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Success(AuthenticationCredential.ForApiKey("key1")));

        var manager = new AuthenticationManager(new[] { mockProvider.Object });
        var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
            .WithField("ApiKey", DataType.String, f => f.AuthenticationRole = "principal");
        var settings = new ConnectionSettings();
        settings.SetParameter("ApiKey", "test-key");

        manager.AuthenticateAsync(settings, config).GetAwaiter().GetResult();
        manager.InvalidateCredential(settings, config);

        // Second call should obtain again
        manager.AuthenticateAsync(settings, config).GetAwaiter().GetResult();
        mockProvider.Verify(x => x.ObtainCredentialAsync(It.IsAny<ConnectionSettings>(), It.IsAny<AuthenticationConfiguration>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Should_Throw_When_AuthenticateAsyncWithNullSettings()
    {
        var manager = new AuthenticationManager();
        var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key");

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.AuthenticateAsync(null!, config));
    }

    [Fact]
    public async Task Should_Throw_When_AuthenticateAsyncWithNullConfig()
    {
        var manager = new AuthenticationManager();
        var settings = new ConnectionSettings();

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.AuthenticateAsync(settings, null!));
    }

    [Fact]
    public void Should_Throw_When_InvalidateCredentialWithNullSettings()
    {
        var manager = new AuthenticationManager();
        var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key");

        Assert.Throws<ArgumentNullException>(() => manager.InvalidateCredential(null!, config));
    }

    [Fact]
    public void Should_Throw_When_RegisterProviderWithNull()
    {
        var manager = new AuthenticationManager();
        Assert.Throws<ArgumentNullException>(() => manager.RegisterProvider(null!));
    }
}
