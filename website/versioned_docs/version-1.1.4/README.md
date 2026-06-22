---
sidebar_position: 1
---

[![NuGet](https://img.shields.io/nuget/v/Ratatosk.Abstractions.svg?label=NuGet)](https://www.nuget.org/packages/Ratatosk.Abstractions/)
[![codecov](https://codecov.io/gh/deveel/ratatosk/graph/badge.svg)](https://codecov.io/gh/deveel/ratatosk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/deveel/ratatosk/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)](https://dotnet.microsoft.com/)
[![Documentation](https://img.shields.io/badge/docs-available-blue)](https://ratatosk.deveel.org)

<p align="center">
  <img src="/img/ratatosk-full-logo.png" alt="Ratatosk" width="400"/>
</p>

# Ratatosk

> **Note:**
> This project was originally named **Deveel Messaging**: on the day _25.05.2026_ it has been rebranded as **Ratatosk**, after the mythological squirrel that runs up and down Yggdrasil, the World Tree, carrying messages between the eagle at the top and the dragon at the roots — a fitting symbol for a messaging framework.

A .NET framework for multi-channel messaging. It provides a single, provider-agnostic programming model for sending and receiving messages through different channel backends — SMS, email, push notifications, and instant messaging.

The same `Message` object, the same `IChannelConnector` interface, and the same `OperationResult<T>` handling pattern work for Twilio, SendGrid, Firebase, Facebook Messenger, Telegram, and any custom provider you implement.

```csharp
var message = new MessageBuilder()
    .WithId("order-confirm-123")
    .FromPhone("+15550001111")
    .ToPhone("+15550002222")
    .WithText("Your order has been confirmed!")
    .Build();

var result = await connector.SendMessageAsync(message, ct);
```

The `Message` and `connector.SendMessageAsync()` call stay identical regardless of whether the connector is Twilio SMS, SendGrid email, Firebase push, or any other provider. Only the connector type and endpoint types change.

## Motivations

Modern applications need to reach users across multiple channels — SMS for transaction alerts, email for receipts and marketing, push notifications for mobile engagement, and chat apps for conversational support. Each channel has its own SDK, its own message format, its own authentication model, and its own error semantics. Wiring each one independently leads to:

- **Duplicated infrastructure** — every channel reimplements the same patterns: connection lifecycle, retries, logging, credential management, input validation
- **Provider lock-in** — business logic becomes coupled to Twilio's SMS model or SendGrid's email model; switching providers means rewriting
- **Inconsistent developer experience** — each SDK has different conventions, error handling, and configuration models
- **Difficult cross-channel routing** — sending the same notification through SMS and email requires building separate message objects and calling separate APIs

Ratatosk solves these problems by defining a **unified message model** (`IMessage`), a **contract-driven connector interface** (`IChannelConnector`), and a **schema system** (`IChannelSchema`) that lets each connector declare its capabilities and constraints. Business code depends only on the abstractions; channel-specific details are encapsulated behind the connector boundary.

The framework is deliberately focused on the messaging contract and connector consistency. It does not include queueing, scheduling, persistence, or retries — those concerns belong to your application layer, where you can choose the tools that fit your architecture.

## Features

- **Unified message model** — `Message` with fluent builder, dedicated `MessageBuilder`, typed endpoints, and 12 content types (text, HTML, media, binary, JSON, location, template, multipart, button, quick reply, carousel, list picker)
- **Schema-driven validation** — every connector declares its capabilities, parameters, and constraints via `IChannelSchema`; validate messages before they reach the provider
- **Pluggable authentication** — API key, token, basic auth, OAuth 2.0 client credentials, Firebase service account, or custom providers
- **DI-first design** — `AddMessaging()` + `AddConnector<T>()` integration with `Microsoft.Extensions.DependencyInjection`
- **Standardized results** — all operations return `OperationResult<T>` with success/failure semantics
- **Schema derivation** — derive restricted schemas from a master for feature tiers or environment-specific restrictions
- **IMessagingClient facade** — disposable high-level client with lazy initialization and named channel routing; simplifies connector lifecycle
- **Extensible** — implement `ChannelConnectorBase` to add any provider; built-in logging scopes, state management, and error wrapping

## Packages

| Package | Description | NuGet |
|---|---|---|
| `Ratatosk.Abstractions` | Message model with fluent builder, dedicated `MessageBuilder`, typed endpoints, 12 content types (including interactive), properties, and batch support. No external dependencies. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Abstractions)](https://nuget.org/packages/Ratatosk.Abstractions) |
| `Ratatosk` | DI registration (`AddMessaging`), `IMessagingClient` facade (disposable), connector factory, and service collection extensions. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk)](https://nuget.org/packages/Ratatosk) |
| `Ratatosk.Connector.Abstractions` | Contracts for connectors, schemas, authentication, validation, and result types. Reference when building custom connector libraries. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Connector.Abstractions)](https://nuget.org/packages/Ratatosk.Connector.Abstractions) |
| `Ratatosk.Connectors` | Abstract connector base class (`ChannelConnectorBase`) with state management and error wrapping, fluent schema builder (`ChannelSchema`), schema registry, auth manager, and connector builder API. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Connectors)](https://nuget.org/packages/Ratatosk.Connectors) |
| `Ratatosk.Twilio` | Twilio SMS, MMS, and WhatsApp messaging with status callbacks and template support. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Twilio)](https://nuget.org/packages/Ratatosk.Twilio) |
| `Ratatosk.Sendgrid` | SendGrid transactional and bulk email with HTML, multipart, templates, attachments, and event webhook processing. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Sendgrid)](https://nuget.org/packages/Ratatosk.Sendgrid) |
| `Ratatosk.Firebase` | Firebase Cloud Messaging push notifications for device tokens and topics, with batch sends and dry-run mode. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Firebase)](https://nuget.org/packages/Ratatosk.Firebase) |
| `Ratatosk.Facebook` | Facebook Messenger Page-based messaging with text, media, quick replies, and webhook inbound processing. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Facebook)](https://nuget.org/packages/Ratatosk.Facebook) |
| `Ratatosk.Telegram` | Telegram bot messaging with rich text, media, locations, and webhook-based update processing. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Telegram)](https://nuget.org/packages/Ratatosk.Telegram) |
| `Ratatosk.Senders` | Sender identity infrastructure: `ISenderRepository<TSender>`, `ISenderResolver`, cache, per-connector configuration, and `MessageBuilder.FromSender()` extension. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Senders)](https://nuget.org/packages/Ratatosk.Senders) |
| `Ratatosk.Senders.InMemory` | In-memory sender repository for development and testing, with optional seed data. | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Senders.InMemory)](https://nuget.org/packages/Ratatosk.Senders.InMemory) |
| `Ratatosk.Senders.EntityFramework` | EF Core persistence for the sender identity registry (`SenderDbContext`, `EntitySenderRepository`, `DbSender`). | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Senders.EntityFramework)](https://nuget.org/packages/Ratatosk.Senders.EntityFramework) |
| `Ratatosk.Extensions.OpenTelemetry` | Convenience extensions for wiring Ratatosk telemetry sources into OpenTelemetry SDK (`WithOpenTelemetry()` on `MessagingBuilder`, `AddRatatoskInstrumentation()` on `TracerProviderBuilder`/`MeterProviderBuilder`). | [![NuGet](https://img.shields.io/nuget/v/Ratatosk.Extensions.OpenTelemetry)](https://nuget.org/packages/Ratatosk.Extensions.OpenTelemetry) |

## Reading path

**New to the framework:**

1. [Framework overview](framework-overview.md) — concepts and building blocks
2. [Installation](installation.md) — package selection and DI wiring
3. [Quickstart](quickstart.md) — send your first message in 5 minutes

**Core concepts:**

- [Message model](messaging-model.md) — Message builder, endpoints, all content types, properties
- [Channel schema](channel-schema.md) — defining connector contracts and validation rules
- [Schema derivation](schema-derivation.md) — creating restricted schemas from master schemas
- [Message validation](message-validation.md) — pre-flight validation before connector calls
- [Senders](senders/README.md) - sender identity guides and navigation hub
- [Sender management](senders/sender-management.md) - sender identity model, registry lifecycle, storage options, and governance
- [Sender resolution](senders/sender-resolution.md) - runtime sender lookup, fallback behavior, cache, and diagnostics
- [IMessagingClient facade](quickstart.md#5-advanced-resolution-strategies) — high-level client with lazy initialization and named channel routing

**Building and wiring:**

- [Connector implementation](connectors-implementation/overview.md) — writing custom connectors with `ChannelConnectorBase`
- [Authentication](authentication.md) — auth providers, credential management, OAuth flows
- [Result types](connectors-implementation/result-types.md) — `OperationResult<T>`, send results, health data
- [Retry policies](connectors-configuration/retry-policies.md) — automatic retry with backoff, jitter, and circuit breaker
- [Telemetry](telemetry.md) — OpenTelemetry tracing and metrics, wiring, configuration
- [Connector configuration](connectors-configuration/README.md) — connection settings, timeouts, health checks
- [Advanced implementation](connectors-implementation/advanced-topics.md) — receive operations, status queries, batch processing, testing

**Connector guides:**

- [Connector index](connectors/README.md) — all supported providers

**Samples:**

- [Samples overview](samples/README.md) — runnable sample applications for each channel
