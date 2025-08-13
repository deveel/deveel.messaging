//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deveel.Messaging
{
    /// <summary>
    /// An authentication provider for Firebase that handles Google Service Account authentication
    /// by passing through the service account key for Firebase SDK initialization.
    /// </summary>
    /// <remarks>
    /// This provider handles service account authentication by extracting the service account
    /// JSON from connection settings and creating a credential that can be used by the Firebase SDK.
    /// It doesn't perform token exchange itself but validates and prepares the service account
    /// information for the Firebase SDK to use.
    /// </remarks>
    public class FirebaseServiceAccountAuthenticationProvider : AuthenticationProviderBase
    {
        private readonly ILogger<FirebaseServiceAccountAuthenticationProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebaseServiceAccountAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic purposes.</param>
        public FirebaseServiceAccountAuthenticationProvider(ILogger<FirebaseServiceAccountAuthenticationProvider>? logger = null)
            : base(AuthenticationType.Certificate, "Firebase Service Account Authentication")
        {
            _logger = logger ?? NullLogger<FirebaseServiceAccountAuthenticationProvider>.Instance;
        }

        /// <inheritdoc/>
        public override bool CanHandle(AuthenticationConfiguration configuration)
        {
            // Handle certificate authentication or any configuration that has service account related fields
            return configuration.AuthenticationType == AuthenticationType.Certificate ||
                   (configuration.AuthenticationType == AuthenticationType.Custom && 
                    configuration.GetAllFieldNames().Any(f => f.Contains("ServiceAccount", StringComparison.OrdinalIgnoreCase)));
        }

        /// <inheritdoc/>
        public override Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Obtaining Firebase service account credential");

                // Get service account key - can be JSON string or file path
                var serviceAccountKey = GetStringParameter(connectionSettings, "ServiceAccountKey") ??
                                      GetStringParameter(connectionSettings, "ServiceAccountJson") ??
                                      GetStringParameter(connectionSettings, "Certificate");

                if (string.IsNullOrWhiteSpace(serviceAccountKey))
                {
                    return Task.FromResult(CreateFailureResult("Service account key is required. Provide ServiceAccountKey, ServiceAccountJson, or Certificate parameter.", "MISSING_SERVICE_ACCOUNT_KEY"));
                }

                // If it looks like a file path, validate the file exists
                if (serviceAccountKey.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && 
                    !serviceAccountKey.Contains("{"))
                {
                    if (!File.Exists(serviceAccountKey))
                    {
                        return Task.FromResult(CreateFailureResult($"Service account key file not found: {serviceAccountKey}", "SERVICE_ACCOUNT_FILE_NOT_FOUND"));
                    }
                    
                    _logger.LogDebug("Using service account key file: {FilePath}", serviceAccountKey);
                }
                else
                {
                    // Validate JSON format
                    try
                    {
                        System.Text.Json.JsonDocument.Parse(serviceAccountKey);
                        _logger.LogDebug("Using service account key JSON string");
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        return Task.FromResult(CreateFailureResult("Service account key is not valid JSON", "INVALID_SERVICE_ACCOUNT_JSON"));
                    }
                }

                // Create credential that contains the service account information
                // The Firebase SDK will handle the actual token exchange
                var credential = new AuthenticationCredential(AuthenticationType.Certificate, serviceAccountKey);
                credential.Properties["CredentialType"] = "ServiceAccount";
                credential.Properties["Provider"] = "Firebase";

                // Store project ID if available
                var projectId = GetStringParameter(connectionSettings, "ProjectId");
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    credential.Properties["ProjectId"] = projectId;
                }

                _logger.LogInformation("Successfully prepared Firebase service account credential");
                return Task.FromResult(CreateSuccessResult(credential));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing Firebase service account credential");
                return Task.FromResult(CreateFailureResult($"Error preparing credential: {ex.Message}", "CREDENTIAL_ERROR"));
            }
        }

        /// <inheritdoc/>
        public override Task<AuthenticationResult> RefreshCredentialAsync(AuthenticationCredential existingCredential, ConnectionSettings connectionSettings, CancellationToken cancellationToken = default)
        {
            // Service account credentials don't need refresh - the Firebase SDK handles token lifecycle
            // Just validate that the existing credential is still valid
            if (existingCredential.AuthenticationType == AuthenticationType.Certificate &&
                !string.IsNullOrWhiteSpace(existingCredential.CredentialValue))
            {
                _logger.LogDebug("Service account credential is still valid, no refresh needed");
                return Task.FromResult(CreateSuccessResult(existingCredential));
            }

            // If credential is invalid, obtain a new one
            _logger.LogDebug("Service account credential is invalid, obtaining new credential");
            return ObtainCredentialAsync(connectionSettings, cancellationToken);
        }
    }
}