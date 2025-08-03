# Deveel Messaging Framework

![Deveel Logo](deveel-logo.png)

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/badge/NuGet-Available-blue)](https://www.nuget.org/)

A modern, extensible messaging framework for .NET that provides a unified abstraction layer for various messaging providers including SMS, email, and push notifications. The framework offers strong type safety, comprehensive validation, and flexible connector architecture.

## 🚀 Motivation

Modern applications often need to send notifications through multiple channels (SMS, email, push notifications, webhooks). Each provider has different APIs, authentication methods, and message formats. The Deveel Messaging Framework solves this by:

- **Unified API**: Single interface for all messaging providers
- **Type Safety**: Strongly-typed endpoints and configurations prevent runtime errors
- **Extensibility**: Easy to add new providers and message types
- **Validation**: Built-in message and configuration validation
- **Testability**: Comprehensive mocking and testing support
- **Performance**: Async/await throughout with efficient resource usage

## 📦 NuGet Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `Deveel.Messaging.Abstractions` | Core messaging abstractions and models | [![NuGet](https://img.shields.io/nuget/v/Deveel.Messaging.Abstractions.svg)](https://www.nuget.org/packages/Deveel.Messaging.Abstractions/) |
| `Deveel.Messaging.Connector.Abstractions` | Base classes and interfaces for connectors | [![NuGet](https://img.shields.io/nuget/v/Deveel.Messaging.Connector.Abstractions.svg)](https://www.nuget.org/packages/Deveel.Messaging.Connector.Abstractions/) |
| `Deveel.Messaging.Connector.Twilio` | Twilio SMS connector implementation | [![NuGet](https://img.shields.io/nuget/v/Deveel.Messaging.Connector.Twilio.svg)](https://www.nuget.org/packages/Deveel.Messaging.Connector.Twilio/) |
| `Deveel.Messaging.Connector.Sendgrid` | SendGrid email connector implementation | [![NuGet](https://img.shields.io/nuget/v/Deveel.Messaging.Connector.Sendgrid.svg)](https://www.nuget.org/packages/Deveel.Messaging.Connector.Sendgrid/) |

## 🔧 Installation

### Install Core Packages

```bash
# Core messaging abstractions
dotnet add package Deveel.Messaging.Abstractions

# Connector base classes (if building custom connectors)
dotnet add package Deveel.Messaging.Connector.Abstractions
```

### Install Provider-Specific Connectors

```bash
# Twilio SMS connector
dotnet add package Deveel.Messaging.Connector.Twilio

# SendGrid email connector
dotnet add package Deveel.Messaging.Connector.Sendgrid
```

## 🏁 Quick Start

### 1. Define a Channel Schema

```csharp
using Deveel.Messaging;

var emailSchema = new ChannelSchema("SendGrid", "Email", "1.0.0")
    .WithDisplayName("SendGrid Email Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.Templates | 
        ChannelCapability.MediaAttachments)
    .AddParameter(new ChannelParameter("ApiKey", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "SendGrid API key"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AddAuthenticationType(AuthenticationType.Token);
```

### 2. Create and Send Messages

```csharp
// Create a message using the fluent builder
var message = new MessageBuilder()
    .WithId("welcome-email-001")
    .WithEmailSender("noreply@company.com")
    .WithEmailReceiver("user@example.com")
    .WithHtmlContent("<h1>Welcome!</h1><p>Thank you for joining us.</p>")
    .WithProperty("Subject", "Welcome to Our Service")
    .Message;

// Initialize and use a connector
var connector = new SendGridConnector(emailSchema);
await connector.InitializeAsync(cancellationToken);

var result = await connector.SendMessageAsync(message, cancellationToken);
if (result.IsSuccess)
{
    Console.WriteLine($"Message sent with ID: {result.Value?.MessageId}");
}
```

### 3. SMS Example with Twilio

```csharp
var smsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddContentType(MessageContentType.PlainText)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

var smsMessage = new MessageBuilder()
    .WithId("sms-notification-001")
    .WithPhoneSender("+1234567890")
    .WithPhoneReceiver("+0987654321")
    .WithTextContent("Your verification code is: 123456")
    .Message;

var twilioConnector = new TwilioConnector(smsSchema);
await twilioConnector.InitializeAsync(cancellationToken);
var smsResult = await twilioConnector.SendMessageAsync(smsMessage, cancellationToken);
```

## 🎯 Core Features

### Strongly-Typed Endpoints

Use the `EndpointType` enumeration for type-safe endpoint configuration:

```csharp
// Type-safe endpoint creation
var emailEndpoint = Endpoint.EmailAddress("user@example.com");
var phoneEndpoint = Endpoint.PhoneNumber("+1234567890");
var webhookEndpoint = Endpoint.Url("https://api.example.com/webhook");

// Builder pattern with type safety
var message = new MessageBuilder()
    .WithEmailSender("sender@company.com")
    .WithPhoneReceiver("+1234567890")
    .WithTextContent("Hello from our service!")
    .Message;
```

### Content Types and Templates

Support for various content types and templating:

```csharp
// HTML content
.WithHtmlContent("<h1>Welcome</h1><p>{{username}}, thanks for joining!</p>")

// Template content with parameters
.WithTemplateContent("welcome-template", new { username = "John", company = "Acme Corp" })

// Multipart content with attachments
.WithMultipartContent(content => content
    .AddTextPart("Please find the report attached.")
    .AddAttachment("report.pdf", pdfBytes, "application/pdf"))
```

### Connector Capabilities

Declare and validate connector capabilities:

```csharp
var schema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(
        ChannelCapability.SendMessages |        // Can send messages
        ChannelCapability.ReceiveMessages |     // Can receive messages
        ChannelCapability.MessageStatusQuery |  // Can track delivery status
        ChannelCapability.BulkMessaging |       // Supports batch operations
        ChannelCapability.Templates |           // Supports templating
        ChannelCapability.MediaAttachments |    // Supports file attachments
        ChannelCapability.HealthCheck)          // Provides health monitoring
```

### Error Handling and Results

Comprehensive error handling with detailed result objects:

```csharp
var result = await connector.SendMessageAsync(message, cancellationToken);
if (result.IsSuccess)
{
    var messageResult = result.Value;
    Console.WriteLine($"Message ID: {messageResult.MessageId}");
    Console.WriteLine($"Status: {messageResult.Status}");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
    Console.WriteLine($"Error Code: {result.ErrorCode}");
}
```

## 🏗️ Custom Connector Development

Create custom connectors by extending the base connector class:

```csharp
public class CustomConnector : ChannelConnectorBase
{
    public CustomConnector(IChannelSchema schema) : base(schema) { }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // Initialize your connector
        SetState(ConnectorState.Connected);
        return ConnectorResult<bool>.Success(true);
    }

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        // Implement your message sending logic
        var messageId = await SendToProvider(message);
        return ConnectorResult<MessageResult>.Success(
            new MessageResult(messageId, MessageStatus.Sent));
    }

    // Implement other abstract methods...
}
```

## 📚 Documentation

- **[Getting Started Guide](docs/getting-started.md)** - Basic setup and usage
- **[Channel Schema Guide](docs/ChannelSchema-Usage.md)** - Complete schema configuration
- **[Connector Implementation Guide](docs/ChannelConnector-Usage.md)** - Building custom connectors
- **[Schema Derivation Guide](docs/ChannelSchema-Derivation-Guide.md)** - Advanced schema patterns
- **[Endpoint Type Safety Guide](docs/EndpointType-Usage.md)** - Working with typed endpoints
- **[Migration Guide](docs/migration-guide.md)** - Upgrading from previous versions

## 🔗 GitHub Packages

This project publishes packages to both NuGet.org and GitHub Packages. To use GitHub Packages:

```xml
<!-- Add to your NuGet.config -->
<packageSources>
    <add key="github-deveel" value="https://nuget.pkg.github.com/deveel/index.json" />
</packageSources>
```

```bash
# Authenticate with GitHub Packages
dotnet nuget add source --username YOUR_USERNAME --password YOUR_PAT --store-password-in-clear-text --name github-deveel "https://nuget.pkg.github.com/deveel/index.json"

# Install from GitHub Packages
dotnet add package Deveel.Messaging.Abstractions --source github-deveel
```

## 🧪 Testing

The framework includes comprehensive test suites:

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test test/Deveel.Messaging.Abstractions.XUnit
dotnet test test/Deveel.Messaging.Connector.Twilio.XUnit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Requirements

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** for source control

### Building the Project

```bash
# Clone the repository
git clone https://github.com/deveel/deveel.message.model.git
cd deveel.message.model

# Build the solution
dotnet build

# Run tests
dotnet test

# Pack NuGet packages
dotnet pack --configuration Release
```

### Code Standards

- Follow Microsoft's C# coding conventions
- Use C# 12.0 language features where appropriate
- Include comprehensive unit tests for new features
- Update documentation for public API changes
- Ensure nullable reference types are properly handled

### Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests for your changes
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏢 About Deveel

Deveel is committed to creating high-quality, open-source software solutions for the .NET ecosystem. Visit us at [deveel.com](https://deveel.com) for more projects and services.

## 📞 Support

- **Documentation**: [docs/README.md](docs/README.md)
- **Issues**: [GitHub Issues](https://github.com/deveel/deveel.message.model/issues)
- **Discussions**: [GitHub Discussions](https://github.com/deveel/deveel.message.model/discussions)
- **Email**: support@deveel.com

---

*Built with ❤️ by the Deveel team*

