//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines the contract for interacting with Firebase Cloud Messaging services.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the Firebase Admin SDK operations to enable
    /// testability and provide a consistent API for Firebase operations.
    /// </remarks>
    public interface IFirebaseService
    {
        /// <summary>
        /// Initializes the Firebase application with the provided service account credentials.
        /// </summary>
        /// <param name="serviceAccountKey">The service account key JSON string.</param>
        /// <param name="projectId">The Firebase project ID.</param>
        /// <returns>A task representing the initialization operation.</returns>
        Task InitializeAsync(string serviceAccountKey, string projectId);

        /// <summary>
        /// Sends a single push notification message.
        /// </summary>
        /// <param name="message">The Firebase message to send.</param>
        /// <param name="dryRun">Whether to send in dry-run mode for testing.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task containing the message ID if successful.</returns>
        Task<string> SendAsync(FirebaseAdmin.Messaging.Message message, bool dryRun = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends multiple push notification messages to different tokens.
        /// </summary>
        /// <param name="messages">The collection of Firebase messages to send.</param>
        /// <param name="dryRun">Whether to send in dry-run mode for testing.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task containing the batch response with individual results.</returns>
        Task<BatchResponse> SendEachAsync(IEnumerable<FirebaseAdmin.Messaging.Message> messages, bool dryRun = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a single message to multiple device tokens.
        /// </summary>
        /// <param name="message">The multicast message to send.</param>
        /// <param name="dryRun">Whether to send in dry-run mode for testing.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task containing the batch response with individual results.</returns>
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to Firebase services.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the connection test operation.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the Firebase application instance.
        /// </summary>
        FirebaseApp? App { get; }

        /// <summary>
        /// Gets a value indicating whether the service is initialized.
        /// </summary>
        bool IsInitialized { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IFirebaseService"/> using the Firebase Admin SDK.
    /// </summary>
    public class FirebaseService : IFirebaseService
    {
        private FirebaseApp? _app;
        private FirebaseMessaging? _messaging;

        /// <inheritdoc/>
        public FirebaseApp? App => _app;

        /// <inheritdoc/>
        public bool IsInitialized => _app != null && _messaging != null;

        /// <inheritdoc/>
        public async Task InitializeAsync(string serviceAccountKey, string projectId)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(serviceAccountKey, nameof(serviceAccountKey));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(projectId, nameof(projectId));

            try
            {
                // Clean up existing app if already initialized
                if (_app != null)
                {
                    _app.Delete();
                    _app = null;
                    _messaging = null;
                }

                // Create credential from service account key
                var credential = GoogleCredential.FromJson(serviceAccountKey);

                // Initialize Firebase app
                _app = FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = projectId
                });

                // Initialize messaging service
                _messaging = FirebaseMessaging.GetMessaging(_app);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Firebase service: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> SendAsync(FirebaseAdmin.Messaging.Message message, bool dryRun = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            try
            {
                return await _messaging!.SendAsync(message, dryRun, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send Firebase message: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<BatchResponse> SendEachAsync(IEnumerable<FirebaseAdmin.Messaging.Message> messages, bool dryRun = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            ArgumentNullException.ThrowIfNull(messages, nameof(messages));

            try
            {
                return await _messaging!.SendEachAsync(messages, dryRun, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send Firebase messages: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            try
            {
                return await _messaging!.SendMulticastAsync(message, dryRun, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send Firebase multicast message: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
                return false;

            try
            {
                // Create a simple test message to validate the connection
                // We'll use dry-run mode so no actual message is sent
                var testMessage = new FirebaseAdmin.Messaging.Message
                {
                    Token = "test-token-for-connection-validation",
                    Notification = new Notification
                    {
                        Title = "Test",
                        Body = "Connection test"
                    }
                };

                // This will validate credentials and connectivity without sending
                await _messaging!.SendAsync(testMessage, dryRun: true, cancellationToken);
                return true;
            }
            catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                // Invalid token is expected in connection test, but it means connection works
                return true;
            }
            catch
            {
                // Any other exception means connection failed
                return false;
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Firebase service is not initialized. Call InitializeAsync first.");
            }
        }
    }
}