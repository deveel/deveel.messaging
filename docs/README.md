# Documentation Index

This directory contains comprehensive documentation for the Deveel Messaging Framework, covering both Channel Schema and Channel Connector usage with the latest strongly-typed endpoint system.

## ?? Documentation Overview

| Document | Description | Audience |
|----------|-------------|----------|
| [README.md](../README.md) | Framework overview and quick start guide | All developers |
| [Channel Schema Usage Guide](ChannelSchema-Usage.md) | Complete guide to creating and configuring channel schemas | Schema designers, connector developers |
| [Channel Connector Implementation Guide](ChannelConnector-Usage.md) | How to implement custom channel connectors | Connector developers |
| [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md) | Creating specialized schemas from base configurations | Advanced users, system architects |
| [Endpoint Type Usage Guide](EndpointType-Usage.md) | Working with strongly-typed endpoint configurations | All developers |

## ?? Quick Navigation

### For Getting Started
- Start with [README.md](../README.md) for framework overview
- Follow [Channel Schema Usage Guide](ChannelSchema-Usage.md) for basic schema creation
- Check [Endpoint Type Usage Guide](EndpointType-Usage.md) for endpoint configuration

### For Implementation
- Use [Channel Connector Implementation Guide](ChannelConnector-Usage.md) to build connectors
- Refer to [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md) for advanced configurations

### For Migration
- See [Endpoint Type Usage Guide](EndpointType-Usage.md#migration-guide) for migrating from string-based endpoints
- Check migration examples in individual guides

## ?? Framework Features

### Channel Schema
- ? **Fluent API** - Method chaining for easy configuration
- ? **Type Safety** - Strongly-typed endpoint and content type enums
- ? **Schema Derivation** - Create specialized schemas from base configurations
- ? **Validation** - Built-in validation for parameters and message properties
- ? **Capability Management** - Declare and validate connector capabilities

### Channel Connectors
- ? **Base Implementation** - `ChannelConnectorBase` with common functionality
- ? **State Management** - Automatic connector state handling
- ? **Error Handling** - Standardized error reporting with `ConnectorResult<T>`
- ? **Async Support** - Full async/await with cancellation token support
- ? **Message Validation** - Automatic message validation against schema

### Endpoint Types
- ? **Strongly Typed** - `EndpointType` enumeration prevents errors
- ? **Backward Compatible** - Automatic conversion from legacy string types
- ? **IntelliSense Support** - Better developer experience with auto-completion
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

### Beginner
1. Read [README.md](../README.md) overview
2. Follow [Channel Schema Usage Guide](ChannelSchema-Usage.md) basics
3. Review [Endpoint Type Usage Guide](EndpointType-Usage.md) fundamentals

### Intermediate
1. Study [Channel Connector Implementation Guide](ChannelConnector-Usage.md)
2. Practice with example implementations
3. Learn error handling patterns

### Advanced
1. Master [Schema Derivation Guide](ChannelSchema-Derivation-Guide.md)
2. Design multi-tenant configurations
3. Implement custom validation logic

## ?? Code Examples

Each guide contains comprehensive, real-world examples:

- **SMTP Email Connector** - Complete implementation with authentication
- **Twilio SMS Connector** - Send/receive with status tracking
- **Webhook Connector** - HTTP-based message delivery
- **Multi-Channel Universal Connector** - Supporting multiple endpoint types
- **Customer-Specific Schemas** - Derivation for different business requirements

## ??? Development Tools

The framework provides excellent development experience:

- **IntelliSense** - Full IDE support with auto-completion
- **Compile-time Validation** - Catch errors before runtime
- **Runtime Validation** - Comprehensive message and configuration validation
- **Debugging Support** - Clear error messages and diagnostic information

## ?? Additional Resources

- Check test projects for more examples:
  - `test\Deveel.Messaging.Abstractions.XUnit`
  - `test\Deveel.Messaging.Connector.Abstractions.XUnit`
- Review schema examples in test files
- Examine integration test scenarios

## ?? Contributing

When contributing to the framework:

1. Follow the patterns shown in the documentation
2. Include comprehensive tests for new features
3. Update documentation for any public API changes
4. Use strongly-typed endpoints in all new code
5. Maintain backward compatibility where possible

---

*This documentation is comprehensive and up-to-date as of the latest framework version. All examples have been tested and reflect current best practices.*