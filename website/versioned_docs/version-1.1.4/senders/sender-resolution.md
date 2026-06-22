---
sidebar_position: 2
---

# Sender Resolution

Sender resolution is the runtime process that determines the concrete sender used for a message right before connector validation and dispatch.

This page focuses on **send-time behavior** (resolution paths, fallback, cache, and diagnostics).
For sender lifecycle and storage operations, see [Sender Management](sender-management.md).

## What sender resolution does

When a message is sent, the framework evaluates the sender input and resolves it to an active sender identity.

Resolution supports three common inputs:

- A logical sender reference (for example, `FromSender("support")`)
- An inline sender instance (for example, `new EmailSender(...)`)
- No sender on the message (fallback to connector settings)

## Resolution flow

At a high level:

1. Build resolution context from message sender and connection settings
2. Choose the path based on the sender input type
3. Resolve from cache or repository
4. Enforce active-state checks
5. Replace message sender with the resolved identity
6. Continue with schema validation and connector send

This means the message sent to the provider always uses a concrete sender.

## Resolution paths

### 1) Logical sender reference (`FromSender`)

This is the preferred pattern for business code.

```csharp
var message = new MessageBuilder()
    .FromSender("support")
    .ToEmail("user@example.com")
    .WithText("Your request has been received.")
    .Build();

await client.SendAsync("sendgrid", message, ct);
```

Behavior:

- Lookup by logical name
- Return only active senders
- Cache successful results by name and endpoint

### 2) Inline sender (`From(new EmailSender(...))`)

Useful when sender details are known at composition time.

```csharp
var message = new MessageBuilder()
    .From(new EmailSender("noreply@example.com", name: "No Reply"))
    .ToEmail("user@example.com")
    .WithText("Password reset instructions")
    .Build();

await client.SendAsync("sendgrid", message, ct);
```

Behavior:

- Lookup by endpoint (type + address)
- Enforce active-state checks
- Cache successful results

### 3) No sender provided (default fallback)

If no sender is set on the message, the resolver falls back to connection settings.

```csharp
var runtimeSettings = ConnectionSettings.Parse(
    "DefaultSenderName=billing;DefaultSenderAddress=billing@example.com;DefaultSenderType=EmailAddress;ApiKey=...");

var message = new MessageBuilder()
    .ToEmail("user@example.com")
    .WithText("Your invoice is ready")
    .Build();

await client.SendAsync("sendgrid", runtimeSettings, message, ct);
```

Fallback sources include:

- `DefaultSenderName`, `DefaultSenderAddress`, `DefaultSenderType`
- Connector-native defaults such as `From`, when supported

## Cache behavior

Resolution uses a sender cache to reduce repeated repository reads.

- Default TTL is 5 minutes
- Entries are indexed by both logical name and endpoint
- Cached entries are reused across sends until expiration

Adjust cache TTL when your sender data changes frequently:

```csharp
builder.Services.AddMessaging()
    .AddSenders(cfg => cfg
        .ConfigureCacheOptions(o => o.DefaultTtl = TimeSpan.FromMinutes(15)));
```

Use distributed cache in scaled deployments so all instances see consistent resolution behavior.

## Failure behavior

Resolution can fail for expected operational reasons:

- Sender name does not exist in the registry
- Sender exists but is inactive
- Endpoint lookup returns no matching sender
- Repository or cache backend is unavailable

In these cases, send operations return a failed `OperationResult` and connector dispatch does not continue.

## Multi-environment and multi-tenant guidance

For stable runtime behavior:

- Keep logical sender names consistent across environments
- Use environment-specific addresses for each logical sender
- If your app is multi-tenant, include tenant context in your sender governance model and naming strategy

A common pattern is prefixing or scoping sender names by tenant or domain area when needed (for example, `tenant-a.support`).

## Diagnostics and observability

Sender resolution is logged with structured events (cache hits, misses, inactive sender, lookup errors).

When troubleshooting runtime issues:

1. Confirm sender name used by message code
2. Check sender active state in the registry
3. Verify cache TTL and invalidation expectations
4. Validate connector settings for default fallback values
5. Inspect logs for resolution warnings/errors before connector send

## Design recommendations

- Prefer logical references (`FromSender`) in application code
- Keep sender identity management in operational/admin workflows
- Use activation/deactivation instead of hard deletes
- Treat sender changes as configuration changes with audit trails

## Related documentation

- [Sender Management](sender-management.md) - registry lifecycle, storage, and governance
- [Message model](../messaging-model.md#sender-identities) - sender types and endpoint model
- [Quickstart](../quickstart.md) - end-to-end setup and messaging flows
