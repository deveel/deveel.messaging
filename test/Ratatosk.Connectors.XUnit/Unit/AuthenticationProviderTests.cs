using Microsoft.Extensions.Logging.Abstractions;

namespace Ratatosk
{
    [Trait("Category", "Unit")]
    [Trait("Feature", "AuthenticationProvider")]
    public class AuthenticationProviderTests
    {
        private class TestAuthenticationProvider : AuthenticationProviderBase
        {
            public TestAuthenticationProvider()
                : base(AuthenticationScheme.ApiKey, "Test API Key")
            {
            }

            public override Task<AuthenticationResult> ObtainCredentialAsync(
                ConnectionSettings connectionSettings,
                AuthenticationConfiguration configuration,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Success(AuthenticationCredential.ForApiKey("test-key")));
            }

            public AuthenticationResult CallFailure(string msg, string? code = null)
                => Failure(msg, code);

            public AuthenticationResult CallSuccess(AuthenticationCredential c)
                => Success(c);
        }

        [Fact]
        public void Should_SetSchemeAndDisplayName_When_Constructed()
        {
            var provider = new TestAuthenticationProvider();
            Assert.Equal(AuthenticationScheme.ApiKey, provider.Scheme);
            Assert.Equal("Test API Key", provider.DisplayName);
        }

