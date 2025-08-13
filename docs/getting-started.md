# Getting Started with Deveel Messaging Framework

This guide walks you through setting up and sending your first message using the Deveel Messaging Framework.

## ?? Prerequisites

- **.NET 8.0** or **.NET 9.0** SDK
- **Visual Studio 2022** (17.4+) or **JetBrains Rider** or **VS Code** with C# extension
- **An IDE** that supports modern C# features

## ?? Quick Start

### Step 1: Install the Framework

Choose the connector package(s) you need:

```bash
# For SMS and WhatsApp messaging
dotnet add package Deveel.Messaging.Connector.Twilio

# For email messaging  
dotnet add package Deveel.Messaging.Connector.Sendgrid

# For push notifications
dotnet add package Deveel.Messaging.Connector.Firebase

# For core abstractions (if building custom connectors)
dotnet add package Deveel.Messaging.Connector.Abstractions
```

### Step 2: Choose Your Messaging Provider

The framework supports multiple messaging providers. Select based on your needs:

| Use Case | Recommended Connector | Package |
|----------|----------------------|---------|
| **SMS Messages** | [Twilio SMS](connectors/twilio-sms-connector.md) | `Deveel.Messaging.Connector.Twilio` |
| **WhatsApp Business** | [Twilio WhatsApp](connectors/twilio-whatsapp-connector.md) | `Deveel.Messaging.Connector.Twilio` |
| **Email** | [SendGrid Email](connectors/sendgrid-email-connector.md) | `Deveel.Messaging.Connector.Sendgrid` |
| **Push Notifications** | [Firebase FCM](connectors/firebase-push-connector.md) | `Deveel.Messaging.Connector.Firebase` |

### Step 3: Your First Message

Let's send a simple SMS using Twilio:

```csharp
using Deveel.Messaging;

// 1. Configure the connector schema
var schema = TwilioChannelSchemas.TwilioSms;

// 2. Set up connection parameters
var connectionSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "your_twilio_account_sid")
    .AddParameter("AuthToken", "your_twilio_auth_token");

// 3. Create the connector
var connector = new TwilioSmsConnector(schema, connectionSettings);

// 4. Initialize the connector
await connector.InitializeAsync(CancellationToken.None);

// 5. Build your message
var message = new MessageBuilder()
    .WithId("welcome-sms-001")
    .WithPhoneSender("+1234567890")      // Your Twilio number
    .WithPhoneReceiver("+0987654321")    // Recipient number
    .WithTextContent("Welcome! Your account has been created successfully.")
    .Message;

// 6. Send the message
var result = await connector.SendMessageAsync(message, CancellationToken.None);

// 7. Check the result
if (result.IsSuccess)
{
    Console.WriteLine($"? Message sent successfully! ID: {result.Value?.MessageId}");
}
else
{
    Console.WriteLine($"? Failed to send message: {result.ErrorMessage}");
}
```

## ?? Email Example (SendGrid)

```csharp
using Deveel.Messaging;

// Configure SendGrid connector
var emailSchema = SendGridChannelSchemas.SendGridEmail;
var emailSettings = new ConnectionSettings()
    .AddParameter("ApiKey", "your_sendgrid_api_key");

var emailConnector = new SendGridEmailConnector(emailSchema, emailSettings);
await emailConnector.InitializeAsync(CancellationToken.None);

// Send HTML email
var email = new MessageBuilder()
    .WithId("welcome-email-001")
    .WithEmailSender("noreply@yourcompany.com")
    .WithEmailReceiver("customer@example.com")
    .WithHtmlContent(@"
        <h1>Welcome to Our Service!</h1>
        <p>Thank you for joining us. Your account is now active.</p>
        <a href='https://yourapp.com/dashboard'>Go to Dashboard</a>
    ")
    .WithProperty("Subject", "Welcome to Our Service!")
    .Message;

var emailResult = await emailConnector.SendMessageAsync(email, CancellationToken.None);
```

## ?? Push Notification Example (Firebase)

```csharp
using Deveel.Messaging;

// Configure Firebase connector
var pushSchema = FirebaseChannelSchemas.FirebasePush;
var pushSettings = new ConnectionSettings()
    .AddParameter("ProjectId", "your-firebase-project")
    .AddParameter("ServiceAccountKey", serviceAccountJson);

var pushConnector = new FirebasePushConnector(pushSchema, pushSettings);
await pushConnector.InitializeAsync(CancellationToken.None);

// Send push notification
var notification = new MessageBuilder()
    .WithId("push-notification-001")
    .WithDeviceReceiver("device-token-here")
    .WithTextContent("You have a new message!")
    .WithProperty("Title", "New Message")
    .WithProperty("ImageUrl", "https://yourapp.com/notification-icon.png")
    .Message;

var pushResult = await pushConnector.SendMessageAsync(notification, CancellationToken.None);
```

