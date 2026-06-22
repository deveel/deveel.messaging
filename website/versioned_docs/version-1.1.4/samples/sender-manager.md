---
sidebar_position: 7
---

# Sender Manager Sample

Demonstrates sender identity management via an ASP.NET Core Web API. The sample uses `ISenderRepository<Sender>` backed by `SenderManager<Sender>` and an in-memory store to provide full CRUD over sender identities, and shows how those identities integrate with the messaging pipeline.

## What it shows

- Registering sender infrastructure with `AddSenders()` and configuring the in-memory store via `UseInMemoryStore()` on the builder
- Creating, reading, updating, and deleting sender identities through REST endpoints
- Sending messages with `SenderRef` — the connector resolves the identity at send time
- Polymorphic sender input via `[JsonDerivedType]` on `IEndpoint`
- Creating, reading, updating, and deleting sender identities through REST endpoints
- Sending messages with `SenderRef` — the connector resolves the identity at send time
- Polymorphic sender input via `[JsonDerivedType]` on `IEndpoint`

## Run

```bash
cd samples/sender-manager
dotnet run
```

The API server starts on `http://localhost:5000`.

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/senders` | List all registered senders |
| `GET` | `/api/senders/{id}` | Get a sender by ID |
| `POST` | `/api/senders` | Create a new sender |
| `PUT` | `/api/senders/{id}` | Update an existing sender |
| `DELETE` | `/api/senders/{id}` | Delete a sender |
| `POST` | `/api/messages` | Send a message through the configured channel |

## Examples

### Create a sender

```http
POST /api/senders
Content-Type: application/json

{
  "name": "marketing",
  "displayName": "Marketing Team",
  "address": "marketing@example.com",
  "endpointType": "EmailAddress",
  "isActive": true
}
```

### Send a message using a sender reference

```http
POST /api/messages
Content-Type: application/json

{
  "channel": "sendgrid",
  "sender": { "$type": "senderref", "senderName": "marketing" },
  "receiverAddress": "user@example.com",
  "text": "Hello from the Marketing Team!"
}
```

The `SenderRef` is resolved by the connector at send time — the API never queries the repository manually.

### Send a message using an inline endpoint

```http
POST /api/messages
Content-Type: application/json

{
  "channel": "sendgrid",
  "sender": { "$type": "email", "address": "direct@example.com", "name": "Direct", "displayName": "Direct", "isActive": true },
  "receiverAddress": "user@example.com",
  "text": "Hello from an inline sender!"
}
```

Available `$type` values: `endpoint`, `sender`, `senderref`, `email`, `phone`, `alphanumeric`, `bot`.

## How resolution works

The sample never resolves the sender in the API handler. It passes a `SenderRef` (`{ "$type": "senderref", "senderName": "marketing" }`) into the message, and the connector pipeline handles resolution:

1. `MessageBuilder.From(sender)` sets `Message.Sender` to the `SenderRef`
2. `IMessagingClient.SendAsync("sendgrid", message)` delegates to the connector
3. `ChannelConnectorBase.SendMessageAsync` calls `ResolveSenderAsync`
4. `SenderResolver` looks up the name in the repository (via cache → `ISenderRepository<SenderEntity>` → `InMemorySenderRepository`)
5. The resolved sender replaces the `SenderRef` on the message before validation and dispatch

## Configuration

See the [`samples/sender-manager/README.md`](https://github.com/deveel/ratatosk/blob/main/samples/sender-manager/README.md) file in the repository for the full configuration reference and SendGrid setup.
