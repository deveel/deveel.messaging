//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Example demonstrating how to use the authentication mechanism with different connector types.
    /// This class shows real-world usage patterns for the authentication framework.
    /// </summary>
    public static class AuthenticationExamples
    {
        /// <summary>
        /// Example 1: Email connector with API key authentication
        /// </summary>
        public static async Task<IChannelConnector> CreateEmailConnectorWithApiKey()
        {
            // Define schema with API key authentication
            var schema = new ChannelSchema("EmailProvider", "Email", "1.0.0")
                .WithCapability(ChannelCapability.SendMessages)
                .AddContentType(MessageContentType.PlainText)
                .AddContentType(MessageContentType.Html)
                .HandlesMessageEndpoint(EndpointType.EmailAddress, e =>
                {
                    e.CanSend = true;
                    e.CanReceive = false;
                })
                .AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication());

            // Connection settings with API key
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "sk-live-email-api-key-12345");

            // Create connector - authentication happens during initialization
            var connector = new ExampleEmailConnector(schema, connectionSettings);
            
            // Initialize (triggers authentication)
            var initResult = await connector.InitializeAsync(CancellationToken.None);
            if (!initResult.Successful)
            {
                throw new InvalidOperationException($"Failed to initialize connector: {initResult.Error?.ErrorMessage}");
            }

            return connector;
        }

        /// <summary>
        /// Example 2: SMS connector with basic authentication (Twilio-style)
        /// </summary>
        public static async Task<IChannelConnector> CreateSmsConnectorWithBasicAuth()
        {
            // Define schema with Twilio-style basic authentication
            var schema = new ChannelSchema("SmsProvider", "SMS", "1.0.0")
                .WithCapability(ChannelCapability.SendMessages)
                .WithCapability(ChannelCapability.MessageStatusQuery)
                .AddContentType(MessageContentType.PlainText)
                .HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
                {
                    e.CanSend = true;
                    e.CanReceive = false;
                })
                .AddAuthenticationConfiguration(AuthenticationConfigurations.TwilioBasicAuthentication());

            // Connection settings with Twilio-style credentials
            var connectionSettings = new ConnectionSettings()
                .SetParameter("AccountSid", "AC1234567890123456789012345678901234")
                .SetParameter("AuthToken", "your_auth_token_here");

            // Create connector
            var connector = new ExampleSmsConnector(schema, connectionSettings);
            
            // Initialize (triggers authentication)
            var initResult = await connector.InitializeAsync(CancellationToken.None);
            if (!initResult.Successful)
            {
                throw new InvalidOperationException($"Failed to initialize connector: {initResult.Error?.ErrorMessage}");
            }

            return connector;
        }

        /// <summary>
        /// Example 3: Push notification connector with OAuth 2.0 client credentials
        /// </summary>
        public static async Task<IChannelConnector> CreatePushConnectorWithOAuth()
        {
            // Define schema with OAuth 2.0 client credentials
            var schema = new ChannelSchema("PushProvider", "Push", "1.0.0")
                .WithCapability(ChannelCapability.SendMessages)
                .WithCapability(ChannelCapability.BulkMessaging)
                .AddContentType(MessageContentType.Json)
                .HandlesMessageEndpoint(EndpointType.DeviceId, e =>
                {
                    e.CanSend = true;
                    e.CanReceive = false;
                })
                .AddAuthenticationConfiguration(AuthenticationConfigurations.ClientCredentialsAuthentication());

            // Connection settings with OAuth credentials
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ClientId", "oauth-client-id-123")
                .SetParameter("ClientSecret", "oauth-client-secret-456")
                .SetParameter("TokenEndpoint", "https://auth.pushprovider.com/oauth/token")
                .SetParameter("Scope", "push.send");

            // Create connector
            var connector = new ExamplePushConnector(schema, connectionSettings);
            
            // Initialize (triggers OAuth flow to get access token)
            var initResult = await connector.InitializeAsync(CancellationToken.None);
            if (!initResult.Successful)
            {
                throw new InvalidOperationException($"Failed to initialize connector: {initResult.Error?.ErrorMessage}");
            }

            return connector;
        }

        /// <summary>
        /// Example 4: Flexible connector supporting multiple authentication methods
        /// </summary>
        public static async Task<IChannelConnector> CreateFlexibleConnector()
        {
            // Define schema with multiple authentication options
            var schema = new ChannelSchema("FlexibleProvider", "Multi", "1.0.0")
                .WithCapability(ChannelCapability.SendMessages)
                .AddContentType(MessageContentType.PlainText)
                .HandlesMessageEndpoint(EndpointType.Any, e =>
                {
                    e.CanSend = true;
                    e.CanReceive = false;
                })
                // Add multiple authentication configurations
                .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleApiKeyAuthentication())
                .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleBasicAuthentication())
                .AddAuthenticationConfiguration(AuthenticationConfigurations.FlexibleTokenAuthentication());

            // Connection settings - the connector will pick the first matching authentication method
            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "flexible-api-key-789"); // Will use API key auth

            // Alternative: use basic auth instead
            // var connectionSettings = new ConnectionSettings()
            //     .SetParameter("Username", "user123")
            //     .SetParameter("Password", "pass456");

            // Create connector
            var connector = new ExampleFlexibleConnector(schema, connectionSettings);
            
            // Initialize (automatically detects and uses appropriate authentication)
            var initResult = await connector.InitializeAsync(CancellationToken.None);
            if (!initResult.Successful)
            {
                throw new InvalidOperationException($"Failed to initialize connector: {initResult.Error?.ErrorMessage}");
            }

            return connector;
        }

        /// <summary>
        /// Example 5: Using a custom authentication manager with custom providers
        /// </summary>
        public static async Task<IChannelConnector> CreateConnectorWithCustomAuthentication()
        {
            // Create custom authentication manager
            var authManager = new AuthenticationManager();
            
            // Register custom providers
            authManager.RegisterProvider(DirectCredentialAuthenticationProvider.CreateApiKeyProvider());
            authManager.RegisterProvider(new ClientCredentialsAuthenticationProvider());
            
            // Define schema
            var schema = new ChannelSchema("CustomProvider", "API", "1.0.0")
                .WithCapability(ChannelCapability.SendMessages)
                .AddAuthenticationConfiguration(AuthenticationConfigurations.ApiKeyAuthentication());

            var connectionSettings = new ConnectionSettings()
                .SetParameter("ApiKey", "custom-api-key-123");

            // Create connector with custom authentication manager
            var connector = new ExampleCustomConnector(schema, connectionSettings, authManager);
            
            var initResult = await connector.InitializeAsync(CancellationToken.None);
            if (!initResult.Successful)
            {
                throw new InvalidOperationException($"Failed to initialize connector: {initResult.Error?.ErrorMessage}");
            }

            return connector;
        }

        /// <summary>
        /// Example 6: How to refresh authentication when tokens expire
        /// </summary>
        public static async Task DemonstrateTokenRefresh()
        {
            var schema = new ChannelSchema("RefreshProvider", "API", "1.0.0")
                .AddAuthenticationConfiguration(AuthenticationConfigurations.TokenAuthentication());

            var connectionSettings = new ConnectionSettings()
                .SetParameter("Token", "initial-token-123");

            var connector = new ExampleRefreshableConnector(schema, connectionSettings);
            
            // Initial authentication
            await connector.InitializeAsync(CancellationToken.None);
            
            // Simulate token expiration and refresh
            await connector.RefreshAuthenticationIfNeeded();
            
            // Continue using the connector with refreshed token
            // (In a real scenario, the refresh would happen automatically)
        }
    }

    #region Example Connector Implementations

    /// <summary>
    /// Example email connector implementation.
    /// </summary>
    public class ExampleEmailConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public ExampleEmailConnector(IChannelSchema schema, ConnectionSettings connectionSettings)
            : base(schema)
        {
            _connectionSettings = connectionSettings;
        }

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            // Authenticate using the provided connection settings
            var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
            if (!authResult.Successful)
            {
                return authResult;
            }

            // Additional initialization logic here...
            return ConnectorResult<bool>.Success(true);
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            // Test connection logic
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            // Use GetApiKey() to get the authenticated API key for requests
            var apiKey = GetApiKey();
            
            // Send email logic here using the API key
            await Task.Delay(10, cancellationToken); // Simulate sending
            
            var result = new SendResult(message.Id, $"email-{Guid.NewGuid()}");
            return ConnectorResult<SendResult>.Success(result);
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Email Connector Ready")));
        }
    }

    /// <summary>
    /// Example SMS connector implementation.
    /// </summary>
    public class ExampleSmsConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public ExampleSmsConnector(IChannelSchema schema, ConnectionSettings connectionSettings)
            : base(schema)
        {
            _connectionSettings = connectionSettings;
        }

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
            return authResult.Successful ? ConnectorResult<bool>.Success(true) : authResult;
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            // Use GetAuthenticationHeader() for HTTP Basic auth
            var authHeader = GetAuthenticationHeader();
            
            // Send SMS logic here using the auth header
            await Task.Delay(10, cancellationToken);
            
            var result = new SendResult(message.Id, $"sms-{Guid.NewGuid()}");
            return ConnectorResult<SendResult>.Success(result);
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("SMS Connector Ready")));
        }
    }

    /// <summary>
    /// Example push notification connector implementation.
    /// </summary>
    public class ExamplePushConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public ExamplePushConnector(IChannelSchema schema, ConnectionSettings connectionSettings)
            : base(schema)
        {
            _connectionSettings = connectionSettings;
        }

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
            return authResult.Successful ? ConnectorResult<bool>.Success(true) : authResult;
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            // Use GetAuthenticationHeader() for Bearer token
            var authHeader = GetAuthenticationHeader();
            
            // Send push notification logic here
            await Task.Delay(10, cancellationToken);
            
            var result = new SendResult(message.Id, $"push-{Guid.NewGuid()}");
            return ConnectorResult<SendResult>.Success(result);
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Push Connector Ready")));
        }
    }

    /// <summary>
    /// Example flexible connector that supports multiple authentication methods.
    /// </summary>
    public class ExampleFlexibleConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public ExampleFlexibleConnector(IChannelSchema schema, ConnectionSettings connectionSettings)
            : base(schema)
        {
            _connectionSettings = connectionSettings;
        }

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
            return authResult.Successful ? ConnectorResult<bool>.Success(true) : authResult;
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            // Use appropriate authentication based on what was configured
            var authHeader = GetAuthenticationHeader() ?? $"ApiKey {GetApiKey()}";
            
            await Task.Delay(10, cancellationToken);
            
            var result = new SendResult(message.Id, $"flexible-{Guid.NewGuid()}");
            return ConnectorResult<SendResult>.Success(result);
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Flexible Connector Ready")));
        }
    }

    /// <summary>
    /// Example connector with custom authentication manager.
    /// </summary>
    public class ExampleCustomConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public ExampleCustomConnector(IChannelSchema schema, ConnectionSettings connectionSettings, IAuthenticationManager authManager)
            : base(schema, authenticationManager: authManager)
        {
            _connectionSettings = connectionSettings;
        }

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
            return authResult.Successful ? ConnectorResult<bool>.Success(true) : authResult;
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            var result = new SendResult(message.Id, $"custom-{Guid.NewGuid()}");
            return ConnectorResult<SendResult>.Success(result);
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Custom Connector Ready")));
        }
    }

    /// <summary>
    /// Example connector that demonstrates token refresh functionality.
    /// </summary>
    public class ExampleRefreshableConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;

        public ExampleRefreshableConnector(IChannelSchema schema, ConnectionSettings connectionSettings)
            : base(schema)
        {
            _connectionSettings = connectionSettings;
        }

        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
            return authResult.Successful ? ConnectorResult<bool>.Success(true) : authResult;
        }

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<bool>.Success(true));
        }

        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            var result = new SendResult(message.Id, $"refresh-{Guid.NewGuid()}");
            return ConnectorResult<SendResult>.Success(result);
        }

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Refreshable Connector Ready")));
        }

        /// <summary>
        /// Public method to demonstrate manual token refresh.
        /// </summary>
        public async Task RefreshAuthenticationIfNeeded()
        {
            // Check if token needs refresh (this logic would be more sophisticated in practice)
            if (AuthenticationCredential?.WillExpireSoon(TimeSpan.FromMinutes(5)) == true)
            {
                await RefreshAuthenticationAsync(_connectionSettings);
            }
        }
    }

    #endregion
}