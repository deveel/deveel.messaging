# Deveel Messaging Model

A comprehensive .NET messaging framework that provides abstractions and connector interfaces for building robust messaging systems. This framework enables developers to create standardized messaging solutions that can work with various messaging providers and protocols.

## Overview

The Deveel Messaging Framework consists of two main packages:

- **Deveel.Messaging.Abstractions** - Core messaging abstractions including messages, endpoints, and content types
- **Deveel.Messaging.Connector.Abstractions** - Connector interfaces for implementing messaging system integrations

## Features

- 🚀 **Unified Messaging Interface** - Standardized contracts for messaging operations
- 🔌 **Connector Architecture** - Pluggable connector system for different messaging providers
- 📧 **Multiple Content Types** - Support for text, HTML, multipart, and template-based content
- ⚡ **Async/Await Support** - Full asynchronous operation support with cancellation tokens
- 🔍 **Message Validation** - Built-in message validation with detailed error reporting
- 📊 **Health Monitoring** - Comprehensive health checking and status reporting
- 🔄 **Batch Operations** - Support for bulk message sending and receiving
- 🎯 **Type Safety** - Strongly-typed interfaces and result objects


## Core Concepts

### Messages and Content Types

The framework supports various content types:

- **Text Content** - Plain text messages
- **HTML Content** - Rich HTML content with attachments
- **Template Content** - Template-based content with parameters
- **Multipart Content** - Complex messages with multiple parts

### Connectors

Connectors implement the `IChannelConnector` interface and provide:

- **Connection Management** - Initialize, test, and manage connections
- **Message Operations** - Send single messages or batches
- **Status Monitoring** - Track message delivery and connector health
- **Validation** - Validate messages before sending

### Connector Capabilities

Connectors can declare their capabilities:

## Target Frameworks

- .NET 8.0
- .NET 9.0
- C# 12.0

