# Connector Documentation Index

This directory contains comprehensive documentation for all available connectors in the Deveel Messaging Framework. Each connector provides detailed specifications, configuration examples, and usage patterns.

## ?? Available Connectors

| Connector | Provider | Type | Documentation | Package |
|-----------|----------|------|---------------|---------|
| **Twilio SMS** | Twilio | SMS | [Documentation](twilio-sms-connector.md) | `Deveel.Messaging.Connector.Twilio` |
| **Twilio WhatsApp** | Twilio | WhatsApp | [Documentation](twilio-whatsapp-connector.md) | `Deveel.Messaging.Connector.Twilio` |
| **Firebase FCM** | Firebase | Push | [Documentation](firebase-push-connector.md) | `Deveel.Messaging.Connector.Firebase` |
| **SendGrid Email** | SendGrid | Email | [Documentation](sendgrid-email-connector.md) | `Deveel.Messaging.Connector.Sendgrid` |

## ?? Quick Start Guide

### 1. Choose Your Connector

Select the appropriate connector based on your messaging needs:

- **SMS Messaging**: Use [Twilio SMS Connector](twilio-sms-connector.md)
- **WhatsApp Business**: Use [Twilio WhatsApp Connector](twilio-whatsapp-connector.md)
- **Push Notifications**: Use [Firebase FCM Connector](firebase-push-connector.md)
- **Email Messaging**: Use [SendGrid Email Connector](sendgrid-email-connector.md)

### 2. Install the Package

```bash
# For SMS and WhatsApp
dotnet add package Deveel.Messaging.Connector.Twilio

# For Push Notifications
dotnet add package Deveel.Messaging.Connector.Firebase

# For Email
dotnet add package Deveel.Messaging.Connector.Sendgrid
```

### 3. Follow the Documentation

Each connector has comprehensive documentation including:
- ? **Schema Specifications** - Available schemas and capabilities
- ?? **Connection Parameters** - Required and optional configuration
- ?? **Message Properties** - Supported message properties and validation
- ?? **Usage Examples** - Code samples for common scenarios
- ?? **Webhook Integration** - Event handling and status updates
- ? **Error Handling** - Common errors and solutions
- ? **Best Practices** - Production-ready patterns

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

## ?? Configuration Patterns

### Basic Configuration

Each connector follows a similar configuration pattern:

```csharp
// 1. Choose schema
var schema = ProviderChannelSchemas.SchemaName;

// 2. Configure connection
var connectionSettings = new ConnectionSettings()
    .AddParameter("RequiredParam", "value")
    .AddParameter("OptionalParam", "value");

// 3. Create connector
var connector = new ProviderConnector(schema, connectionSettings);

// 4. Initialize
await connector.InitializeAsync(cancellationToken);
```

### Advanced Configuration Examples

#### Multi-Connector Setup

```csharp
public class MessagingService
{
    private readonly TwilioSmsConnector _smsConnector;
    private readonly SendGridEmailConnector _emailConnector;
    private readonly FirebasePushConnector _pushConnector;
    
    public MessagingService()
    {
        // Configure all connectors
        _smsConnector = new TwilioSmsConnector(TwilioChannelSchemas.TwilioSms, smsSettings);
        _emailConnector = new SendGridEmailConnector(SendGridChannelSchemas.SendGridEmail, emailSettings);
        _pushConnector = new FirebasePushConnector(FirebaseChannelSchemas.FirebasePush, pushSettings);
    }
    
    public async Task SendMultiChannelNotification(string userId, string message)
    {
        var user = await GetUser(userId);
        
        // Send via preferred channel
        switch (user.PreferredChannel)
        {
            case "sms":
                await SendSms(user.PhoneNumber, message);
                break;
            case "email":
                await SendEmail(user.Email, message);
                break;
            case "push":
                await SendPush(user.DeviceToken, message);
                break;
        }
    }
}
```

#### Environment-Specific Configuration

