//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines a contract for authentication providers that can obtain final authentication
    /// credentials from initial authentication parameters during connector initialization.
    /// </summary>
    /// <remarks>
    /// This interface enables connectors to implement authentication flows where initial
    /// parameters (like Client ID and Client Secret) are used to obtain the actual
    /// authentication factor (like Access Token) needed for API operations.
    /// </remarks>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Gets the authentication type that this provider supports.
        /// </summary>
        AuthenticationType AuthenticationType { get; }

        /// <summary>
        /// Gets the display name of this authentication provider.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Determines whether this provider can handle the given authentication configuration.
        /// </summary>
        /// <param name="configuration">The authentication configuration to check.</param>
        /// <returns>True if this provider can handle the configuration; otherwise, false.</returns>
        bool CanHandle(AuthenticationConfiguration configuration);

        /// <summary>
        /// Obtains the final authentication credential from the provided connection settings.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing initial authentication parameters.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the authentication result.</returns>
        Task<AuthenticationResult> ObtainCredentialAsync(ConnectionSettings connectionSettings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes an existing authentication credential if supported.
        /// </summary>
        /// <param name="existingCredential">The existing credential to refresh.</param>
        /// <param name="connectionSettings">The connection settings containing authentication parameters.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the refreshed authentication result.</returns>
        Task<AuthenticationResult> RefreshCredentialAsync(AuthenticationCredential existingCredential, ConnectionSettings connectionSettings, CancellationToken cancellationToken = default);
    }
}