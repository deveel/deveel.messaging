# Documentation Index

This directory contains comprehensive documentation for the Deveel Messaging Framework, covering both Channel Schema and Channel Connector usage with the latest strongly-typed endpoint system.

## ?? Documentation Overview

| Document | Description | Audience |
|----------|-------------|----------|
| [README.md](../README.md) | Framework overview and quick start guide | All developers |
| [Getting Started Guide](getting-started.md) | Step-by-step setup and first message | New users |
| [Channel Schema Usage Guide](ChannelSchema-Usage.md) | Complete guide to creating and configuring channel schemas | Schema designers, connector developers |
| [Channel Connector Implementation Guide](ChannelConnector-Usage.md) | How to implement custom channel connectors | Connector developers |
| [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md) | Creating specialized schemas from base configurations | Advanced users, system architects |
| [Endpoint Type Usage Guide](EndpointType-Usage.md) | Working with strongly-typed endpoint configurations | All developers |
| [Advanced Configuration Guide](advanced-configuration.md) | Production-ready patterns and optimizations | DevOps, system architects |
| [Migration Guide](migration-guide.md) | Upgrading from previous versions | Existing users |

## ?? Quick Navigation

### For Getting Started
- Start with [README.md](../README.md) for framework overview
- Follow [Getting Started Guide](getting-started.md) for your first implementation
- Check [Endpoint Type Usage Guide](EndpointType-Usage.md) for endpoint configuration

### For Implementation
- Use [Channel Schema Usage Guide](ChannelSchema-Usage.md) for basic schema creation
- Use [Channel Connector Implementation Guide](ChannelConnector-Usage.md) to build connectors
- Refer to [Advanced Configuration Guide](advanced-configuration.md) for production patterns

### For Migration and Maintenance
- See [Migration Guide](migration-guide.md) for upgrading from previous versions
- Check [Advanced Configuration Guide](advanced-configuration.md) for optimization techniques

## ?? Framework Features

### Channel Schema
- ?? **Fluent API** - Method chaining for easy configuration
- ??? **Type Safety** - Strongly-typed endpoint and content type enums
- ?? **Schema Derivation** - Create specialized schemas from base configurations
- ? **Validation** - Built-in validation for parameters and message properties
- ? **Capability Management** - Declare and validate connector capabilities

### Channel Connectors
- ??? **Base Implementation** - `ChannelConnectorBase` with common functionality
- ?? **State Management** - Automatic connector state handling
- ? **Error Handling** - Standardized error reporting with `ConnectorResult<T>`
- ? **Async Support** - Full async/await with cancellation token support
- ? **Message Validation** - Automatic message validation against schema

### Endpoint Types
- ??? **Strongly Typed** - `EndpointType` enumeration prevents errors
- ?? **Backward Compatible** - Automatic conversion from legacy string types
- ?? **IntelliSense Support** - Better developer experience with auto-completion
- ? **Validation** - Compile-time and runtime validation

## ?? Common Use Cases

### Email Connectors
```csharp
var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.PlainText);
```

### SMS Connectors
```csharp
var smsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AddContentType(MessageContentType.PlainText);
```

### Multi-Channel Connectors
```csharp
var multiSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
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

## ?? Learning Path

### Beginner (New to Framework)
1. Read [README.md](../README.md) overview
2. Follow [Getting Started Guide](getting-started.md)
3. Review [Endpoint Type Usage Guide](EndpointType-Usage.md) fundamentals
4. Practice with simple [Channel Schema Usage Guide](ChannelSchema-Usage.md) examples

### Intermediate (Building Applications)
1. Study [Channel Connector Implementation Guide](ChannelConnector-Usage.md)
2. Learn [Advanced Configuration Guide](advanced-configuration.md) patterns
3. Practice with provider-specific connectors
4. Implement error handling and validation

### Advanced (Production Systems)
1. Master [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md)
2. Implement [Advanced Configuration Guide](advanced-configuration.md) patterns
3. Design multi-tenant configurations
4. Build custom connectors and extensions
5. Implement monitoring and performance optimization

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
- **Mock Connector** - Testing and development patterns

### Production Examples
- **SMTP Email Connector** - Complete implementation with authentication
- **Twilio SMS Connector** - Send/receive with status tracking
- **Webhook Connector** - HTTP-based message delivery
- **Multi-Channel Universal Connector** - Supporting multiple endpoint types
- **Customer-Specific Schemas** - Derivation for different business requirements

### Advanced Examples
- **Connector Pools** - Load balancing and resource management
- **Batching Decorators** - Performance optimization patterns
- **Security Implementations** - Secure parameter handling and auditing
- **Health Checks** - Monitoring and diagnostics
- **Multi-Environment Configuration** - Development to production patterns

## ??? Development Tools

The framework provides excellent development experience:

- **IntelliSense** - Full IDE support with auto-completion
- **Compile-time Validation** - Catch errors before runtime
- **Runtime Validation** - Comprehensive message and configuration validation
- **Debugging Support** - Clear error messages and diagnostic information
- **Testing Tools** - Mocking support and test utilities

## ?? Additional Resources

### Example Projects
- Check test projects for more examples:
  - `test\Deveel.Messaging.Abstractions.XUnit` - Core messaging examples
  - `test\Deveel.Messaging.Connector.Abstractions.XUnit` - Connector implementation examples
  - `test\Deveel.Messaging.Connector.Twilio.XUnit` - Twilio connector tests
  - `test\Deveel.Messaging.Connector.Sendgrid.XUnit` - SendGrid connector tests

### Integration Examples
- Review schema examples in test files
- Examine integration test scenarios
- Study connector implementation patterns

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
2. **Review Examples** - Check test projects and code samples
3. **Study Patterns** - Follow established conventions in the guides

### Community Support
1. **GitHub Discussions** - Ask questions and get community help
2. **GitHub Issues** - Report bugs and request features
3. **Stack Overflow** - Tag questions with `deveel-messaging`

### Professional Support
- **Email Support** - support@deveel.com for enterprise inquiries
- **Consulting Services** - Professional implementation assistance available

## ?? Documentation Quality

We maintain high documentation standards:

- **Accuracy** - All examples are tested and verified
- **Completeness** - Comprehensive coverage of all features
- **Clarity** - Clear explanations with practical examples
- **Currency** - Regular updates to match framework evolution
- **Accessibility** - Multiple learning paths for different experience levels

---

*This documentation is comprehensive and up-to-date as of the latest framework version. All examples have been tested and reflect current best practices.*