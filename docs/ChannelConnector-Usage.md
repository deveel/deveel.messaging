# Channel Connector Implementation Guide

This guide covers how to implement channel connectors using the `ChannelConnectorBase` class and the `IChannelConnector` interface. Channel connectors provide the bridge between the messaging framework and external messaging services.

## Table of Contents

1. [Overview](#overview)
2. [Basic Implementation](#basic-implementation)
3. [Connector States](#connector-states)
4. [Core Methods](#core-methods)
5. [Message Handling](#message-handling)
6. [Error Handling](#error-handling)
7. [Capabilities and Validation](#capabilities-and-validation)
8. [Complete Examples](#complete-examples)
9. [Best Practices](#best-practices)
10. [Testing](#testing)

## Overview

The `ChannelConnectorBase` class provides a foundation for implementing connectors with:

- **State Management** - Automatic handling of connector states
- **Capability Validation** - Ensures operations match declared capabilities
- **Error Handling** - Standardized error reporting and handling
- **Async Support** - Full async/await support with cancellation tokens
- **Message Validation** - Built-in message validation against schema

## Basic Implementation

### 1. Create Your Connector Class

```csharp
using Deveel.Messaging;

public class SmtpConnector : ChannelConnectorBase
{
    private SmtpClient? _smtpClient;
    private string? _host;
    private int _port;
    private string? _username;
    private string? _password;
    private bool _enableSsl;

    public SmtpConnector(IChannelSchema schema) : base(schema)
    {
    }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Extract configuration from schema parameters
            _host = GetParameterValue<string>("Host");
            _port = GetParameterValue<int>("Port");
            _username = GetParameterValue<string>("Username");
            _password = GetParameterValue<string>("Password");
            _enableSsl = GetParameterValue<bool>("EnableSsl");

            // Validate required parameters
            if (string.IsNullOrEmpty(_host))
                return ConnectorResult<bool>.Fail("MISSING_HOST", "SMTP host is required");

            if (string.IsNullOrEmpty(_username))
                return ConnectorResult<bool>.Fail("MISSING_USERNAME", "Username is required");

            // Initialize SMTP client
            _smtpClient = new SmtpClient(_host, _port)
            {
                EnableSsl = _enableSsl,
                Credentials = new NetworkCredential(_username, _password)
            };

            SetState(ConnectorState.Connected);
            return ConnectorResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            SetState(ConnectorState.Error);
            return ConnectorResult<bool>.Fail("INIT_ERROR", $"Failed to initialize SMTP connector: {ex.Message}");
        }
    }

    protected override async Task<ConnectorResult<bool>> TestConnectionCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_smtpClient == null)
                return ConnectorResult<bool>.Fail("NOT_INITIALIZED", "Connector not initialized");

            // Test connection by connecting to server
            // Note: Actual SMTP testing would involve connecting to the server
            // This is a simplified example
            
            return ConnectorResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ConnectorResult<bool>.Fail("CONNECTION_TEST_FAILED", ex.Message);
        }
    }

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (_smtpClient == null)
                return ConnectorResult<MessageResult>.Fail("NOT_INITIALIZED", "Connector not initialized");

            // Create email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(message.Sender?.Address ?? "noreply@example.com"),
                Subject = message.Properties?.GetValueOrDefault("Subject")?.ToString() ?? "No Subject",
                Body = GetMessageBody(message.Content),
                IsBodyHtml = message.Content?.ContentType == MessageContentType.Html
            };

            mailMessage.To.Add(message.Receiver?.Address ?? throw new ArgumentException("Receiver address is required"));

            // Send the message
            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);

            var messageId = Guid.NewGuid().ToString();
            return ConnectorResult<MessageResult>.Success(new MessageResult(messageId, MessageStatus.Sent));
        }
        catch (Exception ex)
        {
            return ConnectorResult<MessageResult>.Fail("SEND_ERROR", ex.Message);
        }
    }

    private string GetMessageBody(IMessageContent? content)
    {
        return content switch
        {
            ITextContent textContent => textContent.Text,
            IHtmlContent htmlContent => htmlContent.Html,
            _ => content?.ToString() ?? string.Empty
        };
    }

    private T? GetParameterValue<T>(string parameterName)
    {
        var parameter = Schema.Parameters.FirstOrDefault(p => p.Name == parameterName);
        if (parameter?.DefaultValue is T value)
            return value;
        
        // In a real implementation, you would get values from configuration
        // This is simplified for demonstration
        return default;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _smtpClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Connector States

The framework manages connector states automatically:

```csharp
public enum ConnectorState
{
    Uninitialized,  // Initial state
    Initializing,   // During initialization
    Connected,      // Ready for operations
    Disconnected,   // Temporarily disconnected
    Error,          // Error state
    Disposed        // Disposed/cleaned up
}
```

### State Management

```csharp
protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
{
    // State is automatically set to Initializing when this method is called
    
    try
    {
        // Perform initialization logic
        await ConnectToService();
        
        // Set state to Connected on success
        SetState(ConnectorState.Connected);
        return ConnectorResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        // Set state to Error on failure
        SetState(ConnectorState.Error);
        return ConnectorResult<bool>.Fail("INIT_ERROR", ex.Message);
    }
}
```

## Core Methods

### Required Abstract Methods

```csharp
public abstract class ChannelConnectorBase : IChannelConnector
{
    // Initialize the connector
    protected abstract Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken);
    
    // Test connection to external service
    protected abstract Task<ConnectorResult<bool>> TestConnectionCoreAsync(CancellationToken cancellationToken);
    
    // Send a single message
    protected abstract Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken);
}
```

### Optional Override Methods

```csharp
// Send multiple messages (if BulkMessaging capability is supported)
protected virtual async Task<ConnectorResult<IEnumerable<MessageResult>>> SendMessagesCoreAsync(
    IEnumerable<IMessage> messages, CancellationToken cancellationToken)
{
    // Default implementation sends messages individually
    var results = new List<MessageResult>();
    foreach (var message in messages)
    {
        var result = await SendMessageCoreAsync(message, cancellationToken);
        if (result.IsSuccess && result.Value != null)
            results.Add(result.Value);
    }
    return ConnectorResult<IEnumerable<MessageResult>>.Success(results);
}

// Get message status (if MessageStatusQuery capability is supported)
protected virtual Task<ConnectorResult<MessageStatus>> GetMessageStatusCoreAsync(
    string messageId, CancellationToken cancellationToken)
{
    // Default implementation returns NotSupported
    return Task.FromResult(ConnectorResult<MessageStatus>.Fail("NOT_SUPPORTED", "Message status query not supported"));
}

// Receive messages (if ReceiveMessages capability is supported)
protected virtual Task<ConnectorResult<IEnumerable<IMessage>>> ReceiveMessagesCoreAsync(
    MessageSource source, CancellationToken cancellationToken)
{
    return Task.FromResult(ConnectorResult<IEnumerable<IMessage>>.Fail("NOT_SUPPORTED", "Message receiving not supported"));
}

// Get health status (if HealthCheck capability is supported)
protected virtual Task<ConnectorResult<HealthStatus>> GetHealthCoreAsync(CancellationToken cancellationToken)
{
    return Task.FromResult(ConnectorResult<HealthStatus>.Success(HealthStatus.Healthy));
}
```

## Message Handling

### Message Validation

The base class automatically validates messages against the schema:

```csharp
public async Task<ConnectorResult<MessageResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken)
{
    // Automatic validation includes:
    // 1. Capability check (SendMessages)
    // 2. Operational state check
    // 3. Message content validation
    // 4. Endpoint type validation
    // 5. Message property validation

    try
    {
        return await SendMessageCoreAsync(message, cancellationToken);
    }
    catch (Exception ex)
    {
        return ConnectorResult<MessageResult>.Fail("SEND_ERROR", ex.Message);
    }
}
```

### Custom Message Validation

```csharp
protected override async IAsyncEnumerable<ValidationResult> ValidateMessageAsync(
    IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // Call base validation first
    await foreach (var result in base.ValidateMessageAsync(message, cancellationToken))
    {
        yield return result;
    }

    // Add custom validation logic
    if (message.Content is ITextContent textContent && textContent.Text.Length > 1600)
    {
        yield return new ValidationResult("SMS messages cannot exceed 1600 characters", new[] { "Content" });
    }

    if (message.Receiver?.Type == EndpointType.PhoneNumber)
    {
        var phoneNumber = message.Receiver.Address;
        if (!IsValidPhoneNumber(phoneNumber))
        {
            yield return new ValidationResult("Invalid phone number format", new[] { "Receiver" });
        }
    }
}

private bool IsValidPhoneNumber(string phoneNumber)
{
    // Implement phone number validation logic
    return phoneNumber.StartsWith("+") && phoneNumber.Length >= 10;
}
```

## Error Handling

### Using ConnectorResult

```csharp
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    try
    {
        // Attempt to send message
        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var messageId = await ExtractMessageId(response);
            return ConnectorResult<MessageResult>.Success(new MessageResult(messageId, MessageStatus.Sent));
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return ConnectorResult<MessageResult>.Fail("API_ERROR", $"API returned {response.StatusCode}: {error}");
        }
    }
    catch (HttpRequestException ex)
    {
        return ConnectorResult<MessageResult>.Fail("NETWORK_ERROR", $"Network error: {ex.Message}");
    }
    catch (TaskCanceledException)
    {
        return ConnectorResult<MessageResult>.Fail("TIMEOUT", "Request timed out");
    }
    catch (Exception ex)
    {
        return ConnectorResult<MessageResult>.Fail("UNKNOWN_ERROR", $"Unexpected error: {ex.Message}");
    }
}
```

### Error Codes and Messages

Use consistent error codes and descriptive messages:

```csharp
// Good error reporting
return ConnectorResult<T>.Fail("INVALID_CREDENTIALS", "Authentication failed: Invalid username or password");
return ConnectorResult<T>.Fail("RATE_LIMIT_EXCEEDED", "Rate limit exceeded: Maximum 100 messages per minute");
return ConnectorResult<T>.Fail("INSUFFICIENT_BALANCE", "Account balance insufficient for message delivery");

// Avoid generic errors
return ConnectorResult<T>.Fail("ERROR", "Something went wrong");
```

## Capabilities and Validation

### Capability Validation

The base class automatically validates capabilities before operations:

```csharp
// This method automatically validates that the connector supports SendMessages
public async Task<ConnectorResult<MessageResult>> SendMessageAsync(IMessage message, CancellationToken cancellationToken)
{
    ValidateCapability(ChannelCapability.SendMessages);  // Automatic validation
    ValidateOperationalState();                          // Automatic validation
    
    // ... rest of implementation
}
```

### Manual Capability Checks

```csharp
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    // Check for optional capabilities
    if (message.Content?.ContentType == MessageContentType.Media)
    {
        if (!Schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments))
        {
            return ConnectorResult<MessageResult>.Fail("MEDIA_NOT_SUPPORTED", 
                "This connector does not support media attachments");
        }
    }

    // Proceed with sending
    // ...
}
```

## Complete Examples

### SMS Connector (Twilio)

```csharp
public class TwilioSmsConnector : ChannelConnectorBase
{
    private readonly HttpClient _httpClient;
    private string? _accountSid;
    private string? _authToken;
    private string? _fromNumber;

    public TwilioSmsConnector(IChannelSchema schema, HttpClient httpClient) : base(schema)
    {
        _httpClient = httpClient;
    }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Extract configuration
            _accountSid = GetConfigurationValue("AccountSid");
            _authToken = GetConfigurationValue("AuthToken");
            _fromNumber = GetConfigurationValue("FromNumber");

            // Validate required configuration
            if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken))
            {
                return ConnectorResult<bool>.Fail("MISSING_CONFIG", "Account SID and Auth Token are required");
            }

            // Set up HTTP client
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            SetState(ConnectorState.Connected);
            return ConnectorResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            SetState(ConnectorState.Error);
            return ConnectorResult<bool>.Fail("INIT_ERROR", ex.Message);
        }
    }

    protected override async Task<ConnectorResult<bool>> TestConnectionCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}.json", cancellationToken);
            return ConnectorResult<bool>.Success(response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            return ConnectorResult<bool>.Fail("CONNECTION_TEST_FAILED", ex.Message);
        }
    }

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var toNumber = message.Receiver?.Address;
            var messageBody = GetMessageText(message.Content);

            var parameters = new List<KeyValuePair<string, string>>
            {
                new("To", toNumber!),
                new("From", _fromNumber!),
                new("Body", messageBody)
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var messageId = ExtractMessageId(responseContent);
                return ConnectorResult<MessageResult>.Success(new MessageResult(messageId, MessageStatus.Sent));
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return ConnectorResult<MessageResult>.Fail("TWILIO_ERROR", error);
            }
        }
        catch (Exception ex)
        {
            return ConnectorResult<MessageResult>.Fail("SEND_ERROR", ex.Message);
        }
    }

    protected override async Task<ConnectorResult<MessageStatus>> GetMessageStatusCoreAsync(
        string messageId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages/{messageId}.json",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var status = ParseTwilioStatus(content);
                return ConnectorResult<MessageStatus>.Success(status);
            }
            else
            {
                return ConnectorResult<MessageStatus>.Fail("STATUS_ERROR", "Failed to get message status");
            }
        }
        catch (Exception ex)
        {
            return ConnectorResult<MessageStatus>.Fail("STATUS_ERROR", ex.Message);
        }
    }

    private string GetMessageText(IMessageContent? content)
    {
        return content switch
        {
            ITextContent textContent => textContent.Text,
            _ => content?.ToString() ?? string.Empty
        };
    }

    private string ExtractMessageId(string responseContent)
    {
        // Parse JSON response to extract message ID
        // Simplified for example
        return Guid.NewGuid().ToString();
    }

    private MessageStatus ParseTwilioStatus(string statusResponse)
    {
        // Parse Twilio status response
        // Simplified for example
        return MessageStatus.Delivered;
    }

    private string? GetConfigurationValue(string key)
    {
        // Get configuration from your preferred configuration source
        // Could be from Schema.Parameters, IConfiguration, etc.
        return null;
    }
}
```

### Webhook Receiver Connector

```csharp
public class WebhookReceiverConnector : ChannelConnectorBase
{
    private readonly ILogger<WebhookReceiverConnector> _logger;
    private string? _webhookUrl;
    private string? _secretKey;

