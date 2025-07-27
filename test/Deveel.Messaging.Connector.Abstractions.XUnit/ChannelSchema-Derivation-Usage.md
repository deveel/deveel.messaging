# ChannelSchema Derivation Usage Examples

This document demonstrates how to use the new schema derivation functionality in the `ChannelSchema` class with the updated endpoint type system.

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
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: true)
    .AllowsMessageEndpoint(EndpointType.Url, asSender: false, asReceiver: true)
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
var customerSmsSchema = new ChannelSchema(twilioBaseSchema, "Customer Corp SMS Notifications")
    .RemoveCapability(ChannelCapability.ReceiveMessages) // Remove receiving capabilities
    .RemoveParameter("WebhookUrl") // Remove webhook support
    .RestrictContentTypes(MessageContentType.PlainText) // Only plain text messages
    .RemoveEndpoint(EndpointType.Url) // Remove webhook endpoint
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1234567890"; // Set a default number
        param.Description = "Customer-specific sender phone number";
    })
    .UpdateMessageProperty("PhoneNumber", prop =>
    {
        prop.Description = "Customer phone number in E.164 format";
    })
    .RemoveMessageProperty("IsUrgent"); // Remove urgency levels
```

## Logical Identity Preservation

Derived schemas maintain the same logical identity as their parent:

```csharp
// Core properties are identical
Console.WriteLine(twilioBaseSchema.ChannelProvider);   // "Twilio"
Console.WriteLine(customerSmsSchema.ChannelProvider);  // "Twilio"

Console.WriteLine(twilioBaseSchema.ChannelType);       // "SMS"
Console.WriteLine(customerSmsSchema.ChannelType);      // "SMS"

Console.WriteLine(twilioBaseSchema.Version);           // "2.1.0"
Console.WriteLine(customerSmsSchema.Version);          // "2.1.0"

// Logical identity is the same
Console.WriteLine(twilioBaseSchema.GetLogicalIdentity());   // "Twilio/SMS/2.1.0"
Console.WriteLine(customerSmsSchema.GetLogicalIdentity()); // "Twilio/SMS/2.1.0"

// Schemas are compatible
Console.WriteLine(twilioBaseSchema.IsCompatibleWith(customerSmsSchema)); // True
```

## Endpoint Management in Derived Schemas

### Removing Endpoints

```csharp
var multiChannelBase = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AllowsMessageEndpoint(EndpointType.ApplicationId);

// Create email-only schema
var emailOnlySchema = new ChannelSchema(multiChannelBase, "Email Only Service")
    .RemoveEndpoint(EndpointType.PhoneNumber)
    .RemoveEndpoint(EndpointType.Url)
    .RemoveEndpoint(EndpointType.ApplicationId);

// Result: Only EmailAddress endpoint remains
Console.WriteLine($"Endpoints: {emailOnlySchema.Endpoints.Count}"); // 1
```

### Updating Endpoint Configurations

```csharp
var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.PhoneNumber, asSender: true, asReceiver: true);

// Make phone number send-only and required
var sendOnlySchema = new ChannelSchema(baseSchema, "Send Only SMS")
    .UpdateEndpoint(EndpointType.PhoneNumber, endpoint => 
    {
        endpoint.CanReceive = false;  // Remove receive capability
        endpoint.IsRequired = true;   // Make required
        endpoint.Description = "Phone number for outbound SMS only";
    });
```

## Content Type Restrictions

```csharp
var richContentBase = new ChannelSchema("Provider", "RichMessaging", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Json);

// Restrict to text-only content
var textOnlySchema = new ChannelSchema(richContentBase, "Text Only Messaging")
    .RestrictContentTypes(MessageContentType.PlainText, MessageContentType.Html);

// Alternative: Remove specific content types
var noMediaSchema = new ChannelSchema(richContentBase, "No Media Messaging")
    .RemoveContentType(MessageContentType.Media)
    .RemoveContentType(MessageContentType.Json);
