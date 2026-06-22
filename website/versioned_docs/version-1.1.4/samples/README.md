# Samples

These sample applications demonstrate how to use the Ratatosk framework with each supported channel provider. Each sample is a standalone project with its own solution file and README.

## Available samples

| Sample | Channel | Type | Description |
|--------|---------|------|-------------|
| [Facebook Messenger](facebook-messenger.md) | Facebook Messenger | Console | Send/receive messages via Facebook Page |
| [Telegram Bot](telegram-bot.md) | Telegram Bot | Console | Send messages via Telegram Bot API |
| [SendGrid Email](sendgrid-email.md) | SendGrid | Console | Send transactional emails |
| [Firebase Push](firebase-push.md) | Firebase FCM | Console | Send push notifications |
| [Twilio SMS & WhatsApp](twilio.md) | Twilio | Console | Send SMS and WhatsApp messages |
| [Multi-Connector](multi-connector.md) | All channels | ASP.NET Core API | REST API exposing all connectors |
| [Sender Manager](sender-manager.md) | SendGrid | ASP.NET Core API | CRUD sender identities + message send with polymorphic sender resolution |

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Credentials for the messaging service(s) you want to test (see each sample's README)

## How the samples work

The sample projects reference the framework source projects directly via `<ProjectReference>` in their `.csproj` files. This means:

- The framework is built automatically when you build or run a sample
- Changes to framework source code are immediately reflected in the sample
- No pre-built DLLs or manual build steps are required

Each sample has its own solution (`.sln`) file that includes both the sample project and the framework projects it depends on.

## Running a sample

Each sample includes a `run.sh` convenience script:

```bash
cd samples/<name>
./run.sh -- <connector> <command>
```

Use the `-v` flag to enable verbose logging:

```bash
./run.sh -v -- <connector> <command>
```

To build without running:

```bash
dotnet build
```

To open in an IDE, open the sample's `.sln` file (e.g., `samples/facebook-messenger/Facebook.sln`).

## Per-sample setup

Before running a sample, you need credentials for the messaging service. Each sample's README includes:

- Provider-specific setup instructions
- Configuration wizard (`./run.sh -- <name> configure`)
- Available commands (send, validate, receive, status, etc.)
- Environment variable reference
