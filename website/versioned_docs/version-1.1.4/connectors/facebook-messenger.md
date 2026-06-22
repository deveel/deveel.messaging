---
sidebar_position: 5
---

# Facebook Messenger Connector

Send and receive messages through the Facebook Messenger Platform API using a Facebook Page.

## Package

```bash
dotnet add package Ratatosk.Facebook
```

## Configuration Settings

| Parameter | Type | Required | Is Secret | Default | Description |
|-----------|------|----------|-----------|---------|-------------|
| `PageAccessToken` | `string` | **Yes** | **Yes** | — | Facebook Page access token |
| `PageId` | `string` | **Yes** | No | — | Facebook Page ID (numeric) |
| `WebhookUrl` | `string` | No | No | — | URL for webhook events |
| `VerifyToken` | `string` | No | **Yes** | — | Webhook verification token |

**Notes:**
- `PageAccessToken` and `VerifyToken` are marked as sensitive and redacted in logs
- `PageAccessToken` must be a long-lived token (valid ~60 days)
- Obtain token from Facebook Developer Console → Your App → Messenger → Settings

## Configuration Examples

### Connection String

**Basic:**
```
PageAccessToken=EAAxxxxxxxxxxxx;PageId=123456789
```

**With Webhook:**
```
PageAccessToken=EAAxxxxxxxxxxxx;PageId=123456789;WebhookUrl=https://myapp.com/fb;VerifyToken=mytoken
```

**With Timeout:**
```
PageAccessToken=EAA...;PageId=123456789;Timeout.Send=00:01:00;Timeout.Receive=00:00:30
```

### From appsettings.json

```json
{
  "Facebook": {
    "PageAccessToken": "EAAxxxxxxxxxxxx",
    "PageId": "123456789",
    "WebhookUrl": "https://myapp.com/fb",
    "VerifyToken": "my-verify-token",
    "Timeout": {
      "Send": "00:01:00",
      "Receive": "00:00:30"
    }
  }
}
```

```csharp
builder.Services
    .AddMessaging()
    .AddConnector<FacebookMessengerConnector>(cfg => cfg
        .WithSettings("Facebook"));
```

## Schema

| Property | Value |
|---|---|
| Provider | `Facebook` |
| Type | `Messenger` |
| Version | `1.0.0` |
| Capabilities | `SendMessages`, `ReceiveMessages`, `MediaAttachments`, `InteractiveContent` |
| Content types | `PlainText`, `Media`, `Json`, `Button`, `QuickReply`, `Carousel`, `ListPicker` |
| Endpoints | `UserId` (PSID), `Id` (page-scoped ID) |
| Authentication | API Key (`PageAccessToken`) |

## Send examples

### Text message

```csharp
var settings = new ConnectionSettings()
    .SetParameter("PageAccessToken", "EAA...")
    .SetParameter("PageId", "1234567890");

var connector = new FacebookMessengerConnector(settings);
await connector.InitializeAsync(ct);

var message = new MessageBuilder()
    .WithId("fb-1")
    .To(Endpoint.User("facebook-user-psid"))
    .WithText("Hello from Facebook Messenger")
    .WithMessagingType("RESPONSE")
    .Build();

var result = await connector.SendMessageAsync(message, ct);
```

### Message with media (image)

```csharp
new MessageBuilder()
    .To(Endpoint.User("psid-123"))
    .WithContent(new MediaContent(MediaType.Image, "photo.jpg",
        "https://example.com/photo.jpg"))
    .Build();
```

### Message with quick replies

```csharp
new MessageBuilder()
    .To(Endpoint.User("psid-123"))
    .WithText("Choose an option:")
    .WithQuickReplies("[{\"content_type\":\"text\",\"title\":\"Yes\",\"payload\":\"YES\"},{\"content_type\":\"text\",\"title\":\"No\",\"payload\":\"NO\"}]")
    .Build();
```

### Message with buttons

```csharp
// Using the convenience method
new MessageBuilder()
    .To(Endpoint.User("psid-123"))
    .WithButton("Open Website", ButtonType.Url, "https://example.com")
    .Build();
```

### Message with quick reply options

```csharp
// Single quick reply option
new MessageBuilder()
    .To(Endpoint.User("psid-123"))
    .WithQuickReply("Yes", "YES_PAYLOAD")
    .Build();
```

