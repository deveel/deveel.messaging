//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Twilio;
using Twilio.Rest.Api.V2010;
using Twilio.Rest.Api.V2010.Account;

namespace Deveel.Messaging;

/// <summary>
/// Default implementation of <see cref="ITwilioService"/> that wraps the actual Twilio SDK calls.
/// </summary>
public class TwilioService : ITwilioService
{
    /// <inheritdoc/>
    public void Initialize(string accountSid, string authToken)
    {
        TwilioClient.Init(accountSid, authToken);
    }

    /// <inheritdoc/>
    public async Task<AccountResource?> FetchAccountAsync(string accountSid, CancellationToken cancellationToken = default)
    {
        return await AccountResource.FetchAsync(accountSid);
    }

    /// <inheritdoc/>
    public async Task<MessageResource> CreateMessageAsync(CreateMessageOptions options, CancellationToken cancellationToken = default)
    {
        return await MessageResource.CreateAsync(options);
    }

    /// <inheritdoc/>
    public async Task<MessageResource> FetchMessageAsync(string messageSid, CancellationToken cancellationToken = default)
    {
        return await MessageResource.FetchAsync(messageSid);
    }
}