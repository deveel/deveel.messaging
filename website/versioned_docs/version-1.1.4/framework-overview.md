---
sidebar_position: 2
---

# Framework Overview

A single programming model for SMS, email, push, and chat. The framework provides the contracts and base infrastructure; you provide the business logic.

## Mental model

```
┌──────────────┐     ┌───────────────────┐     ┌──────────────────┐
│   IMessage   │ ──▶ │ IChannelConnector │ ──▶ │ OperationResult  │
│  (fluent     │     │ (send, receive,   │     │ (success or      │
│   builder)   │     │  status, health)  │     │  typed error)    │
└──────────────┘     └───────────────────┘     └──────────────────┘
```

1. **Build** an `IMessage` using the fluent `Message` builder — set sender, receiver, content, and properties
2. **Send or receive** through an `IChannelConnector` — the same interface for SMS, email, push, and chat
3. **Handle** the `OperationResult<T>` — check `.IsSuccess`, read `.Value`, or inspect `.Error` / `.ValidationErrors`

These three steps are identical regardless of which provider connector you use. Swapping from Twilio SMS to SendGrid email means changing the connector type and endpoint types — the send flow stays the same.

## Example: one message shape, two channels

```csharp
// Build the message once
var message = new MessageBuilder()
    .WithId("welcome-1")
    .FromEmail("team@example.com")
    .ToEmail("newuser@example.com")
    .WithText("Welcome to the platform!")
    .WithSubject("Welcome!")
    .Build();

// Send via SendGrid
var emailResult = await sendGridConnector.SendMessageAsync(message, ct);

// Or send the same content via Twilio SMS (just change endpoint type)
var smsMessage = new MessageBuilder()
    .WithId("welcome-1")
    .FromPhone("+15550001111")
    .ToPhone("+15550002222")
    .WithText("Welcome to the platform!")
    .Build();

var smsResult = await twilioConnector.SendMessageAsync(message, ct);
```

## Core building blocks

### Message and endpoints

**`Message` / `IMessage`** — the central unit of work. Carries an ID, sender endpoint, receiver endpoint, content, and optional properties. Construct messages using `MessageBuilder` for a fluent API, or use `new Message { ... }` object initializers for explicit construction.

**`Endpoint` / `IEndpoint`** — a typed address. Instead of passing raw strings, every address is tagged with its type: `PhoneNumber`, `EmailAddress`, `UserId`, `DeviceId`, `Url`, `Topic`, `ApplicationId`, `Id`, `Label`, or `Any`. This enables schema validation to reject an email address where a phone number is expected before any provider API call.

### Content

**`MessageContent` / `IMessageContent`** — abstract base for 12 content types:

| Type | Use case |
|---|---|
| `TextContent` | Plain text (SMS, chat, fallback) |
| `HtmlContent` | Rich HTML (email) |
| `MediaContent` | Images, audio, video, documents |
| `BinaryContent` | Raw binary data |
| `JsonContent` | Structured JSON payloads |
| `LocationContent` | Geo coordinates (chat) |
| `TemplateContent` | Provider-side template rendering |
| `MultipartContent` | Combination of multiple parts |
| `ButtonContent` | Single interactive button (URL, postback, or phone number) |
| `QuickReplyContent` | One-tap quick reply option above the keyboard |
| `CarouselContent` | Horizontally scrollable set of cards with buttons |
| `ListPickerContent` | Vertically scrollable list of selectable items |

The last four types implement the `IInteractiveContent` marker interface, which extends `IMessageContent`. Connectors that do not support interactive elements can check for this interface to reject unsupported content types early, during schema validation.

### Schema

**`IChannelSchema`** — the contract that defines what a connector can do and what it needs to operate. Every connector has exactly one schema that describes:

- Its capabilities (send, receive, status query, bulk, templates, health checks, etc.)
- Required and optional connection parameters (API keys, endpoints, timeouts)
- Which endpoint types it handles and in which direction (send, receive, or both)
- Which content types it supports
- What message properties it understands and their validation rules
- What authentication methods it supports

**`ChannelSchema`** — the built-in fluent implementation of `IChannelSchema`. You construct it inline or via a schema factory.

### Connectors

**`IChannelConnector`** — the primary contract for messaging operations:

- `InitializeAsync` / `ShutdownAsync` — lifecycle control of the connector instance
- `SendMessageAsync` / `SendBatchAsync` — outbound messaging operations
- `ReceiveMessageStatusAsync` — outbound status change signaling
- `GetMessageStatusAsync` — pulls the delivery state from the provider
- `ReceiveMessagesAsync` — inbound messaging operation
- `TestConnectionAsync` / `GetHealthAsync` — diagnostic operations of the connector
- `ValidateMessageAsync` — per-message validation

**`ChannelConnectorBase`** — abstract base class that implements `IChannelConnector`. Provides state management, capability validation, message validation, authentication integration, structured logging scopes, and standardized error wrapping. To build a connector you override 4 abstract methods and optionally 7 virtual methods.

### Messaging client

