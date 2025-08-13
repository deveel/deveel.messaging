# Documentation Index

This directory contains comprehensive documentation for the Deveel Messaging Framework, covering Channel Schema, Channel Connector usage, and the latest features including Firebase push notifications, enhanced WhatsApp Business integration, and webhook support.

## ?? Documentation Overview

| Document | Description | Audience |
|----------|-------------|----------|
| [README.md](../README.md) | Framework overview and quick start guide | All developers |
| [Getting Started Guide](getting-started.md) | Step-by-step setup and first message | New users |
| [Connector Documentation](connectors/README.md) | **Complete connector specifications and usage** | **All developers** |
| [Channel Schema Usage Guide](ChannelSchema-Usage.md) | Complete guide to creating and configuring channel schemas | Schema designers, connector developers |
| [Channel Connector Implementation Guide](ChannelConnector-Usage.md) | How to implement custom channel connectors | Connector developers |
| [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md) | Creating specialized schemas from base configurations | Advanced users, system architects |
| [Endpoint Type Usage Guide](EndpointType-Usage.md) | Working with strongly-typed endpoint configurations | All developers |
| [Advanced Configuration Guide](advanced-configuration.md) | Production-ready patterns and optimizations | DevOps, system architects |
| [Migration Guide](migration-guide.md) | Upgrading from previous versions | Existing users |

## ?? Connector Documentation

### Individual Connector Guides

| Connector | Provider | Type | Documentation |
|-----------|----------|------|---------------|
| **Twilio SMS** | Twilio | SMS | [Complete Guide](connectors/twilio-sms-connector.md) |
| **Twilio WhatsApp** | Twilio | WhatsApp | [Complete Guide](connectors/twilio-whatsapp-connector.md) |
| **Firebase FCM** | Firebase | Push | [Complete Guide](connectors/firebase-push-connector.md) |
| **SendGrid Email** | SendGrid | Email | [Complete Guide](connectors/sendgrid-email-connector.md) |

Each connector guide includes:
- ? **Schema Specifications** - Available schemas and capabilities
- ?? **Connection Parameters** - Required and optional configuration with examples
- ?? **Message Properties** - Supported properties, validation rules, and limits
- ?? **Usage Examples** - Complete code samples for common scenarios
- ?? **Webhook Integration** - Event handling and bidirectional messaging
- ? **Error Handling** - Common errors, solutions, and retry strategies
- ? **Best Practices** - Production-ready patterns and optimization tips

### ?? Quick Connector Comparison

| Feature | SMS | WhatsApp | Push | Email |
|---------|-----|----------|------|-------|
| **Send Messages** | ? | ? | ? | ? |
| **Receive Messages** | ? | ? | ? | ? |
| **Templates** | ? | ? | ? | ? |
| **Media Support** | ? | ? | ? | ? |
| **Batch Operations** | ? | ? | ? | ? |
| **Interactive Elements** | ? | ? | ? | ? |

## ?? Quick Navigation

### For Getting Started
- Start with [README.md](../README.md) for framework overview
- Follow [Getting Started Guide](getting-started.md) for your first implementation
- Check [Connector Documentation](connectors/README.md) for specific provider setup
- Review [Endpoint Type Usage Guide](EndpointType-Usage.md) for endpoint configuration

### For Implementation
- Use [Connector Documentation](connectors/README.md) for provider-specific implementation
- Use [Channel Schema Usage Guide](ChannelSchema-Usage.md) for basic schema creation
- Use [Channel Connector Implementation Guide](ChannelConnector-Usage.md) to build custom connectors
- Refer to [Advanced Configuration Guide](advanced-configuration.md) for production patterns

### For Migration and Maintenance
- See [Migration Guide](migration-guide.md) for upgrading from previous versions
- Check [Advanced Configuration Guide](advanced-configuration.md) for optimization techniques

## ?? Latest Framework Features

