---
sidebar_position: 11
---

# Result Types

If every connector used its own error model — one throwing exceptions, another returning tuples, a third using custom error objects — the application code that calls connectors would be a mess of adapters and conditionals. The framework standardizes on a single result type for every operation: `OperationResult<T>`.

The result type carries three possible outcomes:
- **Success** — the operation completed, and `.Value` contains the result
- **Validation failure** — the input did not pass schema validation
- **Failure** — the operation failed, and `.Error` provides a machine-readable code and a human-readable message

This tri-state model avoids the ambiguity of throwing exceptions for validation errors (which are not exceptional — they indicate caller bugs) and distinguishes transient provider failures from preventable input errors.

Every connector operation returns an `OperationResult<T>`. This gives you a consistent pattern for handling success, failure, and validation errors across all channels.

## OperationResult\<T>

`OperationResult<T>` is the standard return type for all connector operations. It comes from the `Deveel.Results` package.

### Properties

| Property | Type | Description |
|---|---|---|
| `IsSuccess()` | `bool` | Operation completed successfully (method) |
| `IsFailure()` | `bool` | Operation failed (method) |
| `Value` | `T?` | Result value (non-null when `IsSuccess`) |
| `Error` | `IMessagingError?` | Error code and message (non-null when `IsFailure`) |

Note: `OperationResult<T>` is provided by the `Deveel.Results` package. Use `.IsSuccess()` and `.IsFailure()` as methods.

### Usage patterns

```csharp
var result = await connector.SendMessageAsync(message, ct);

if (result.IsSuccess())
{
    var sendResult = result.Value!;
    Console.WriteLine($"Sent: {sendResult.RemoteMessageId}");
}
else if (result.IsFailure() && result.Error is IValidationError)
{
    var validationError = (IValidationError)result.Error;
    Console.WriteLine("Validation errors:");
    foreach (var error in validationError.Errors)
        Console.WriteLine($"  - {error.ErrorMessage}");
}
else
{
    Console.WriteLine($"Error [{result.Error!.Code}]: {result.Error.Message}");
}
```

### Factory methods

```csharp
// Success
return OperationResult<SendResult>.Success(new SendResult { ... });

// Failure with error code and message
return OperationResult<SendResult>.Fail("RATE_LIMITED", "SMS", "Too many requests");

// Validation failure
return OperationResult<SendResult>.ValidationFailed(
    "VALIDATION_ERROR", "SMS", validationResults);

// Implicit conversion from T
SendResult sendResult = await SendToProviderAsync(message);
return sendResult;  // auto-wraps in OperationResult<SendResult>
```

The implicit conversion is what makes `ChannelConnectorBase` overrides clean — your core methods return raw values, and the base class wraps them.

## SendResult

Returned by `IChannelConnector.SendMessageAsync`.

```csharp
var result = await connector.SendMessageAsync(message, ct);

if (result.IsSuccess())
{
    Console.WriteLine($"Local ID: {result.Value!.MessageId}");
    Console.WriteLine($"Provider ID: {result.Value.RemoteMessageId}");
    Console.WriteLine($"Initial status: {result.Value.Status}");
    Console.WriteLine($"Timestamp: {result.Value.Timestamp}");
}
```

| Property | Type | Description |
|---|---|---|
| `MessageId` | `string` | The local message ID you assigned |
| `RemoteMessageId` | `string` | Provider-assigned message identifier |
| `Status` | `MessageStatus?` | Initial delivery status from provider |
| `Timestamp` | `DateTimeOffset?` | When the provider accepted the message |
| `AdditionalData` | `IDictionary<string, object>` | Provider-specific metadata; includes `RetryAttempts` when [retry policy](../connectors-configuration/retry-policies.md) is active |

The `GetRetryAttempts()` extension method reads the retry count from `AdditionalData`:

```csharp
var result = await connector.SendMessageAsync(message, ct);
Console.WriteLine($"Attempts: {result.Value.GetRetryAttempts()}"); // 1 if no retries
```

### In a connector override

```csharp
protected override async Task<SendResult> SendMessageCoreAsync(
    IMessage message, CancellationToken ct)
{
    var apiResult = await _httpClient.PostAsync("/send", ...);

    return new SendResult
    {
        MessageId = message.Id,
        RemoteMessageId = apiResult.Id,
        Status = MessageStatus.Sent,
        Timestamp = DateTimeOffset.UtcNow,
        AdditionalData = new Dictionary<string, object>
        {
            ["provider_fee"] = apiResult.Fee,
            ["remaining_balance"] = apiResult.Balance
        }
    };
}
```

