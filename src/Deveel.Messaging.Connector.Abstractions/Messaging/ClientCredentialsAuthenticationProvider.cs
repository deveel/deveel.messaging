//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Deveel.Messaging
{
    /// <summary>
    /// An authentication provider that implements OAuth 2.0 Client Credentials flow to obtain access tokens.
    /// </summary>
    public class ClientCredentialsAuthenticationProvider : AuthenticationProviderBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClientCredentialsAuthenticationProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCredentialsAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for token requests.</param>
        /// <param name="logger">Optional logger for diagnostic purposes.</param>
        public ClientCredentialsAuthenticationProvider(HttpClient? httpClient = null, ILogger<ClientCredentialsAuthenticationProvider>? logger = null)
            : base(AuthenticationType.ClientCredentials, "OAuth 2.0 Client Credentials")
        {
            _httpClient = httpClient ?? new HttpClient();
            _logger = logger ?? NullLogger<ClientCredentialsAuthenticationProvider>.Instance;
        }

        /// <inheritdoc/>
        public override async Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Obtaining access token using client credentials flow");

                // Validate required parameters
                var validation = ValidateRequiredParameters(connectionSettings, "ClientId", "ClientSecret", "TokenEndpoint");
                if (!validation.IsValid)
                {
                    return CreateFailureResult(validation.ErrorMessage!, "MISSING_PARAMETERS");
                }

                var clientId = GetStringParameter(connectionSettings, "ClientId")!;
                var clientSecret = GetStringParameter(connectionSettings, "ClientSecret")!;
                var tokenEndpoint = GetStringParameter(connectionSettings, "TokenEndpoint")!;
                var scope = GetStringParameter(connectionSettings, "Scope"); // Optional

                // Prepare token request
                var tokenRequest = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                };

                if (!string.IsNullOrWhiteSpace(scope))
                {
                    tokenRequest["scope"] = scope;
                }

                var requestContent = new FormUrlEncodedContent(tokenRequest);

                // Make token request
                _logger.LogDebug("Requesting access token from {TokenEndpoint}", tokenEndpoint);
                var response = await _httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Token request failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                    return CreateFailureResult($"Token request failed: {response.StatusCode} - {errorContent}", "TOKEN_REQUEST_FAILED");
                }

                // Parse token response
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
                {
                    _logger.LogError("Token response does not contain access_token");
                    return CreateFailureResult("Invalid token response: missing access_token", "INVALID_TOKEN_RESPONSE");
                }

                var accessToken = accessTokenElement.GetString();
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return CreateFailureResult("Empty access token received", "EMPTY_ACCESS_TOKEN");
                }

                // Extract expiration time if available
                DateTime? expiresAt = null;
                if (tokenResponse.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out var expiresIn))
                {
                    expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                }

                // Extract token type
                var tokenType = "Bearer"; // Default
                if (tokenResponse.TryGetProperty("token_type", out var tokenTypeElement))
                {
                    tokenType = tokenTypeElement.GetString() ?? "Bearer";
                }

                // Create credential
                var credential = AuthenticationCredential.CreateToken(accessToken, expiresAt, tokenType);

                // Add additional properties from token response
                if (tokenResponse.TryGetProperty("scope", out var scopeElement))
                {
                    credential.Properties["Scope"] = scopeElement.GetString();
                }

                if (tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement))
                {
                    credential.Properties["RefreshToken"] = refreshTokenElement.GetString();
                }

                _logger.LogInformation("Successfully obtained access token (expires at: {ExpiresAt})", expiresAt);
                return CreateSuccessResult(credential);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during token request");
                return CreateFailureResult($"Network error: {ex.Message}", "NETWORK_ERROR");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Token request timed out");
                return CreateFailureResult("Token request timed out", "TIMEOUT");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse token response");
                return CreateFailureResult($"Invalid JSON response: {ex.Message}", "INVALID_JSON");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token request");
                return CreateFailureResult($"Unexpected error: {ex.Message}", "UNEXPECTED_ERROR");
            }
        }

        /// <inheritdoc/>
        public override async Task<AuthenticationResult> RefreshCredentialAsync(AuthenticationCredential existingCredential, ConnectionSettings connectionSettings, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if we have a refresh token
                if (existingCredential.Properties.TryGetValue("RefreshToken", out var refreshTokenObj) &&
                    refreshTokenObj is string refreshToken && !string.IsNullOrWhiteSpace(refreshToken))
                {
                    _logger.LogDebug("Refreshing access token using refresh token");
                    return await RefreshUsingRefreshToken(refreshToken, connectionSettings, cancellationToken);
                }

                _logger.LogDebug("No refresh token available, obtaining new token");
                return await ObtainCredentialAsync(connectionSettings, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return CreateFailureResult($"Token refresh failed: {ex.Message}", "REFRESH_FAILED");
            }
        }

        private async Task<AuthenticationResult> RefreshUsingRefreshToken(string refreshToken, ConnectionSettings connectionSettings, CancellationToken cancellationToken)
        {
            var validation = ValidateRequiredParameters(connectionSettings, "ClientId", "ClientSecret", "TokenEndpoint");
            if (!validation.IsValid)
            {
                return CreateFailureResult(validation.ErrorMessage!, "MISSING_PARAMETERS");
            }

            var clientId = GetStringParameter(connectionSettings, "ClientId")!;
            var clientSecret = GetStringParameter(connectionSettings, "ClientSecret")!;
            var tokenEndpoint = GetStringParameter(connectionSettings, "TokenEndpoint")!;

            var refreshRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };

            var requestContent = new FormUrlEncodedContent(refreshRequest);
            var response = await _httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Token refresh failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                
                // If refresh fails, try to get a new token
                _logger.LogDebug("Refresh token failed, attempting to obtain new token");
                return await ObtainCredentialAsync(connectionSettings, cancellationToken);
            }

            // Parse refresh response (same format as initial token response)
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
            {
                return CreateFailureResult("Invalid refresh response: missing access_token", "INVALID_REFRESH_RESPONSE");
            }

            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return CreateFailureResult("Empty access token received from refresh", "EMPTY_REFRESH_TOKEN");
            }

            DateTime? expiresAt = null;
            if (tokenResponse.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out var expiresIn))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            }

            var tokenType = "Bearer";
            if (tokenResponse.TryGetProperty("token_type", out var tokenTypeElement))
            {
                tokenType = tokenTypeElement.GetString() ?? "Bearer";
            }

            var credential = AuthenticationCredential.CreateToken(accessToken, expiresAt, tokenType);

            // Preserve or update refresh token
            if (tokenResponse.TryGetProperty("refresh_token", out var newRefreshTokenElement))
            {
                credential.Properties["RefreshToken"] = newRefreshTokenElement.GetString();
            }
            else
            {
                // Keep the existing refresh token if a new one wasn't provided
                credential.Properties["RefreshToken"] = refreshToken;
            }

            _logger.LogInformation("Successfully refreshed access token (expires at: {ExpiresAt})", expiresAt);
            return CreateSuccessResult(credential);
        }

        /// <summary>
        /// Releases the resources used by this authentication provider.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}