### ?? Firebase Cloud Messaging (FCM) Support
- **Push Notifications**: Complete Firebase connector for mobile and web push notifications
- **Device Targeting**: Send to specific device tokens or broadcast to topics
- **Platform-Specific**: Android, iOS (APNS), and Web Push configurations
- **Rich Notifications**: Images, actions, custom data, and interactive elements
- **Batch Operations**: Efficient multicast delivery to thousands of devices
- **Health Monitoring**: Built-in connection testing and health checks

**?? [Complete Firebase Documentation](connectors/firebase-push-connector.md)**

### ?? Enhanced WhatsApp Business Integration
- **Webhook Support**: Complete JSON and form-data webhook handling for incoming messages
- **Message Receiving**: Process incoming WhatsApp messages with business-specific fields
- **Status Updates**: Real-time delivery status tracking including read receipts
- **Interactive Elements**: Button responses, list selections, and menu interactions
- **Business Features**: Profile names, business display names, verified accounts
- **Media Support**: Images, documents, audio, video, and location sharing
- **Template Messaging**: WhatsApp Business approved message templates

**?? [Complete WhatsApp Documentation](connectors/twilio-whatsapp-connector.md)**

### ?? Advanced SMS & Communication
- **Twilio Integration**: Enhanced SMS connector with webhook support
- **Two-Way Messaging**: Send and receive SMS messages via webhooks
- **Status Tracking**: Comprehensive delivery status monitoring
- **Bulk Operations**: Efficient batch SMS sending
- **International Support**: Global SMS delivery with proper formatting

**?? [Complete SMS Documentation](connectors/twilio-sms-connector.md)**

### ?? Email Integration
- **SendGrid Connector**: Production-ready email sending
- **Template Support**: Dynamic email templates with variable substitution
- **Attachment Support**: Send files and media via email
- **HTML/Text Content**: Rich email formatting capabilities
- **Marketing Features**: Campaign tracking, A/B testing, analytics

**?? [Complete Email Documentation](connectors/sendgrid-email-connector.md)**

## ?? Framework Features

### Channel Schema
- ?? **Fluent API** - Method chaining for easy configuration
- ? **Type Safety** - Strongly-typed endpoint and content type enums
- ?? **Schema Derivation** - Create specialized schemas from base configurations
- ? **Validation** - Built-in validation for parameters and message properties
- ?? **Capability Management** - Declare and validate connector capabilities

### Channel Connectors
- ? **Base Implementation** - `ChannelConnectorBase` with common functionality
- ?? **State Management** - Automatic connector state handling
- ? **Error Handling** - Standardized error reporting with `ConnectorResult<T>`
- ? **Async Support** - Full async/await with cancellation token support
- ? **Message Validation** - Automatic message validation against schema
- ?? **Webhook Integration** - Built-in webhook processing for bidirectional communication

### Endpoint Types
- ? **Strongly Typed** - `EndpointType` enumeration prevents errors
- ?? **Backward Compatible** - Automatic conversion from legacy string types
- ?? **IntelliSense Support** - Better developer experience with auto-completion
- ? **Validation** - Compile-time and runtime validation

### Message Processing
- ?? **Batch Operations** - Efficient bulk message processing
- ?? **Content Types** - Text, HTML, templates, media, and multipart content
- ?? **Status Tracking** - Real-time delivery status monitoring
- ?? **Webhook Support** - Bidirectional message flow with webhook integration

## ?? Common Use Cases

### Email Connectors
```csharp
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.PlainText)
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.Templates);
```

### SMS Connectors
```csharp
var smsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AddContentType(MessageContentType.PlainText)
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages);
```

### WhatsApp Business
```csharp
var whatsAppSchema = new ChannelSchema("Twilio", "WhatsApp", "2.1.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Template)
    .AddContentType(MessageContentType.Media)
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.Templates |
        ChannelCapability.MediaAttachments);
```