## BatchSendResult

Returned by `IChannelConnector.SendBatchAsync`.

```csharp
var batch = new MessageBatch();
batch.Messages.Add(msg1);
batch.Messages.Add(msg2);

var result = await connector.SendBatchAsync(batch, ct);

if (result.IsSuccess())
{
    var data = result.Value!;
    Console.WriteLine($"Batch {data.BatchId}: {data.MessageResults.Count} messages");

    foreach (var (msgId, sendResult) in data.MessageResults)
    {
        if (sendResult.Status == MessageStatus.Sent)
            Console.WriteLine($"  {msgId}: sent ({sendResult.RemoteMessageId})");
        else
            Console.WriteLine($"  {msgId}: {sendResult.Status}");
    }
}
```

| Property | Type | Description |
|---|---|---|
| `BatchId` | `string` | Local batch identifier |
| `RemoteBatchId` | `string?` | Provider-assigned batch ID (if supported) |
| `MessageResults` | `IDictionary<string, SendResult>` | Per-message results keyed by message ID |

## ReceiveResult

Returned by `IChannelConnector.ReceiveMessagesAsync`.

```csharp
var source = new MessageSource("application/json", webhookBody);
var result = await connector.ReceiveMessagesAsync(source, ct);

if (result.IsSuccess())
{
    var data = result.Value!;
    Console.WriteLine($"Received {data.Messages.Count} message(s)");

    foreach (var message in data.Messages)
    {
        Console.WriteLine($"  From: {message.Sender?.Address}");
        Console.WriteLine($"  Content: {(message.Content as TextContent)?.Text}");
    }
}
```

| Property | Type | Description |
|---|---|---|
| `BatchId` | `string` | Identifier for this receive batch |
| `Messages` | `IReadOnlyList<IMessage>` | Received messages |

### MessageSource

The `MessageSource` struct represents an inbound message payload (from webhooks, callbacks, etc.):

```csharp
public readonly struct MessageSource
{
    public string ContentType { get; }       // e.g., "application/json"
    public string? ContentEncoding { get; }  // optional encoding
    public ReadOnlyMemory<byte> Content { get; } // raw payload

    // Static factories for common formats
    MessageSource.Json(...)
    MessageSource.Xml(...)
    MessageSource.Text(...)
    MessageSource.Binary(...)
    MessageSource.UrlPost(...)

    // Parsing helpers
    ReadOnlySpan<char> AsText();
    T AsJson<T>();
}
```

## StatusUpdateResult / StatusUpdatesResult

Returned by `IChannelConnector.ReceiveMessageStatusAsync` and `GetMessageStatusAsync`.

```csharp
// Get message delivery history
var statusResult = await connector.GetMessageStatusAsync("msg-1", ct);

if (statusResult.IsSuccess())
{
    foreach (var update in statusResult.Value!.Updates)
    {
        Console.WriteLine(
            $"[{update.Timestamp:O}] {update.Status}" +
            $"{(update.Description != null ? $": {update.Description}" : "")}");
    }
}
```

**StatusUpdateResult** properties:

| Property | Type | Description |
|---|---|---|
| `MessageId` | `string` | The message ID |
| `Status` | `MessageStatus` | Status value (`Received`, `Queued`, `Sent`, `Delivered`, `DeliveryFailed`, etc.) |
| `Timestamp` | `DateTimeOffset` | When the status was recorded |
| `Description` | `string?` | Optional human-readable description |
| `AdditionalData` | `IDictionary<string, object>` | Provider-specific metadata; includes `RetryAttempts` when [retry policy](../connectors-configuration/retry-policies.md) is active |

**StatusUpdatesResult** properties:

| Property | Type | Description |
|---|---|---|
| `MessageId` | `string` | The message ID |
| `Updates` | `IList<StatusUpdateResult>` | Ordered list of status updates |

## StatusInfo

Returned by `IChannelConnector.GetStatusAsync`.

```csharp
var status = await connector.GetStatusAsync(ct);

if (status.IsSuccess())
{
    var info = status.Value!;
    Console.WriteLine($"Connector status: {info.Status}");

    if (info.Description != null)
        Console.WriteLine($"Details: {info.Description}");

    Console.WriteLine($"Last updated: {info.Timestamp:O}");
}
```

