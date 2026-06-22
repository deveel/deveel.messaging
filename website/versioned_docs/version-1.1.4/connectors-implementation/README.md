# Connector Implementation Guide

This section provides comprehensive guidance for building custom connectors when you need to integrate with messaging providers not covered by the built-in connectors.

## When to Build a Custom Connector

Build a custom connector when:
- You need to integrate with a proprietary or internal messaging system
- A provider doesn't have an existing connector in the framework
- You need custom authentication or message translation logic
- You want to wrap legacy systems with the framework's unified interface

## What You'll Learn

| Guide | Description |
|-------|-------------|
| [Overview](overview.md) | Architecture, patterns, and what the base class provides |
| [Minimum Implementation](minimum-implementation.md) | Four required methods with complete working example |
| [Authentication](authentication.md) | Declaring and using authentication in custom connectors |
| [Message Validation](message-validation.md) | Custom validation rules and error handling |
| [Advanced Topics](advanced-topics.md) | Receive messages, status queries, batch sending, testing |

## Architecture Overview

The framework uses the **Template Method pattern**: the base class (`ChannelConnectorBase`) defines the skeleton of each operation and calls into your overrides for provider-specific logic.

**Base class handles:**
- ✅ State management (lifecycle tracking)
- ✅ Capability validation (schema-based guards)
- ✅ Message validation (pre-flight checks)
- ✅ Authentication (credential acquisition and caching)
- ✅ Error wrapping (exceptions → `OperationResult<T>`)
- ✅ Logging scopes (structured tracing)

**You implement:**
- Provider-specific initialization
- Message translation (`IMessage` → provider API)
- API calls and response parsing
- Connection testing

## Getting Started

Start with the [Minimum Implementation](minimum-implementation.md) guide to build a working connector in under 100 lines of code. Then explore advanced topics as needed.

## Prerequisites

- Understanding of the [Messaging Model](../messaging-model.md)
- Familiarity with [Channel Schema](../channel-schema.md) concepts
- Basic knowledge of [Authentication](../authentication.md) patterns