    public WebhookReceiverConnector(IChannelSchema schema, ILogger<WebhookReceiverConnector> logger) : base(schema)
    {
        _logger = logger;
    }

    protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            _webhookUrl = GetConfigurationValue("WebhookUrl");
            _secretKey = GetConfigurationValue("SecretKey");

            if (string.IsNullOrEmpty(_webhookUrl))
            {
                return ConnectorResult<bool>.Fail("MISSING_CONFIG", "Webhook URL is required");
            }

            SetState(ConnectorState.Connected);
            return ConnectorResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            SetState(ConnectorState.Error);
            return ConnectorResult<bool>.Fail("INIT_ERROR", ex.Message);
        }
    }

    protected override async Task<ConnectorResult<bool>> TestConnectionCoreAsync(CancellationToken cancellationToken)
    {
        // For webhook receivers, testing might involve making a test POST to the webhook URL
        return ConnectorResult<bool>.Success(true);
    }

    protected override Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        // Webhook receivers typically don't send messages
        return Task.FromResult(ConnectorResult<MessageResult>.Fail("NOT_SUPPORTED", "Webhook receivers cannot send messages"));
    }

    protected override async Task<ConnectorResult<IEnumerable<IMessage>>> ReceiveMessagesCoreAsync(
        MessageSource source, CancellationToken cancellationToken)
    {
        try
        {
            // This would typically be called by a webhook endpoint controller
            // The MessageSource would contain the webhook payload
            
            var messages = new List<IMessage>();
            var payload = source.GetContent();

            // Parse webhook payload and create messages
            var message = ParseWebhookPayload(payload);
            if (message != null)
            {
                messages.Add(message);
            }

            return ConnectorResult<IEnumerable<IMessage>>.Success(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive webhook message");
            return ConnectorResult<IEnumerable<IMessage>>.Fail("RECEIVE_ERROR", ex.Message);
        }
    }

    private IMessage? ParseWebhookPayload(string payload)
    {
        // Parse webhook payload and create message
        // Implementation depends on webhook format
        return null;
    }

    private string? GetConfigurationValue(string key)
    {
        return null; // Implement configuration retrieval
    }
}
```

## Best Practices

### 1. Use Proper Error Handling

```csharp
// ? Good - Specific error codes and messages
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    try
    {
        await SendToApi(message);
        return ConnectorResult<MessageResult>.Success(result);
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("401"))
    {
        return ConnectorResult<MessageResult>.Fail("UNAUTHORIZED", "Invalid API credentials");
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("429"))
    {
        return ConnectorResult<MessageResult>.Fail("RATE_LIMITED", "API rate limit exceeded");
    }
    catch (TaskCanceledException)
    {
        return ConnectorResult<MessageResult>.Fail("TIMEOUT", "Request timed out");
    }
}

