//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Represents the result of an authentication operation, containing the obtained credential
    /// and related metadata.
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResult"/> class.
        /// </summary>
        /// <param name="isSuccessful">Indicates whether the authentication was successful.</param>
        /// <param name="credential">The obtained authentication credential (if successful).</param>
        /// <param name="errorMessage">The error message (if not successful).</param>
        /// <param name="errorCode">The error code (if not successful).</param>
        public AuthenticationResult(bool isSuccessful, AuthenticationCredential? credential = null, string? errorMessage = null, string? errorCode = null)
        {
            IsSuccessful = isSuccessful;
            Credential = credential;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            Timestamp = DateTime.UtcNow;
            AdditionalData = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Gets a value indicating whether the authentication operation was successful.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Gets the obtained authentication credential, if the operation was successful.
        /// </summary>
        public AuthenticationCredential? Credential { get; }

        /// <summary>
        /// Gets the error message, if the operation was not successful.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the error code, if the operation was not successful.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Gets the timestamp when this authentication result was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets additional data associated with this authentication result.
        /// </summary>
        public Dictionary<string, object?> AdditionalData { get; }

        /// <summary>
        /// Creates a successful authentication result.
        /// </summary>
        /// <param name="credential">The obtained authentication credential.</param>
        /// <returns>A successful authentication result.</returns>
        public static AuthenticationResult Success(AuthenticationCredential credential)
        {
            ArgumentNullException.ThrowIfNull(credential, nameof(credential));
            return new AuthenticationResult(true, credential);
        }

        /// <summary>
        /// Creates a failed authentication result.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <param name="errorCode">An optional error code.</param>
        /// <returns>A failed authentication result.</returns>
        public static AuthenticationResult Failure(string errorMessage, string? errorCode = null)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));
            return new AuthenticationResult(false, null, errorMessage, errorCode);
        }
    }
}