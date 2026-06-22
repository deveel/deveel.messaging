---
sidebar_position: 1
---

# Sender Management

Sender management is the operational side of sender identities: defining who is allowed to send, where those identities are stored, and how teams maintain them over time.

This page focuses on **managing sender identities** (registration, lifecycle, storage, governance).
For runtime behavior (how a sender is selected for a message), see [Sender Resolution](sender-resolution.md).

## Why sender management exists

Hardcoding sender addresses in application code (`FromEmail("support@example.com")`) creates deployment friction:

- Sender changes require a code change and redeploy
- Different environments (dev/staging/prod) need different sender values
- Operations teams cannot manage sender lifecycles independently
- Auditability is weak when sender ownership is not centralized

Sender management solves this by moving sender identities into a managed registry and allowing message code to reference logical names.

## Sender management vs sender resolution

Use this split to reason about the feature:

- **Sender management**: create, update, activate/deactivate, and store sender identities
- **Sender resolution**: at send time, map message sender input to a concrete active sender

If you are building admin flows, seed scripts, or storage integrations, stay on this page.
If you are debugging send-time behavior, fallback logic, or cache hits, use [Sender Resolution](sender-resolution.md).

## Sender identity model

A sender identity is typically defined by:

- Logical name (for example, `support`, `billing`, `alerts`)
- Address (email, phone number, bot id, and so on)
- Endpoint type
- Display name
- Active state

The logical name is stable for application code. The concrete address can evolve without changing message composition code.

## Packages and installation

Install the core sender management package:

```bash
dotnet add package Ratatosk.Senders
```

Choose one storage implementation:

```bash
dotnet add package Ratatosk.Senders.InMemory
```

```bash
dotnet add package Ratatosk.Senders.EntityFramework
```

## Registration patterns

### In-memory store (development/testing)

```csharp
using Ratatosk.Senders;

builder.Services.AddMessaging()
    .AddSenders(cfg => cfg
        .UseInMemoryStore(seedSenders));
```

### Entity Framework store (production)

```csharp
using Ratatosk.Senders;

builder.Services.AddMessaging()
    .AddSenders(cfg => cfg
        .UseEntityFramework(options =>
            options.UseSqlServer(connectionString)));
```

### Custom repository

```csharp
using Ratatosk.Senders;

builder.Services.AddMessaging()
    .AddSenders(cfg => cfg
        .ConfigureCacheOptions(o => o.DefaultTtl = TimeSpan.FromMinutes(10)));

builder.Services.AddScoped<ISenderRepository<MySender>, MySenderRepository>();
builder.Services.AddScoped<ISenderValidator<MySender>, SenderValidator<MySender>>();
builder.Services.AddScoped<SenderManager<MySender>>();
builder.Services.AddScoped<ISenderRepository<ISender>>(sp =>
    new SenderRepositoryAdapter<MySender>(sp.GetRequiredService<ISenderRepository<MySender>>()));
```

## Managing sender identities

### Create

```csharp
var sender = new SenderBuilder()
    .WithName("support")
    .WithDisplayName("Customer Support")
    .WithAddress("support@example.com")
    .WithEndpointType(EndpointType.EmailAddress)
    .AsActive()
    .Build();

await repository.AddAsync(sender, ct);
```

### Lookup and update

```csharp
var existing = await repository.FindByNameAsync("support", ct);
if (existing is not null)
{
    existing.Update(displayName: "Support Team");
    await repository.UpdateAsync(existing, ct);
}
```

### Activate / deactivate

```csharp
var active = await senderManager.ActivateAsync("sender-id");
var inactive = await senderManager.DeactivateAsync("sender-id");
```

### Query active senders

```csharp
var result = await senderManager.GetAllActiveAsync();
if (result.IsSuccess)
{
    var senders = result.Value;
    // render in admin UI or validate operational readiness
}
```

## Seeding strategy

Seed sender identities for each environment so default routing and operational flows are ready on first startup.

```csharp
var seedSenders = new[]
{
    new SenderEntity
    {
        Id = "seed-support",
        Name = "support",
        DisplayName = "Customer Support",
        Address = "support@example.com",
        Type = EndpointType.EmailAddress
    },
    new SenderEntity
    {
        Id = "seed-alerts",
        Name = "alerts",
        DisplayName = "Alert System",
        Address = "+15551234567",
        Type = EndpointType.PhoneNumber
    }
};

seedSenders[0].Activate();
seedSenders[1].Activate();
```

Recommended conventions:

- Reserve short logical names for business functions (`support`, `billing`, `alerts`)
- Keep names environment-agnostic; change addresses per environment
- Avoid deleting sender identities used by historical messages; deactivate instead

## Operational governance

Treat sender identities as operational configuration with controlled ownership.

- **Ownership**: define who can create or deactivate senders
- **Change safety**: validate address and type before activation
- **Auditability**: capture who changed a sender and why
- **Rollback**: deactivate a bad sender and reactivate the previous one quickly

For high-volume systems, combine this with a lightweight admin UI and an approval workflow for production changes.

## Using managed senders in message code

Message composition should reference logical names, not raw addresses:

```csharp
var message = new MessageBuilder()
    .FromSender("support")
    .ToEmail("user@example.com")
    .WithText("Hello!")
    .Build();

await client.SendAsync("sendgrid", message, ct);
```

This keeps business code stable while operations evolves sender assignments.

## Common migration path

When adopting sender management in an existing codebase:

1. Identify hardcoded sender values in message builders
2. Create equivalent sender identities in the registry
3. Replace `FromEmail` / `FromPhone` calls with `FromSender("...")`
4. Validate by sending through staging and checking resolution logs
5. Deactivate deprecated sender identities after rollout

## Troubleshooting (management side)

- **Sender cannot be found in admin flows**: verify repository registration and storage connection
- **Activation fails**: confirm validation rules (name, address, endpoint type)
- **Unexpected duplicates**: enforce uniqueness on logical name and endpoint in storage
- **Environment drift**: run startup checks to ensure required sender names exist

## Related documentation

- [Sender Resolution](sender-resolution.md) - how send-time resolution, cache, and fallback work
- [Message model](../messaging-model.md#sender-identities) - sender types and sender references
- [Quickstart](../quickstart.md) - end-to-end messaging setup
