//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using SendGrid;
using SendGrid.Helpers.Mail;

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines the contract for SendGrid API operations, allowing for testing and abstraction.
    /// </summary>
    public interface ISendGridService
    {
        /// <summary>
        /// Initializes the SendGrid client with the provided API key.
        /// </summary>
        /// <param name="apiKey">The SendGrid API key for authentication.</param>
        void Initialize(string apiKey);

        /// <summary>
        /// Sends an email message using the SendGrid API.
        /// </summary>
        /// <param name="message">The email message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The SendGrid response containing the result of the send operation.</returns>
        Task<Response> SendEmailAsync(SendGridMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Tests the connection to SendGrid by validating the API key.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>True if the connection is successful, false otherwise.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves email activity data from SendGrid.
        /// </summary>
        /// <param name="messageId">The message ID to query.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The activity data for the specified message.</returns>
        Task<Response> GetEmailActivityAsync(string messageId, CancellationToken cancellationToken);
    }
}