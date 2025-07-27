# Schema Derivation Guide

Schema derivation allows you to create specialized schemas from base configurations, enabling scenarios like customer-specific restrictions, department-specific configurations, and multi-tenant messaging systems. This guide covers the complete schema derivation functionality.

## Table of Contents

1. [Overview](#overview)
2. [Basic Schema Derivation](#basic-schema-derivation)
3. [Logical Identity](#logical-identity)
4. [Capability Management](#capability-management)
5. [Parameter Management](#parameter-management)
6. [Endpoint Management](#endpoint-management)
7. [Content Type Management](#content-type-management)
8. [Message Property Management](#message-property-management)
9. [Multi-Generation Hierarchies](#multi-generation-hierarchies)
10. [Validation and Compatibility](#validation-and-compatibility)
11. [Real-World Examples](#real-world-examples)
12. [Best Practices](#best-practices)

## Overview

Schema derivation allows you to create new schemas based on existing ones while maintaining compatibility and logical identity. Derived schemas can:

- **Restrict capabilities** - Remove capabilities from the base schema
- **Remove parameters** - Remove optional parameters
- **Update parameters** - Modify parameter properties like defaults and requirements
- **Restrict content types** - Limit supported content types
- **Manage endpoints** - Remove or modify endpoint configurations
- **Update message properties** - Modify message property configurations

**Key Principle**: Derived schemas maintain the same logical identity (provider/type/version) as their parent, ensuring compatibility.

## Basic Schema Derivation

### Creating a Derived Schema

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
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String)
    {
        IsRequired = true,
        Description = "Recipient phone number in E.164 format"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("IsUrgent", ParameterType.Boolean)
    {
        IsRequired = false,
        Description = "Whether message requires urgent delivery"
    })
    .AddAuthenticationType(AuthenticationType.Token);

// Create a derived schema for customer-specific restrictions
var customerSmsSchema = new ChannelSchema(twilioBaseSchema, "Customer SMS Notifications")
    .RemoveCapability(ChannelCapability.ReceiveMessages)  // Outbound only
    .RemoveCapability(ChannelCapability.BulkMessaging)    // Single messages only
    .RemoveParameter("WebhookUrl")                        // No webhook needed
    .RemoveContentType(MessageContentType.Media)         // Text only
    .RemoveEndpoint(EndpointType.Url)                     // Phone numbers only
    .RemoveMessageProperty("IsUrgent")                    // No urgency levels
    .UpdateMessageProperty("PhoneNumber", prop => 
    {
        prop.Description = "Customer phone number in E.164 format";
    })
    .UpdateEndpoint(EndpointType.PhoneNumber, endpoint => 
    {
        endpoint.CanReceive = false; // Outbound only
        endpoint.IsRequired = true;  // Must specify phone number
    });
```

### Constructor Variations

```csharp
// Derive with custom display name
var derivedSchema1 = new ChannelSchema(baseSchema, "Custom Display Name");

// Derive with automatic display name (adds " (Copy)")
var derivedSchema2 = new ChannelSchema(baseSchema);
// Results in: "Twilio SMS Connector (Copy)"
```

## Logical Identity

All derived schemas maintain the same **logical identity** as their parent:

```csharp
var baseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Base Twilio SMS");

var derivedSchema = new ChannelSchema(baseSchema, "Customer Specific SMS");

// Core properties are identical
Console.WriteLine(baseSchema.ChannelProvider);   // "Twilio"
Console.WriteLine(derivedSchema.ChannelProvider); // "Twilio"

Console.WriteLine(baseSchema.ChannelType);       // "SMS"
Console.WriteLine(derivedSchema.ChannelType);     // "SMS"

Console.WriteLine(baseSchema.Version);           // "2.1.0"
Console.WriteLine(derivedSchema.Version);        // "2.1.0"

// Logical identity is the same
Console.WriteLine(baseSchema.GetLogicalIdentity());   // "Twilio/SMS/2.1.0"
Console.WriteLine(derivedSchema.GetLogicalIdentity()); // "Twilio/SMS/2.1.0"

// Schemas are compatible
Console.WriteLine(baseSchema.IsCompatibleWith(derivedSchema)); // True
Console.WriteLine(derivedSchema.IsCompatibleWith(baseSchema)); // True

// Display names can be different
Console.WriteLine(baseSchema.DisplayName);    // "Base Twilio SMS"
Console.WriteLine(derivedSchema.DisplayName); // "Customer Specific SMS"
```

## Capability Management

### Removing Capabilities

```csharp
var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.BulkMessaging |
        ChannelCapability.Templates);

// Remove specific capabilities
var restrictedSchema = new ChannelSchema(baseSchema, "Restricted")
    .RemoveCapability(ChannelCapability.ReceiveMessages)
    .RemoveCapability(ChannelCapability.BulkMessaging);

// Result: Only SendMessages and Templates remain
Console.WriteLine(restrictedSchema.Capabilities); 
// Output: SendMessages, Templates
```

### Restricting to Specific Capabilities

```csharp
// Keep only specific capabilities (removes all others)
var sendOnlySchema = new ChannelSchema(baseSchema, "Send Only")
    .RestrictCapabilities(ChannelCapability.SendMessages);

// Result: Only SendMessages capability
Console.WriteLine(sendOnlySchema.Capabilities); 
// Output: SendMessages
```

### Adding Capabilities to Derived Schemas

```csharp
// Add capabilities not present in base schema
var enhancedSchema = new ChannelSchema(baseSchema, "Enhanced")
    .WithCapability(ChannelCapability.HealthCheck)
    .WithCapability(ChannelCapability.MediaAttachments);
```

## Parameter Management

### Removing Parameters

```csharp
var baseSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
    .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
    .AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean) { DefaultValue = true })
    .AddParameter(new ChannelParameter("Timeout", ParameterType.Integer) { DefaultValue = 30 });

// Remove optional parameters for simplified configuration
var simplifiedSchema = new ChannelSchema(baseSchema, "Simplified SMTP")
    .RemoveParameter("EnableSsl")    // Use default SSL settings
    .RemoveParameter("Timeout");     // Use default timeout

// Result: Only Host and Port parameters remain
```

### Updating Parameters

```csharp
var derivedSchema = new ChannelSchema(baseSchema, "Custom SMTP")
    .UpdateParameter("Port", param => 
    {
        param.DefaultValue = 465;     // Change default port
        param.Description = "SMTP port (usually 465 for SSL)";
    })
    .UpdateParameter("Timeout", param => 
    {
        param.IsRequired = true;      // Make timeout required
        param.DefaultValue = null;    // Remove default value
    });
```

### Adding New Parameters

```csharp
var extendedSchema = new ChannelSchema(baseSchema, "Extended SMTP")
    .AddParameter(new ChannelParameter("RetryCount", ParameterType.Integer)
    {
        DefaultValue = 3,
        Description = "Number of retry attempts for failed sends"
    });
```

## Endpoint Management

### Removing Endpoints

```csharp
var multiChannelSchema = new ChannelSchema("Universal", "Multi", "1.0.0")
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AllowsMessageEndpoint(EndpointType.ApplicationId);

// Create email-only schema
var emailOnlySchema = new ChannelSchema(multiChannelSchema, "Email Only")
    .RemoveEndpoint(EndpointType.PhoneNumber)
    .RemoveEndpoint(EndpointType.Url)
    .RemoveEndpoint(EndpointType.ApplicationId);

// Result: Only EmailAddress endpoint remains
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

### Adding New Endpoints

```csharp
var extendedSchema = new ChannelSchema(baseSchema, "Extended")
    .AllowsMessageEndpoint(EndpointType.DeviceId, asSender: true, asReceiver: false);
```

## Content Type Management

### Restricting Content Types

```csharp
var baseSchema = new ChannelSchema("Provider", "Multi", "1.0.0")
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Json);

// Restrict to only text-based content
var textOnlySchema = new ChannelSchema(baseSchema, "Text Only")
    .RestrictContentTypes(MessageContentType.PlainText, MessageContentType.Html);

// Result: Only PlainText and Html content types remain
```

### Removing Specific Content Types

```csharp
var restrictedSchema = new ChannelSchema(baseSchema, "No Media")
    .RemoveContentType(MessageContentType.Media)
    .RemoveContentType(MessageContentType.Json);

// Result: PlainText and Html remain
```

## Message Property Management

### Removing Message Properties

```csharp
var baseSchema = new ChannelSchema("Email", "Provider", "1.0.0")
    .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
    .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer) { IsRequired = false })
    .AddMessageProperty(new MessagePropertyConfiguration("Category", ParameterType.String) { IsRequired = false })
    .AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean) { IsRequired = false });

// Remove optional properties for simplified email
var simpleEmailSchema = new ChannelSchema(baseSchema, "Simple Email")
    .RemoveMessageProperty("Priority")
    .RemoveMessageProperty("Category");

// Result: Only Subject and IsHtml properties remain
```

### Updating Message Properties

```csharp
var customSchema = new ChannelSchema(baseSchema, "Custom Email")
    .UpdateMessageProperty("Priority", prop => 
    {
        prop.IsRequired = true;       // Make priority required
        prop.Description = "Email priority (1=Low, 2=Normal, 3=High)";
    })
    .UpdateMessageProperty("Subject", prop => 
    {
        prop.Description = "Email subject line - required for all emails";
    });
```

### Adding New Message Properties

```csharp
var extendedSchema = new ChannelSchema(baseSchema, "Extended Email")
    .AddMessageProperty(new MessagePropertyConfiguration("TrackingId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Optional tracking identifier for analytics"
    });
```

## Multi-Generation Hierarchies

You can create multiple levels of derivation:

```csharp
// Grandparent schema - comprehensive base
var universalSchema = new ChannelSchema("Universal", "Messaging", "1.0.0")
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
    .AllowsMessageEndpoint(EndpointType.Url);

// Parent schema - department level restrictions
var marketingSchema = new ChannelSchema(universalSchema, "Marketing Department")
    .RemoveCapability(ChannelCapability.ReceiveMessages)  // Outbound only
    .WithCapability(ChannelCapability.BulkMessaging);     // Keep bulk for campaigns

// Child schema - campaign specific restrictions
var promotionalSchema = new ChannelSchema(marketingSchema, "Promotional Campaigns")
    .RemoveEndpoint(EndpointType.PhoneNumber)            // Email and web only
    .RestrictContentTypes(MessageContentType.Html);      // Rich content only

// All schemas maintain the same logical identity
Console.WriteLine(universalSchema.GetLogicalIdentity());   // "Universal/Messaging/1.0.0"
Console.WriteLine(marketingSchema.GetLogicalIdentity());   // "Universal/Messaging/1.0.0"
Console.WriteLine(promotionalSchema.GetLogicalIdentity()); // "Universal/Messaging/1.0.0"

// All are compatible with each other
Console.WriteLine(universalSchema.IsCompatibleWith(promotionalSchema)); // True
```

## Validation and Compatibility

### Schema Compatibility

```csharp
var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0");
var derivedSchema = new ChannelSchema(baseSchema, "Derived");

// Schemas with same logical identity are compatible
Console.WriteLine(baseSchema.IsCompatibleWith(derivedSchema)); // True

// Different logical identities are not compatible
var differentSchema = new ChannelSchema("Provider", "Type", "2.0.0");
Console.WriteLine(baseSchema.IsCompatibleWith(differentSchema)); // False
```

### Restriction Validation

```csharp
var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    .AddParameter(new ChannelParameter("Param1", ParameterType.String))
    .AddContentType(MessageContentType.PlainText);

var restrictedSchema = new ChannelSchema(baseSchema, "Restricted")
    .RestrictCapabilities(ChannelCapability.SendMessages)
    .RemoveParameter("Param1");

// Validate that restricted schema is a valid restriction of base
var validationResults = restrictedSchema.ValidateAsRestrictionOf(baseSchema);

if (validationResults.Any())
{
    foreach (var error in validationResults)
    {
        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
    }
}
else
{
    Console.WriteLine("Schema is a valid restriction of the base schema");
}
```

## Real-World Examples

### Customer-Specific SMS Configuration

```csharp
// Base Twilio SMS schema for all customers
var twilioBaseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Base")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.MessageStatusQuery |
        ChannelCapability.BulkMessaging)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddParameter(new ChannelParameter("FromNumber", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String) { IsRequired = false })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url);

// Customer A: Send-only with media support
var customerASchema = new ChannelSchema(twilioBaseSchema, "Customer A SMS")
    .RemoveCapability(ChannelCapability.ReceiveMessages)
    .RemoveParameter("WebhookUrl")
    .RemoveEndpoint(EndpointType.Url)
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1555001234"; // Customer-specific number
    });

// Customer B: Bidirectional but text-only
var customerBSchema = new ChannelSchema(twilioBaseSchema, "Customer B SMS") 
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .RestrictContentTypes(MessageContentType.PlainText)
    .UpdateParameter("FromNumber", param => 
    {
        param.DefaultValue = "+1555005678"; // Different customer number
    });

// Customer C: Receive-only webhook system
var customerCSchema = new ChannelSchema(twilioBaseSchema, "Customer C Webhooks")
    .RestrictCapabilities(ChannelCapability.ReceiveMessages)
    .RemoveEndpoint(EndpointType.PhoneNumber)
    .RestrictContentTypes(MessageContentType.PlainText)
    .UpdateParameter("WebhookUrl", param => 
    {
        param.IsRequired = true;
        param.DefaultValue = "https://customer-c.com/webhooks/sms";
    });
```

### Department-Specific Email Configurations

```csharp
// Corporate email base schema
var corporateEmailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
    .WithDisplayName("Corporate SMTP")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.Templates | 
        ChannelCapability.MediaAttachments |
        ChannelCapability.BulkMessaging)
    .AddParameter(new ChannelParameter("SmtpHost", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("SmtpPort", ParameterType.Integer) { DefaultValue = 587 })
    .AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean) { DefaultValue = true })
    .AddParameter(new ChannelParameter("MaxAttachmentSize", ParameterType.Integer) { DefaultValue = 25 })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Multipart)
    .AllowsMessageEndpoint(EndpointType.EmailAddress);

// HR Department: Secure, no attachments
var hrEmailSchema = new ChannelSchema(corporateEmailSchema, "HR Secure Email")
    .RemoveCapability(ChannelCapability.MediaAttachments)
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .RestrictContentTypes(MessageContentType.PlainText)
    .RemoveParameter("MaxAttachmentSize")
    .AddMessageProperty(new MessagePropertyConfiguration("EmployeeId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Employee ID for HR tracking"
    });

// Marketing Department: Rich content, bulk messaging
var marketingEmailSchema = new ChannelSchema(corporateEmailSchema, "Marketing Email")
    .WithCapability(ChannelCapability.BulkMessaging)
    .UpdateParameter("MaxAttachmentSize", param => 
    {
        param.DefaultValue = 50; // Larger attachments for marketing materials
    })
    .AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Marketing campaign identifier"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("SegmentId", ParameterType.String)
    {
        IsRequired = false,
        Description = "Customer segment identifier"
    });

// Legal Department: Compliance focused
var legalEmailSchema = new ChannelSchema(corporateEmailSchema, "Legal Compliance Email")
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .AddMessageProperty(new MessagePropertyConfiguration("RetentionPeriod", ParameterType.Integer)
    {
        IsRequired = true,
        Description = "Email retention period in days"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("ComplianceLevel", ParameterType.String)
    {
        IsRequired = true,
        Description = "Compliance classification (Public, Internal, Confidential)"
    });
```

### Multi-Tenant SaaS Configuration

```csharp
// SaaS platform base schema
var saasBaseSchema = new ChannelSchema("Platform", "MultiChannel", "3.0.0")
    .WithDisplayName("SaaS Messaging Platform")
    .WithCapabilities(
        ChannelCapability.SendMessages | 
        ChannelCapability.ReceiveMessages |
        ChannelCapability.BulkMessaging |
        ChannelCapability.Templates |
        ChannelCapability.MediaAttachments |
        ChannelCapability.HealthCheck)
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Html)
    .AddContentType(MessageContentType.Media)
    .AddContentType(MessageContentType.Template)
    .AllowsMessageEndpoint(EndpointType.EmailAddress)
    .AllowsMessageEndpoint(EndpointType.PhoneNumber)
    .AllowsMessageEndpoint(EndpointType.Url)
    .AllowsMessageEndpoint(EndpointType.ApplicationId);

// Startup tier: Basic features only
var startupTierSchema = new ChannelSchema(saasBaseSchema, "Startup Tier")
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .RemoveCapability(ChannelCapability.MediaAttachments)
    .RestrictContentTypes(MessageContentType.PlainText, MessageContentType.Html)
    .RemoveEndpoint(EndpointType.ApplicationId)
    .AddMessageProperty(new MessagePropertyConfiguration("TierLimit", ParameterType.Integer)
    {
        DefaultValue = 1000,
        Description = "Monthly message limit for startup tier"
    });

// Enterprise tier: Full features plus compliance
var enterpriseTierSchema = new ChannelSchema(saasBaseSchema, "Enterprise Tier")
    .AddMessageProperty(new MessagePropertyConfiguration("ComplianceMode", ParameterType.Boolean)
    {
        DefaultValue = true,
        Description = "Enable compliance logging and retention"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("CustomerId", ParameterType.String)
    {
        IsRequired = true,
        Description = "Enterprise customer identifier"
    })
    .AddMessageProperty(new MessagePropertyConfiguration("DataRegion", ParameterType.String)
    {
        IsRequired = true,
        Description = "Data residency region for compliance"
    });
```

## Best Practices

### 1. Start with Comprehensive Base Schemas

```csharp
// ? Good - Comprehensive base schema
var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(/* All possible capabilities */)
    .AddParameter(/* All possible parameters */)
    .AddContentType(/* All supported content types */)
    .AllowsMessageEndpoint(/* All supported endpoints */);

// Then restrict as needed
var restrictedSchema = new ChannelSchema(baseSchema, "Restricted")
    .RemoveCapability(ChannelCapability.BulkMessaging)
    .RestrictContentTypes(MessageContentType.PlainText);
```

### 2. Use Meaningful Display Names

```csharp
// ? Good - Descriptive names
var customerSchema = new ChannelSchema(baseSchema, "Customer Corp - SMS Notifications");
var departmentSchema = new ChannelSchema(baseSchema, "HR Department - Secure Email");

// ? Avoid - Generic names
var derivedSchema = new ChannelSchema(baseSchema, "Derived");
```

### 3. Document Changes in Descriptions

```csharp
// ? Good - Document why parameters were updated
var customSchema = new ChannelSchema(baseSchema, "Custom Configuration")
    .UpdateParameter("Timeout", param => 
    {
        param.DefaultValue = 60; // Increased for slow customer networks
        param.Description = "Connection timeout in seconds (increased for customer requirements)";
    });
```

### 4. Validate Derived Schemas

```csharp
// ? Good - Always validate derived schemas
var derivedSchema = new ChannelSchema(baseSchema, "Derived");

var validation = derivedSchema.ValidateAsRestrictionOf(baseSchema);
if (validation.Any())
{
    throw new InvalidOperationException($"Invalid schema derivation: {validation.First().ErrorMessage}");
}
```

### 5. Maintain Logical Identity

```csharp
// ? Good - Derived schemas maintain base identity
var baseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0");
var derivedSchema = new ChannelSchema(baseSchema, "Customer Specific");

Console.WriteLine(baseSchema.GetLogicalIdentity());   // "Twilio/SMS/2.1.0"
Console.WriteLine(derivedSchema.GetLogicalIdentity()); // "Twilio/SMS/2.1.0" - Same!

// ? Don't create schemas with different logical identities for derivation
var wrongSchema = new ChannelSchema("DifferentProvider", "SMS", "2.1.0"); // Wrong!
```

### 6. Test Schema Compatibility

```csharp
// ? Good - Test compatibility
[Test]
public void DerivedSchema_IsCompatibleWithBase()
{
    var baseSchema = CreateBaseSchema();
    var derivedSchema = new ChannelSchema(baseSchema, "Derived");
    
    Assert.True(baseSchema.IsCompatibleWith(derivedSchema));
    Assert.True(derivedSchema.IsCompatibleWith(baseSchema));
    Assert.Equal(baseSchema.GetLogicalIdentity(), derivedSchema.GetLogicalIdentity());
}
```

### 7. Use Inheritance Chains Wisely

```csharp
// ? Good - Logical inheritance chain
var universal = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(/* All capabilities */);

var departmental = new ChannelSchema(universal, "Department Level")
    .RemoveCapability(ChannelCapability.BulkMessaging);

var userSpecific = new ChannelSchema(departmental, "User Specific")
    .RestrictCapabilities(ChannelCapability.SendMessages);

// ? Avoid - Too many levels can become hard to manage
// Don't create more than 3-4 levels of derivation
```

This comprehensive guide covers all aspects of schema derivation, enabling you to create flexible, maintainable messaging configurations that can adapt to different requirements while maintaining compatibility and type safety.