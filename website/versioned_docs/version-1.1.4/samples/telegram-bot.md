---
sidebar_position: 6
---

# Telegram Bot Sample

Demonstrates the Telegram Bot connector (`TelegramBotConnector`): sending messages via the Telegram Bot API, schema validation, and webhook update processing.

## What it shows

- Building a `Message` with text, media, or location content for a Telegram chat
- Sending via `IMessagingClient` using the `telegram` channel name
- Validating messages against the Telegram channel schema before sending
- Retry policy with exponential backoff for rate limit errors (`RATE_LIMITED`, `RETRY_AFTER`)
- Viewing the channel schema, capabilities, and parameters
- Checking connector runtime status

## Run

```bash
cd samples/telegram-bot
./run.sh -- telegram <command>
```

## Commands

| Command | Description |
|---------|-------------|
| `schema [name]` | Show the Telegram channel schema |
| `configure` | Prompt for Bot Token, Chat ID, and other settings |
| `send` | Build and send a live Telegram message interactively |
| `validate -k <kind>` | Validate a sample message (`text` or `location`) |
| `status` | Show connector runtime status |

## Example

```bash
./run.sh -- telegram configure
./run.sh -- telegram send
```

You will be prompted to select the message type (Text, Media, or Location) and compose the content.
