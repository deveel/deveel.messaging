---
sidebar_position: 3
---

# Firebase Cloud Messaging Sample

Demonstrates the Firebase push connector (`FirebasePushConnector`): sending push notifications through Firebase Cloud Messaging (FCM) to devices and topics.

## What it shows

- Building a `Message` for push notification delivery to a device token or topic
- Sending via `IMessagingClient` using the `firebase` channel name
- Sending batch notifications to multiple targets
- Using dry-run mode for testing without actual delivery
- Validating messages against the Firebase channel schema before sending
- Retry policy with exponential backoff for transient provider errors (`UNAVAILABLE`, `INTERNAL`, `DEADLINE_EXCEEDED`)
- Viewing the channel schema, capabilities, and parameters
- Checking connector runtime status

## Run

```bash
cd samples/firebase-push
./run.sh -- firebase <command>
```

## Commands

| Command | Description |
|---------|-------------|
| `schema [name]` | Show the Firebase channel schema |
| `configure` | Prompt for Project ID, service account key, and other settings |
| `send` | Build and send a live push notification |
| `batch` | Send a batch of push notifications |
| `validate -k <kind>` | Validate a sample message (`device` or `topic`) |
| `status` | Show connector runtime status |

## Example

```bash
./run.sh -- firebase configure
./run.sh -- firebase send
```

You will be prompted to select the target type (Device or Topic) and compose the notification message.
