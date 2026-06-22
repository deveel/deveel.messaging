---
sidebar_position: 1
---

# Twilio SMS & WhatsApp Sample

Demonstrates the Twilio connectors (`TwilioSmsConnector` and `TwilioWhatsAppConnector`): sending SMS and WhatsApp messages through the Twilio API, status callbacks, and webhook inbound processing.

## What it shows

- Building a `Message` for SMS or WhatsApp delivery
- Sending via `IMessagingClient` using the `sms` or `whatsapp` channel name
- Validating messages against the Twilio channel schema before sending
- Parsing inbound webhook payloads and delivery status callbacks
- Using Messaging Service SID for SMS sending
- Retry policy with exponential backoff for transient failures (`RATE_LIMITED`, `SERVICE_UNAVAILABLE`)
- Viewing channel schemas, capabilities, and parameters
- Checking connector runtime status

## Run

```bash
cd samples/twilio
./run.sh -- twilio <command>
```

## Root commands

| Command | Description |
|---------|-------------|
| `schema [name]` | Show the Twilio channel schema |
| `configure` | Prompt for Account SID, Auth Token, and other settings |

## SMS subcommands

```bash
./run.sh -- twilio sms <command>
```

| Command | Description |
|---------|-------------|
| `send` | Build and send a live SMS message |
| `validate` | Validate a sample SMS message |
| `status` | Show SMS connector runtime status |
| `receive [-f file] [-m mode]` | Parse a sample SMS webhook payload |
| `receive-status [-f file] [-m mode]` | Parse a sample SMS status callback |

## WhatsApp subcommands

```bash
./run.sh -- twilio whatsapp <command>
```

| Command | Description |
|---------|-------------|
| `send` | Build and send a live WhatsApp message |
| `validate -k <kind>` | Validate a sample WhatsApp message (`text` or `template`) |
| `status` | Show WhatsApp connector runtime status |
| `receive [-f file] [-m mode]` | Parse a sample WhatsApp webhook payload |
| `receive-status [-f file] [-m mode]` | Parse a sample WhatsApp status callback |

## Example

```bash
./run.sh -- twilio configure
./run.sh -- twilio sms send
```

You will be prompted to select the sender mode (From number or Messaging service), enter the recipient, and compose the message body.
