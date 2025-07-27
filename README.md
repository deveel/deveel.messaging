# Deveel Messaging Framework

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
- 🔧 **Schema Derivation** - Create specialized schemas from base configurations
- 📋 **Endpoint Type Safety** - Strongly-typed endpoint configuration using enums

## Target Frameworks

- .NET 8.0
- .NET 9.0
- C# 12.0

## Quick Start

### 1. Channel Schema Configuration

Create a schema that defines your connector's capabilities and configuration:

```csharp
using Deveel.Messaging;

// Create an email connector schema
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .WithDisplayName("SMTP Email Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.Templates | 
        ChannelCapability.MediaAttachments)
    .AddParameter(new ChannelParameter("Host", ParameterType.String)
    {
        IsRequired = true,
        Description = "SMTP server hostname"
    })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer)
    {
        IsRequired = true,
        DefaultValue = 587,
        Description = "SMTP server port"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AddAuthenticationType(AuthenticationType.Basic);
```

### 2. Implement a Channel Connector

```csharp
public class SmtpConnector : ChannelConnectorBase
{
    public SmtpConnector(IChannelSchema schema) : base(schema)
    {
    }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // Initialize SMTP client
        // Validate configuration parameters
        // Set up authentication
        
        SetState(ConnectorState.Connected);
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<bool>> TestConnectionCoreAsync(CancellationToken cancellationToken)
    {
        // Test SMTP connection
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        // Send email message
        var messageId = Guid.NewGuid().ToString();
        return ConnectorResult<MessageResult>.Success(new MessageResult(messageId, MessageStatus.Sent));
    }
}
```

### 3. Use the Connector

```csharp
// Create and configure the connector
var connector = new SmtpConnector(emailSchema);

// Initialize the connector
var initResult = await connector.InitializeAsync(CancellationToken.None);
if (!initResult.IsSuccess)
{
    Console.WriteLine($"Failed to initialize: {initResult.ErrorMessage}");
    return;
}

// Create and send a message
var message = new MessageBuilder()
    .WithId("msg-001")
    .WithEmailSender("sender@example.com")
    .WithEmailReceiver("recipient@example.com")
    .WithTextContent("Hello, World!")
    .Message;

var sendResult = await connector.SendMessageAsync(message, CancellationToken.None);
if (sendResult.IsSuccess)
{
    Console.WriteLine($"Message sent with ID: {sendResult.Value?.MessageId}");
}
```

## Core Concepts

### Messages and Content Types

The framework supports various content types:

- **Text Content** - Plain text messages
- **HTML Content** - Rich HTML content with attachments
- **Template Content** - Template-based content with parameters
- **Multipart Content** - Complex messages with multiple parts

### Endpoints

Endpoints are strongly-typed using the `EndpointType` enumeration:

- `EndpointType.EmailAddress` - Email addresses
- `EndpointType.PhoneNumber` - Phone numbers for SMS
- `EndpointType.Url` - URLs for webhooks
- `EndpointType.UserId` - User identifiers
- `EndpointType.ApplicationId` - Application identifiers
- `EndpointType.DeviceId` - Device identifiers
- `EndpointType.Label` - Alpha-numeric labels
- `EndpointType.Topic` - Queue/topic names
- `EndpointType.Any` - Wildcard for any endpoint type

### Connectors

Connectors implement the `IChannelConnector` interface and provide:

- **Connection Management** - Initialize, test, and manage connections
- **Message Operations** - Send single messages or batches
- **Status Monitoring** - Track message delivery and connector health
- **Validation** - Validate messages before sending

### Connector Capabilities

Connectors can declare their capabilities:

- `ChannelCapability.SendMessages` - Can send messages
- `ChannelCapability.ReceiveMessages` - Can receive messages
- `ChannelCapability.MessageStatusQuery` - Can query message status
- `ChannelCapability.BulkMessaging` - Supports batch operations
- `ChannelCapability.Templates` - Supports template-based content
- `ChannelCapability.MediaAttachments` - Supports media attachments
- `ChannelCapability.HealthCheck` - Provides health monitoring

## Documentation

- [Channel Schema Usage Guide](docs/ChannelSchema-Usage.md)
- [Channel Connector Implementation Guide](docs/ChannelConnector-Usage.md)
- [Schema Derivation Guide](docs/ChannelSchema-Derivation-Guide.md)
- [Endpoint Type Safety Guide](docs/EndpointType-Usage.md)

## Examples

Check the test projects for comprehensive examples:
- `test\Deveel.Messaging.Abstractions.XUnit` - Core messaging examples
- `test\Deveel.Messaging.Connector.Abstractions.XUnit` - Connector implementation examples

## Contributing

This project follows Microsoft's coding standards and uses C# 12.0 features. All contributions should include appropriate unit tests and documentation updates.

## License

Licensed under the MIT License. See LICENSE file for details.