### Push Notifications
```csharp
var pushSchema = new ChannelSchema("Firebase", "Push", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.DeviceId)
    .AllowsMessageEndpoint(EndpointType.Topic)
    .AddContentType(MessageContentType.PlainText)
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.BulkMessaging |
        ChannelCapability.HealthCheck);
```

### Multi-Channel Connectors
```csharp
var multiSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.DeviceId)
    .AllowsMessageEndpoint(EndpointType.Url);
```

### Customer-Specific Configurations
```csharp
var customerSchema = new ChannelSchema(baseSchema, "Customer Specific")
    .RemoveCapability(ChannelCapability.ReceiveMessages)
    .RestrictContentTypes(MessageContentType.PlainText);
```

## ?? Target Framework Information

- **.NET 8.0** and **.NET 9.0** support
- **C# 12.0** language features
- Modern async/await patterns
- Nullable reference types enabled
- Cross-platform compatibility

## ?? Learning Path

### Beginner (New to Framework)
1. Read [README.md](../README.md) overview
2. Follow [Getting Started Guide](getting-started.md)
3. Choose your connector from [Connector Documentation](connectors/README.md)
4. Review [Endpoint Type Usage Guide](EndpointType-Usage.md) fundamentals
5. Practice with simple [Channel Schema Usage Guide](ChannelSchema-Usage.md) examples

### Intermediate (Building Applications)
1. Study specific [Connector Documentation](connectors/README.md) for your providers
2. Learn [Advanced Configuration Guide](advanced-configuration.md) patterns
3. Practice with provider-specific connectors (Twilio, Firebase, SendGrid)
4. Implement error handling and validation
5. Set up webhook endpoints for bidirectional communication

### Advanced (Production Systems)
1. Master [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md)
2. Implement [Advanced Configuration Guide](advanced-configuration.md) patterns
3. Design multi-tenant configurations
4. Build custom connectors using [Channel Connector Implementation Guide](ChannelConnector-Usage.md)
5. Implement monitoring and performance optimization
6. Set up comprehensive webhook processing systems

### Migration (Existing Users)
1. Start with [Migration Guide](migration-guide.md)
2. Update endpoint types using [Endpoint Type Usage Guide](EndpointType-Usage.md)
3. Modernize schemas with [Channel Schema Usage Guide](ChannelSchema-Usage.md)
4. Apply production patterns from [Advanced Configuration Guide](advanced-configuration.md)

## ?? Code Examples

Each guide contains comprehensive, real-world examples:

### Getting Started Examples
- **Simple Email Connector** - Basic setup and first message
- **SMS Connector** - Twilio integration walkthrough
- **WhatsApp Business** - Template and interactive messaging
- **Firebase Push** - Device and topic notifications
- **Mock Connector** - Testing and development patterns

### Production Examples
- **SMTP Email Connector** - Complete implementation with authentication
- **Twilio SMS Connector** - Send/receive with status tracking
- **WhatsApp Business Integration** - Webhook processing with interactive elements
- **Firebase FCM Connector** - Multicast push notifications
- **Webhook Connector** - HTTP-based message delivery
- **Multi-Channel Universal Connector** - Supporting multiple endpoint types
- **Customer-Specific Schemas** - Derivation for different business requirements

### Advanced Examples
- **Connector Pools** - Load balancing and resource management
- **Batching Decorators** - Performance optimization patterns
- **Security Implementations** - Secure parameter handling and auditing
- **Health Checks** - Monitoring and diagnostics
- **Multi-Environment Configuration** - Development to production patterns
- **Webhook Processing** - Complete bidirectional message flow

## ??? Development Tools

The framework provides excellent development experience:

- **IntelliSense** - Full IDE support with auto-completion
- **Compile-time Validation** - Catch errors before runtime
- **Runtime Validation** - Comprehensive message and configuration validation
- **Debugging Support** - Clear error messages and diagnostic information
- **Testing Tools** - Mocking support and test utilities
- **Webhook Testing** - Built-in tools for webhook development and testing

