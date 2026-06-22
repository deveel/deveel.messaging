---
sidebar_position: 4
---

# Firebase Push Connector

Push notifications via Firebase Cloud Messaging (FCM) HTTP v1 API.

## Package

```bash
dotnet add package Ratatosk.Firebase
```

## Configuration Settings

| Parameter | Type | Required | Is Secret | Default | Description |
|-----------|------|----------|-----------|---------|-------------|
| `ProjectId` | `string` | **Yes** | No | — | Firebase project ID |
| `ServiceAccountKey` | `string` | **Yes** | **Yes** | — | Service account JSON key content |
| `DryRun` | `bool` | No | No | `false` | Validate without sending |

**Notes:**
- `ServiceAccountKey` is marked as sensitive and will be redacted in logs
- `ServiceAccountKey` must be the complete JSON content, not a file path
- `DryRun=true` is recommended for testing message validation

## Configuration Examples

### Connection String

**Basic:**
```
ProjectId=my-project-id;ServiceAccountKey={"type":"service_account",...}
```

**With Dry Run:**
```
ProjectId=my-project-id;ServiceAccountKey={"type":"service_account",...};DryRun=true
```

**With Timeout:**
```
ProjectId=my-project-id;ServiceAccountKey=...;Timeout.Send=00:00:30;Timeout.RetryOnTimeout=true
```

**Note:** For connection strings with JSON values, use quoted values:
```
ProjectId=my-project;ServiceAccountKey='{"type":"service_account","project_id":"my-project"}'
```

### From appsettings.json

```json
{
  "Firebase": {
    "ProjectId": "my-project-id",
    "ServiceAccountKey": "{\"type\":\"service_account\",\"project_id\":\"my-project\",...}",
    "DryRun": false,
    "Timeout": {
      "Send": "00:00:30"
    },
    "Retry": {
      "MaxAttempts": 3
    }
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<FirebasePushConnector>(cfg => cfg
        .WithSettings("Firebase"));
```

## Schema

| Property | Value |
|---|---|
| Provider | `Firebase` |
| Type | `Push` |
| Version | `1.0.0` |
| Capabilities | `SendMessages`, `BulkMessaging` |
| Content types | `PlainText`, `Json` |
| Endpoints | `Device` (FCM token), `Id` (topic) |
| Authentication | Firebase service account JWT |

## Send examples

### Notification to a device token

```csharp
var settings = new ConnectionSettings()
    .SetParameter("ProjectId", "my-project")
    .SetParameter("ServiceAccountKey", serviceAccountJson);

var connector = new FirebasePushConnector(FirebaseChannelSchemas.FirebasePush, settings);
await connector.InitializeAsync(ct);

var message = new MessageBuilder()
    .WithId("push-1")
    .To(Endpoint.Device("fcm-device-token-here"))
    .WithText("You have a new notification")
    .WithTitle("New message")
    .Build();

var result = await connector.SendMessageAsync(message, ct);
```

### Notification to a topic

```csharp
new MessageBuilder()
    .WithId("push-topic-1")
    .To(Endpoint.Id("/topics/news"))
    .WithText("Breaking news: ...")
    .WithTitle("News alert")
    .Build();
```

### Push with custom data payload

```csharp
new MessageBuilder()
    .To(Endpoint.Device("fcm-token"))
    .WithContent(new JsonContent("{\"order_id\": \"ORD-123\", \"status\": \"shipped\"}"))
    .WithTitle("Order shipped")
    .WithSound("default")
    .WithBadge(1)
    .Build();
```

### Batch push to multiple devices

```csharp
var batch = new MessageBatch();
foreach (var token in deviceTokens)
{
    batch.Messages.Add(new MessageBuilder()
        .WithId(Guid.NewGuid().ToString("n"))
        .To(Endpoint.Device(token))
        .WithText("You have a new message")
        .WithTitle("New message")
        .Build());
}

var result = await connector.SendBatchAsync(batch, ct);
```

### Dry run (validation without sending)

```csharp
settings.SetParameter("DryRun", true);
// All sends will be validated by Firebase but not delivered
```

## Message properties

| Property | Type | Description |
|---|---|---|
| `Title` | `string` | Notification title |
| `sound` | `string` | Sound file to play (e.g., `"default"`) |
| `badge` | `int` | Badge count for the app icon |
| `image` | `string` | Image URL for rich notifications |
| `click_action` | `string` | Action to perform on notification tap |
| `priority` | `string` | `"normal"` or `"high"` |
| `ttl` | `int` | Time-to-live in seconds |

## Authentication

Firebase uses OAuth 2.0 with a JWT signed by the service account's private key. The `FirebaseServiceAccountAuthenticationProvider` handles this automatically:

1. Reads `ProjectId` and `ServiceAccountKey` from settings
2. Creates a signed JWT with the service account's private key
3. Exchanges the JWT for a Firebase OAuth 2.0 access token
4. Caches the token and refreshes it before expiry

## Error codes

Firebase-specific error codes are defined in `FirebaseErrorCodes` with domain `"Firebase"`.

| Code | Description |
|---|---|
| `MISSING_SERVICE_ACCOUNT_KEY` | Service account key is missing or invalid |
| `MISSING_PROJECT_ID` | Firebase project ID is missing |
| `INITIALIZATION_FAILED` | Firebase app initialization failed |
| `UNREGISTERED_TOKEN` | Device registration token is no longer valid |
| `INVALID_ARGUMENT` | Request contains an invalid argument |
| `SENDER_ID_MISMATCH` | Sender ID does not match the registered token |
| `THIRD_PARTY_AUTH_ERROR` | Third-party authentication error occurred |
| `SERVICE_UNAVAILABLE` | FCM service is temporarily unavailable |
| `INTERNAL_ERROR` | Internal error in the Firebase service |

Standard `MessagingErrorCodes` are also used — see the [error codes reference](../connectors-implementation/result-types.md#error-code-tables).

### Original provider codes

Firebase Admin SDK errors (`FirebaseMessagingException`) are mapped to framework error codes in `FirebaseService.MapFirebaseErrorCode()`:

| FCM MessagingErrorCode | Mapped framework code |
|---|---|
| `InvalidArgument` | `INVALID_ARGUMENT` |
| `Unregistered` | `UNREGISTERED_TOKEN` |
| `SenderIdMismatch` | `SENDER_ID_MISMATCH` |
| `QuotaExceeded` | `RATE_LIMIT_EXCEEDED` |
| `ThirdPartyAuthError` | `THIRD_PARTY_AUTH_ERROR` |
| `Unavailable` | `SERVICE_UNAVAILABLE` |
| `Internal` | `INTERNAL_ERROR` |
| Other | `SEND_MESSAGE_FAILED` |

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `INVALID_CREDENTIALS` | Wrong ProjectId or ServiceAccountKey | Verify in Firebase Console → Project Settings → Service Accounts |
| `INVALID_RECIPIENT` | Invalid FCM token | Token may be stale; request a new token from the client |
| `PROVIDER_VALIDATION_FAILED` | Payload too large | FCM limit is 4096 bytes for most payloads |
| Device not receiving | Token was unregistered | Remove token from your store; Firebase returns `UNREGISTERED` |
| Topic send failing | Topic doesn't exist | Topics are auto-created; verify the format starts with `/topics/` |
| Token expired | Token refreshed | Listen for token refresh on the client and update your store |

## FirebaseChannelSchemas

```csharp
// Push notification schema
FirebaseChannelSchemas.FirebasePush
```
