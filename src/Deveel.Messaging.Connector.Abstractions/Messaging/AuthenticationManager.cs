//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Deveel.Messaging
{
    /// <summary>
    /// Manages authentication providers and handles the authentication process for connectors.
    /// </summary>
    public class AuthenticationManager : IAuthenticationManager
    {
        private readonly List<IAuthenticationProvider> _providers;
        private readonly ILogger<AuthenticationManager> _logger;
        private readonly Dictionary<string, AuthenticationCredential> _credentialCache;
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationManager"/> class.
        /// </summary>
        /// <param name="providers">The authentication providers to register.</param>
        /// <param name="logger">Optional logger for diagnostic purposes.</param>
        public AuthenticationManager(IEnumerable<IAuthenticationProvider>? providers = null, ILogger<AuthenticationManager>? logger = null)
        {
            _providers = new List<IAuthenticationProvider>(providers ?? Enumerable.Empty<IAuthenticationProvider>());
            _logger = logger ?? NullLogger<AuthenticationManager>.Instance;
            _credentialCache = new Dictionary<string, AuthenticationCredential>();

            // Register default providers if none are provided
            if (_providers.Count == 0)
            {
                RegisterDefaultProviders();
            }
        }

        /// <summary>
        /// Registers an authentication provider.
        /// </summary>
        /// <param name="provider">The authentication provider to register.</param>
        public void RegisterProvider(IAuthenticationProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider, nameof(provider));
            
            _providers.Add(provider);
            _logger.LogDebug("Registered authentication provider: {ProviderName} for {AuthenticationType}", 
                provider.DisplayName, provider.AuthenticationType);
        }

        /// <summary>
        /// Authenticates using the provided connection settings and authentication configuration.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing authentication parameters.</param>
        /// <param name="configuration">The authentication configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous authentication operation.</returns>
        public async Task<AuthenticationResult> AuthenticateAsync(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            try
            {
                _logger.LogDebug("Authenticating using {AuthenticationType} authentication", configuration.AuthenticationType);

                // Find a provider that can handle this configuration
                var provider = FindProvider(configuration);
                if (provider == null)
                {
                    _logger.LogWarning("No authentication provider found for {AuthenticationType}", configuration.AuthenticationType);
                    return AuthenticationResult.Failure($"No authentication provider available for {configuration.AuthenticationType}", "NO_PROVIDER");
                }

                // Check cache first
                var cacheKey = CreateCacheKey(connectionSettings, configuration);
                var cachedCredential = GetCachedCredential(cacheKey);
                
                if (cachedCredential != null && !ShouldRefreshCredential(cachedCredential))
                {
                    _logger.LogDebug("Using cached credential for {AuthenticationType}", configuration.AuthenticationType);
                    return AuthenticationResult.Success(cachedCredential);
                }

                // Obtain new credential or refresh existing one
                AuthenticationResult result;
                if (cachedCredential != null && ShouldRefreshCredential(cachedCredential))
                {
                    _logger.LogDebug("Refreshing credential for {AuthenticationType}", configuration.AuthenticationType);
                    result = await provider.RefreshCredentialAsync(cachedCredential, connectionSettings, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("Obtaining new credential for {AuthenticationType}", configuration.AuthenticationType);
                    result = await provider.ObtainCredentialAsync(connectionSettings, cancellationToken);
                }

                // Cache successful results
                if (result.IsSuccessful && result.Credential != null)
                {
                    CacheCredential(cacheKey, result.Credential);
                    _logger.LogInformation("Successfully authenticated using {AuthenticationType}", configuration.AuthenticationType);
                }
                else
                {
                    _logger.LogWarning("Authentication failed for {AuthenticationType}: {ErrorMessage}", 
                        configuration.AuthenticationType, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during authentication");
                return AuthenticationResult.Failure($"Authentication error: {ex.Message}", "AUTHENTICATION_ERROR");
            }
        }

        /// <summary>
        /// Clears the credential cache.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _credentialCache.Clear();
                _logger.LogDebug("Authentication credential cache cleared");
            }
        }

        /// <summary>
        /// Removes a specific credential from the cache.
        /// </summary>
        /// <param name="connectionSettings">The connection settings.</param>
        /// <param name="configuration">The authentication configuration.</param>
        public void InvalidateCredential(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            var cacheKey = CreateCacheKey(connectionSettings, configuration);
            
            lock (_cacheLock)
            {
                if (_credentialCache.Remove(cacheKey))
                {
                    _logger.LogDebug("Invalidated cached credential for {AuthenticationType}", configuration.AuthenticationType);
                }
            }
        }

        private void RegisterDefaultProviders()
        {
            // Register built-in providers
            RegisterProvider(DirectCredentialAuthenticationProvider.CreateApiKeyProvider());
            RegisterProvider(DirectCredentialAuthenticationProvider.CreateTokenProvider());
            RegisterProvider(DirectCredentialAuthenticationProvider.CreateBasicProvider());
            RegisterProvider(new ClientCredentialsAuthenticationProvider());

            _logger.LogDebug("Registered default authentication providers");
        }

        private IAuthenticationProvider? FindProvider(AuthenticationConfiguration configuration)
        {
            return _providers.FirstOrDefault(p => p.CanHandle(configuration));
        }

        private string CreateCacheKey(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration)
        {
            // Create a cache key based on authentication type and relevant parameters
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(configuration.AuthenticationType.ToString());

            // Add relevant field values to the key
            var relevantFields = configuration.GetAllFieldNames();
            foreach (var field in relevantFields.OrderBy(f => f))
            {
                var value = connectionSettings.GetParameter(field);
                if (value != null)
                {
                    keyBuilder.Append($"|{field}={value.GetHashCode()}");
                }
            }

            return keyBuilder.ToString();
        }

        private AuthenticationCredential? GetCachedCredential(string cacheKey)
        {
            lock (_cacheLock)
            {
                if (_credentialCache.TryGetValue(cacheKey, out var credential))
                {
                    // Check if credential is still valid
                    if (!credential.IsExpired)
                    {
                        return credential;
                    }

                    // Remove expired credential
                    _credentialCache.Remove(cacheKey);
                }
            }

            return null;
        }

        private void CacheCredential(string cacheKey, AuthenticationCredential credential)
        {
            lock (_cacheLock)
            {
                _credentialCache[cacheKey] = credential;
            }
        }

        private bool ShouldRefreshCredential(AuthenticationCredential credential)
        {
            // Refresh if expired or will expire within 5 minutes
            return credential.IsExpired || credential.WillExpireSoon(TimeSpan.FromMinutes(5));
        }
    }

    /// <summary>
    /// Defines the contract for authentication management services.
    /// </summary>
    public interface IAuthenticationManager
    {
        /// <summary>
        /// Registers an authentication provider.
        /// </summary>
        /// <param name="provider">The authentication provider to register.</param>
        void RegisterProvider(IAuthenticationProvider provider);

        /// <summary>
        /// Authenticates using the provided connection settings and authentication configuration.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing authentication parameters.</param>
        /// <param name="configuration">The authentication configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous authentication operation.</returns>
        Task<AuthenticationResult> AuthenticateAsync(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the credential cache.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Removes a specific credential from the cache.
        /// </summary>
        /// <param name="connectionSettings">The connection settings.</param>
        /// <param name="configuration">The authentication configuration.</param>
        void InvalidateCredential(ConnectionSettings connectionSettings, AuthenticationConfiguration configuration);
    }
}