## ?? Available Connectors

### Production-Ready Connectors
- **Twilio SMS** - SMS messaging with webhook support
- **Twilio WhatsApp** - WhatsApp Business messaging with interactive features
- **SendGrid Email** - Email delivery with templates and attachments
- **Firebase FCM** - Push notifications for mobile and web

### Connector Capabilities Matrix

| Connector | Send | Receive | Status | Batch | Templates | Media | Health |
|-----------|------|---------|--------|-------|-----------|-------|---------|
| Twilio SMS | ? | ? | ? | ? | ? | ? | ? |
| Twilio WhatsApp | ? | ? | ? | ? | ? | ? | ? |
| SendGrid Email | ? | ? | ? | ? | ? | ? | ? |
| Firebase FCM | ? | ? | ? | ? | ? | ? | ? |

## ?? Additional Resources

### Example Projects
- Check test projects for more examples:
  - `test\Deveel.Messaging.Abstractions.XUnit` - Core messaging examples
  - `test\Deveel.Messaging.Connector.Abstractions.XUnit` - Connector implementation examples
  - `test\Deveel.Messaging.Connector.Twilio.XUnit` - Twilio connector tests (500+ tests)
  - `test\Deveel.Messaging.Connector.Sendgrid.XUnit` - SendGrid connector tests
  - `test\Deveel.Messaging.Connector.Firebase.XUnit` - Firebase connector tests

### Integration Examples
- Review schema examples in test files
- Examine integration test scenarios
- Study connector implementation patterns
- Webhook processing examples

### Community Resources
- [GitHub Discussions](https://github.com/deveel/deveel.message.model/discussions) - Ask questions and share ideas
- [GitHub Issues](https://github.com/deveel/deveel.message.model/issues) - Report bugs and request features
- [Contributing Guide](../CONTRIBUTING.md) - How to contribute to the project

## ?? Contributing

When contributing to the framework:

1. Follow the patterns shown in the documentation
2. Include comprehensive tests for new features
3. Update documentation for any public API changes
4. Use strongly-typed endpoints in all new code
5. Maintain backward compatibility where possible
6. Review the [Contributing Guide](../CONTRIBUTING.md) for detailed guidelines

## ?? Getting Help

### Self-Service Resources
1. **Search Documentation** - Use the table of contents above
2. **Review Connector Guides** - Check connector-specific documentation
3. **Review Examples** - Check test projects and code samples
4. **Study Patterns** - Follow established conventions in the guides

### Community Support
1. **GitHub Discussions** - Ask questions and get community help
2. **GitHub Issues** - Report bugs and request features
3. **Stack Overflow** - Tag questions with `deveel-messaging`

### Professional Support
- **Email Support** - support@deveel.com for enterprise inquiries
- **Consulting Services** - Professional implementation assistance available

## ?? Documentation Quality

We maintain high documentation standards:

- **Accuracy** - All examples are tested and verified (500+ tests)
- **Completeness** - Comprehensive coverage of all features
- **Clarity** - Clear explanations with practical examples
- **Currency** - Regular updates to match framework evolution
- **Accessibility** - Multiple learning paths for different experience levels

## ?? What's New

### Recent Additions
- **?? NEW: [Connector Documentation](connectors/README.md)** - Comprehensive connector specifications
- **Firebase Cloud Messaging** - Complete FCM connector implementation
- **Enhanced WhatsApp Business** - Webhook support, interactive elements, business features
- **Improved SMS Integration** - Two-way messaging with webhook support
- **Batch Processing** - Efficient bulk operations across all connectors
- **Health Monitoring** - Comprehensive health checks and diagnostics
- **Webhook Framework** - Built-in webhook processing capabilities

---

*This documentation is comprehensive and up-to-date as of the latest framework version. All examples have been tested and reflect current best practices with the latest connector implementations.*