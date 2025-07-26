# ChannelSchema Derivation - Updated Implementation

## Overview

The ChannelSchema derivation functionality allows creating specialized schemas based on existing parent schemas while maintaining the core identity properties (ChannelProvider, ChannelType, and Version). This ensures compatibility while allowing for configuration restrictions and customizations.

## Key Principle

**Channel Provider, Channel Type, and Version must remain the same between parent and child schemas.**

This ensures that:
- Derived schemas maintain compatibility with their parent
- The core identity of the channel remains consistent
- Only configuration aspects can be customized or restricted

## Constructor

```csharp
// Creates a derived schema with the same core identity as parent
public ChannelSchema(IChannelSchema parentSchema, string? derivedDisplayName = null)
```

### Parameters:
- `parentSchema`: The parent schema to derive from
- `derivedDisplayName`: Optional display name for the derived schema (defaults to "{ParentDisplayName} (Derived)")

## Usage Example

```csharp
// 1. Create base Twilio SMS schema
var twilioBaseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithDisplayName("Twilio SMS Connector")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
    .AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
    .AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true, IsSensitive = true })
    .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String) { IsRequired = false })
    .AddContentType(MessageContentType.PlainText)
    .AddContentType(MessageContentType.Media)
    .AllowsMessageEndpoint("sms", asSender: true, asReceiver: true)
    .AllowsMessageEndpoint("webhook", asSender: false, asReceiver: true);

// 2. Create derived schema with restrictions for specific customer
var customerSmsSchema = new ChannelSchema(twilioBaseSchema, "Customer Corp Restricted SMS")
    .RestrictCapabilities(ChannelCapability.SendMessages) // Remove receiving capabilities
    .RemoveParameter("WebhookUrl") // Remove webhook support
    .RestrictContentTypes(MessageContentType.PlainText) // Only plain text messages
    .RemoveEndpoint("webhook") // Remove webhook endpoint
    .UpdateEndpoint("sms", endpoint => {
        endpoint.CanReceive = false; // Make it send-only
        endpoint.IsRequired = true;
    });

// Core properties remain the same:
Assert.Equal("Twilio", customerSmsSchema.ChannelProvider);     // Same as parent
Assert.Equal("SMS", customerSmsSchema.ChannelType);            // Same as parent
Assert.Equal("2.1.0", customerSmsSchema.Version);             // Same as parent

// But configuration can be different:
Assert.Equal("Customer Corp Restricted SMS", customerSmsSchema.DisplayName);
Assert.Equal(ChannelCapability.SendMessages, customerSmsSchema.Capabilities);
Assert.Single(customerSmsSchema.ContentTypes); // Only PlainText
```

## Benefits

### 1. **Identity Consistency**
- Parent and child schemas have the same core identity
- Ensures compatibility between different configurations
- Maintains clear relationship hierarchy

### 2. **Configuration Flexibility**
- Restrict capabilities, parameters, content types, endpoints
- Update existing configurations
- Add new parameters or message properties
- Customize display names and descriptions

### 3. **Independent Modifications**
- Changes to derived schemas don't affect parent schemas
- Deep copies ensure isolation
- Multiple derived schemas can coexist independently

### 4. **Validation Consistency**
- Derived schemas validate only against their own restricted configuration
- Removed parameters/properties are treated as unknown
- Updated requirements are enforced

## Available Restriction Methods

### Capability Management
```csharp
.RestrictCapabilities(ChannelCapability.SendMessages)  // Keep only specified capabilities
.RemoveCapability(ChannelCapability.ReceiveMessages)   // Remove specific capability
```

### Parameter Management
```csharp
.RemoveParameter("WebhookUrl")                         // Remove parameter
.UpdateParameter("FromNumber", param => {              // Modify parameter
    param.DefaultValue = "+1234567890";
    param.IsRequired = true;
})
```

### Content Type Management
```csharp
.RestrictContentTypes(MessageContentType.PlainText)   // Keep only specified types
.RemoveContentType(MessageContentType.Media)          // Remove specific type
```

### Endpoint Management
```csharp
.RemoveEndpoint("webhook")                             // Remove endpoint
.UpdateEndpoint("sms", endpoint => {                   // Modify endpoint
    endpoint.CanReceive = false;
    endpoint.IsRequired = true;
})
```

### Message Property Management
```csharp
.RemoveMessageProperty("IsUrgent")                     // Remove message property
.UpdateMessageProperty("PhoneNumber", prop => {        // Modify message property
    prop.Description = "Customer phone number";
})
```

## Multi-Generation Hierarchies

You can create multiple levels of derivation:

```csharp
var grandparent = new ChannelSchema("Provider", "Type", "1.0.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | ChannelCapability.Templates);

var parent = new ChannelSchema(grandparent, "Restricted")
    .RemoveCapability(ChannelCapability.Templates);

var child = new ChannelSchema(parent, "Very Restricted")
    .RestrictCapabilities(ChannelCapability.SendMessages);

// All schemas maintain the same core identity:
// ChannelProvider: "Provider", ChannelType: "Type", Version: "1.0.0"
```

## Best Practices

1. **Start with Comprehensive Base Schemas**: Create base schemas with all possible parameters and capabilities, then restrict as needed in derived schemas.

2. **Use Meaningful Display Names**: Give derived schemas clear names that indicate their purpose and restrictions.

3. **Document Restrictions**: Use the `Description` property to explain why certain parameters or capabilities were removed or modified.

4. **Validate Derived Schemas**: Always test that your derived schemas validate correctly for their intended use cases.

5. **Maintain Parent References**: The `ParentSchema` property allows you to trace the derivation hierarchy for debugging and documentation purposes.

6. **Independent Evolution**: Remember that changes to parent schemas don't automatically propagate to derived schemas - this is by design to maintain stability.

## Real-World Scenarios

### Department-Specific Email Configurations
```csharp
var baseEmailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.MediaAttachments);

var hrEmailSchema = new ChannelSchema(baseEmailSchema, "HR Secure Email")
    .RemoveCapability(ChannelCapability.MediaAttachments)  // No attachments for HR
    .RestrictContentTypes(MessageContentType.PlainText);   // Plain text only

var marketingEmailSchema = new ChannelSchema(baseEmailSchema, "Marketing Bulk Email")
    .WithCapability(ChannelCapability.BulkMessaging);       // Add bulk capability
```

### Customer-Specific SMS Restrictions
```csharp
var twilioBaseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
    .WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages);

var customerASchema = new ChannelSchema(twilioBaseSchema, "Customer A - Send Only")
    .RestrictCapabilities(ChannelCapability.SendMessages);

var customerBSchema = new ChannelSchema(twilioBaseSchema, "Customer B - Enhanced")
    .AddParameter(new ChannelParameter("CustomerId", ParameterType.String) { IsRequired = true });
```

This pattern ensures that all derived schemas maintain their relationship to the parent while allowing for specific customizations and restrictions as needed.