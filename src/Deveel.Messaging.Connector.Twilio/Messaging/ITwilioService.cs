//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Api.V2010;

namespace Deveel.Messaging;

/// <summary>
/// Abstracts Twilio API operations to enable unit testing and mocking.
/// </summary>
public interface ITwilioService
{
    /// <summary>
    /// Initializes the Twilio client with the provided credentials.
    /// </summary>
    /// <param name="accountSid">The Twilio Account SID.</param>
    /// <param name="authToken">The Twilio Auth Token.</param>
    void Initialize(string accountSid, string authToken);

    /// <summary>
    /// Fetches account information to test connectivity.
    /// </summary>
    /// <param name="accountSid">The account SID to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account resource.</returns>
    Task<AccountResource?> FetchAccountAsync(string accountSid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and sends an SMS message.
    /// </summary>
    /// <param name="options">The message creation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created message resource.</returns>
    Task<MessageResource> CreateMessageAsync(CreateMessageOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a message by its SID to get status information.
    /// </summary>
    /// <param name="messageSid">The message SID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message resource.</returns>
    Task<MessageResource> FetchMessageAsync(string messageSid, CancellationToken cancellationToken = default);
}