# ChannelSchema Derivation Usage Examples

This document demonstrates how to use the new schema derivation functionality in the `ChannelSchema` class.

## Basic Schema Derivation

You can create a derived schema from any existing schema using the new constructor:

```csharp
// Create a base Twilio SMS schema
var twilioBaseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
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

// Create a derived schema for a specific customer with restrictions
var customerSmsSchema = new ChannelSchema(twilioBaseSchema, "CustomerCorp", "RestrictedSMS", "1.0.0")
    .WithDisplayName("Customer Corp Restricted SMS")
    .RestrictCapabilities(ChannelCapability.SendMessages) // Remove receiving capabilities
    .RemoveParameter("WebhookUrl") // Remove webhook support
    .RestrictContentTypes(MessageContentType.PlainText) // Only plain text messages
    .RemoveEndpoint("webhook") // Remove webhook endpoint
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1234567890"; // Set a default number
        param.Description = "Customer's designated sender number";
    })
    .UpdateMessageProperty("PhoneNumber", prop =>
    {
        prop.Description = "Customer phone number in E.164 format";
    })
    .RemoveMessageProperty("IsUrgent") // Remove urgency property
    .UpdateEndpoint("sms", endpoint =>
    {
        endpoint.CanReceive = false; // Make it send-only
        endpoint.IsRequired = true;
    });
```

## Key Benefits

### 1. Inheritance and Customization
- Start with a well-defined base schema (like a Twilio SMS connector)
- Create specialized versions for different customers or use cases
- Maintain consistency while allowing customization

### 2. Configuration Restriction
- Remove parameters that shouldn't be configurable by certain users
- Restrict content types for security or compliance reasons
- Limit capabilities based on subscription levels or organizational policies

### 3. Independent Modifications
- Changes to derived schemas don't affect the parent schema
- Multiple derived schemas can be created from the same parent
- Each derived schema maintains its own configuration

## Advanced Usage Examples

### Email Connector with Department-Specific Restrictions

```csharp
// Base email schema with full capabilities
var baseEmailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
    .WithDisplayName("Corporate SMTP Connector")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.Templates | 
        ChannelCapability.MediaAttachments |
        ChannelCapability.BulkMessaging)
    .AddParameter(new ChannelParameter("SmtpHost", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("SmtpPort", ParameterType.Integer) { IsRequired = true, DefaultValue = 587 })
    .AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Password", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean) { DefaultValue = true })
    .AddParameter(new ChannelParameter("MaxAttachmentSize", ParameterType.Integer) { DefaultValue = 25 })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AllowsMessageEndpoint("email")
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer) { IsRequired = false })
    .AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean) { IsRequired = false });

// HR Department - restricted to plain text only, no attachments
var hrEmailSchema = new ChannelSchema(baseEmailSchema, "HR", "SecureEmail", "1.0.0")
    .WithDisplayName("HR Secure Email")
    .RemoveCapability(ChannelCapability.MediaAttachments) // No attachments for HR
    .RemoveCapability(ChannelCapability.BulkMessaging) // No bulk emails
    .RestrictContentTypes(MessageContentType.PlainText) // Plain text only
    .RemoveParameter("MaxAttachmentSize") // Not needed without attachments
    .UpdateParameter("EnableSsl", param => 
    {
        param.IsRequired = true; // Force SSL for HR
        param.DefaultValue = true;
    })
    .RemoveMessageProperty("IsHtml"); // No HTML emails

// Marketing Department - full featured but with bulk restrictions
var marketingEmailSchema = new ChannelSchema(baseEmailSchema, "Marketing", "BulkEmail", "1.0.0")
    .WithDisplayName("Marketing Bulk Email")
    .UpdateParameter("MaxAttachmentSize", param =>
    {
        param.DefaultValue = 10; // Smaller attachments for bulk emails
        param.Description = "Maximum attachment size in MB for bulk emails";
    })
    .AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Marketing campaign identifier"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("SegmentId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Target segment identifier"
    });
```

### Multi-Channel Connector with Channel-Specific Derivations