```csharp
public static class ConnectorFactory
{
    public static TConnector CreateConnector<TConnector>(IConfiguration config, string environment)
        where TConnector : IChannelConnector
    {
        var settings = environment switch
        {
            "Development" => CreateDevelopmentSettings(config),
            "Staging" => CreateStagingSettings(config),
            "Production" => CreateProductionSettings(config),
            _ => throw new ArgumentException($"Unknown environment: {environment}")
        };
        
        return (TConnector)Activator.CreateInstance(typeof(TConnector), settings);
    }
}
```

## ?? Performance Considerations

### Batch Processing

| Connector | Batch Size | Rate Limit | Best Practice |
|-----------|------------|------------|---------------|
| **Twilio SMS** | 100 messages | 1/second | Use messaging service for bulk |
| **Twilio WhatsApp** | Single only | 1/second | Sequential sending |
| **Firebase FCM** | 500 tokens | 600k/minute | Use multicast for efficiency |
| **SendGrid Email** | 1000 emails | No limit | Batch by 100-500 for tracking |

### Connection Pooling

```csharp
// ? Good - Reuse connectors
public class SingletonConnectorService
{
    private static readonly TwilioSmsConnector _smsConnector = 
        new TwilioSmsConnector(schema, settings);
        
    public static async Task<IChannelConnector> GetSmsConnectorAsync()
    {
        if (_smsConnector.State != ConnectorState.Ready)
        {
            await _smsConnector.InitializeAsync(CancellationToken.None);
        }
        return _smsConnector;
    }
}
```

## ?? Security Best Practices

### Credential Management

```csharp
// ? Good - Use secure configuration
var settings = new ConnectionSettings()
    .AddParameter("ApiKey", Environment.GetEnvironmentVariable("SENDGRID_API_KEY"))
    .AddParameter("AccountSid", config["Twilio:AccountSid"])
    .AddParameter("AuthToken", config["Twilio:AuthToken"]);

// ? Avoid - Hardcoded credentials
var badSettings = new ConnectionSettings()
    .AddParameter("ApiKey", "SG.hardcoded-key-here");
```

### Webhook Security

```csharp
// ? Good - Validate webhook signatures
[HttpPost("webhook/twilio")]
public async Task<IActionResult> HandleWebhook([FromForm] Dictionary<string, string> data)
{
    if (!ValidateSignature(Request, data))
    {
        return Unauthorized();
    }
    
    // Process webhook
    return Ok();
}
```

## ?? Testing Strategies

### Unit Testing

```csharp
[Test]
public async Task SendMessage_ValidInput_ReturnsSuccess()
{
    // Arrange
    var mockConnector = new Mock<IChannelConnector>();
    var message = CreateTestMessage();
    
    // Act
    var result = await mockConnector.SendMessageAsync(message, CancellationToken.None);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
}
```

### Integration Testing

```csharp
[Test]
[Category("Integration")]
public async Task SendMessage_RealConnector_DeliversMessage()
{
    // Only run with real credentials
    Skip.IfNot(HasTestCredentials(), "Test credentials not available");
    
    var connector = CreateRealConnector();
    var result = await connector.SendMessageAsync(testMessage, CancellationToken.None);
    
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

- **[GitHub Repository](https://github.com/deveel/deveel.message.model)** - Source code and issues
- **[GitHub Discussions](https://github.com/deveel/deveel.message.model/discussions)** - Community support
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute

### Provider Resources

| Provider | Documentation | Console | Support |
|----------|---------------|---------|---------|
| **Twilio** | [Docs](https://www.twilio.com/docs) | [Console](https://console.twilio.com) | [Support](https://support.twilio.com) |
| **Firebase** | [Docs](https://firebase.google.com/docs) | [Console](https://console.firebase.google.com) | [Support](https://firebase.google.com/support) |
| **SendGrid** | [Docs](https://docs.sendgrid.com) | [Console](https://app.sendgrid.com) | [Support](https://support.sendgrid.com) |

---

*Choose the connector that best fits your messaging needs and follow the detailed documentation for implementation guidance.*