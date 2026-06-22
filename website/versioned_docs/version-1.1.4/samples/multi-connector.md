---
sidebar_position: 2
---

# Multi-Connector Sample

Demonstrates running multiple Ratatosk connectors simultaneously in a single ASP.NET Core web application, exposing each through REST API endpoints. Supports all six channel providers: Facebook Messenger, Firebase Cloud Messaging, SendGrid Email, Telegram Bot, Twilio SMS, and Twilio WhatsApp.

## What it shows

- Registering multiple connectors with distinct names in the same DI container
- Using `IMessagingClient` to route messages to different channels by name
- Exposing channel operations through REST endpoints
- Sending messages, receiving webhooks, and checking status over HTTP
- Handling all connector types in a single application with shared configuration
- Shared retry policy applied to all connectors (exponential backoff, rate limit retries)

## Run

```bash
cd samples/multi-connector
./run.sh
```

The API server starts on `http://localhost:5000`.

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/schemas` | List all registered channel schemas |
| `POST` | `/{channel}/message` | Send a message through the specified channel |
| `POST` | `/{channel}/receive` | Submit a webhook payload for inbound processing |
| `POST` | `/{channel}/message/status` | Submit a delivery status update |
| `GET` | `/{channel}/status` | Get connector runtime status |

`{channel}` is one of: `facebook`, `firebase`, `sendgrid`, `telegram`, `sms`, `whatsapp`.

## Example

```http
POST /sendgrid/message
Content-Type: application/json

{
  "id": "msg-001",
  "sender": { "type": "email", "address": "sender@example.com" },
  "receiver": { "type": "email", "address": "recipient@example.com" },
  "content": { "type": "text", "text": "Hello from the API!" },
  "properties": {
    "subject": { "name": "Subject", "value": "API Test" }
  }
}
```

## Configuration

Configure credentials in `appsettings.json` or via environment variables. See the [`samples/multi-connector/README.md`](https://github.com/deveel/ratatosk/blob/main/samples/multi-connector/README.md) file in the repository for the full configuration reference.