**`IMessagingClient`** — a high-level facade that resolves connectors from DI (by name or by type), handles lazy initialization, and exposes the 4 most common operations (`SendAsync`, `ReceiveAsync`, `GetStatusAsync`, `ReceiveMessageStatusAsync`). Implements `IDisposable` and `IAsyncDisposable` — when the client is disposed, all cached connectors are shut down gracefully. Callers do not manage connector state directly. Register it via `.AddClient()` on the `MessagingBuilder`.

The client supports three resolution strategies:

- **By name** — `SendAsync("channel-name", message)` resolves pre-configured named connectors via `IChannelConnectorResolver` (backed by DI keyed services)
- **By type** — `SendAsync<TConnector>(message)` resolves anonymous (unnamed) connectors from DI by type
- **At runtime with settings** — `SendAsync("name", settings, message)` or `SendAsync<TConnector>(settings, message)` creates connectors on-the-fly from runtime-provided `ConnectionSettings`, using schema discovery and `ActivatorUtilities.CreateInstance`

### Connector type catalog

**`ConnectorTypeCatalog`** — a singleton registry that maps channel names to connector types (without connection settings). Populated at startup via `AddConnectorType<TConnector>()`. Used by the client's runtime overloads to know which connector type to instantiate for a given channel name.

### Connector resolver

**`IChannelConnectorResolver`** — an abstraction for resolving pre-configured connectors by name. The default implementation (`ServiceProviderConnectorResolver`) delegates to DI keyed services. The client uses this internally for name-based resolution of statically registered connectors.

### Message builder

**`MessageBuilder`** — an alternative fluent builder that separates construction from the model. Use it when the `Message` class's self-builder pattern is not desired:

```csharp
var message = new MessageBuilder()
    .WithId("msg-1")
    .FromPhone("+15551234567")
    .ToEmail("user@example.com")
    .WithText("Hello!")
    .WithSubject("Greeting")
    .Build();
```

### Results

**`OperationResult<T>`** — every operation returns this. It carries:
- `.IsSuccess` / `.IsFailure` / `.IsValidationFailure` — status checks
- `.Value` — the result value on success
- `.Error` — `IMessagingError` with `ErrorCode` and `ErrorMessage` on failure
- `.ValidationErrors` — collection of `ValidationResult` on validation failure

### Registry

**`IChannelSchemaRegistry`** — provides read-only access to all registered connector schemas. Use it to discover schemas at runtime (e.g., to drive a UI or validate a derived schema).

## Package responsibilities

| Package | Role | Dependencies |
|---|---|---|
| `Ratatosk.Abstractions` | Message model, `MessageBuilder`, endpoints, content types (including interactive). Pure model — no infrastructure. | None |
| `Ratatosk` | `AddMessaging()` DI entry point, `MessagingBuilder`, `IMessagingClient` facade (disposable), `ChannelConnectorFactory`. | `Connector.Abstractions` |
| `Ratatosk.Connector.Abstractions` | Interfaces for connectors, schemas, auth, and result types. Contracts only. | `Abstractions` |
| `Ratatosk.Connectors` | `ChannelConnectorBase`, `ChannelSchema` builder, `ChannelSchemaRegistry`, authentication manager, `ChannelConnectorBuilder`. | `Connector.Abstractions` |
| `Ratatosk.Connector.*` | Provider-specific implementations. Each references `Connectors` or `Connector.Abstractions`. | Provider SDK + `Connectors` |
| `Ratatosk.Senders` | Sender identity infrastructure: `ISenderRepository<TSender>`, `ISenderResolver`, `SenderManager<TSender>`, distributed cache, and `MessageBuilder.FromSender()`. | `Abstractions` |
| `Ratatosk.Senders.InMemory` | In-memory sender repository for development and testing, with optional seed data. | `Ratatosk.Senders` |
| `Ratatosk.Senders.EntityFramework` | EF Core persistence for the sender registry (`SenderDbContext`, `EntitySenderRepository`, `DbSender`). | `Ratatosk.Senders` + EF Core |

## What the framework does not do

The framework focuses on the messaging contract and connector consistency. It intentionally **does not** include:

- **Queueing** — messages are sent immediately; use a background queue (e.g., RabbitMQ, Azure Service Bus) for async delivery
- **Scheduling** — no built-in delayed delivery; combine with a scheduler library
- **Persistence** — messages are in-memory; persist to your database for auditing and retries
- **Retries** — transient failures are surfaced as errors; implement retry policies (Polly) at the application layer
- **Templating engines** — template rendering is delegated to providers (SendGrid, Twilio, etc.)
- **Business workflows** — approval chains, fallback routing, and orchestration are your application's responsibility

## Design decisions

- **MessageBuilder** — the fluent `MessageBuilder` class (`new MessageBuilder().WithX().WithY().Build()`) separates construction from the model, and supports connector-specific extension methods (e.g., `WithTitle()`, `WithParseMode()`). Each `With*()` returns the builder for chaining. Explicit `new Message { ... }` syntax is also supported for direct construction.
- **Raw values from overrides** — when extending `ChannelConnectorBase`, your core methods return raw values (like `SendResult`), not `OperationResult<T>`. The base class wraps them, catches exceptions, and handles validation errors. This keeps connector code focused on API translation.
- **Schema-first** — every connector has an `IChannelSchema` that drives validation, capability checks, and documentation. This makes connector behavior predictable and testable without calling provider APIs.