// ? Avoid - Generic error handling
catch (Exception ex)
{
    return ConnectorResult<MessageResult>.Fail("ERROR", ex.Message);
}
```

### 2. Validate Configuration Early

```csharp
// ? Good - Validate during initialization
protected override async Task<ConnectorResult<bool>> InitializeCoreAsync(CancellationToken cancellationToken)
{
    var apiKey = GetConfigurationValue("ApiKey");
    if (string.IsNullOrEmpty(apiKey))
    {
        return ConnectorResult<bool>.Fail("MISSING_API_KEY", "API key is required for authentication");
    }

    if (apiKey.Length < 20)
    {
        return ConnectorResult<bool>.Fail("INVALID_API_KEY", "API key appears to be invalid (too short)");
    }

    // Continue with initialization...
}
```

### 3. Implement Proper Disposal

```csharp
public class MyConnector : ChannelConnectorBase
{
    private readonly HttpClient _httpClient;
    private readonly Timer _healthCheckTimer;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            _healthCheckTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### 4. Use Cancellation Tokens

```csharp
// ? Good - Respect cancellation tokens
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    var response = await _httpClient.PostAsync(url, content, cancellationToken);
    
    cancellationToken.ThrowIfCancellationRequested();
    
    return ProcessResponse(response);
}
```

### 5. Log Important Events

```csharp
public class MyConnector : ChannelConnectorBase
{
    private readonly ILogger<MyConnector> _logger;

    protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
        IMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending message {MessageId} to {Receiver}", message.Id, message.Receiver?.Address);
        
        try
        {
            var result = await SendMessage(message);
            _logger.LogInformation("Message {MessageId} sent successfully with provider ID {ProviderId}", 
                message.Id, result.MessageId);
            return ConnectorResult<MessageResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message {MessageId}", message.Id);
            return ConnectorResult<MessageResult>.Fail("SEND_ERROR", ex.Message);
        }
    }
}
```

## Testing

### Unit Testing Your Connector

```csharp
[Test]
public async Task SendMessage_ValidMessage_ReturnsSuccess()
{
    // Arrange
    var schema = CreateTestSchema();
    var connector = new MyConnector(schema);
    await connector.InitializeAsync(CancellationToken.None);
    
    var message = new MessageBuilder()
        .WithId("test-001")
        .WithEmailSender("sender@test.com")
        .WithEmailReceiver("receiver@test.com")
        .WithTextContent("Test message")
        .Message;

    // Act
    var result = await connector.SendMessageAsync(message, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.NotEmpty(result.Value.MessageId);
}

[Test]
public async Task SendMessage_InvalidConfiguration_ReturnsFail()
{
    // Arrange
    var schema = CreateInvalidSchema(); // Missing required parameters
    var connector = new MyConnector(schema);

    // Act
    var initResult = await connector.InitializeAsync(CancellationToken.None);

    // Assert
    Assert.False(initResult.IsSuccess);
    Assert.Equal("MISSING_CONFIG", initResult.ErrorCode);
}
```

### Integration Testing

```csharp
[Test]
public async Task Integration_SendAndReceive_WorksEndToEnd()
{
    // Arrange
    var senderSchema = CreateSenderSchema();
    var receiverSchema = CreateReceiverSchema();
    
    var sender = new MySenderConnector(senderSchema);
    var receiver = new MyReceiverConnector(receiverSchema);
    
    await sender.InitializeAsync(CancellationToken.None);
    await receiver.InitializeAsync(CancellationToken.None);

    // Act
    var message = CreateTestMessage();
    var sendResult = await sender.SendMessageAsync(message, CancellationToken.None);
    
    // Wait for message delivery
    await Task.Delay(1000);
    
    var receiveResult = await receiver.ReceiveMessagesAsync(MessageSource.Empty, CancellationToken.None);

    // Assert
    Assert.True(sendResult.IsSuccess);
    Assert.True(receiveResult.IsSuccess);
    Assert.NotEmpty(receiveResult.Value);
}
```

This comprehensive guide covers all aspects of implementing channel connectors. The `ChannelConnectorBase` class provides a solid foundation that handles common concerns while allowing you to focus on the specific logic for your messaging service integration.