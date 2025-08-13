//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deveel.Messaging
{
    /// <summary>
    /// An authentication provider that handles direct credential passthrough for authentication
    /// types that don't require token exchange (API Keys, Bearer tokens, basic auth).
    /// </summary>
    public class DirectCredentialAuthenticationProvider : AuthenticationProviderBase
    {
        private readonly ILogger<DirectCredentialAuthenticationProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectCredentialAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="authenticationType">The authentication type this provider handles.</param>
        /// <param name="displayName">The display name for this provider.</param>
        /// <param name="logger">Optional logger for diagnostic purposes.</param>
        public DirectCredentialAuthenticationProvider(
            AuthenticationType authenticationType, 
            string displayName, 
            ILogger<DirectCredentialAuthenticationProvider>? logger = null)
            : base(authenticationType, displayName)
        {
            _logger = logger ?? NullLogger<DirectCredentialAuthenticationProvider>.Instance;
        }

        /// <inheritdoc/>
        public override Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Obtaining {AuthenticationType} credential", AuthenticationType);

                return AuthenticationType switch
                {
                    AuthenticationType.ApiKey => ObtainApiKeyCredential(connectionSettings),
                    AuthenticationType.Token => ObtainTokenCredential(connectionSettings),
                    AuthenticationType.Basic => ObtainBasicCredential(connectionSettings),
                    _ => Task.FromResult(CreateFailureResult($"Unsupported authentication type: {AuthenticationType}", "UNSUPPORTED_TYPE"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining {AuthenticationType} credential", AuthenticationType);
                return Task.FromResult(CreateFailureResult($"Error obtaining credential: {ex.Message}", "CREDENTIAL_ERROR"));
            }
        }

        private Task<AuthenticationResult> ObtainApiKeyCredential(ConnectionSettings connectionSettings)
        {
            // Try common API key parameter names
            var possibleFields = new[] { "ApiKey", "Key", "AccessKey", "Token" };
            
            foreach (var field in possibleFields)
            {
                var apiKey = GetStringParameter(connectionSettings, field);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogDebug("Found API key in field: {Field}", field);
                    var credential = AuthenticationCredential.CreateApiKey(apiKey);
                    credential.Properties["FieldName"] = field;
                    return Task.FromResult(CreateSuccessResult(credential));
                }
            }

            return Task.FromResult(CreateFailureResult(
                $"API key not found. Expected one of: {string.Join(", ", possibleFields)}", 
                "MISSING_API_KEY"));
        }

        private Task<AuthenticationResult> ObtainTokenCredential(ConnectionSettings connectionSettings)
        {
            // Try common token parameter names
            var possibleFields = new[] { "Token", "AccessToken", "BearerToken", "AuthToken" };
            
            foreach (var field in possibleFields)
            {
                var token = GetStringParameter(connectionSettings, field);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogDebug("Found token in field: {Field}", field);
                    
                    // Check for token type
                    var tokenType = GetStringParameter(connectionSettings, "TokenType") ?? "Bearer";
                    
                    // Check for expiration
                    DateTime? expiresAt = null;
                    var expirationString = GetStringParameter(connectionSettings, "ExpiresAt");
                    if (!string.IsNullOrWhiteSpace(expirationString) && DateTime.TryParse(expirationString, out var expirationDate))
                    {
                        expiresAt = expirationDate;
                    }

                    var credential = AuthenticationCredential.CreateToken(token, expiresAt, tokenType);
                    credential.Properties["FieldName"] = field;
                    return Task.FromResult(CreateSuccessResult(credential));
                }
            }

            return Task.FromResult(CreateFailureResult(
                $"Token not found. Expected one of: {string.Join(", ", possibleFields)}", 
                "MISSING_TOKEN"));
        }

        private Task<AuthenticationResult> ObtainBasicCredential(ConnectionSettings connectionSettings)
        {
            // Try different combinations of username/password fields
            var userFields = new[] { "Username", "User", "AccountSid", "ClientId" };
            var passFields = new[] { "Password", "Pass", "AuthToken", "ClientSecret" };
            
            foreach (var userField in userFields)
            {
                var username = GetStringParameter(connectionSettings, userField);
                if (string.IsNullOrWhiteSpace(username))
                    continue;

                foreach (var passField in passFields)
                {
                    var password = GetStringParameter(connectionSettings, passField);
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        _logger.LogDebug("Found basic auth credentials in fields: {UserField}, {PassField}", userField, passField);
                        
                        var credential = AuthenticationCredential.CreateBasic(username, password);
                        credential.Properties["UserField"] = userField;
                        credential.Properties["PassField"] = passField;
                        return Task.FromResult(CreateSuccessResult(credential));
                    }
                }
            }

            return Task.FromResult(CreateFailureResult(
                "Basic authentication credentials not found. Expected combinations like (Username,Password), (AccountSid,AuthToken), etc.", 
                "MISSING_BASIC_CREDENTIALS"));
        }

        /// <summary>
        /// Creates an API key authentication provider.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A direct credential provider for API keys.</returns>
        public static DirectCredentialAuthenticationProvider CreateApiKeyProvider(ILogger<DirectCredentialAuthenticationProvider>? logger = null)
        {
            return new DirectCredentialAuthenticationProvider(AuthenticationType.ApiKey, "API Key Authentication", logger);
        }

        /// <summary>
        /// Creates a token authentication provider.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A direct credential provider for tokens.</returns>
        public static DirectCredentialAuthenticationProvider CreateTokenProvider(ILogger<DirectCredentialAuthenticationProvider>? logger = null)
        {
            return new DirectCredentialAuthenticationProvider(AuthenticationType.Token, "Token Authentication", logger);
        }

        /// <summary>
        /// Creates a basic authentication provider.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A direct credential provider for basic authentication.</returns>
        public static DirectCredentialAuthenticationProvider CreateBasicProvider(ILogger<DirectCredentialAuthenticationProvider>? logger = null)
        {
            return new DirectCredentialAuthenticationProvider(AuthenticationType.Basic, "Basic Authentication", logger);
        }
    }
}