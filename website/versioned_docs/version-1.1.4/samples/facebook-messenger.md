---
sidebar_position: 4
---

# Facebook Messenger Sample

Demonstrates the Facebook Messenger connector (`FacebookMessengerConnector`): sending and receiving messages through a Facebook Page, schema validation, and webhook inbound processing.

## What it shows

- Building a `Message` with text or media content targeted at a Facebook Page recipient
- Sending via `IMessagingClient` using the `facebook` channel name
- Validating messages against the Facebook channel schema before sending
- Parsing inbound Facebook webhook payloads into `IMessage` objects
- Retry policy with exponential backoff for rate limit errors (`RATE_LIMITED`)
- Viewing the channel schema, capabilities, and parameters
- Checking connector runtime status

## Run

```bash
cd samples/facebook-messenger
./run.sh -- facebook <command>
```

## Commands

| Command | Description |
|---------|-------------|
| `schema [name]` | Show the Facebook channel schema |
| `configure` | Prompt for Page Access Token, Page ID, and other settings |
| `send` | Build and send a live Facebook message interactively |
| `validate -k <kind>` | Validate a sample message (`text` or `media`) |
| `status` | Show connector runtime status |
| `receive [-f file]` | Parse a sample Facebook webhook payload |

## Example

```bash
./run.sh -- facebook configure
./run.sh -- facebook send
```

You will be prompted to select the message type (Text or Media) and compose the content.
