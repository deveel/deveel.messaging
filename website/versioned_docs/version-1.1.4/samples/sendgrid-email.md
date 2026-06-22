---
sidebar_position: 5
---

# SendGrid Email Sample

Demonstrates the SendGrid email connector (`SendGridEmailConnector`): sending transactional emails, schema validation, and event webhook processing.

## What it shows

- Building a `Message` with HTML or template content for email delivery
- Sending via `IMessagingClient` using the `sendgrid` channel name
- Validating messages against the SendGrid channel schema before sending
- Parsing inbound email webhook payloads and delivery event callbacks
- Retry policy with exponential backoff for transient failures (`RATE_LIMITED`, `SERVER_ERROR`)
- Viewing the channel schema, capabilities, and parameters
- Checking connector runtime status

## Run

```bash
cd samples/sendgrid-email
./run.sh -- sendgrid <command>
```

## Commands

| Command | Description |
|---------|-------------|
| `schema [name]` | Show the SendGrid channel schema |
| `configure` | Prompt for API Key, sender/recipient addresses, and other settings |
| `send` | Build and send a live email interactively |
| `validate -k <kind>` | Validate a sample message (`html` or `template`) |
| `status` | Show connector runtime status |
| `receive [-f file] [-m mode]` | Parse a sample inbound webhook payload |
| `receive-status [-f file] [-m mode]` | Parse a sample delivery event callback |

## Example

```bash
./run.sh -- sendgrid configure
./run.sh -- sendgrid send
```

You will be prompted to select the message type (HTML or Template), enter sender/recipient, subject, and body.
