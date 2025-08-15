# Connector Documentation Index

This directory contains comprehensive documentation for all available connectors in the Deveel Messaging Framework. Each connector provides detailed specifications, configuration examples, and usage patterns.

## ?? Available Connectors

| Connector | Provider | Type | Documentation | Package |
|-----------|----------|------|---------------|---------|
| **Twilio SMS** | Twilio | SMS | [?? Complete Guide](twilio-sms-connector.md) | `Deveel.Messaging.Connector.Twilio` |
| **Twilio WhatsApp** | Twilio | WhatsApp | [?? Complete Guide](twilio-whatsapp-connector.md) | `Deveel.Messaging.Connector.Twilio` |
| **Firebase FCM** | Firebase | Push | [?? Complete Guide](firebase-push-connector.md) | `Deveel.Messaging.Connector.Firebase` |
| **SendGrid Email** | SendGrid | Email | [?? Complete Guide](sendgrid-email-connector.md) | `Deveel.Messaging.Connector.Sendgrid` |

## ?? Quick Start by Provider

### SMS Messaging
**Install and configure Twilio SMS connector:**
```bash
dotnet add package Deveel.Messaging.Connector.Twilio
```
?? **[Complete Twilio SMS Setup Guide](twilio-sms-connector.md)**

### WhatsApp Business  
**Install and configure WhatsApp Business messaging:**
```bash
dotnet add package Deveel.Messaging.Connector.Twilio
```
?? **[Complete WhatsApp Business Setup Guide](twilio-whatsapp-connector.md)**

### Push Notifications
**Install and configure Firebase Cloud Messaging:**
```bash
dotnet add package Deveel.Messaging.Connector.Firebase
```
?? **[Complete Firebase FCM Setup Guide](firebase-push-connector.md)**

### Email Delivery
**Install and configure SendGrid email:**
```bash
dotnet add package Deveel.Messaging.Connector.Sendgrid
```
?? **[Complete SendGrid Email Setup Guide](sendgrid-email-connector.md)**

## ?? What Each Guide Includes

Each connector documentation provides comprehensive coverage:

### ? **Installation & Setup**
- NuGet package installation instructions
- Required dependencies and prerequisites  
- Configuration parameter setup
- Authentication and credential management

### ?? **Configuration & Schemas**
- Available channel schemas and capabilities
- Required and optional connection parameters
- Schema customization and derivation examples
- Environment-specific configuration patterns

### ?? **Usage Examples**
- Basic message sending examples
- Advanced feature demonstrations
- Template and media message examples
- Batch processing and bulk operations

### ?? **Integration Patterns**
- Webhook setup and configuration
- Bidirectional messaging (send/receive)
- Status tracking and delivery confirmations
- Error handling and retry strategies

### ?? **Testing & Development**
- Unit testing examples and patterns
- Integration testing with real providers
- Mock connector setup for development
- Debugging and troubleshooting guides

### ?? **Production Considerations**
- Performance optimization techniques
- Security best practices and credential management
- Rate limiting and quota management
- Monitoring and health checks

## ?? Connector Capabilities Matrix

| Capability | Twilio SMS | Twilio WhatsApp | Firebase FCM | SendGrid Email |
|------------|------------|------------------|--------------|----------------|
| **Send Messages** | ? | ? | ? | ? |
| **Receive Messages** | ? | ? | ? | ? |
| **Status Tracking** | ? | ? | ? | ? |
| **Batch Operations** | ? | ? | ? | ? |
| **Templates** | ? | ? | ? | ? |
| **Media Attachments** | ? | ? | ? | ? |
| **Health Monitoring** | ? | ? | ? | ? |
| **Webhook Support** | ? | ? | ? | ? |

## ?? Use Case Recommendations

### Transactional Messaging

| Use Case | Recommended Connector | Why |
|----------|----------------------|-----|
| **Order Confirmations** | [SendGrid Email](sendgrid-email-connector.md) | Rich formatting, reliable delivery |
| **SMS Verification** | [Twilio SMS](twilio-sms-connector.md) | High delivery rates, global reach |
| **Push Notifications** | [Firebase FCM](firebase-push-connector.md) | Real-time, cross-platform |
| **WhatsApp Business** | [Twilio WhatsApp](twilio-whatsapp-connector.md) | High engagement, rich media |

### Marketing Campaigns