### Message with carousel (horizontal cards)

```csharp
// Using the sub-builder API
new MessageBuilder()
    .To(Endpoint.User("psid-123"))
    .WithCarousel(carousel => carousel
        .AddCard("https://example.com/img1.jpg", "Product A", "Amazing", card =>
            card.WithButton("Buy", ButtonType.Postback, "BUY_A")
                .WithButton("Details", ButtonType.Url, "https://example.com/a"))
        .AddCard("https://example.com/img2.jpg", "Product B", "Even better", card =>
            card.WithButton("Buy", ButtonType.Postback, "BUY_B")
                .WithButton("Details", ButtonType.Url, "https://example.com/b")))
    .Build();
```

### Message with list picker

```csharp
// Using the sub-builder API
new MessageBuilder()
    .To(Endpoint.User("psid-123"))
    .WithListPicker(list => list
        .WithStyle(ListPickerStyle.Compact)
        .AddItem("Pizza", "Delicious cheese pizza")
        .AddItem("Burger", "Juicy beef burger"))
    .Build();
```

## Webhook setup

Facebook Messenger uses webhooks for inbound messages and delivery events.

### 1. Configure webhook in Facebook Developer Console

- Go to your App → Messenger → Settings
- Set the callback URL to your endpoint
- Set a verify token (must match the one in your settings)

### 2. Handle verification challenge

Facebook sends a GET request with `hub.mode`, `hub.verify_token`, and `hub.challenge` parameters. The connector handles this automatically via `ReceiveMessagesAsync`.

### 3. Process inbound messages

```csharp
[HttpGet("/webhooks/facebook")]
[HttpPost("/webhooks/facebook")]
public async Task<IActionResult> FacebookWebhook(CancellationToken ct)
{
    if (Request.Method == "GET")
    {
        // Verification challenge — handled by the connector
        using var reader = new StreamReader(Request.Body);
        var query = Request.QueryString.Value!;
        var source = MessageSource.UrlPost(query);
        var result = await _connector.ReceiveMessagesAsync(source, ct);
        return Content(result.Data?.Messages.FirstOrDefault()?.Id ?? "error");
    }

    // Inbound message
    using var postReader = new StreamReader(Request.Body);
    var body = await postReader.ReadToEndAsync(ct);
    var msgSource = MessageSource.Json(body);

    // Verify signature
    var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
    if (!IsValidSignature(body, signature))
        return Unauthorized();

    var result = await _connector.ReceiveMessagesAsync(msgSource, ct);
    return Ok();
}
```

Validate `X-Hub-Signature-256` header using your Facebook app secret before processing.

## Error codes

Facebook-specific error codes are defined in `FacebookErrorCodes` with domain `"Facebook"`.

| Code | Description |
|---|---|
| `INVALID_ACCESS_TOKEN` | Page Access Token is invalid or expired |
| `MISSING_PAGE_ID` | Page ID is required but not configured |
| `INVALID_PAGE_ID` | Page ID does not exist or token lacks permission |
| `CONNECTION_TEST_FAILED` | Connection test to Graph API failed |
| `GRAPH_API_ERROR` | Facebook Graph API returned an error |
| `OPERATION_NOT_SUPPORTED` | Operation not available for the current Page or API version |

Standard `MessagingErrorCodes` are also used — see the [error codes reference](../connectors-implementation/result-types.md#error-code-tables).

### Original provider codes

Facebook errors are assigned directly via `ConnectorException` without mapping from Graph API error codes. The Graph API returns its own error objects (e.g., `{"error": {"message": "...", "type": "OAuthException", "code": 190}}`) which are surfaced through the `GRAPH_API_ERROR` code.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `INVALID_CREDENTIALS` | Token expired or invalid | Regenerate long-lived token (valid ~60 days) |
| `INVALID_RECIPIENT` | PSID doesn't exist or page can't message user | User must have interacted with the page |
| Webhook verification fails | VerifyToken mismatch | Ensure `VerifyToken` matches Facebook Console |
| Message not delivered | User blocked the page | Remove user from your store |
| `RATE_LIMITED` | Too many messages | Facebook limits: 250 messages per page per hour (standard) |