| Property | Type | Description |
|---|---|---|
| `Status` | `string` | Status string (provider-specific) |
| `Description` | `string?` | Optional description |
| `Timestamp` | `DateTimeOffset` | When the status was determined |
| `AdditionalData` | `IDictionary<string, object>` | Provider-specific metadata; includes `RetryAttempts` when [retry policy](../connectors-configuration/retry-policies.md) is active |

## ConnectorHealth

Returned by `IChannelConnector.GetHealthAsync`.

```csharp
var health = await connector.GetHealthAsync(ct);

if (health.IsSuccess())
{
    var h = health.Value!;
    Console.WriteLine($"Healthy: {h.IsHealthy}");
    Console.WriteLine($"State: {h.State}");
    Console.WriteLine($"Uptime: {h.Uptime}");
    Console.WriteLine($"Last check: {h.LastHealthCheck:O}");

    if (h.Issues.Count > 0)
    {
        Console.WriteLine("Issues:");
        foreach (var issue in h.Issues)
            Console.WriteLine($"  - {issue}");
    }

    if (h.Metrics.Count > 0)
    {
        Console.WriteLine("Metrics:");
        foreach (var (key, value) in h.Metrics)
            Console.WriteLine($"  {key}: {value}");
    }
}
```

| Property | Type | Description |
|---|---|---|
| `State` | `ConnectorState` | Current lifecycle state |
| `IsHealthy` | `bool` | `true` if the connector is operating normally |
| `LastHealthCheck` | `DateTime` | When the health check was last run |
| `Uptime` | `TimeSpan` | Time since last successful initialization |
| `Metrics` | `Dictionary<string, object>` | Custom metrics (message count, error rate, latency, etc.) |
| `Issues` | `List<string>` | Human-readable issue descriptions |

## IMessagingError

```csharp
public interface IMessagingError
{
    string Code { get; }
    string? Message { get; }
}
```

## Error handling

The framework uses a three-layer error handling model:

1. **Return values** — `OperationResult<T>` is the standard return type for all connector operations
2. **Exceptions** — `ConnectorException` and `MessagingException` are thrown for non-recoverable errors during initialization, connection testing, and send operations (caught by the base class and converted to `OperationResult<T>`)
3. **Error codes** — Every error has a machine-readable string code and a human-readable message

### ConnectorException

Thrown inside connector operations to signal a recoverable or provider-specific error. The base class catches it and wraps it in `OperationResult<T>.Fail()`:

```csharp
throw new ConnectorException(
    MessagingErrorCodes.InvalidRecipient,
    TwilioErrorCodes.ErrorDomain,
    "Recipient phone number is required");
```

### Error code hierarchy

Error codes are organized by scope in static classes:

| Class | Domain | Scope |
|---|---|---|
| `MessagingErrorCodes` | `"messaging"` | General messaging errors |
| `ConnectorErrorCodes` | `"messaging"` | Connector lifecycle and operation errors |
| `FacebookErrorCodes` | `"Facebook"` | Facebook Messenger API errors |
| `FirebaseErrorCodes` | `"Firebase"` | Firebase Cloud Messaging errors |
| `TelegramErrorCodes` | `"Telegram"` | Telegram Bot API errors |
| `TwilioErrorCodes` | `"Twilio"` | Twilio SMS/WhatsApp errors |
| `SendGridErrorCodes` | `"SendGrid"` | SendGrid email errors |

### ConnectorException

Thrown inside connector lifecycle methods (`InitializeAsync`, `TestConnectionAsync`, `SendMessageCoreAsync`, `ReceiveMessagesCoreAsync`, etc.) to signal a specific error. Contains an error code, a domain string, and a human-readable message.

### Authentication errors

Authentication providers (`AuthenticationProviderBase` subclasses) return `AuthenticationResult` with a string error code when authentication fails. The `AuthenticationManager` wraps these into a `ConnectorException` with code `AUTHENTICATION_FAILED` or reports the provider-specific code directly.

### Error code tables

#### MessagingErrorCodes (general)

These codes are used for messaging-level errors outside the scope of channel connectors, such as routing and configuration.