```

## Real-World Customer Scenarios

### Customer A: Outbound Marketing SMS

```csharp
var customerASchema = new ChannelSchema(twilioBaseSchema, "Customer A - Marketing SMS")
    .RemoveCapability(ChannelCapability.ReceiveMessages) // Outbound only
    .RemoveParameter("WebhookUrl") // No webhooks needed
    .RemoveEndpoint(EndpointType.Url) // Phone numbers only
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1555MARKET"; // Marketing shortcode
        param.Description = "Marketing SMS shortcode";
    })
    .AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Marketing campaign identifier"
    })
    .RemoveMessageProperty("IsUrgent"); // Not applicable for marketing
```

### Customer B: Two-Way Support SMS

```csharp
var customerBSchema = new ChannelSchema(twilioBaseSchema, "Customer B - Support SMS")
    .RemoveCapability(ChannelCapability.BulkMessaging) // One-on-one support only
    .RestrictContentTypes(MessageContentType.PlainText) // Text only for support
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1555SUPPORT"; // Support number
        param.Description = "Customer support SMS number";
    })
    .AddMessageProperty(new MessagePropertyConfiguration("TicketId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Support ticket identifier"
    })
    .UpdateMessageProperty("IsUrgent", prop =>
    {
        prop.IsRequired = true; // All support messages need urgency level
        prop.Description = "Support message urgency (true for urgent tickets)";
    });
```

### Customer C: Webhook-Only Integration

```csharp
var customerCSchema = new ChannelSchema(twilioBaseSchema, "Customer C - Webhook Integration")
    .RestrictCapabilities(ChannelCapability.ReceiveMessages) // Receive-only
    .RemoveEndpoint(EndpointType.PhoneNumber) // Webhooks only
    .RestrictContentTypes(MessageContentType.PlainText) // Simple text notifications
    .UpdateParameter("WebhookUrl", param => 
    {
        param.IsRequired = true;
        param.DefaultValue = "https://customerc.com/sms-webhook";
        param.Description = "Customer C webhook endpoint for SMS notifications";
    })
    .RemoveParameter("FromNumber") // Not needed for receive-only
    .AddMessageProperty(new MessagePropertyConfiguration("WebhookSecret", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Webhook verification secret"
    });
```

## Multi-Channel Derivation Example

```csharp
// Universal base schema supporting multiple channels
var universalBase = new ChannelSchema("Universal", "Messaging", "2.0.0")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.BulkMessaging |
        ChannelCapability.Templates |
        ChannelCapability.MediaAttachments)
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Media)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AllowsMessageEndpoint(EndpointType.ApplicationId);

// Email-only derivation
var emailSchema = new ChannelSchema(universalBase, "Email Service")
    .RemoveEndpoint(EndpointType.PhoneNumber)
    .RemoveEndpoint(EndpointType.Url)
    .RemoveEndpoint(EndpointType.ApplicationId)
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
    {
        IsRequired = true,
        Description = "Email subject line"
    });

// SMS-only derivation
var smsSchema = new ChannelSchema(universalBase, "SMS Service")
    .RemoveEndpoint(EndpointType.EmailAddress)
    .RemoveEndpoint(EndpointType.Url)
    .RemoveEndpoint(EndpointType.ApplicationId)
    .RemoveCapability(ChannelCapability.MediaAttachments) // SMS limitations
    .RestrictContentTypes(MessageContentType.PlainText)
    .AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Recipient phone number"
    });

// Push notification derivation
var pushSchema = new ChannelSchema(universalBase, "Push Notification Service")
    .RemoveEndpoint(EndpointType.EmailAddress)
    .RemoveEndpoint(EndpointType.PhoneNumber)
    .RemoveEndpoint(EndpointType.Url)
    .RestrictContentTypes(MessageContentType.PlainText, MessageContentType.Json)
    .AddMessageProperty(new MessagePropertyConfiguration("DeviceToken", ParameterType.String)
    {
        IsRequired = true,
        IsSensitive = true,
        Description = "Device push notification token"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Badge", ParameterType.Integer)
    {
        IsRequired = false,
        Description = "App badge count"
    });
```

## Department-Specific Email Derivations

```csharp
// Corporate email base
var corporateEmail = new ChannelSchema("SMTP", "Email", "2.0.0")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.Templates | 
        ChannelCapability.MediaAttachments)
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AllowsMessageEndpoint(EndpointType.EmailAddress);