## ?? WhatsApp Business Example

```csharp
using Deveel.Messaging;

// Configure WhatsApp connector
var whatsAppSchema = TwilioChannelSchemas.TwilioWhatsApp;
var whatsAppSettings = new ConnectionSettings()
    .AddParameter("AccountSid", "your_twilio_account_sid")
    .AddParameter("AuthToken", "your_twilio_auth_token");

var whatsAppConnector = new TwilioWhatsAppConnector(whatsAppSchema, whatsAppSettings);
await whatsAppConnector.InitializeAsync(CancellationToken.None);

// Send WhatsApp message
var whatsAppMessage = new MessageBuilder()
    .WithId("whatsapp-001")
    .WithPhoneSender("whatsapp:+1234567890")
    .WithPhoneReceiver("whatsapp:+0987654321")
    .WithTextContent("Hello! Thanks for contacting us. How can we help you today?")
    .Message;

var whatsAppResult = await whatsAppConnector.SendMessageAsync(whatsAppMessage, CancellationToken.None);
```

## ?? Receiving Messages (Webhooks)

Many connectors support receiving messages through webhooks. Here's a basic webhook handler:

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly TwilioSmsConnector _smsConnector;
    private readonly TwilioWhatsAppConnector _whatsAppConnector;

    public WebhookController(
        TwilioSmsConnector smsConnector, 
        TwilioWhatsAppConnector whatsAppConnector)
    {
        _smsConnector = smsConnector;
        _whatsAppConnector = whatsAppConnector;
    }

    [HttpPost("twilio/sms")]
    public async Task<IActionResult> ReceiveSms([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _smsConnector.ReceiveMessagesAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            foreach (var message in result.Value.Messages)
            {
                // Process incoming SMS
                await ProcessIncomingSms(message);
            }
        }
        
        return Ok();
    }

    [HttpPost("twilio/whatsapp")]
    public async Task<IActionResult> ReceiveWhatsApp([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _whatsAppConnector.ReceiveMessagesAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            foreach (var message in result.Value.Messages)
            {
                // Process incoming WhatsApp message
                await ProcessWhatsAppMessage(message);
            }
        }
        
        return Ok();
    }

    [HttpPost("twilio/status")]
    public async Task<IActionResult> ReceiveStatus([FromForm] Dictionary<string, string> formData)
    {
        var messageSource = MessageSource.FromFormData(formData);
        var result = await _whatsAppConnector.ReceiveMessageStatusAsync(messageSource, CancellationToken.None);
        
        if (result.IsSuccess)
        {
            // Process status update
            await ProcessStatusUpdate(result.Value);
        }
        
        return Ok();
    }
}
```

## Next Steps

1. **[Connector Documentation](connectors/README.md)** - Detailed guides for each connector
2. **[Channel Schema Guide](ChannelSchema-Usage.md)** - Learn advanced schema configuration
3. **[Endpoint Type Guide](EndpointType-Usage.md)** - Master type-safe endpoints
4. **[Connector Implementation](ChannelConnector-Usage.md)** - Build custom connectors
5. **[Examples](../test/)** - Study the test projects for more examples

## Troubleshooting

### Common Issues

**Issue**: "Endpoint type not supported"
```csharp
// ? Wrong endpoint type for schema
var schema = new ChannelSchema("SMS", "Provider", "1.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress); // SMS should use PhoneNumber

var message = new MessageBuilder()
    .WithPhoneReceiver("+1234567890") // This will fail validation
    .Message;
```

**Solution**: Match endpoint types with schema configuration
```csharp
// ? Correct
var schema = new ChannelSchema("SMS", "Provider", "1.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

var message = new MessageBuilder()
    .WithPhoneReceiver("+1234567890") // This works
    .Message;
```

**Issue**: "Required parameter missing"
```csharp
// Make sure all required parameters are provided
var schema = new ChannelSchema("Provider", "Type", "1.0")
    .AddParameter(new ChannelParameter("ApiKey", DataType.String)
    {
        IsRequired = true // This must be provided during initialization
    });
```

**Issue**: Firebase authentication errors
```csharp
// Ensure your service account JSON is valid
var configuration = new ConnectionSettings()
    .AddParameter("ProjectId", "your-project-id")
    .AddParameter("ServiceAccountKey", File.ReadAllText("path/to/service-account.json"));
```

## Support

If you encounter issues:
1. Check the [connector documentation](connectors/README.md)
2. Review [examples](../test/)
3. [Open an issue](https://github.com/deveel/deveel.message.model/issues)
4. Join our [discussions](https://github.com/deveel/deveel.message.model/discussions)