| Code | Description |
|---|---|
| `MESSAGING_ERROR` | Unspecified or unexpected messaging error |
| `MESSAGE_ROUTING_FAILED` | Message could not be routed to the intended recipient or channel |
| `MESSAGE_SERIALIZATION_FAILED` | Message serialization failed |
| `MESSAGE_DESERIALIZATION_FAILED` | Message deserialization failed |
| `INVALID_CONFIGURATION` | Connector configuration is invalid or incomplete |
| `UNSUPPORTED_CONTENT_TYPE` | Unsupported message content type encountered |
| `CONNECTOR_NOT_FOUND` | No connector was found for the requested channel type |
| `INVALID_WEBHOOK_DATA` | Webhook data is invalid or malformed |
| `INVALID_RECIPIENT` | Recipient endpoint is missing, invalid, or unreachable |
| `MISSING_CREDENTIALS` | Required credentials are missing |
| `INVALID_CREDENTIALS` | Provided credentials are invalid or expired |
| `MISSING_SENDER` | Sender endpoint is missing or not configured |
| `MESSAGE_TOO_LONG` | Message exceeds the maximum allowed length |
| `CONNECTION_FAILED` | Connection to the remote service failed |
| `SEND_MESSAGE_FAILED` | Sending a message failed |
| `RATE_LIMIT_EXCEEDED` | API rate limit has been exceeded |

#### ConnectorErrorCodes (connector operations)

These codes are defined in `Ratatosk.Connectors` and used by `ChannelConnectorBase`.

| Code | Description |
|---|---|
| `ALREADY_INITIALIZED` | Connector has already been initialized |
| `INITIALIZATION_ERROR` | Error occurred during connector initialization |
| `AUTHENTICATION_FAILED` | Authentication with the remote service failed |
| `CONNECTION_TEST_ERROR` | Error testing connection to the external service |
| `MESSAGE_VALIDATION_FAILED` | Message validation failed before sending |
| `SEND_MESSAGE_ERROR` | Error sending a single message |
| `BATCH_VALIDATION_FAILED` | Batch validation failed before sending |
| `SEND_BATCH_ERROR` | Error sending a batch of messages |
| `GET_STATUS_ERROR` | Error retrieving connector status |
| `GET_MESSAGE_STATUS_ERROR` | Error retrieving message status |
| `GET_HEALTH_ERROR` | Error performing health check |
| `RECEIVE_STATUS_ERROR` | Error receiving status updates |
| `RECEIVE_MESSAGES_ERROR` | Error receiving messages |

#### Authentication error codes

These codes are returned by authentication providers when obtaining or refreshing credentials fails.

| Code | Description |
|---|---|
| `MISSING_API_KEY` | API key not found in connection settings |
| `MISSING_TOKEN` | Bearer token not found |
| `MISSING_BASIC_CREDENTIALS` | Basic authentication credentials (username/password) not found |
| `MISSING_PARAMETERS` | Required OAuth parameters (client ID, secret) are missing |
| `MISSING_TOKEN_ENDPOINT` | Token endpoint URL is required but missing |
| `TOKEN_REQUEST_FAILED` | Token request to the provider failed |
| `INVALID_TOKEN_RESPONSE` | Token response is missing the access token |
| `EMPTY_ACCESS_TOKEN` | Empty access token received from provider |
| `INVALID_REFRESH_RESPONSE` | Refresh response is missing the access token |
| `EMPTY_REFRESH_TOKEN` | Empty access token received from refresh |
| `REFRESH_FAILED` | Token refresh operation failed |
| `NETWORK_ERROR` | Network error during token request |
| `TIMEOUT` | Token request timed out |
| `INVALID_JSON` | Invalid JSON in provider response |
| `UNEXPECTED_ERROR` | Unexpected error during authentication |
| `MISSING_SERVICE_ACCOUNT_KEY` | Service account key is required but missing |
| `SERVICE_ACCOUNT_FILE_NOT_FOUND` | Service account key file does not exist |
| `INVALID_SERVICE_ACCOUNT_JSON` | Service account key is not valid JSON |
| `CREDENTIAL_ERROR` | Error preparing credential |
| `NO_PROVIDER` | No authentication provider available for the requested scheme |
| `AUTHENTICATION_ERROR` | Unspecified authentication error |

### Error code mapping

Channel connectors map provider-specific API errors to the framework's error codes through dedicated mapping methods in their service implementations:

- **Twilio**: `TwilioService.MapTwilioErrorCode()` maps Twilio `ApiException.Code` integers
- **Telegram**: `TelegramService.MapTelegramErrorCode()` and `MapTelegramSendErrorCode()` map Telegram `ApiRequestException.ErrorCode` integers
- **Firebase**: `FirebaseService.MapFirebaseErrorCode()` maps Firebase `MessagingErrorCode` enum values
- **Facebook**: Error codes are assigned directly via `ConnectorException`, without provider error code translation
- **SendGrid**: HTTP status codes are mapped in the connector; no custom error code mapping

See the channel-specific documentation for detailed mapping tables.