```csharp
// Universal messaging base schema
var baseMessagingSchema = new ChannelSchema("Universal", "MultiChannel", "1.0.0")
    .WithDisplayName("Universal Messaging Platform")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.Templates |
        ChannelCapability.MediaAttachments)
    .AddParameter(new ChannelParameter("ApiKey", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddParameter(new ChannelParameter("Region", ParameterType.String) { IsRequired = true, DefaultValue = "us-east-1" })
    .AddParameter(new ChannelParameter("Timeout", ParameterType.Integer) { DefaultValue = 30 })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Template)
    .AllowsMessageEndpoint("email")
    .AllowsMessageEndpoint("sms")
    .AllowsMessageEndpoint("push")
    .AllowsMessageEndpoint("webhook")
    .AddMessageProperty(new MessagePropertyConfiguration("Recipient", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer) { IsRequired = false });

// SMS-only derivation
var smsOnlySchema = new ChannelSchema(baseMessagingSchema, "SMSOnly", "SMS", "1.0.0")
    .WithDisplayName("SMS Only Messaging")
    .RemoveCapability(ChannelCapability.MediaAttachments) // SMS doesn't support large media
    .RestrictContentTypes(MessageContentType.PlainText) // SMS is text only
    .RestrictAuthenticationTypes(AuthenticationType.Token) // Simplified auth
    .RemoveEndpoint("email")
    .RemoveEndpoint("push")
    .UpdateEndpoint("sms", endpoint => { endpoint.IsRequired = true; })
    .UpdateMessageProperty("Recipient", prop =>
    {
        prop.Description = "Phone number in E.164 format";
    })
    .AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String)
    {
        IsRequired = false,
        Description = "SMS message type (transactional, promotional)"
    });

// Email-only derivation with enhanced features
var emailOnlySchema = new ChannelSchema(baseMessagingSchema, "EmailOnly", "Email", "1.0.0")
    .WithDisplayName("Enhanced Email Messaging")
    .WithCapability(ChannelCapability.BulkMessaging) // Add bulk capability
    .RemoveEndpoint("sms")
    .RemoveEndpoint("push")
    .UpdateEndpoint("email", endpoint => { endpoint.IsRequired = true; })
    .UpdateMessageProperty("Recipient", prop =>
    {
        prop.Description = "Email address";
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
    {
        IsRequired = true,
        Description = "Email subject line"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean)
    {
        IsRequired = false,
        Description = "Whether email content is HTML formatted"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("AttachmentCount", ParameterType.Integer)
    {
        IsRequired = false,
        Description = "Number of attachments in the email"
    });
```

## Schema Hierarchy and Validation

### Parent Schema Reference
```csharp
// Check if a schema is derived
if (customerSmsSchema.ParentSchema != null)
{
    Console.WriteLine($"This schema is derived from: {customerSmsSchema.ParentSchema.ChannelProvider} {customerSmsSchema.ParentSchema.ChannelType}");
}

// Validate that derived schema restrictions are working
var connectionSettings = new ConnectionSettings();
connectionSettings.SetParameter("AccountSid", "AC123...");
connectionSettings.SetParameter("AuthToken", "auth_token");
connectionSettings.SetParameter("FromNumber", "+1234567890");

var validationResults = customerSmsSchema.ValidateConnectionSettings(connectionSettings);
// Should pass validation for the restricted schema
```

### Message Property Validation in Derived Schema
```csharp
var messageProperties = new Dictionary<string, object?>
{
    { "PhoneNumber", "+9876543210" },
    { "MessageType", "transactional" }
    // Note: "IsUrgent" property was removed in the derived schema
};

var validationResults = customerSmsSchema.ValidateMessageProperties(messageProperties);
// Should pass validation (IsUrgent is not required or expected)

// But trying to use IsUrgent would fail
messageProperties.Add("IsUrgent", true);
var invalidResults = customerSmsSchema.ValidateMessageProperties(messageProperties);
// Should contain validation error about unknown property
```

## Method Reference

### Restriction Methods
- `RestrictCapabilities(ChannelCapability)` - Limit capabilities to specified flags
- `RemoveCapability(ChannelCapability)` - Remove a specific capability
- `RestrictContentTypes(params MessageContentType[])` - Replace all content types with specified ones
- `RestrictAuthenticationTypes(params AuthenticationType[])` - Replace all auth types with specified ones

### Removal Methods
- `RemoveParameter(string)` - Remove a parameter by name
- `RemoveMessageProperty(string)` - Remove a message property by name
- `RemoveContentType(MessageContentType)` - Remove a specific content type
- `RemoveAuthenticationType(AuthenticationType)` - Remove a specific authentication type
- `RemoveEndpoint(string)` - Remove an endpoint by type

### Update Methods
- `UpdateParameter(string, Action<ChannelParameter>)` - Modify an existing parameter
- `UpdateMessageProperty(string, Action<MessagePropertyConfiguration>)` - Modify an existing message property
- `UpdateEndpoint(string, Action<ChannelEndpointConfiguration>)` - Modify an existing endpoint

### Properties
- `ParentSchema` - Read-only reference to the parent schema (null for base schemas)

## Best Practices

1. **Start with Comprehensive Base Schemas**: Create base schemas with all possible parameters and capabilities, then restrict as needed in derived schemas.

2. **Use Meaningful Names**: Give derived schemas clear names that indicate their purpose and restrictions.

3. **Document Restrictions**: Use the `Description` property to explain why certain parameters or capabilities were removed or modified.

4. **Validate Derived Schemas**: Always test that your derived schemas validate correctly for their intended use cases.

5. **Maintain Parent References**: The `ParentSchema` property allows you to trace the derivation hierarchy for debugging and documentation purposes.

6. **Independent Evolution**: Remember that changes to parent schemas don't automatically propagate to derived schemas - this is by design to maintain stability.