// HR Department - Secure, text-only
var hrEmail = new ChannelSchema(corporateEmail, "HR Secure Email")
    .RemoveCapability(ChannelCapability.MediaAttachments) // No attachments for security
    .RestrictContentTypes(MessageContentType.PlainText) // Text only
    .AddMessageProperty(new MessagePropertyConfiguration("EmployeeId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Employee ID for HR tracking"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Confidential", ParameterType.Boolean)
    {
        IsRequired = true,
        Description = "Mark as confidential HR communication"
    });

// Marketing Department - Rich content, bulk capable
var marketingEmail = new ChannelSchema(corporateEmail, "Marketing Email")
    .WithCapability(ChannelCapability.BulkMessaging) // Add bulk for campaigns
    .AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Marketing campaign identifier"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("Segment", ParameterType.String)
    {
        IsRequired = false,
        Description = "Customer segment for targeting"
    });
```

## Validation and Compatibility

```csharp
// All derived schemas maintain compatibility with base
var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0");
var derivedSchema = new ChannelSchema(baseSchema, "Derived");

// Compatibility check
Console.WriteLine(baseSchema.IsCompatibleWith(derivedSchema)); // True
Console.WriteLine(derivedSchema.IsCompatibleWith(baseSchema)); // True

// Validate that derived schema is a proper restriction
var validationResults = derivedSchema.ValidateAsRestrictionOf(baseSchema);
if (validationResults.Any())
{
    foreach (var error in validationResults)
    {
        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    }
}
else
{
    Console.WriteLine("Derived schema is a valid restriction of base schema");
}
```

## Multi-Generation Hierarchies

```csharp
// Three-level hierarchy example
var grandparent = new ChannelSchema("Platform", "Messaging", "1.0.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | ChannelCapability.BulkMessaging)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber);

var parent = new ChannelSchema(grandparent, "Department Level")
    .RemoveCapability(ChannelCapability.BulkMessaging) // Restrict bulk messaging
    .RemoveEndpoint(EndpointType.PhoneNumber); // Email only

var child = new ChannelSchema(parent, "User Level")
    .RestrictCapabilities(ChannelCapability.SendMessages); // Send-only

// All maintain same logical identity
Console.WriteLine(grandparent.GetLogicalIdentity()); // "Platform/Messaging/1.0.0"
Console.WriteLine(parent.GetLogicalIdentity());      // "Platform/Messaging/1.0.0"
Console.WriteLine(child.GetLogicalIdentity());       // "Platform/Messaging/1.0.0"

// All are compatible
Console.WriteLine(grandparent.IsCompatibleWith(child)); // True
```

## Best Practices for Schema Derivation

### 1. Start with Comprehensive Base Schemas

```csharp
// ? Good - Comprehensive base with all possible features
var comprehensiveBase = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(/* All capabilities */)
    .AddContentType(/* All content types */)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AllowsMessageEndpoint(EndpointType.ApplicationId);

// Then restrict as needed
var restrictedSchema = new ChannelSchema(comprehensiveBase, "Restricted")
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .RemoveEndpoint(EndpointType.Url);
```

### 2. Use Descriptive Display Names

```csharp
// ? Good - Clear purpose and restrictions
var schema = new ChannelSchema(baseSchema, "Customer Corp - Outbound SMS Only");

// ? Avoid - Generic names
var schema = new ChannelSchema(baseSchema, "Derived Schema");
```

### 3. Document Changes with Descriptions

```csharp
// ? Good - Document why parameters were updated
.UpdateParameter("Timeout", param => 
{
    param.DefaultValue = 60; // Increased for customer's slow network
    param.Description = "Connection timeout (increased for customer requirements)";
})
.UpdateEndpoint(EndpointType.PhoneNumber, endpoint =>
{
    endpoint.CanReceive = false; // Customer requirement: send-only
    endpoint.Description = "Phone number for outbound notifications only";
})
```

### 4. Validate Derived Schemas

```csharp
// ? Good - Always validate derived schemas
var derivedSchema = CreateCustomerSchema(baseSchema);
var validation = derivedSchema.ValidateAsRestrictionOf(baseSchema);

if (validation.Any())
{
    throw new InvalidOperationException($"Invalid schema derivation: {validation.First().ErrorMessage}");
}