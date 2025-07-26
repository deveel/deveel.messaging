# ChannelSchema Derivation - Twilio Example

This example demonstrates the principle mentioned in the requirements: "a 'twilio' module should define a general ChannelSchema for the twilio sms channel, but a user can configure a new instance of channel schema that derives from it, restricting the usage of properties, content types, endpoints, etc."

## Base Twilio SMS Schema

```csharp
// Define a general Twilio SMS schema that covers all possible capabilities
var twilioSmsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery |
        ChannelCapability.BulkMessaging)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String)
    {
        IsRequired = true,
        Description = "Twilio Account SID"
    })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Twilio Auth Token"
    })
    .AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Sender phone number"
    })
    .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String)
    {
        IsRequired = false,
        Description = "Webhook URL for receiving messages"
    })
    .AddParameter(new ChannelParameter("StatusCallback", ParameterType.String)
    {
        IsRequired = false,
        Description = "URL for delivery status callbacks"
    })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .AddAuthenticationType(AuthenticationType.Token)
    .AllowsMessageEndpoint("sms", asSender: true, asReceiver: true)
    .AllowsMessageEndpoint("webhook", asSender: false, asReceiver: true)
    .AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Recipient phone number in E.164 format"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String)
    {
        IsRequired = false,
        Description = "Type of SMS message (transactional, promotional, etc.)"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("IsUrgent", ParameterType.Boolean)
    {
        IsRequired = false,
        Description = "Whether message requires urgent delivery"
    });
```

## User-Configured Derived Schemas

### Example 1: Send-Only Customer Schema

```csharp
// A customer that only needs to send simple text messages
var sendOnlySchema = new ChannelSchema(twilioSmsSchema, "CustomerA", "SimpleSMS", "1.0.0")
    .WithDisplayName("Customer A - Send Only SMS")
    .RestrictCapabilities(ChannelCapability.SendMessages) // Remove receiving
    .RemoveParameter("WebhookUrl") // No webhooks needed
    .RemoveParameter("StatusCallback") // No status tracking
    .RestrictContentTypes(MessageContentType.PlainText) // Text only
    .RemoveEndpoint("webhook") // Remove webhook endpoint
    .RemoveMessageProperty("IsUrgent") // Simplify message properties
    .UpdateEndpoint("sms", endpoint => 
    {
        endpoint.CanReceive = false; // Send-only
        endpoint.IsRequired = true;
    });
```

### Example 2: Enterprise Customer with Enhanced Security

```csharp
// An enterprise customer that needs full features but with security restrictions
var enterpriseSchema = new ChannelSchema(twilioSmsSchema, "EnterpriseB", "SecureSMS", "1.0.0")
    .WithDisplayName("Enterprise B - Secure SMS")
    .UpdateParameter("WebhookUrl", param => 
    {
        param.IsRequired = true; // Make webhook mandatory for audit trail
        param.Description = "Required webhook URL for enterprise compliance";
    })
    .AddParameter(new ChannelParameter("EncryptionKey", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Encryption key for sensitive message content"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("ComplianceLevel", ParameterType.String)
    {
        IsRequired = true,
        Description = "Required compliance level (HIPAA, SOX, etc.)"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("BusinessUnit", ParameterType.String)
    {
        IsRequired = true,
        Description = "Business unit for cost allocation"
    });
```

### Example 3: Marketing Department with Bulk Messaging

```csharp
// Marketing department that focuses on bulk messaging campaigns
var marketingSchema = new ChannelSchema(twilioSmsSchema, "Marketing", "BulkSMS", "1.0.0")
    .WithDisplayName("Marketing - Bulk SMS Campaigns")
    .RemoveCapability(ChannelCapability.ReceiveMessages) // Outbound only
    .UpdateParameter("FromNumber", param =>
    {
        param.DefaultValue = "+1800COMPANY"; // Set marketing number
        param.Description = "Marketing department sender number";
    })
    .AddParameter(new ChannelParameter("CampaignBudget", ParameterType.Number)
    {
        IsRequired = true,
        Description = "Budget limit for the campaign in USD"
    })
    .AddParameter(new ChannelParameter("MaxRecipientsPerBatch", ParameterType.Integer)
    {
        IsRequired = false,
        DefaultValue = 1000,
        Description = "Maximum recipients per batch send"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Marketing campaign identifier"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("SegmentId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Target audience segment identifier"
    })
    .RemoveMessageProperty("IsUrgent") // Not relevant for marketing
    .UpdateMessageProperty("MessageType", prop =>
    {
        prop.IsRequired = true; // Make message type mandatory for marketing
        prop.Description = "Required: promotional, transactional, or reminder";
    });
```

### Example 4: Development/Testing Environment

```csharp
// Development environment with reduced functionality and test-safe defaults
var devSchema = new ChannelSchema(twilioSmsSchema, "Development", "TestSMS", "0.1.0")
    .WithDisplayName("Development - Test SMS")
    .RemoveCapability(ChannelCapability.BulkMessaging) // Prevent accidental bulk sends
    .UpdateParameter("FromNumber", param =>
    {
        param.DefaultValue = "+15005550006"; // Twilio test number
        param.Description = "Test sender number (magic number)";
    })
    .AddParameter(new ChannelParameter("TestMode", ParameterType.Boolean)
    {
        IsRequired = false,
        DefaultValue = true,
        Description = "Enable test mode to prevent real message sends"
    })
    .AddParameter(new ChannelParameter("MaxMessagesPerHour", ParameterType.Integer)
    {
        IsRequired = false,
        DefaultValue = 10,
        Description = "Rate limit for development environment"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("TestCaseId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Test case identifier for tracking test messages"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("DeveloperId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Developer identifier for audit and debugging"
    });
```

## Benefits Demonstrated

1. **Module Definition**: The Twilio module defines a comprehensive base schema with all possible features
2. **User Customization**: Users can create derived schemas that fit their specific needs
3. **Restriction Capabilities**: 
   - Remove unnecessary parameters (WebhookUrl for send-only)
   - Restrict content types (PlainText only for simple use cases)
   - Limit capabilities (remove receiving for outbound-only scenarios)
   - Remove endpoints (webhook for simplified configurations)
4. **Enhancement Options**:
   - Add new parameters for specific requirements (EncryptionKey, CampaignBudget)
   - Add new message properties (ComplianceLevel, CampaignId)
   - Make optional parameters required (WebhookUrl for enterprise)
5. **Environment-Specific Configurations**: Development environments can have test-safe defaults and rate limits

## Validation Benefits

Each derived schema validates only against its own configuration:

```csharp
// This would fail validation in the enterprise schema (missing ComplianceLevel)
// but pass in the simple send-only schema
var messageProps = new Dictionary<string, object?>
{
    { "PhoneNumber", "+1234567890" },
    { "MessageType", "transactional" }
};

var sendOnlyValidation = sendOnlySchema.ValidateMessageProperties(messageProps);
// ? Passes - no ComplianceLevel required

var enterpriseValidation = enterpriseSchema.ValidateMessageProperties(messageProps);  
// ? Fails - ComplianceLevel is required for enterprise
```

This approach provides maximum flexibility while maintaining type safety and validation consistency.