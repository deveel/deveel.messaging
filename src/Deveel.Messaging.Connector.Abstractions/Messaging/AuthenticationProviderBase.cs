//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides a base implementation for authentication providers.
    /// </summary>
    public abstract class AuthenticationProviderBase : IAuthenticationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProviderBase"/> class.
        /// </summary>
        /// <param name="authenticationType">The authentication type this provider supports.</param>
        /// <param name="displayName">The display name for this provider.</param>
        protected AuthenticationProviderBase(AuthenticationType authenticationType, string displayName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
            AuthenticationType = authenticationType;
            DisplayName = displayName;
        }

        /// <inheritdoc/>
        public AuthenticationType AuthenticationType { get; }

        /// <inheritdoc/>
        public string DisplayName { get; }

        /// <inheritdoc/>
        public virtual bool CanHandle(AuthenticationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            return configuration.AuthenticationType == AuthenticationType;
        }

        /// <inheritdoc/>
        public abstract Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public virtual Task<AuthenticationResult> RefreshCredentialAsync(AuthenticationCredential existingCredential, ConnectionSettings connectionSettings, CancellationToken cancellationToken = default)
        {
            // Default implementation: just obtain a new credential
            return ObtainCredentialAsync(connectionSettings, cancellationToken);
        }

        /// <summary>
        /// Validates that the required parameters are present in the connection settings.
        /// </summary>
        /// <param name="connectionSettings">The connection settings to validate.</param>
        /// <param name="requiredParameters">The names of required parameters.</param>
        /// <returns>A validation result containing any missing parameters.</returns>
        protected static AuthenticationValidationResult ValidateRequiredParameters(ConnectionSettings connectionSettings, params string[] requiredParameters)
        {
            ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));
            ArgumentNullException.ThrowIfNull(requiredParameters, nameof(requiredParameters));

            var missingParameters = new List<string>();

            foreach (var parameter in requiredParameters)
            {
                var value = connectionSettings.GetParameter(parameter);
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    missingParameters.Add(parameter);
                }
            }

            return new AuthenticationValidationResult(missingParameters.Count == 0, missingParameters);
        }

        /// <summary>
        /// Gets a parameter value from connection settings as a string.
        /// </summary>
        /// <param name="connectionSettings">The connection settings.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter value as a string, or null if not found.</returns>
        protected static string? GetStringParameter(ConnectionSettings connectionSettings, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(connectionSettings, nameof(connectionSettings));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(parameterName, nameof(parameterName));
            
            return connectionSettings.GetParameter(parameterName)?.ToString();
        }

        /// <summary>
        /// Creates a failed authentication result with a standardized error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorCode">Optional error code.</param>
        /// <returns>A failed authentication result.</returns>
        protected static AuthenticationResult CreateFailureResult(string errorMessage, string? errorCode = null)
        {
            return AuthenticationResult.Failure(errorMessage, errorCode);
        }

        /// <summary>
        /// Creates a successful authentication result.
        /// </summary>
        /// <param name="credential">The obtained credential.</param>
        /// <returns>A successful authentication result.</returns>
        protected static AuthenticationResult CreateSuccessResult(AuthenticationCredential credential)
        {
            return AuthenticationResult.Success(credential);
        }
    }

    /// <summary>
    /// Represents the result of validating authentication parameters.
    /// </summary>
    public class AuthenticationValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">Indicates whether the validation passed.</param>
        /// <param name="missingParameters">The list of missing required parameters.</param>
        public AuthenticationValidationResult(bool isValid, IList<string> missingParameters)
        {
            IsValid = isValid;
            MissingParameters = missingParameters ?? new List<string>();
        }

        /// <summary>
        /// Gets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the list of missing required parameters.
        /// </summary>
        public IList<string> MissingParameters { get; }

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        public string? ErrorMessage
        {
            get
            {
                if (IsValid)
                    return null;

                return MissingParameters.Count switch
                {
                    0 => "Unknown validation error",
                    1 => $"Missing required parameter: {MissingParameters[0]}",
                    _ => $"Missing required parameters: {string.Join(", ", MissingParameters)}"
                };
            }
        }
    }
}