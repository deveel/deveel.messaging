# Ratatosk Framework — Roadmap

This document describes the planned evolution of the **Ratatosk Framework** — a .NET library that provides a unified, provider-agnostic model for sending and receiving messages across multiple channels and connectors. Each milestone below includes a summary of its intent, detailed descriptions of every planned feature, and the rationale behind each decision.

> **Current stable release:** `v1.0.1`  
> Connectors available: Facebook Messenger, Firebase Push, SendGrid Email, Telegram Bot, Twilio SMS/WhatsApp  
> Framework includes: `IMessagingClient` facade, `MessageBuilder`, auto-initialization, `ChannelSchema` derivation, expanded validation, retry policies, OpenTelemetry tracing & metrics

---

## Milestones at a Glance

| Milestone | Version | Features |
|-----------|---------|----------|
| [Framework Foundations](#v040--framework-foundations) | v0.4.0 | [Test Coverage Target](#test-coverage-target--80) · [CI/CD Pipeline Hardening](#cicd-pipeline-hardening) · [Structured Logging Improvements](#structured-logging-improvements) · [Documentation](#documentation) |
| [Inbound Messaging](#v050--inbound-messaging) | v0.5.0 | [Twilio Inbound Messages](#twilio-inbound-messages-sms--whatsapp) · [SendGrid Inbound Messages](#sendgrid-inbound-messages-inbound-parse) · [Firebase Inbound Messages](#firebase-inbound-messages-data--notification-messages) |
| [First Stable Release](#v100--first-stable-release) | v1.0.0 | [API Freeze](#api-freeze) · [NuGet GA Release](#nuget-ga-release) · [Interactive Content](#interactive-content) · [Sender Identity Model](#sender-identity-model) |
| [Resilience & Observability](#v110--resilience--observability) | v1.1.0 | [Retry Policies](#retry-policies) ✓ · [OpenTelemetry Tracing & Metrics](#opentelemetry-tracing--metrics) ✓ · [Health Checks](#health-checks) ✓ · [Connector-Level Timeout Configuration](#connector-level-timeout-configuration) |
| [New SaaS Connectors](#v120--new-saas-connectors) | v1.2.0 | [Slack](#slack-connector) · [Microsoft Teams](#microsoft-teams-connector) · [WhatsApp Business API](#whatsapp-business-api-connector-direct-cloud-api) · [Viber Business](#viber-business-connector) · [LINE](#line-connector) |
| [Protocol Connectors](#v130--protocol-connectors) | v1.3.0 | [Base Classes](#protocol-connector-base-classes) · [SMPP](#smpp-connector) · [SMTP](#smtp-connector) · [RCS](#rcs-connector) · [APNs](#apns-connector-direct) |
| [Content Adaptation & Transcoding](#v140--content-adaptation--transcoding) | v1.4.0 | [IContentTranscoder Abstraction](#icontenttrancoder-abstraction) · [Built-In Transcoders](#built-in-transcoders) · [Channel-Aware Fallback](#channel-aware-content-fallback) · [SMS Segmentation](#sms-segmentation) · [Character Encoding Detection](#character-encoding-detection) |
| [Address & Number Validation](#v150--address--number-validation) | v1.5.0 | [E.164 Normalization](#e164-normalization) · [HLR Lookup](#hlr-home-location-register-lookup) · [Number Portability](#number-portability-awareness) · [Email Validation](#email-address-validation) · [IAddressValidator Abstraction](#iaddressvalidator-abstraction) |
| [Tooling & Instrumentation](#v160--tooling--instrumentation) | v1.6.0 | [dotnet new Connector Scaffold](#dotnet-new-connector-scaffold) · [ASP.NET Core Diagnostic Middleware](#aspnet-core-diagnostic-middleware) |
| [Conversations](#v200--conversations) | v2.0.0 | [IConversation Abstraction](#iconversation-abstraction) · [Conversation State Model](#conversation-state-model) · [Conversation Correlation](#conversation-correlation) · [Multi-Channel Conversations](#multi-channel-conversations) · [Multi-Tenant Channel Registry](#multi-tenant-channel-registry) · [xUnit Test Helpers](#xunit-test-helpers-for-conversation-flows) |
| [Message Templates](#v210--message-templates) | v2.1.0 | [IMessageTemplate Abstraction](#imessagetemplate-abstraction) · [Variable Substitution Engine](#variable-substitution-engine) · [Per-Connector Template Rendering](#per-connector-template-rendering) · [Template Registry](#template-registry) |

---

## v0.4.0 — Framework Foundations

Today the framework has a working outbound message model and five connector packages, but several cross-cutting concerns — test coverage, release automation, observability, and documentation — are incomplete or inconsistent. This milestone addresses those gaps so that the codebase is production-ready in everything except breadth of connector coverage before the API is frozen at v1.0.0.

---

### Test Coverage Target (≥ 80%)

> *"Untested code is a liability disguised as a feature."*

#### The Problem Today

Test coverage is uneven across library projects. Some areas have thorough coverage; others — particularly edge cases in connector logic and validation — are undertested.

#### What We Are Building

A coverage measurement step in the CI pipeline (Coverlet + ReportGenerator) that fails the build if any library project falls below 80% line coverage. Gaps identified during this work are addressed before the 1.0.0 release.

#### Benefits

- A minimum coverage bar is enforced automatically on every pull request
- Regressions in test coverage are caught before they reach main

---

### CI/CD Pipeline Hardening

> *"A release process that requires manual steps is a release process waiting to fail."*

#### The Problem Today

The CI/CD pipeline covers the core build and test flow, but some steps (package signing, release notes generation, NuGet publishing) require manual intervention or are not fully automated.

#### What We Are Building

A fully automated release pipeline: build → test → coverage check → API compatibility check → pack → sign → publish to NuGet.org, triggered by a `v*` tag on main. Release notes are generated automatically from conventional commit messages.

#### Benefits

- Releases are reproducible and require no manual steps
- The time between tagging a release and packages being available on NuGet is minimised

---

### Structured Logging Improvements

> *"A log message that cannot be queried might as well not exist."*

#### The Problem Today

Log messages across connectors are inconsistent in level, scope, and event ID. Some connectors use `LoggerMessage` source generators, others use interpolated strings. There are no shared event ID conventions, making log-based alerting across connectors unreliable.

#### What We Are Building

A standardised logging contract for all connectors: shared event ID ranges per operation type (send, receive, status query, health check), consistent log scopes (connector name, channel ID, message ID), and `LoggerMessage`-based implementations across all existing connectors. A base class in `Ratatosk.Connector.Abstractions` enforces the contract for new connectors.

#### Benefits

- Logs can be queried and alerted on uniformly across all connectors
- Reduced logging overhead via `LoggerMessage` source generation
- New connector authors have a clear logging contract to follow

---

### Documentation

> *"Public API without documentation is an API nobody can use; a connector that works but cannot be explained might as well not exist."*

#### The Problem Today

XML documentation coverage is incomplete across the abstractions and connector libraries — many public types and members lack summary, parameter, and remarks documentation. At the same time, the `docs/connectors/` guides are uneven: some connectors have detailed guides, others are missing configuration references, authentication guides, or usage examples.

#### What We Are Building

Complete `<summary>`, `<param>`, `<returns>`, and `<remarks>` XML documentation on every public type and member across all library projects, with documentation generation enabled and XML warnings treated as errors to prevent regressions. Alongside this, full prose documentation for every connector covering: prerequisites, DI registration, authentication configuration, supported capabilities, content types, endpoint types, and at least one end-to-end usage example — all reviewed for accuracy against the current API.

#### Benefits

- IntelliSense surfaces useful descriptions for every public API
- Generated API reference documentation is complete and accurate
- Any developer can onboard a connector from documentation alone
- Documentation is versioned alongside the code and reviewed on every release

---


## v0.5.0 — Inbound Messaging

Today the framework provides a solid foundation for sending messages through multiple providers. However, real-world A2P messaging is bidirectional: recipients reply, devices send data back, and providers deliver status callbacks. This milestone completes the receive-side story for all existing connectors, so that the framework models the full message lifecycle — not just the outbound leg.

Each feature in this milestone ships with its corresponding xUnit test helper extensions as part of its definition of done — fake inbound payload builders, `MessageSource` factories, and `ReceiveResult` assertion helpers are delivered alongside the connector implementation, not as a separate step.

---

### Twilio Inbound Messages (SMS & WhatsApp)

> *"A message sent is only half the conversation — we need to hear back."*

#### The Problem Today

The Twilio connector can send SMS and WhatsApp messages but has no way to receive inbound messages from users replying to those messages. Developers who need to handle replies must build their own parsing logic on top of Twilio's raw payload format, with no integration into the framework's message model.

#### What We Are Building

A `ReceiveMessagesCoreAsync` implementation for the Twilio SMS and WhatsApp connectors that parses inbound Twilio payloads into the framework's `IMessage` model. This includes text bodies, media attachments (MMS), and sender/recipient endpoint resolution into the standard `IEndpoint` types.

#### Benefits

- Inbound Twilio messages are represented as first-class `IMessage` objects
- No provider-specific parsing code required in consuming applications
- Unified receive path shared with all other connectors

---

### SendGrid Inbound Messages (Inbound Parse)

> *"Email replies carry intent — the framework should surface them, not discard them."*

#### The Problem Today

SendGrid's Inbound Parse feature forwards inbound emails to a configured endpoint as a multipart form post. There is currently no connector-level support for parsing these payloads into the framework's message model, leaving developers to handle raw HTTP body parsing themselves.

#### What We Are Building

A `ReceiveMessagesCoreAsync` implementation for the SendGrid connector that parses the Inbound Parse multipart payload into an `IMessage` with the appropriate `ITextContent` or `IHtmlContent`, sender/recipient endpoints, and any attachments mapped to `IAttachment`.

#### Benefits

- Inbound emails are surfaced as typed `IMessage` objects within the framework
- Attachments and multipart bodies are automatically mapped to the content model
- Consistent receive interface regardless of whether the channel is SMS, push, or email

---

### Firebase Inbound Messages (Data & Notification Messages)

> *"A push notification is not a monologue — devices can and do respond."*

#### The Problem Today

Firebase Cloud Messaging supports upstream messaging from devices (data messages sent back to the application server), but the Firebase connector only handles the outbound push direction. There is no model for receiving and interpreting upstream device messages.

#### What We Are Building

A `ReceiveMessagesCoreAsync` implementation for the Firebase connector that handles upstream FCM data messages, mapping the device token and message payload into the framework's endpoint and content model.

#### Benefits

- Device-originated messages are represented consistently with messages from other channels
- Enables bidirectional communication patterns over Firebase without leaving the framework's abstractions

---


## v1.0.0 — First Stable Release

All preceding milestones establish the framework's core capabilities: a complete bidirectional message model, rich interactive content, a sender identity abstraction, and production-grade resilience and observability. Version 1.0.0 is not a feature release — it is a commitment. The API is frozen, documentation is complete, test coverage meets a defined bar, and all packages are published as stable NuGet releases. From this point, breaking changes require a new major version.

---

### API Freeze

> *"A library that changes its public API unpredictably cannot be trusted as a dependency."*

#### The Problem Today

In the pre-1.0 phase, public APIs have been evolving freely. Consumers who take a dependency on the framework risk breaking changes between minor versions.

#### What We Are Building

A formal commitment that no breaking changes will be introduced to any public API without a new major version number. A compatibility analyser (Microsoft.DotNet.ApiCompat) is added to the CI pipeline to enforce this automatically on every pull request.

#### Benefits

- Consumers can upgrade minor and patch versions safely
- The framework can be adopted in production systems without fear of churn

---

### NuGet GA Release

> *"A pre-release suffix is a warning label — version 1.0.0 removes it."*

#### The Problem Today

All packages are published with pre-release NuGet suffixes. Many package managers and enterprise environments exclude pre-release packages from dependency resolution by default.

#### What We Are Building

Stable NuGet releases (no pre-release suffix) for all framework packages, published to NuGet.org under the `Ratatosk.*` package ID namespace. Release notes are included for every package. The CD pipeline is verified to produce reproducible, deterministic builds.

#### Benefits

- Packages are available to all NuGet consumers by default
- Stable versioning signals production readiness to the community

### Interactive Content

> *"Buttons, carousels, and quick replies should be described once and rendered everywhere."*

#### The Problem Today

Modern A2P channels go far beyond plain text. Users interact with buttons, select from carousels, tap quick replies, and pick from lists. There is no shared model for these interactive elements — each connector that supports them implements its own proprietary structures, coupling application code to a specific provider's API.

#### What We Are Building

A new `IInteractiveContent` interface and concrete content types covering the most common interactive patterns: `ButtonContent` (CTAs with URL or postback payload), `QuickReplyContent` (fast one-tap responses), `CarouselContent` (horizontally scrollable cards), and `ListPickerContent` (structured option lists). These types extend the existing `IMessageContent` hierarchy. A new `InteractiveContent` value is added to `ChannelCapability` so connectors can declare support. Per-connector mapping implementations translate the generic model to provider-native formats at send time; channel schema validation enforces provider-specific constraints (e.g. maximum button count). xUnit test helpers are provided for asserting mapping correctness without a live API call.

#### Benefits

- One model for interactive content shared across all connectors
- Application code is decoupled from provider-specific interactive APIs
- Unsupported interactive elements are rejected at validation time, not at runtime
- New connectors declare interactive support by implementing a mapping, not by introducing new content types


### Sender Identity Model

> *"Who a message comes from is as important as what it says."*

#### What We Built

A generic sender identity system that decouples message composition from sender configuration:

- **`ISender`** — common interface for all sender types (`PhoneSender`, `EmailSender`, `AlphaNumericSender`, `BotSender`, `Sender`)
- **`IUnresolvedSender`** — marker interface for `SenderRef`, which carries a logical name resolved at send time
- **`ISenderRepository<TSender>`** — generic repository extending Kista's `IRepository<TSender>`, allowing custom storage-bound entity types
- **`ISenderResolver`** — resolves `IUnresolvedSender` references to concrete `ISender` instances via the repository, with caching
- **`ISenderConfigurationRegistry`** — per-connector sender configuration (default senders, cache settings)
- **`SenderBuilder`** — fluent API for constructing `Sender` instances, decoupled from persistence metadata

#### Benefits

- Sender identity is a first-class, typed concept in the framework's model
- Applications can configure and validate sender identity independently of provider-specific settings
- Sender pools (regional numbers, multiple virtual numbers) are managed by the framework, not by the application
- Invalid or unsupported identity types are caught at configuration time, not at first send
- Per-connector sender configuration allows different defaults and cache settings per channel


---

## v1.1.0 — Resilience & Observability

A messaging connector that fails silently, cannot be monitored, or brings down dependent services on provider outages is not production-ready. This milestone adds the operational layer that connectors need to be trusted in production: structured retry and circuit-breaker policies, distributed tracing and metrics via OpenTelemetry, health check endpoints, and consistent structured logging across all connectors.

Retry policies, OpenTelemetry tracing & metrics, and health checks are complete in this release. Timeout configuration remains in progress.

---

### Retry Policies

> *"A transient failure from a provider should never be the caller's problem."*

#### The Problem Today

When a provider API returns a transient error (rate limit, temporary unavailability), the connector immediately returns a failure result. Callers are responsible for deciding whether and how to retry, leading to inconsistent retry logic scattered across applications.

#### What We Are Building

Polly-based retry and circuit-breaker policies integrated at the connector base level. Each connector can declare which error codes are retryable and configure backoff strategies (exponential with jitter by default). The circuit breaker prevents cascading failures when a provider is consistently unavailable. Policies are configurable per-connector via the DI registration API.

#### Benefits

- Transient provider failures are handled transparently without caller involvement
- Circuit breaker prevents resource exhaustion during provider outages
- Retry behaviour is consistent and configurable across all connectors

---

### OpenTelemetry Tracing & Metrics ✓

> *"You cannot improve what you cannot observe."*

#### The Problem (Before)

There was no built-in telemetry in the connector layer. Developers who wanted to trace message send latency, track delivery rates, or count failures had to instrument their own wrapper code on top of the connectors.

#### What We Built

Activity sources and metric instruments built into the connector base: an `ActivitySource` per connector for distributed tracing (spans for send, receive, status query), and `Meter` instruments for counters (messages sent, received, failed) and histograms (send latency, payload size). All telemetry follows OpenTelemetry semantic conventions and is automatically exported via the application's configured OTLP pipeline. A dedicated `Ratatosk.Extensions.OpenTelemetry` package provides convenience extensions (`WithOpenTelemetry()` on `MessagingBuilder`, `AddRatatoskInstrumentation()` on `TracerProviderBuilder`/`MeterProviderBuilder`). Telemetry options (`EnableTracing`, `EnableMetrics`, `EnablePayloadSizeMetrics`) are configurable per connector through connection settings or the builder API, following the same pattern as retry policies.

#### Benefits

- End-to-end distributed traces include connector-level spans out of the box
- Dashboards and alerts can be built on connector metrics without custom instrumentation
- Fully compatible with any OpenTelemetry-compliant backend (Jaeger, Prometheus, Azure Monitor, etc.)
- Payload size metrics are opt-in (disabled by default) to avoid serialisation overhead in high-throughput scenarios

---

### Health Checks

> *"If a connector cannot reach its provider, the application should know before a message fails."*

#### The Problem Today

There is no standard way to check whether a connector is operational. The `HealthCheck` capability flag exists but there is no integration with ASP.NET Core's `IHealthCheck` infrastructure.

#### What We Built

A `Ratatosk.Extensions.HealthChecks` package providing a single `ConnectorHealthCheck` that implements ASP.NET Core's `IHealthCheck`. At check time it discovers all registered connectors (both unnamed and named) via DI, probes each through the connector's existing `GetHealthAsync()` method, and reports `Healthy`, `Degraded`, or `Unhealthy` with per-connector structured detail (state, issues, uptime, metrics) in the result data dictionary.

Registration is a one-liner on `IHealthChecksBuilder`:
```csharp
services.AddHealthChecks()
    .AddRatatoskHealthChecks();
```

The check is exposed through the standard ASP.NET Core health endpoint with no additional configuration.

#### Benefits

- Connector health is visible in standard health check dashboards and load balancer probes
- Degraded provider connectivity is surfaced before messages are attempted
- Zero additional configuration required — health checks register themselves via DI

---

### Connector-Level Timeout Configuration

> *"An unanswered API call should never block indefinitely."*

#### The Problem Today

Connectors rely on the default timeouts of their underlying HTTP clients. There is no per-connector, per-operation timeout configuration exposed through the framework's DI registration API.

#### What We Are Building

A `ConnectorTimeoutOptions` configuration class, settable per-connector via the DI builder, covering send timeout, receive timeout, status query timeout, and health check timeout. Defaults are sensible and documented. Timeout cancellation is propagated through the existing `CancellationToken` pattern.

#### Benefits

- Slow provider APIs cannot block indefinitely
- Timeouts are tunable per environment (e.g. shorter in tests, longer for large batch sends)

---

## v1.2.0 — New SaaS Connectors

With a stable, well-documented framework in place, this milestone extends the connector ecosystem to cover additional major cloud messaging providers. Each new connector implements the full `IChannelConnector` contract, declares its capabilities honestly in its channel schema, and ships with the same xUnit test helper package pattern established by the existing connectors.

---

### Slack Connector

> *"Slack is where teams live — A2P messages belong there too."*

#### The Problem Today

There is no Slack connector. Developers who want to send structured messages or notifications to Slack channels or users through the framework cannot do so without leaving the framework's abstractions.

#### What We Are Building

A `Ratatosk.Connector.Slack` package implementing `IChannelConnector` for Slack, supporting Incoming Webhooks for simple channel posting and the Slack Bot API for direct messages and interactive responses. Content mapping covers text, HTML-to-Block-Kit conversion, and interactive content (buttons, sections).

#### Benefits

- Slack becomes a first-class messaging channel in the framework
- Block Kit message structures are abstracted behind the framework's content model

#### Depends on

- Interactive Content (v1.0.3)

---

### Microsoft Teams Connector

> *"Teams is the enterprise Slack — the framework should speak both languages."*

#### The Problem Today

There is no Microsoft Teams connector. Enterprise applications that need to deliver notifications or interactive messages to Teams channels or users cannot use the framework for this channel.

#### What We Are Building

A `Ratatosk.Connector.Teams` package supporting Incoming Webhooks (for channel messages) and the Bot Framework API (for direct messages and adaptive card delivery). Content mapping covers text, HTML-to-Adaptive-Card conversion, and interactive content.

#### Benefits

- Teams becomes a first-class channel for enterprise notification scenarios
- Adaptive Cards are generated from the framework's content model, not handcrafted per-message

#### Depends on

- Interactive Content (v1.0.3)

---

### WhatsApp Business API Connector (Direct Cloud API)

> *"Not every team wants Twilio in the middle of their WhatsApp integration."*

#### The Problem Today

WhatsApp messaging is currently only available through the Twilio connector. Teams that have a direct WhatsApp Business account with Meta's Cloud API cannot use the framework without routing messages through a third-party provider.

#### What We Are Building

A `Ratatosk.Connector.WhatsApp` package that connects directly to Meta's WhatsApp Business Cloud API. It supports text, media, interactive, and template messages, and implements both send and receive (inbound message webhooks handled at the model level, not via a webhook framework).

The connector is compatible with any BSP that exposes the standard WhatsApp Cloud API, including self-hosted platforms like [OpenBSP](https://github.com/matiasbattocchia/open-bsp-api) — an open-source, multi-tenant WhatsApp Business platform built with Deno and Supabase. OpenBSP provides the server-side BSP infrastructure (message storage, webhook management, AI agent orchestration, MCP server, multi-tenant isolation) while the Ratatosk connector provides the .NET client-side abstraction, giving teams a complete end-to-end WhatsApp messaging stack without proprietary SaaS dependencies.

#### Benefits

- Direct WhatsApp integration without a SaaS intermediary
- Compatible with self-hosted BSP platforms like OpenBSP for teams that need multi-tenant isolation, message archival, or AI agent integration
- Enables access to Cloud API features not exposed through Twilio (e.g. native template management)

#### Depends on

- Inbound Messaging (v0.4.0)
- Interactive Content (v1.0.3)
- Sender Identity Model (v1.0.4)

---

### Viber Business Connector

> *"Viber is the dominant messaging platform across Eastern Europe and Southeast Asia — it should not be an afterthought."*

#### The Problem Today

There is no Viber Business connector. Applications targeting markets where Viber is the primary messaging channel must build their own integration outside the framework.

#### What We Are Building

A `Ratatosk.Connector.Viber` package implementing the Viber Business Messages API, supporting text, image, rich media, and keyboard (interactive) messages to Viber users and channels.

#### Benefits

- Viber is addressable through the same connector interface as SMS, email, and push
- Interactive Viber keyboards are mapped from the framework's `IInteractiveContent` model

#### Depends on

- Interactive Content (v1.0.3)

---

### LINE Connector

> *"LINE dominates messaging in Japan, Thailand, and Taiwan — regional coverage matters."*

#### The Problem Today

There is no LINE connector. Applications that need to reach users in LINE-dominant markets must implement their own LINE Messaging API integration.

#### What We Are Building

A `Ratatosk.Connector.Line` package implementing the LINE Messaging API, supporting text, image, video, audio, and Flex Message (interactive template) content types.

#### Benefits

- LINE is reachable through the same connector abstractions as every other channel
- Flex Messages are generated from the framework's content model

#### Depends on

- Interactive Content (v1.0.3)

---

## v1.3.0 — Protocol Connectors

SaaS messaging providers abstract away the underlying transport protocol, but they also introduce dependency, cost, and constraints. This milestone adds connectors that speak raw industry protocols directly — SMPP for SMS, SMTP for email, APNs for Apple push — so that teams with direct carrier or Apple developer relationships can use the framework without any SaaS intermediary.

---

### Protocol Connector Base Classes

> *"Protocol connectors share enough structure that their base should be built once."*

#### The Problem Today

Protocol-level connectors (SMPP, SMTP, APNs) have concerns that SaaS connectors do not: persistent connection management, session lifecycle, framing, and binary protocol encoding. There are no base classes in the framework that help with these concerns.

#### What We Are Building

A `Ratatosk.Connector.Protocol` library (or extensions to `Ratatosk.Connector.Abstractions`) providing base classes for session-oriented connectors, connection lifecycle hooks, and reconnection policies. A corresponding xUnit test helper package provides fake session simulators for testing protocol connectors without a live server.

#### Benefits

- Protocol connector authors start from a correct session management foundation
- Connection lifecycle bugs are fixed once, in the base class, not in every protocol connector
- Protocol connectors are testable without a real SMSC, SMTP server, or APNs endpoint

---

### SMPP Connector

> *"Sending SMS through a SaaS provider is convenient — sending through a direct SMSC connection is sovereign."*

#### The Problem Today

All SMS messaging in the framework goes through Twilio. Teams that have a direct SMSC agreement (common for high-volume A2P senders and telecoms) cannot use the framework for their primary SMS channel.

#### What We Are Building

A `Ratatosk.Connector.Smpp` package implementing an SMPP v3.4 session client. The connector manages the SMPP session lifecycle (bind, keepalive, unbind), maps `IMessage` to `submit_sm` PDUs, and handles `deliver_sm` for inbound messages and `deliver_sm_resp`/`submit_sm_resp` for delivery receipts.

#### Benefits

- Direct SMSC connectivity without any SaaS provider dependency
- Delivery receipts are surfaced as `IMessageState` updates through the standard framework path
- High-throughput SMS is achievable through persistent SMPP session management

#### Depends on

- Inbound Messaging (v0.4.0)
- Sender Identity Model (v1.0.4)
- Content Adaptation & Transcoding (v1.1.0) — for GSM-7/UCS-2 encoding

---

### SMTP Connector

> *"Email at its most direct is just SMTP — the framework should support that."*

#### The Problem Today

All email messaging goes through SendGrid. Teams that want to send email through their own SMTP server, a self-hosted relay, or a simple transactional SMTP service without a full SaaS API integration cannot use the framework.

#### What We Are Building

A `Ratatosk.Connector.Smtp` package using MailKit to deliver messages over SMTP. It maps `IMessage` content (text, HTML, attachments) to RFC 5321-compliant MIME messages, handles authentication (plain, OAuth2, NTLM), and supports TLS/STARTTLS.

#### Benefits

- Any SMTP-capable server is now a valid messaging channel
- HTML and plain text content are mapped to correct MIME parts automatically
- Attachments from `IAttachment` are included as MIME attachments

#### Depends on

- Sender Identity Model (v1.0.4)

---

### RCS Connector

> *"RCS is SMS with the richness of a chat app — the next generation of A2P messaging."*

#### The Problem Today

Rich Communication Services (RCS) is the industry-standard successor to SMS, supported natively on Android and increasingly on iOS. There is no RCS connector in the framework, leaving this growing channel unaddressed.

#### What We Are Building

A `Ratatosk.Connector.Rcs` package implementing the RCS Business Messaging API (via a compatible aggregator or direct GSMA connection). It maps interactive content (carousels, suggested replies, rich cards) from the framework's `IInteractiveContent` model to RCS message objects.

#### Benefits

- RCS is accessible through the same interface as SMS, WhatsApp, and email
- Rich cards and interactive elements are automatically mapped from the framework's content model

#### Depends on

- Inbound Messaging (v0.4.0)
- Interactive Content (v1.0.3)
- Sender Identity Model (v1.0.4)

---

### APNs Connector (Direct)

> *"Firebase is not the only path to an iPhone — teams with Apple developer accounts should have a direct route."*

#### The Problem Today

Apple Push Notifications are currently only addressable through the Firebase connector (which proxies to APNs). Teams that have a direct Apple developer account and want to send APNs payloads without Firebase cannot use the framework.

#### What We Are Building

A `Ratatosk.Connector.Apns` package using the APNs HTTP/2 API directly. It maps `IMessage` content to APNs payloads (alert, background, VoIP), handles provider certificate and token-based authentication (JWT), and manages connection pooling to the APNs endpoint.

#### Benefits

- Direct APNs delivery without a Firebase dependency
- Fine-grained control over APNs payload fields (priority, expiration, collapse ID) through message properties
- Token-based auth (`.p8` key) and certificate-based auth are both supported

#### Depends on

- Sender Identity Model (v1.0.4)

---

## v1.4.0 — Content Adaptation & Transcoding

A message model that works across channels must acknowledge that channels are not equivalent: SMS has a 160-character GSM-7 limit, WhatsApp renders Markdown, HTML emails cannot be delivered as SMS, and a connector without media support cannot accept a rich card. This milestone introduces a transcoding layer that converts content from one type to another, and an adaptation layer that selects the best representation of a message for a given channel — automatically, and without requiring the sender to know the details of every provider.

---

### `IContentTranscoder` Abstraction

> *"Convert once, send anywhere."*

#### The Problem Today

There is no shared model for converting message content between types. When an application wants to send a message across both an email channel and an SMS channel, it must prepare two separate content objects with completely separate code paths. There is no mechanism for the framework to assist with that conversion.

#### What We Are Building

An `IContentTranscoder<TSource, TTarget>` abstraction that converts a content object of one type into another. A `ContentTranscoderRegistry` holds named transcoders and resolves the best available transcoding path between any two content types, including multi-step chains (e.g. HTML → Markdown → GSM-7 text). The registry is registered in DI and available to the connector base for automatic adaptation.

#### Benefits

- Content conversion is defined once and reused across all send paths
- Multi-step transcoding chains are resolved automatically
- Application code sends one message; the framework adapts the content per channel

---

### Built-In Transcoders

> *"The most common conversions should not require custom code."*

#### The Problem Today

Even with an `IContentTranscoder` abstraction, developers would need to implement every conversion themselves, including common ones like HTML to plain text.

#### What We Are Building

A set of built-in transcoders registered by default: HTML → plain text (using a lightweight HTML stripper), HTML → Markdown, Markdown → GSM-7 plain text, `ILocationContent` → formatted text address, `IMediaContent` → text fallback (caption or alt text), and `IInteractiveContent` → text summary (for channels that do not support interactive elements).

#### Benefits

- The most common cross-channel conversions work out of the box
- Built-in transcoders serve as reference implementations for custom transcoders

#### Depends on

- `IContentTranscoder` Abstraction

---

### Channel-Aware Content Fallback

> *"A message that cannot be delivered as-is should be delivered in the best available form, not dropped."*

#### The Problem Today

When a connector receives a content type it does not support (e.g. an HTML message sent to an SMS connector), it currently returns a validation error. The caller is responsible for providing an alternative, which often means duplicating message preparation logic.

#### What We Are Building

An automatic fallback mechanism in the connector base that, when a content type is not supported by the target channel, queries the `ContentTranscoderRegistry` for a transcoding path to a supported type and applies it transparently. The fallback chain is configurable (opt-in per connector, with override options on the message).

#### Benefits

- Messages are delivered in the best available form for the target channel, automatically
- Fallback behaviour is configurable and predictable — no silent content mangling
- Eliminates duplicated message preparation in multi-channel send scenarios

#### Depends on

- `IContentTranscoder` Abstraction
- Built-In Transcoders

---

### SMS Segmentation

> *"An SMS that is longer than 160 characters is not one message — it is several."*

#### The Problem Today

When sending long text messages through an SMS connector (Twilio or SMPP), the framework has no awareness of GSM-7 / UCS-2 encoding limits or segment boundaries. Applications cannot determine how many SMS segments a message will consume, or the associated cost, before sending.

#### What We Are Building

A `SmsSegmenter` utility that counts characters according to GSM-7 (160 chars / segment) and UCS-2 (70 chars / segment for messages containing characters outside the GSM-7 alphabet), calculates the number of segments for a given text (accounting for the UDH header that reduces capacity in concatenated SMS), and exposes this information as metadata on the send result and as a pre-send analysis API.

#### Benefits

- Applications can estimate SMS cost before sending
- Segment count and encoding are surfaced as structured data, not inferred from provider responses

#### Depends on

- Character Encoding Detection

---

### Character Encoding Detection

> *"Sending a GSM-7 message with a single emoji doubles its segment count — the framework should warn you."*

#### The Problem Today

There is no utility in the framework for detecting whether a given text can be encoded in GSM-7 or requires UCS-2, and no mechanism to warn when a single non-GSM-7 character forces an entire message to UCS-2 (doubling the segment count).

#### What We Are Building

A `SmsEncodingAnalyzer` utility that inspects a text string, identifies characters outside the GSM-7 basic and extended character sets, classifies the required encoding (GSM-7 or UCS-2), and flags any individual characters that are forcing UCS-2 encoding. The SMPP and Twilio SMS connectors use this utility automatically when preparing outbound messages.

#### Benefits

- Encoding surprises are surfaced before the message is sent
- Applications can replace or remove problematic characters based on structured analysis output

---

## v1.5.0 — Address & Number Validation

Sending a message to an invalid or unreachable address wastes resources, generates misleading delivery failures, and, in the case of SMS, incurs real cost. This milestone adds a validation layer that normalises recipient addresses to standard formats, optionally verifies reachability before sending, and provides a clean `IAddressValidator` abstraction that connectors and applications can use uniformly.

---

### E.164 Normalization

> *"A phone number is only unambiguous when it carries its country code."*

#### The Problem Today

Phone number endpoints in the framework carry no normalization requirement. Numbers may be provided in local format (e.g. `07911 123456`), with or without country code, with varying use of spaces, dashes, and parentheses. Connectors receive whatever the caller provides, leading to inconsistent API behaviour and opaque provider errors.

#### What We Are Building

An E.164 normalization step applied to `PhoneNumber` endpoints before they are passed to a connector. Using Google's `libphonenumber` (via `libphonenumber-csharp`), the normalizer attempts to parse and reformat any phone number to E.164 (e.g. `+447911123456`). Numbers that cannot be parsed to a valid E.164 form are rejected with a structured validation error before any API call is made.

#### Benefits

- Connectors always receive phone numbers in a canonical, unambiguous format
- Invalid numbers are caught and reported before they reach the provider API
- Normalization rules follow the ITU-T E.164 standard, the same standard used by every major provider

---

### HLR (Home Location Register) Lookup

> *"Knowing a number exists is not the same as knowing it can receive a message."*

#### The Problem Today

A phone number that is syntactically valid may belong to an inactive SIM, a ported number on a different network, or a number that has been decommissioned. Sending an SMS to such a number incurs cost and produces a failed delivery, with no advance warning.

#### What We Are Building

An optional `INumberReachabilityChecker` abstraction with provider-specific implementations (e.g. Twilio Lookup, GSMA HLR via an aggregator). When enabled in the connector or at the framework level, an HLR query is performed before sending to a mobile number, and the result (active, inactive, ported, unknown) is surfaced as a pre-send check result. Callers can configure the framework to fail-fast, warn, or proceed based on the HLR result.

#### Benefits

- Unreachable numbers are identified before SMS cost is incurred
- HLR results can be cached to avoid repeated lookups for known numbers

---

### Number Portability Awareness

> *"A number that moved to a new network should still find its way to the right SMSC."*

#### The Problem Today

When a number has been ported to a different network operator, routing it through the original carrier's SMSC may result in failed or delayed delivery. The framework has no awareness of ported numbers.

#### What We Are Building

Integration of number portability data (where available from the HLR lookup result or from a dedicated NP database) into the SMPP connector's routing logic. When a number is known to have been ported, the connector selects an appropriate SMSC route if multiple are configured.

#### Benefits

- Ported numbers are routed correctly without manual intervention
- Delivery reliability improves for high-churn mobile markets

#### Depends on

- HLR Lookup
- SMPP Connector (v1.3.0)

---

### Email Address Validation

> *"An email address that cannot receive mail should not be sent to."*

#### The Problem Today

Email endpoints are accepted as-is. Syntactically invalid addresses, addresses with non-existent domains, and disposable email addresses all pass through the framework to the provider API, where they produce delivery failures.

#### What We Are Building

An `IEmailAddressValidator` that performs: (1) RFC 5321 syntax validation, (2) MX record resolution to verify the domain can receive mail, and (3) optionally, disposable email domain detection. The validator is applied to email endpoints before send, and invalid addresses produce structured validation errors.

#### Benefits

- Invalid email addresses are rejected before an API call is made
- MX record checks prevent sending to domains that cannot receive mail at all

---

### `IAddressValidator` Abstraction

> *"Address validation should be as pluggable as the connectors themselves."*

#### The Problem Today

Even with built-in validators for phone numbers and email addresses, teams may need to apply custom validation rules (e.g. checking against a suppression list, verifying against an internal CRM). There is no extension point for this.

#### What We Are Building

An `IAddressValidator` abstraction that connectors invoke before send, and that applications can extend with custom implementations. A default composite validator chains built-in validators (E.164 normalization, email syntax/MX) with any application-registered custom validators. The chain is configurable per-connector via the DI registration API.

#### Benefits

- Custom validation rules (suppression lists, internal CRM checks) plug in alongside built-in validators
- Validation is always performed in a consistent, ordered, auditable chain
- Validation results carry structured details, not just pass/fail

---

## v1.6.0 — Tooling & Instrumentation

A framework is only as productive as the tools built around it. This milestone introduces two developer-facing additions: a `dotnet new` scaffold that lets connector authors bootstrap a correctly structured project in minutes, and an ASP.NET Core diagnostic middleware that gives operators a live view of registered connectors and their runtime state — without attaching a debugger.

---

### `dotnet new` Connector Scaffold

> *"The fastest path to a correct connector is starting from one that already compiles."*

#### The Problem Today

There is no formal starting point for implementing a new connector. Contributors must infer the expected project structure, base class overrides, capability declarations, DI registration pattern, and xUnit test helper conventions by reading existing connectors — a slow and error-prone process.

#### What We Are Building

A `dotnet new` template package (`Ratatosk.Connector.Templates`) that generates a fully wired connector solution: the connector project with correct base class overrides and a `IChannelSchema` factory, a companion xUnit test helper package, and a minimal usage sample — all pre-configured and immediately buildable.

#### Benefits

- New connectors compile and pass a baseline test suite from the first `dotnet build`
- Naming conventions, project layout, and package references are consistent across all connectors
- Lowers the barrier for community contributions

---

### ASP.NET Core Diagnostic Middleware

> *"An operator should never need a debugger to answer 'is the Twilio connector healthy?'"*

#### The Problem Today

There is no runtime view of the connectors registered in a running application. Diagnosing misconfiguration, capability mismatches, or degraded connector state requires attaching a debugger or reading logs — neither of which is practical in staging or production.

#### What We Are Building

An opt-in ASP.NET Core middleware that exposes a `/messaging/diagnostics` endpoint returning a structured JSON document: all registered connectors, their declared channel schemas and capabilities, current health status, and rolling send/receive counters. The endpoint is secured by a configurable authorization policy and disabled by default in production unless explicitly enabled.

#### Benefits

- Connector registration and capability declarations are verifiable at runtime without code changes
- Health and throughput state is accessible to ops dashboards and alerting systems
- Misconfigured or unhealthy connectors are immediately visible without log diving

---

## v2.0.0 — Conversations

A conversation is more than a sequence of messages. It has participants, a history, a state, and — most importantly — continuity across channels and time. This major version introduces conversation management as a first-class concept in the framework, enabling applications to correlate inbound replies with outgoing messages, track the state of multi-turn exchanges, and span a single conversation across multiple channels.

---

### `IConversation` Abstraction

> *"A conversation is the unit of meaning — a message is just its atom."*

#### The Problem Today

The framework models individual messages well, but has no concept of a conversation that groups related messages together. Applications that need to track exchanges (e.g. a two-factor authentication flow, a customer support thread) must build their own correlation and state model on top of raw messages.

#### What We Are Building

An `IConversation` interface representing a thread of messages between an application and one or more participants. A conversation has a unique identifier, a list of participants (as `IEndpoint` instances), a channel, a status (open, closed, expired), and an ordered message history. Concrete implementations can back the conversation state in any persistence store.

#### Benefits

- Conversations are a first-class, typed concept in the framework
- Applications can reason about message threads without building custom correlation logic

#### Depends on

- Inbound Messaging (v0.4.0)

---

### Conversation State Model

> *"A conversation that cannot remember what was said is not a conversation."*

#### The Problem Today

Even with an `IConversation` abstraction, tracking state across multiple turns requires a persistence mechanism. There is no standard model for what conversation state looks like or how it transitions.

#### What We Are Building

A conversation state model that tracks participants, channel, status transitions (open → active → closed / expired), message history (both sent and received messages), and arbitrary metadata. A default in-memory implementation is provided for testing; the state store interface is designed for easy replacement with database-backed implementations.

#### Benefits

- Conversation state is structured and queryable, not ad-hoc
- The in-memory store enables conversation flow testing without a database
- The state store interface makes persistence backend replacement trivial

#### Depends on

- `IConversation` Abstraction

---

### Conversation Correlation

> *"When someone replies, the framework should know what they are replying to."*

#### The Problem Today

Inbound messages arrive with sender and recipient information but no connection to the outbound message that prompted them. Correlating a reply to its original message requires custom logic keyed on phone numbers, session identifiers, or other application-level markers.

#### What We Are Building

A correlation mechanism that links inbound messages to the outgoing conversation that originated them, using the sender/recipient endpoint pair and a configurable correlation window (time-based or explicit conversation ID passed via provider thread identifiers where supported). When an inbound message is correlated to a conversation, it is appended to that conversation's history automatically.

#### Benefits

- Replies are automatically associated with their originating conversation
- No custom correlation logic required in the application
- Correlation works across providers that support thread identifiers and falls back gracefully to endpoint-based matching for those that do not

#### Depends on

- `IConversation` Abstraction
- Conversation State Model
- Inbound Messaging (v0.4.0)

---

### Multi-Channel Conversations

> *"A conversation that started on SMS should be able to continue on WhatsApp."*

#### The Problem Today

Conversations are implicitly single-channel. There is no model for a conversation that spans multiple providers or endpoint types (e.g. starting as an SMS exchange and continuing in a WhatsApp thread).

#### What We Are Building

An extension to the conversation model that allows a conversation to be associated with multiple channels and endpoint pairs simultaneously. A participant resolution layer maps the same logical contact across different channel endpoints (e.g. the same phone number used as both an SMS and a WhatsApp endpoint). Messages in the conversation are tagged with the channel through which they were sent or received.

#### Benefits

- A single conversation object spans all channels used with a given contact
- Channel transitions within a conversation are explicit and auditable
- Applications can choose to respond on any active channel within the conversation

#### Depends on

- `IConversation` Abstraction
- Conversation Correlation

---

### Multi-Tenant Channel Registry

> *"Different tenants have different channels — the registry should know the difference."*

#### The Problem Today

The channel registry is a single shared instance. Applications that serve multiple tenants — each with their own connector credentials, sender identities, and channel configurations — must manage per-tenant connector instances themselves, outside the framework.

#### What We Are Building

A tenant-aware channel registry that resolves connector instances and channel schemas per tenant identifier. The tenant ID is supplied at send time (via message properties or a configurable ambient context). Per-tenant channel configurations are loaded from a configurable source (in-memory, database, configuration provider).

#### Benefits

- Multi-tenant messaging is supported natively, with no application-level workaround
- Tenant isolation is enforced at the registry level — one tenant's configuration cannot bleed into another's
- Tenant-specific sender identities and credentials are resolved automatically per message

#### Depends on

- Sender Identity Model (v1.0.4)

---

### xUnit Test Helpers for Conversation Flows

> *"A conversation that cannot be tested in isolation is a conversation waiting to break."*

#### The Problem Today

Testing multi-turn conversation flows requires setting up real or fake inbound messages, correlating them to conversations, and asserting on state transitions — all with no framework-level test utilities.

#### What We Are Building

Extensions to the xUnit helper packages with conversation builders (pre-populate a conversation with a history), fake inbound message injectors, and assertion helpers for conversation state, participant lists, message history, and correlation results.

#### Benefits

- Conversation flow logic can be unit-tested without a live connector or database
- Common conversation test scenarios (reply correlation, state transitions, channel switching) are one-liners

#### Depends on

- Conversation Correlation
- Conversation State Model

---

## v2.1.0 — Message Templates

Templates are how organisations send consistent, personalised messages at scale. Every major provider has its own template format — WhatsApp requires pre-approved template names, SendGrid uses dynamic template IDs, Firebase supports notification templates. This milestone introduces a provider-agnostic template model that maps to each provider's native template mechanism, and includes a local variable substitution engine for providers that do not have server-side templating.

---

### `IMessageTemplate` Abstraction

> *"A template is a promise about the shape of a message — define it once, honour it everywhere."*

#### The Problem Today

There is no shared template model. `ITemplateContent` exists in the content model but carries only an identifier and parameters, with no definition of what the template looks like or how parameters are bound. Each connector interprets the identifier in its own way, and there is no registry or schema for templates.

#### What We Are Building

An `IMessageTemplate` interface that defines a template by name, locale, content structure (using the existing content model), and named variable placeholders. Templates can be defined in code or loaded from a store. The `ITemplateContent` sent in a message references the template by name and provides variable values; the framework resolves the template and either renders it locally or delegates to the provider's server-side templating.

#### Benefits

- Templates are first-class typed objects, not opaque string identifiers
- Template structure and variable schema are defined and validated in the framework
- The same template definition drives both local rendering and provider-native template references

---

### Variable Substitution Engine

> *"Every personalised message is a template with names filled in."*

#### The Problem Today

There is no local variable substitution engine. Providers that do not support server-side templating (or where a pre-approved provider template is not available) require the application to prepare the fully-rendered content manually.

#### What We Are Building

A local rendering engine that resolves an `IMessageTemplate` with a set of variable values, producing a fully-rendered `IMessageContent`. The engine supports simple named placeholder substitution (e.g. `{{first_name}}`), type-safe variable binding, and missing-variable handling (fail-fast or fallback value). The rendered content is then sent through the normal connector send path.

#### Benefits

- Applications send template + variables; the framework handles rendering
- Missing variables and type mismatches are caught at render time, not at provider API time
- Local rendering works for any connector, regardless of whether the provider supports server-side templates

#### Depends on

- `IMessageTemplate` Abstraction

---

### Per-Connector Template Rendering

> *"WhatsApp wants a template name and parameters. SendGrid wants a template ID. The framework should speak both."*

#### The Problem Today

Providers that have their own server-side template systems (WhatsApp Business, SendGrid, Firebase) expect the template to be referenced by a provider-specific identifier and delivered in a provider-specific format. There is no bridge between the framework's template model and these provider-native systems.

#### What We Are Building

Per-connector template adapters that, when a connector detects that a message references a template, determine whether to: (a) delegate to the provider's own template system (passing the provider-native template ID and variable parameters), or (b) render locally via the variable substitution engine and send the rendered content. Each connector's template adapter is configurable — teams that manage their own templates in the provider's console can map framework template names to provider template IDs.

#### Benefits

- Provider-managed templates (WhatsApp approved templates, SendGrid dynamic templates) are used natively when available
- Locally-defined templates are rendered and sent as regular content for providers without server-side templating
- The template reference in the message is the same regardless of which path is taken

#### Depends on

- `IMessageTemplate` Abstraction
- Variable Substitution Engine

---

### Template Registry

> *"A template that cannot be found at send time is a template that cannot be trusted."*

#### The Problem Today

There is no centralised store for template definitions. Templates are referenced by name in messages, but there is no place to register, look up, validate, or manage them within the framework.

#### What We Are Building

An `ITemplateRegistry` that stores and retrieves `IMessageTemplate` instances by name and locale. A default in-memory registry is provided; the interface supports database-backed implementations. The registry validates that all templates referenced in a channel schema are registered and that their variable schemas are complete before the connector accepts messages.

#### Benefits

- All templates are defined in one place and validated at startup
- Locale-specific template variants are resolved automatically based on the recipient's locale
- Missing or misconfigured templates are caught before any message is sent

#### Depends on

- `IMessageTemplate` Abstraction

---

## Contributing

If you have feature ideas, bug reports, or want to work on any of the items listed above, please open an issue or pull request on [GitHub](https://github.com/deveel/deveel.messaging). See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on setting up the development environment, coding standards, and the pull request process.

Automated agents (AI) may be used only in accessory contexts such as documentation and tests. The core body of source code must be written by humans to ensure code quality and maintainability.
