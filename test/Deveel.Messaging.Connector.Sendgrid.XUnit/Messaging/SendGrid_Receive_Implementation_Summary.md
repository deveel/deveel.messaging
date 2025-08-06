# SendGrid Email Connector - Receive Messages Implementation Summary

## Overview

This document summarizes the implementation of receive messages and status update capabilities for the SendGrid Email Connector, following the same patterns established in the Twilio WhatsApp connector.

## Implementation Details

### 1. Core Capabilities Added

The SendGrid Email Connector now supports:
- **ReceiveMessages**: Processing inbound emails via SendGrid Inbound Parse webhooks
- **HandleMessageState**: Processing email event webhooks (delivered, bounced, opened, etc.)

### 2. Schema Updates

#### Updated `SendGridChannelSchemas.cs`:
- **SendGridEmail**: Added `ReceiveMessages` and `HandleMessageState` capabilities
- **SimpleEmail**: Explicitly removes receive capabilities (send-only)
- **TransactionalEmail**: Explicitly removes receive capabilities (send-only)
- **TemplateEmail**: Explicitly removes receive capabilities (template-only)
- **MarketingEmail**: Keeps receive capabilities (full-featured)
- **BulkEmail**: Keeps receive capabilities (full-featured)

### 3. Connector Implementation

#### Added new methods in `SendGridEmailConnector.cs`:

1. **`ReceiveMessagesCoreAsync`**:
   - Supports both JSON and form data content types
   - Parses SendGrid Inbound Parse webhooks
   - Filters for `inbound` and `processed` events only
   - Handles HTML vs plain text content preference
   - Preserves all webhook metadata as message properties

2. **`ReceiveMessageStatusCoreAsync`**:
   - Supports both JSON and form data content types
   - Parses SendGrid Event webhooks
   - Maps SendGrid events to standard message statuses
   - Preserves all event data as additional properties

3. **Supporting methods**:
   - `ParseSendGridWebhookJson`: Handles JSON webhook payloads
   - `ParseSendGridWebhookFormData`: Handles form data webhooks
   - `ParseSendGridJsonEvent`: Parses individual JSON events
   - `ParseSendGridStatusCallbackJson`: Parses JSON status callbacks
   - `ParseSendGridStatusCallbackFormData`: Parses form data status callbacks
   - `MapSendGridEventToMessageStatus`: Maps SendGrid events to MessageStatus enum

### 4. Event Mapping

SendGrid events are mapped to standard message statuses as follows:

| SendGrid Event | MessageStatus |
|---------------|---------------|
| processed | Queued |
| deferred | Queued |
| delivered | Delivered |
| open | Delivered |
| click | Delivered |
| bounce | DeliveryFailed |
| dropped | DeliveryFailed |
| spamreport | DeliveryFailed |
| unsubscribe | Delivered |
| group_unsubscribe | Delivered |
| group_resubscribe | Delivered |
| inbound | Received |
| *others* | Unknown |

### 5. Error Codes

Added new error codes in `SendGridErrorCodes.cs`:
- `InvalidWebhookData`: Invalid or malformed webhook data
- `UnsupportedContentType`: Unsupported content type for webhooks
- `ReceiveMessageFailed`: Failed to receive messages
- `ReceiveStatusFailed`: Failed to receive status updates

### 6. Content Type Handling

The connector intelligently handles content:
- **HTML preferred**: If both HTML and text are present, HTML is used
- **Plain text fallback**: If only text is present, plain text is used
- **Empty content**: If neither is present, empty text content is created

### 7. JSON Parsing Robustness

Implemented robust JSON parsing that handles different value types:
- Strings: Direct string extraction
- Numbers: Converted to string representation
- Booleans: Converted to "true"/"false"
- Arrays/Objects: JSON representation

## Testing

### Test Coverage

Created comprehensive test suites:

1. **`SendGridEmailConnectorJsonTests.cs`** (19 tests):
   - Basic JSON webhook parsing
   - Single email processing
   - Batch email processing
   - Status callback handling
   - Event type mapping
   - Content type preference
   - Error scenarios

2. **`SendGridEmailConnectorJsonEdgeCaseTests.cs`** (17 tests):
   - Empty JSON handling
   - Null value handling
   - Large content handling
   - Unicode content preservation
   - Invalid timestamp handling
   - Complex envelope data
   - Attachment metadata
   - Error conditions

3. **`SendGridEmailConnectorWebhookTests.cs`** (16 tests):
   - Form data webhook parsing
   - JSON webhook parsing
   - Content type preferences
   - Schema capability verification
   - Error scenarios
   - All event type mappings

### Test Results

All 258 tests pass successfully, including:
- 52 new tests specifically for receive functionality
- Updated schema tests to reflect new capabilities
- Backward compatibility with existing functionality

## Usage Examples

### Receiving Inbound Emails

```csharp
// JSON webhook
var jsonWebhook = """
{
    "event": "inbound",
    "sg_message_id": "message_123",
    "from": "sender@example.com",
    "to": "inbox@yourdomain.com",
    "subject": "Hello!",
    "html": "<p>Hello World!</p>"
}
""";

var source = MessageSource.Json(jsonWebhook);
var result = await connector.ReceiveMessagesAsync(source, cancellationToken);
```

### Receiving Status Updates

```csharp
// Form data webhook
var formData = "event=delivered&sg_message_id=msg123&email=recipient@example.com";
var source = MessageSource.UrlPost(formData);
var result = await connector.ReceiveMessageStatusAsync(source, cancellationToken);
```

## Schema Differentiation

The implementation maintains clear schema differentiation:

- **Full-featured schemas** (`SendGridEmail`, `MarketingEmail`, `BulkEmail`): Support both sending and receiving
- **Send-only schemas** (`SimpleEmail`, `TransactionalEmail`, `TemplateEmail`): Support only sending operations

This allows developers to choose the appropriate schema based on their use case, with the connector automatically enforcing the capabilities.

## Future Considerations

1. **Attachment Handling**: The current implementation preserves attachment metadata in message properties but doesn't directly handle attachment content
2. **Webhook Verification**: Future versions could add SendGrid webhook signature verification for security
3. **Rate Limiting**: Consider implementing rate limiting for webhook processing
4. **Batch Processing**: Could be optimized for high-volume webhook processing

## Conclusion

The SendGrid Email Connector now provides comprehensive support for both sending and receiving emails, with robust error handling, extensive test coverage, and flexible schema options. The implementation follows established patterns from the Twilio connector while addressing SendGrid-specific requirements and event types.