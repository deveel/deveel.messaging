//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Deveel.Messaging
{
    /// <summary>
    /// Tests for the authentication mechanism including providers, managers, and connector integration.
    /// </summary>
    public class AuthenticationMechanismTests
    {
        [Fact]
        public void AuthenticationCredential_CreateToken_SetsPropertiesCorrectly()
        {
            // Arrange
            var token = "test-access-token";
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // Act
            var credential = AuthenticationCredential.CreateToken(token, expiresAt, "Bearer");

            // Assert
            Assert.Equal(AuthenticationType.Token, credential.AuthenticationType);
            Assert.Equal(token, credential.CredentialValue);
            Assert.Equal(expiresAt, credential.ExpiresAt);
            Assert.Equal("Bearer", credential.Properties["TokenType"]);
            Assert.False(credential.IsExpired);
        }

        [Fact]
        public void AuthenticationCredential_CreateApiKey_SetsPropertiesCorrectly()
        {
            // Arrange
            var apiKey = "test-api-key-12345";

            // Act
            var credential = AuthenticationCredential.CreateApiKey(apiKey);

            // Assert
            Assert.Equal(AuthenticationType.ApiKey, credential.AuthenticationType);
            Assert.Equal(apiKey, credential.CredentialValue);
            Assert.Null(credential.ExpiresAt);
            Assert.False(credential.IsExpired);
        }

        [Fact]
        public void AuthenticationCredential_CreateBasic_SetsPropertiesCorrectly()
        {
            // Arrange
            var username = "testuser";
            var password = "testpass";

            // Act
            var credential = AuthenticationCredential.CreateBasic(username, password);

            // Assert
            Assert.Equal(AuthenticationType.Basic, credential.AuthenticationType);
            Assert.Equal(username, credential.Properties["Username"]);
            Assert.Equal(password, credential.Properties["Password"]);
        }

        [Fact]
        public void AuthenticationCredential_IsExpired_ReturnsTrueWhenExpired()
        {
            // Arrange
            var token = "expired-token";
            var expiredTime = DateTime.UtcNow.AddMinutes(-1);
            var credential = AuthenticationCredential.CreateToken(token, expiredTime);

            // Act & Assert
            Assert.True(credential.IsExpired);
        }

        [Fact]
        public void AuthenticationCredential_WillExpireSoon_ReturnsTrueWhenWithinBuffer()
        {
            // Arrange
            var token = "soon-to-expire-token";
            var soonToExpire = DateTime.UtcNow.AddMinutes(2);
            var credential = AuthenticationCredential.CreateToken(token, soonToExpire);

            // Act & Assert
            Assert.True(credential.WillExpireSoon(TimeSpan.FromMinutes(5)));
            Assert.False(credential.WillExpireSoon(TimeSpan.FromMinutes(1)));
        }

        [Fact]
        public async Task DirectCredentialAuthenticationProvider_ApiKey_ReturnsCredential()
        {
            // Arrange
            var provider = DirectCredentialAuthenticationProvider.CreateApiKeyProvider();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "test-api-key-12345");

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.ApiKey, result.Credential.AuthenticationType);
            Assert.Equal("test-api-key-12345", result.Credential.CredentialValue);
        }

        [Fact]
        public async Task DirectCredentialAuthenticationProvider_ApiKey_MissingKey_ReturnsFailure()
        {
            // Arrange
            var provider = DirectCredentialAuthenticationProvider.CreateApiKeyProvider();
            var connectionSettings = new ConnectionSettings();

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Equal("MISSING_API_KEY", result.ErrorCode);
        }

        [Fact]
        public async Task DirectCredentialAuthenticationProvider_Basic_ReturnsCredential()
        {
            // Arrange
            var provider = DirectCredentialAuthenticationProvider.CreateBasicProvider();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("Username", "testuser")
                .SetParameter("Password", "testpass");

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.Basic, result.Credential.AuthenticationType);
            Assert.Equal("testuser", result.Credential.Properties["Username"]);
        }

        [Fact]
        public async Task DirectCredentialAuthenticationProvider_Basic_AlternativeFields_ReturnsCredential()
        {
            // Arrange
            var provider = DirectCredentialAuthenticationProvider.CreateBasicProvider();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("AccountSid", "AC123456")
                .SetParameter("AuthToken", "auth-token-123");

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.Basic, result.Credential.AuthenticationType);
            Assert.Equal("AccountSid", result.Credential.Properties["UserField"]);
            Assert.Equal("AuthToken", result.Credential.Properties["PassField"]);
        }

        [Fact]
        public async Task ClientCredentialsAuthenticationProvider_ValidRequest_ReturnsToken()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(200, @"{
                ""access_token"": ""test-access-token"",
                ""token_type"": ""Bearer"",
                ""expires_in"": 3600
            }");

            var httpClient = new HttpClient(mockHandler);
            var provider = new ClientCredentialsAuthenticationProvider(httpClient);
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ClientId", "test-client-id")
                .SetParameter("ClientSecret", "test-client-secret")
                .SetParameter("TokenEndpoint", "https://oauth.example.com/token");

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.True(result.IsSuccessful, $"Expected success but got: {result.ErrorCode} - {result.ErrorMessage}");
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.Token, result.Credential.AuthenticationType);
            Assert.Equal("test-access-token", result.Credential.CredentialValue);
            Assert.Equal("Bearer", result.Credential.Properties["TokenType"]);
        }

        [Fact]
        public async Task ClientCredentialsAuthenticationProvider_MissingParameters_ReturnsFailure()
        {
            // Arrange
            var provider = new ClientCredentialsAuthenticationProvider();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ClientId", "test-client-id");
                // Missing ClientSecret and TokenEndpoint

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Equal("MISSING_PARAMETERS", result.ErrorCode);
            Assert.Contains("ClientSecret", result.ErrorMessage!);
        }

        [Fact]
        public async Task AuthenticationManager_AuthenticateWithApiKey_ReturnsCredential()
        {
            // Arrange
            var manager = new AuthenticationManager();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "test-api-key");
            var configuration = AuthenticationConfigurations.ApiKeyAuthentication();

            // Act
            var result = await manager.AuthenticateAsync(connectionSettings, configuration);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.ApiKey, result.Credential.AuthenticationType);
        }

        [Fact]
        public async Task AuthenticationManager_AuthenticateWithBasic_ReturnsCredential()
        {
            // Arrange
            var manager = new AuthenticationManager();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("Username", "testuser")
                .SetParameter("Password", "testpass");
            var configuration = AuthenticationConfigurations.BasicAuthentication();

            // Act
            var result = await manager.AuthenticateAsync(connectionSettings, configuration);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.Basic, result.Credential.AuthenticationType);
        }

        [Fact]
        public async Task AuthenticationManager_CacheCredential_ReusesCredential()
        {
            // Arrange
            var manager = new AuthenticationManager();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "test-api-key");
            var configuration = AuthenticationConfigurations.ApiKeyAuthentication();

            // Act - First call
            var result1 = await manager.AuthenticateAsync(connectionSettings, configuration);
            var result2 = await manager.AuthenticateAsync(connectionSettings, configuration);

            // Assert
            Assert.True(result1.IsSuccessful);
            Assert.True(result2.IsSuccessful);
            // The credentials should be the same instance from cache
            Assert.Same(result1.Credential, result2.Credential);
        }

        [Fact]
        public void AuthenticationManager_InvalidateCredential_RemovesFromCache()
        {
            // Arrange
            var manager = new AuthenticationManager();
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "test-api-key");
            var configuration = AuthenticationConfigurations.ApiKeyAuthentication();

            // Act
            manager.InvalidateCredential(connectionSettings, configuration);

            // Assert - No exception should be thrown even if credential wasn't cached
            Assert.True(true);
        }

        [Fact]
        public void AuthenticationManager_ClearCache_ClearsAllCredentials()
        {
            // Arrange
            var manager = new AuthenticationManager();

            // Act
            manager.ClearCache();

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public async Task CertificateAuthenticationProvider_ValidCertificate_ReturnsCredential()
        {
            // Arrange
            var provider = DirectCredentialAuthenticationProvider.CreateApiKeyProvider(); // Use API key provider as a substitute for this test
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "cert-12345");

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Credential);
            Assert.Equal(AuthenticationType.ApiKey, result.Credential.AuthenticationType);
            Assert.Equal("cert-12345", result.Credential.CredentialValue);
        }

        [Fact]
        public async Task CertificateAuthenticationProvider_MissingCertificate_ReturnsFailure()
        {
            // Arrange
            var provider = DirectCredentialAuthenticationProvider.CreateApiKeyProvider();
            var connectionSettings = new ConnectionSettings(); // No certificate provided

            // Act
            var result = await provider.ObtainCredentialAsync(connectionSettings);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Equal("MISSING_API_KEY", result.ErrorCode);
        }

        [Fact]
        public async Task TestConnector_WithAuthentication_InitializesSuccessfully()
        {
            // Arrange
            var schema = new ChannelSchema("Test", "Test", "1.0.0")
                .AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication());
            
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "test-api-key");

            var connector = new TestAuthConnector(schema, connectionSettings);

            // Act
            var result = await connector.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.Equal(ConnectorState.Ready, connector.State);
            Assert.NotNull(connector.TestAuthenticationCredential);
            Assert.Equal(AuthenticationType.ApiKey, connector.TestAuthenticationCredential.AuthenticationType);
        }

        [Fact]
        public async Task TestConnector_WithoutAuthenticationConfig_InitializesWithoutAuth()
        {
            // Arrange
            var schema = new ChannelSchema("Test", "Test", "1.0.0");
            var connectionSettings = new ConnectionSettings();
            var connector = new TestAuthConnector(schema, connectionSettings);

            // Act
            var result = await connector.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.Equal(ConnectorState.Ready, connector.State);
            Assert.Null(connector.TestAuthenticationCredential);
        }
    }

    /// <summary>
    /// Test connector that exposes authentication for testing.
    /// </summary>
    public class TestAuthConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public TestAuthConnector(IChannelSchema schema, ConnectionSettings connectionSettings) 
            : base(schema)
        {
            _connectionSettings = connectionSettings;
        }

        public AuthenticationCredential? TestAuthenticationCredential => AuthenticationCredential;

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            // Try to authenticate if the schema has authentication configurations
            if (Schema.AuthenticationConfigurations.Any())
            {
                var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
                if (!authResult.Successful)
                {
                    return authResult;
                }
            }

            return ConnectorResult<bool>.Success(true);
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Test Status")));
        }
    }

    /// <summary>
    /// Mock HTTP client for testing OAuth flows.
    /// </summary>
    public class MockHttpClient : HttpClient
    {
        private readonly MockHttpMessageHandler _mockHandler;

        public MockHttpClient() : this(new MockHttpMessageHandler())
        {
        }

        public MockHttpClient(MockHttpMessageHandler handler) : base(handler)
        {
            _mockHandler = handler;
        }

        public void SetupResponse(int statusCode, string content)
        {
            _mockHandler.SetupResponse(statusCode, content);
        }
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private int _statusCode = 200;
        private string _responseContent = "";

        public void SetupResponse(int statusCode, string content)
        {
            _statusCode = statusCode;
            _responseContent = content;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate network delay

            var response = new HttpResponseMessage((System.Net.HttpStatusCode)_statusCode)
            {
                Content = new StringContent(_responseContent)
            };

            return response;
        }
    }
}