        private class NullSchemeProvider : AuthenticationProviderBase
        {
            public NullSchemeProvider() : base(null!, "Test") { }
            public override Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_SchemeIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NullSchemeProvider());
        }

        private class NullDisplayNameProvider : AuthenticationProviderBase
        {
            public NullDisplayNameProvider() : base(AuthenticationScheme.ApiKey, null!) { }
            public override Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_DisplayNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NullDisplayNameProvider());
        }

        [Fact]
        public void Should_ReturnTrue_When_CanHandleMatchingScheme()
        {
            var provider = new TestAuthenticationProvider();
            var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key");
            Assert.True(provider.CanHandle(config));
        }

        [Fact]
        public void Should_ReturnFalse_When_CanHandleNonMatchingScheme()
        {
            var provider = new TestAuthenticationProvider();
            var config = new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Bearer");
            Assert.False(provider.CanHandle(config));
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_CanHandleWithNullConfig()
        {
            var provider = new TestAuthenticationProvider();
            Assert.Throws<ArgumentNullException>(() => provider.CanHandle(null!));
        }

        [Fact]
        public async Task Should_FallbackToObtainCredential_When_RefreshCredentialCalled()
        {
            var provider = new TestAuthenticationProvider();
            var settings = new ConnectionSettings();
            var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key");
            var credential = AuthenticationCredential.ForApiKey("old-key");

            var result = await provider.RefreshCredentialAsync(credential, settings, config);

            Assert.True(result.IsSuccessful);
            Assert.Equal("test-key", result.Credential?.Value);
        }

        [Fact]
        public void Should_CreateFailureResult()
        {
            var provider = new TestAuthenticationProvider();
            var result = provider.CallFailure("Something went wrong", "ERR_001");

            Assert.False(result.IsSuccessful);
            Assert.Equal("Something went wrong", result.ErrorMessage);
            Assert.Equal("ERR_001", result.ErrorCode);
        }

        [Fact]
        public void Should_CreateFailureResult_WithoutErrorCode()
        {
            var provider = new TestAuthenticationProvider();
            var result = provider.CallFailure("Failed");

            Assert.False(result.IsSuccessful);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void Should_CreateSuccessResult()
        {
            var provider = new TestAuthenticationProvider();
            var credential = AuthenticationCredential.ForApiKey("my-key");
            var result = provider.CallSuccess(credential);

            Assert.True(result.IsSuccessful);
            Assert.Same(credential, result.Credential);
        }

        [Fact]
        public async Task Should_ObtainApiKey_When_ConnectionHasKey()
        {
            var provider = new ApiKeyAuthenticationProvider(NullLogger<ApiKeyAuthenticationProvider>.Instance);
            var settings = new ConnectionSettings();
            settings.SetParameter("ApiKey", "my-api-key");
            var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
                .WithField("ApiKey", DataType.String, f => f.AuthenticationRole = "principal");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.Equal("my-api-key", result.Credential?.Value);
        }

        [Fact]
        public async Task Should_ObtainApiKey_UsingFirstMatchingField()
        {
            var provider = new ApiKeyAuthenticationProvider();
            var settings = new ConnectionSettings();
            settings.SetParameter("Key", "found-key");
            var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
                .WithField("ApiKey", DataType.String, f => f.AuthenticationRole = "principal")
                .WithField("Key", DataType.String, f => f.AuthenticationRole = "principal");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.Equal("found-key", result.Credential?.Value);
        }

        [Fact]
        public async Task Should_Fail_When_NoApiKeyFound()
        {
            var provider = new ApiKeyAuthenticationProvider();
            var settings = new ConnectionSettings();
            var config = new AuthenticationConfiguration(AuthenticationScheme.ApiKey, "API Key")
                .WithField("ApiKey", DataType.String, f => f.AuthenticationRole = "principal");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.False(result.IsSuccessful);
            Assert.Contains("ApiKey", result.ErrorMessage);
        }

        [Fact]
        public async Task Should_ObtainBasicCredentials()
        {
            var provider = new BasicAuthenticationProvider(NullLogger<BasicAuthenticationProvider>.Instance);
            var settings = new ConnectionSettings();
            settings.SetParameter("Username", "admin");
            settings.SetParameter("Password", "secret");
            var config = new AuthenticationConfiguration(AuthenticationScheme.Basic, "Basic")
                .WithField("Username", DataType.String, f => f.AuthenticationRole = "principal")
                .WithField("Password", DataType.String, f => f.AuthenticationRole = "credential");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
        }

        [Fact]
        public async Task Should_FailBasicAuth_When_UsernameMissing()
        {
            var provider = new BasicAuthenticationProvider();
            var settings = new ConnectionSettings();
            settings.SetParameter("Password", "secret");
            var config = new AuthenticationConfiguration(AuthenticationScheme.Basic, "Basic")
                .WithField("Username", DataType.String, f => f.AuthenticationRole = "principal")
                .WithField("Password", DataType.String, f => f.AuthenticationRole = "credential");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public async Task Should_FailBasicAuth_When_PasswordMissing()
        {
            var provider = new BasicAuthenticationProvider();
            var settings = new ConnectionSettings();
            settings.SetParameter("Username", "admin");
            var config = new AuthenticationConfiguration(AuthenticationScheme.Basic, "Basic")
                .WithField("Username", DataType.String, f => f.AuthenticationRole = "principal")
                .WithField("Password", DataType.String, f => f.AuthenticationRole = "credential");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public async Task Should_ObtainBasicCredentials_WithMultipleFieldCombinations()
        {
            var provider = new BasicAuthenticationProvider();
            var settings = new ConnectionSettings();
            settings.SetParameter("AccountSid", "sid123");
            settings.SetParameter("Password", "secret");
            var config = new AuthenticationConfiguration(AuthenticationScheme.Basic, "Basic")
                .WithField("Username", DataType.String, f => f.AuthenticationRole = "principal")
                .WithField("AccountSid", DataType.String, f => f.AuthenticationRole = "principal")
                .WithField("Password", DataType.String, f => f.AuthenticationRole = "credential")
                .WithField("AuthToken", DataType.String, f => f.AuthenticationRole = "credential");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal("Password", result.Credential.Properties["PassField"]);
        }

        [Fact]
        public async Task Should_ObtainBearerToken()
        {
            var provider = new BearerTokenAuthenticationProvider(NullLogger<BearerTokenAuthenticationProvider>.Instance);
            var settings = new ConnectionSettings();
            settings.SetParameter("AccessToken", "my-token");
            var config = new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Bearer")
                .WithField("AccessToken", DataType.String, f => f.AuthenticationRole = "principal");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.Equal("my-token", result.Credential?.Value);
            Assert.Equal("Bearer", result.Credential?.Properties["TokenType"]);
        }

        [Fact]
        public async Task Should_ObtainBearerToken_WithCustomTokenType()
        {
            var provider = new BearerTokenAuthenticationProvider();
            var settings = new ConnectionSettings();
            settings.SetParameter("Token", "my-token");
            settings.SetParameter("TokenType", "JWT");
            var config = new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Bearer")
                .WithField("Token", DataType.String, f => f.AuthenticationRole = "principal");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.Equal("JWT", result.Credential?.Properties["TokenType"]);
        }

        [Fact]
        public async Task Should_ObtainBearerToken_WithExpiration()
        {
            var provider = new BearerTokenAuthenticationProvider();
            var expiresAt = DateTime.UtcNow.AddHours(1);
            var settings = new ConnectionSettings();
            settings.SetParameter("BearerToken", "my-token");
            settings.SetParameter("ExpiresAt", expiresAt.ToString("O"));
            var config = new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Bearer")
                .WithField("BearerToken", DataType.String, f => f.AuthenticationRole = "principal");

            var result = await provider.ObtainCredentialAsync(settings, config);

            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential?.ExpiresAt);
        }

    [Fact]
    public async Task Should_FailBearerToken_When_NoTokenFound()
    {
        var provider = new BearerTokenAuthenticationProvider();
        var settings = new ConnectionSettings();
        var config = new AuthenticationConfiguration(AuthenticationScheme.Bearer, "Bearer")
            .WithField("Token", DataType.String, f => f.AuthenticationRole = "principal");

        var result = await provider.ObtainCredentialAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Contains("Token", result.ErrorMessage);
    }

    [Fact]
    public async Task Should_FailClientCredentials_When_MissingClientId()
    {
        var provider = new ClientCredentialsAuthenticationProvider();
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientSecret", "secret");
        settings.SetParameter("TokenEndpoint", "https://example.com/token");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var result = await provider.ObtainCredentialAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Equal("MISSING_PARAMETERS", result.ErrorCode);
    }

    [Fact]
    public async Task Should_FailClientCredentials_When_MissingTokenEndpoint()
    {
        var provider = new ClientCredentialsAuthenticationProvider();
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientId", "client-id");
        settings.SetParameter("ClientSecret", "secret");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var result = await provider.ObtainCredentialAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Equal("MISSING_TOKEN_ENDPOINT", result.ErrorCode);
    }

    [Fact]
    public async Task Should_FailClientCredentials_When_NonSuccessResponse()
    {
        var httpHandler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid_client\"}")
            });
        var httpClient = new HttpClient(httpHandler);
        var provider = new ClientCredentialsAuthenticationProvider(httpClient);
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientId", "client-id");
        settings.SetParameter("ClientSecret", "secret");
        settings.SetParameter("TokenEndpoint", "https://example.com/token");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var result = await provider.ObtainCredentialAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Equal("TOKEN_REQUEST_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task Should_FailClientCredentials_When_MissingAccessToken()
    {
        var httpHandler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token_type\":\"Bearer\"}")
            });
        var httpClient = new HttpClient(httpHandler);
        var provider = new ClientCredentialsAuthenticationProvider(httpClient);
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientId", "client-id");
        settings.SetParameter("ClientSecret", "secret");
        settings.SetParameter("TokenEndpoint", "https://example.com/token");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var result = await provider.ObtainCredentialAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Equal("INVALID_TOKEN_RESPONSE", result.ErrorCode);
    }

    [Fact]
    public async Task Should_FailClientCredentials_When_HttpRequestException()
    {
        var httpHandler = new MockHttpMessageHandler(req =>
            throw new HttpRequestException("Network error"));
        var httpClient = new HttpClient(httpHandler);
        var provider = new ClientCredentialsAuthenticationProvider(httpClient);
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientId", "client-id");
        settings.SetParameter("ClientSecret", "secret");
        settings.SetParameter("TokenEndpoint", "https://example.com/token");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var result = await provider.ObtainCredentialAsync(settings, config);

        Assert.False(result.IsSuccessful);
        Assert.Equal("NETWORK_ERROR", result.ErrorCode);
    }

    [Fact]
    public async Task Should_RefreshClientCredentials_When_RefreshTokenAvailable()
    {
        var httpHandler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"new-token\",\"expires_in\":3600}")
            });
        var httpClient = new HttpClient(httpHandler);
        var provider = new ClientCredentialsAuthenticationProvider(httpClient);
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientId", "client-id");
        settings.SetParameter("ClientSecret", "secret");
        settings.SetParameter("TokenEndpoint", "https://example.com/token");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var credential = AuthenticationCredential.ForClientCredentials("old-token", null, "Bearer");
        credential.Properties["RefreshToken"] = "refresh-token";

        var result = await provider.RefreshCredentialAsync(credential, settings, config);

        Assert.True(result.IsSuccessful);
        Assert.Equal("new-token", result.Credential?.Value);
    }

    [Fact]
    public async Task Should_FallbackToObtain_When_RefreshClientCredentialsWithoutRefreshToken()
    {
        var httpHandler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"new-token\",\"expires_in\":3600}")
            });
        var httpClient = new HttpClient(httpHandler);
        var provider = new ClientCredentialsAuthenticationProvider(httpClient);
        var settings = new ConnectionSettings();
        settings.SetParameter("ClientId", "client-id");
        settings.SetParameter("ClientSecret", "secret");
        settings.SetParameter("TokenEndpoint", "https://example.com/token");
        var config = new AuthenticationConfiguration(AuthenticationScheme.OAuthClientCredentials, "OAuth")
            .WithField("ClientId", DataType.String, f => f.AuthenticationRole = "principal")
            .WithField("ClientSecret", DataType.String, f => f.AuthenticationRole = "credential");

        var credential = AuthenticationCredential.ForClientCredentials("old-token", null, "Bearer");

        var result = await provider.RefreshCredentialAsync(credential, settings, config);

        Assert.True(result.IsSuccessful);
        Assert.Equal("new-token", result.Credential?.Value);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
}
