//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Defines the contract for Facebook Graph API operations.
    /// </summary>
    public interface IFacebookService
    {
        /// <summary>
        /// Initializes the Facebook service with the specified access token.
        /// </summary>
        /// <param name="pageAccessToken">The Facebook Page Access Token.</param>
        void Initialize(string pageAccessToken);

        /// <summary>
        /// Fetches information about the Facebook page.
        /// </summary>
        /// <param name="pageId">The Facebook Page ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Page information or null if the page cannot be accessed.</returns>
        Task<FacebookPageInfo?> FetchPageAsync(string pageId, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message through the Facebook Messenger platform.
        /// </summary>
        /// <param name="request">The message request containing recipient and message details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The message response containing the message ID and status.</returns>
        Task<FacebookMessageResponse> SendMessageAsync(FacebookMessageRequest request, CancellationToken cancellationToken);
    }
}