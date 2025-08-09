//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Represents an authentication credential obtained from an authentication provider.
    /// </summary>
    public class AuthenticationCredential
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationCredential"/> class.
        /// </summary>
        /// <param name="authenticationType">The type of authentication this credential represents.</param>
        /// <param name="credentialValue">The primary credential value (e.g., token, key, etc.).</param>
        /// <param name="expiresAt">Optional expiration time for the credential.</param>
        public AuthenticationCredential(AuthenticationType authenticationType, string credentialValue, DateTime? expiresAt = null)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(credentialValue, nameof(credentialValue));
            AuthenticationType = authenticationType;
            CredentialValue = credentialValue;
            ExpiresAt = expiresAt;
            ObtainedAt = DateTime.UtcNow;
            Properties = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Gets the type of authentication this credential represents.
        /// </summary>
        public AuthenticationType AuthenticationType { get; }

        /// <summary>
        /// Gets the primary credential value (e.g., access token, API key, etc.).
        /// </summary>
        public string CredentialValue { get; }

        /// <summary>
        /// Gets the expiration time of this credential, if applicable.
        /// </summary>
        public DateTime? ExpiresAt { get; }

        /// <summary>
        /// Gets the timestamp when this credential was obtained.
        /// </summary>
        public DateTime ObtainedAt { get; }

        /// <summary>
        /// Gets additional properties associated with this credential.
        /// </summary>
        public Dictionary<string, object?> Properties { get; }

        /// <summary>
        /// Gets a value indicating whether this credential has expired.
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;

        /// <summary>
        /// Gets a value indicating whether this credential will expire soon (within the specified buffer time).
        /// </summary>
        /// <param name="buffer">The buffer time before expiration to consider as "soon".</param>
        /// <returns>True if the credential will expire within the buffer time; otherwise, false.</returns>
        public bool WillExpireSoon(TimeSpan buffer)
        {
            return ExpiresAt.HasValue && DateTime.UtcNow.Add(buffer) >= ExpiresAt.Value;
        }

        /// <summary>
        /// Gets the remaining time until this credential expires.
        /// </summary>
        /// <returns>The time remaining until expiration, or null if the credential doesn't expire.</returns>
        public TimeSpan? GetTimeUntilExpiration()
        {
            if (!ExpiresAt.HasValue)
                return null;

            var remaining = ExpiresAt.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// Creates a token-based authentication credential.
        /// </summary>
        /// <param name="token">The access token.</param>
        /// <param name="expiresAt">Optional expiration time.</param>
        /// <param name="tokenType">Optional token type (e.g., "Bearer").</param>
        /// <returns>A new authentication credential.</returns>
        public static AuthenticationCredential CreateToken(string token, DateTime? expiresAt = null, string? tokenType = null)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(token, nameof(token));
            
            var credential = new AuthenticationCredential(AuthenticationType.Token, token, expiresAt);
            
            if (!string.IsNullOrWhiteSpace(tokenType))
            {
                credential.Properties["TokenType"] = tokenType;
            }
            
            return credential;
        }

        /// <summary>
        /// Creates an API key-based authentication credential.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A new authentication credential.</returns>
        public static AuthenticationCredential CreateApiKey(string apiKey)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
            return new AuthenticationCredential(AuthenticationType.ApiKey, apiKey);
        }

        /// <summary>
        /// Creates a basic authentication credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A new authentication credential.</returns>
        public static AuthenticationCredential CreateBasic(string username, string password)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(username, nameof(username));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(password, nameof(password));
            
            var basicAuthValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
            var credential = new AuthenticationCredential(AuthenticationType.Basic, basicAuthValue);
            
            credential.Properties["Username"] = username;
            credential.Properties["Password"] = password;
            
            return credential;
        }
    }
}