| Use Case | Recommended Connector | Why |
|----------|----------------------|-----|
| **Email Newsletters** | [SendGrid Email](sendgrid-email-connector.md) | Advanced tracking, templates |
| **SMS Campaigns** | [Twilio SMS](twilio-sms-connector.md) | Bulk messaging, opt-out handling |
| **App Promotions** | [Firebase FCM](firebase-push-connector.md) | Topic messaging, segmentation |
| **WhatsApp Marketing** | [Twilio WhatsApp](twilio-whatsapp-connector.md) | Interactive elements, templates |

### Customer Support

| Use Case | Recommended Connector | Why |
|----------|----------------------|-----|
| **Support Tickets** | [SendGrid Email](sendgrid-email-connector.md) | Threading, attachments |
| **Urgent Alerts** | [Twilio SMS](twilio-sms-connector.md) | Immediate delivery |
| **App Notifications** | [Firebase FCM](firebase-push-connector.md) | In-app alerts |
| **WhatsApp Support** | [Twilio WhatsApp](twilio-whatsapp-connector.md) | Two-way conversation |

## ?? Multi-Connector Patterns

### Installation for Multiple Providers
```bash
# Install multiple connectors for comprehensive messaging
dotnet add package Deveel.Messaging.Connector.Twilio      # SMS + WhatsApp
dotnet add package Deveel.Messaging.Connector.Firebase    # Push notifications  
dotnet add package Deveel.Messaging.Connector.Sendgrid    # Email delivery
```

### Multi-Channel Service Example
```csharp
public class NotificationService
{
    private readonly TwilioSmsConnector _smsConnector;
    private readonly SendGridEmailConnector _emailConnector;
    private readonly FirebasePushConnector _pushConnector;
    
    public async Task SendNotification(User user, string message, NotificationChannel channel)
    {
        switch (channel)
        {
            case NotificationChannel.SMS:
                await _smsConnector.SendMessageAsync(CreateSmsMessage(user, message));
                break;
            case NotificationChannel.Email:
                await _emailConnector.SendMessageAsync(CreateEmailMessage(user, message));
                break;
            case NotificationChannel.Push:
                await _pushConnector.SendMessageAsync(CreatePushMessage(user, message));
                break;
        }
    }
}
```

## ?? Testing Strategies

### Unit Testing
Each connector guide includes unit testing examples:
```csharp
[Test]
public async Task SendMessage_ValidInput_ReturnsSuccess()
{
    // Arrange
    var connector = new MockConnector(schema);
    var message = CreateTestMessage();
    
    // Act  
    var result = await connector.SendMessageAsync(message);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
}
```

### Integration Testing
```csharp
[Test]
[Category("Integration")]
public async Task SendMessage_RealProvider_DeliversMessage()
{
    // Only run with real credentials
    Skip.IfNot(HasTestCredentials());
    
    var connector = CreateRealConnector();
    var result = await connector.SendMessageAsync(testMessage);
    
    Assert.IsTrue(result.IsSuccess);
}
```

## ?? Support and Resources

### Documentation Links
- **[Framework Overview](../README.md)** - Main framework documentation
- **[Getting Started](../getting-started.md)** - Quick start guide
- **[Channel Schemas](../ChannelSchema-Usage.md)** - Schema configuration
- **[Connector Implementation](../ChannelConnector-Usage.md)** - Custom connector guide

### Community Resources
- **[GitHub Repository](https://github.com/deveel/deveel.messaging)** - Source code and issues
- **[GitHub Discussions](https://github.com/deveel/deveel.messaging/discussions)** - Community support
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute

### Provider Resources

| Provider | Documentation | Console | Support |
|----------|---------------|---------|---------|
| **Twilio** | [Docs](https://www.twilio.com/docs) | [Console](https://console.twilio.com) | [Support](https://support.twilio.com) |
| **Firebase** | [Docs](https://firebase.google.com/docs) | [Console](https://console.firebase.google.com) | [Support](https://firebase.google.com/support) |
| **SendGrid** | [Docs](https://docs.sendgrid.com) | [Console](https://app.sendgrid.com) | [Support](https://support.sendgrid.com) |

## ?? Contributing New Connectors

Planning to add a new connector? Each guide follows our standard template:

1. **? Installation** - Package installation and setup
2. **?? Configuration** - Schema and parameter setup  
3. **?? Usage Examples** - Basic to advanced usage patterns
4. **?? Integration** - Webhooks and bidirectional messaging
5. **?? Testing** - Unit and integration testing guidance
6. **?? Production** - Performance and security considerations

See our [Connector Implementation Guide](../ChannelConnector-Usage.md) for creating new connectors.

---

*Choose the connector that best fits your messaging needs and follow the detailed documentation for implementation guidance.*