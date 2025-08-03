//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using SendGrid;
using SendGrid.Helpers.Mail;

namespace Deveel.Messaging
{
    /// <summary>
    /// Default implementation of <see cref="ISendGridService"/> that provides
    /// direct access to the SendGrid API.
    /// </summary>
    public class SendGridService : ISendGridService
    {
        private ISendGridClient? _client;

        /// <inheritdoc/>
        public void Initialize(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

            _client = new SendGridClient(apiKey);
        }

        /// <inheritdoc/>
        public async Task<Response> SendEmailAsync(SendGridMessage message, CancellationToken cancellationToken)
        {
            if (_client == null)
                throw new InvalidOperationException("SendGrid client is not initialized");

            return await _client.SendEmailAsync(message, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
        {
            if (_client == null)
                return false;

            try
            {
                // Use a simple API call to test connectivity
                // We'll try to get API key scopes as a test
                var response = await _client.RequestAsync(
                    method: SendGridClient.Method.GET,
                    urlPath: "scopes",
                    cancellationToken: cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<Response> GetEmailActivityAsync(string messageId, CancellationToken cancellationToken)
        {
            if (_client == null)
                throw new InvalidOperationException("SendGrid client is not initialized");

            // Note: This is a simplified implementation. In a real scenario, you might need to use
            // SendGrid's Event Webhook or Email Activity API which requires additional setup
            var response = await _client.RequestAsync(
                method: SendGridClient.Method.GET,
                urlPath: $"messages/{messageId}",
                cancellationToken: cancellationToken);

            return response;
        }
    }
}