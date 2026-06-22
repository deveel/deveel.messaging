# Advanced Connector Implementation Topics

This guide covers advanced implementation patterns for custom connectors: receive operations, status queries, batch processing, and testing strategies.

## Receive Operations

Implement `ReceiveMessagesCoreAsync()` to receive messages from providers that support inbound messaging:

### Basic Implementation

```csharp
protected override async IAsyncEnumerable<OperationResult<ReceivedMessage>> ReceiveMessagesCoreAsync(
    ReceiveOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var batchSize = options.BatchSize ?? 10;
    var timeout = options.Timeout ?? TimeSpan.FromSeconds(30);

    // Poll provider for messages
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ProviderMessage[]>(
                $"/messages/inbox?limit={batchSize}", cancellationToken);

            if (response == null || response.Length == 0)
            {
                // No messages - wait before polling again
                await Task.Delay(options.PollInterval ?? TimeSpan.FromSeconds(5), cancellationToken);
                continue;
            }

            // Convert and yield messages
            foreach (var providerMessage in response)
            {
                var receivedMessage = new ReceivedMessage(
                    providerMessage.Id,
                    Endpoint.Address(providerMessage.From),
                    Endpoint.Address(providerMessage.To),
                    providerMessage.Content,
                    providerMessage.ReceivedAt);

                yield return OperationResult<ReceivedMessage>.Success(receivedMessage);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // Rate limited - wait and retry
            var retryAfter = GetRetryAfterSeconds(ex);
            await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
        }
        catch (Exception ex)
        {
            yield return OperationResult<ReceivedMessage>.Failure(
                ConnectorErrorCodes.ReceiveFailed,
                MessagingErrorCodes.ErrorDomain,
                "Failed to receive messages",
                ex);
            yield break;
        }
    }
}
```

### Long Polling

For providers that support long polling:

```csharp
protected override async IAsyncEnumerable<OperationResult<ReceivedMessage>> ReceiveMessagesCoreAsync(
    ReceiveOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            // Long poll - server holds connection open until message arrives
            using var request = new HttpRequestMessage(
                HttpMethod.Get, 
                $"/messages/inbox?timeout={options.Timeout?.TotalSeconds ?? 30}");
            
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                // Timeout - no messages, retry
                continue;
            }

            response.EnsureSuccessStatusCode();
            
            var providerMessage = await response.Content.ReadFromJsonAsync<ProviderMessage>(cancellationToken: cancellationToken);
            
            if (providerMessage != null)
            {
                var receivedMessage = ConvertToReceivedMessage(providerMessage);
                yield return OperationResult<ReceivedMessage>.Success(receivedMessage);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
            yield break;
        }
        catch (Exception ex)
        {
            yield return OperationResult<ReceivedMessage>.Failure(
                ConnectorErrorCodes.ReceiveFailed,
                MessagingErrorCodes.ErrorDomain,
                "Receive operation failed",
                ex);
            yield break;
        }
    }
}
```

### Webhook Integration

For webhook-based message delivery, implement a controller that forwards to your connector:

```csharp
[ApiController]
[Route("api/webhooks/messaging")]
public class MessagingWebhookController : ControllerBase
{
    private readonly IMessageChannel _messageChannel;
    private readonly ILogger<MessagingWebhookController> _logger;

    public MessagingWebhookController(
        IMessageChannel messageChannel,
        ILogger<MessagingWebhookController> logger)
    {
        _messageChannel = messageChannel;
        _logger = logger;
    }

    [HttpPost("twilio")]
    public async Task<IActionResult> HandleTwilioWebhook(
        [FromForm] TwilioWebhookPayload payload,
        CancellationToken ct)
    {
        try
        {
            var receivedMessage = new ReceivedMessage(
                payload.MessageSid,
                Endpoint.Address(payload.From),
                Endpoint.Address(payload.To),
                new TextContent(payload.Body),
                DateTimeOffset.UtcNow);

            // Forward to message channel for processing
            await _messageChannel.PublishAsync(receivedMessage, ct);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook");
            return StatusCode(500);
        }
    }
}
```

## Status Query Operations

Implement `GetMessageStatusCoreAsync()` to query message delivery status:

### Basic Implementation

```csharp
protected override async ValueTask<OperationResult<MessageStatus>> GetMessageStatusCoreAsync(
    string messageId, CancellationToken cancellationToken)
{
    try
    {
        var response = await _httpClient.GetFromJsonAsync<ProviderStatus>(
            $"/messages/{messageId}/status", cancellationToken);

        if (response == null)
        {
            return OperationResult<MessageStatus>.Failure(
                ConnectorErrorCodes.StatusQueryFailed,
                MessagingErrorCodes.ErrorDomain,
                "Message not found");
        }

        var status = ConvertToMessageStatus(response);
        return OperationResult<MessageStatus>.Success(status);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return OperationResult<MessageStatus>.Failure(
            ConnectorErrorCodes.MessageNotFound,
            MessagingErrorCodes.ErrorDomain,
            $"Message '{messageId}' not found");
    }
    catch (Exception ex)
    {
        return OperationResult<MessageStatus>.Failure(
            ConnectorErrorCodes.StatusQueryFailed,
            MessagingErrorCodes.ErrorDomain,
            "Failed to query message status",
            ex);
    }
}

private MessageStatus ConvertToMessageStatus(ProviderStatus provider)
{
    var deliveryState = provider.Status switch
    {
        "sent" => DeliveryState.Sent,
        "delivered" => DeliveryState.Delivered,
        "failed" => DeliveryState.Failed,
        "pending" => DeliveryState.Pending,
        _ => DeliveryState.Unknown
    };

    return new MessageStatus(
        provider.MessageId,
        deliveryState,
        provider.Timestamp,
        provider.ErrorMessage);
}
```

## Batch Processing

Implement `SendMessagesCoreAsync()` for efficient bulk sending:

### Basic Batch Implementation

```csharp
protected override async ValueTask<OperationResult<SendResult[]>> SendMessagesCoreAsync(
    IMessage[] messages, CancellationToken cancellationToken)
{
    var results = new SendResult[messages.Length];
    var errors = new List<OperationError>();

    try
    {
        // Group messages for batch API
        var batchPayload = messages.Select(m => new
        {
            to = m.Receiver?.Address,
            from = m.Sender?.Address,
            body = (m.Content as TextContent)?.Text,
            statusCallback = m.CallbackUrl
        }).ToArray();

        var response = await _httpClient.PostAsJsonAsync(
            "/messages/batch", batchPayload, cancellationToken);

        response.EnsureSuccessStatusCode();

        var providerResults = await response.Content.ReadFromJsonAsync<ProviderResult[]>(
            cancellationToken: cancellationToken);

        if (providerResults == null)
        {
            return OperationResult<SendResult[]>.Failure(
                ConnectorErrorCodes.SendFailed,
                MessagingErrorCodes.ErrorDomain,
                "Invalid response from provider");
        }

        // Map results back to original messages
        for (int i = 0; i < messages.Length; i++)
        {
            var providerResult = providerResults[i];
            
            if (providerResult.Success)
            {
                results[i] = new SendResult(
                    providerResult.MessageId,
                    messages[i].Id,
                    ChannelSchema.ProviderName,
                    ChannelSchema.ChannelType);
            }
            else
            {
                errors.Add(new OperationError(
                    ConnectorErrorCodes.SendFailed,
                    MessagingErrorCodes.ErrorDomain,
                    providerResult.ErrorMessage,
                    memberNames: new[] { $"Messages[{i}]" }));
                
                results[i] = null;
            }
        }

        return errors.Count > 0
            ? OperationResult<SendResult[]>.PartialSuccess(results, errors)
            : OperationResult<SendResult[]>.Success(results);
    }
    catch (Exception ex)
    {
        return OperationResult<SendResult[]>.Failure(
            ConnectorErrorCodes.SendFailed,
            MessagingErrorCodes.ErrorDomain,
            "Batch send failed",
            ex);
    }
}
```

### Parallel Sending with Concurrency Control

For providers without batch APIs:

```csharp
protected override async ValueTask<OperationResult<SendResult[]>> SendMessagesCoreAsync(
    IMessage[] messages, CancellationToken cancellationToken)
{
    var results = new SendResult[messages.Length];
    var errors = new List<OperationError>();
    var semaphore = new SemaphoreSlim(maxConcurrency: 10);
    var tasks = new List<Task>();

    try
    {
        for (int i = 0; i < messages.Length; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await SendMessageCoreAsync(messages[index], cancellationToken);
                    
                    if (result.IsSuccess())
                    {
                        results[index] = result.Value;
                    }
                    else if (result.Error != null)
                    {
                        errors.Add(result.Error.WithMemberNames(
                            new[] { $"Messages[{index}]" }));
                        results[index] = null;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        return errors.Count > 0
            ? OperationResult<SendResult[]>.PartialSuccess(results, errors)
            : OperationResult<SendResult[]>.Success(results);
    }
    catch (Exception ex)
    {
        return OperationResult<SendResult[]>.Failure(
            ConnectorErrorCodes.SendFailed,
            MessagingErrorCodes.ErrorDomain,
            "Batch send failed",
            ex);
    }
    finally
    {
        semaphore.Dispose();
    }
}
```

## Testing Strategies

### Unit Testing with Mocks

Test connector logic without external dependencies:

```csharp
public class MyConnectorTests
{
    private readonly Mock<IAuthenticationManager> _mockAuthManager;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly MyConnector _connector;

    public MyConnectorTests()
    {
        _mockAuthManager = new Mock<IAuthenticationManager>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationManager)))
            .Returns(_mockAuthManager.Object);

        _connector = new MyConnector(_mockServiceProvider.Object);
    }

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ReturnsSuccess()
    {
        // Arrange
        await _connector.InitializeAsync(CancellationToken.None);
        
        var message = new MessageBuilder()
            .WithId("test-1")
            .To(Endpoint.Address("+1234567890"))
            .WithText("Hello")
            .Build();

        // Act
        var result = await _connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value?.MessageId);
    }

    [Fact]
    public async Task SendMessageAsync_InvalidReceiver_ReturnsValidationError()
    {
        // Arrange
        await _connector.InitializeAsync(CancellationToken.None);
        
        var message = new MessageBuilder()
            .WithId("test-2")
            .To(Endpoint.Address("invalid"))  // Invalid format
            .WithText("Hello")
            .Build();

        // Act
        var result = await _connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure());
        Assert.Equal(ConnectorErrorCodes.MessageValidationFailed, result.Error?.Code);
    }
}
```

### Integration Testing

Test with real credentials in isolated environment:

```csharp
public class MyConnectorIntegrationTests : IClassFixture<TestConfigurationFixture>
{
    private readonly TestConfigurationFixture _fixture;

    public MyConnectorIntegrationTests(TestConfigurationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendMessageAsync_WithRealCredentials_SendsMessage()
    {
        // Arrange
        var connector = _fixture.CreateConnector();
        await connector.InitializeAsync(CancellationToken.None);

        var message = new MessageBuilder()
            .WithId($"test-{Guid.NewGuid():N}")
            .To(Endpoint.Address(_fixture.TestPhoneNumber))
            .WithText($"Test message at {DateTime.UtcNow:O}")
            .Build();

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess(), result.Error?.Message);
        Assert.NotNull(result.Value?.MessageId);
        
        // Verify message was sent (check provider dashboard or webhook)
        var status = await connector.GetMessageStatusAsync(
            result.Value.MessageId, CancellationToken.None);
        
        Assert.True(status.IsSuccess());
        Assert.Equal(DeliveryState.Sent, status.Value?.DeliveryState);
    }
}

public class TestConfigurationFixture : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _testPhoneNumber;

    public TestConfigurationFixture()
    {
        // Load test configuration from appsettings.test.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: false)
            .Build();

        _testPhoneNumber = configuration["TestPhoneNumber"];

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        
        // Register connector with test configuration
        services.AddMessaging()
            .AddChannel<MyConnector>("TestChannel", options =>
            {
                options.ApiKey = configuration["ApiKey"];
            });

        _serviceProvider = services.BuildServiceProvider();
    }

    public string TestPhoneNumber => _testPhoneNumber;

    public MyConnector CreateConnector()
    {
        return _serviceProvider.GetRequiredService<IChannelConnector>("TestChannel") 
            as MyConnector;
    }

    public void Dispose()
    {
        // Clean up test resources
    }
}
```

### Testing Timeout Behavior

Test timeout handling:

```csharp
[Fact]
public async Task SendMessageAsync_Timeout_ReturnsTimeoutError()
{
    // Arrange
    var connector = new FakeTimeoutConnector(
        TimeSpan.FromMilliseconds(10));  // Very short timeout

    await connector.InitializeAsync(CancellationToken.None);

    var message = new MessageBuilder()
        .WithId("test-1")
        .To(Endpoint.Address("+1234567890"))
        .WithText("Hello")
        .Build();

    // Act
    var result = await connector.SendMessageAsync(message, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure());
    Assert.Equal(ConnectorErrorCodes.SendTimeout, result.Error?.Code);
}
```

## Best Practices

### ✅ DO: Handle Transient Failures

Implement retry logic for transient errors:

```csharp
try
{
    var response = await _httpClient.GetAsync("/messages", cancellationToken);
}
catch (HttpRequestException ex) when (
    ex.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway)
{
    // Transient error - retry with backoff
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    throw;
}
```

### ✅ DO: Log Operation Details

```csharp
protected override async ValueTask<OperationResult<SendResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    Logger.LogInformation(
        "Sending message {MessageId} to {Receiver}",
        message.Id,
        message.Receiver?.Address);

    try
    {
        var result = await SendToProviderAsync(message, cancellationToken);
        
        Logger.LogInformation(
            "Message {MessageId} sent successfully, provider ID: {ProviderId}",
            message.Id,
            result.MessageId);
        
        return OperationResult<SendResult>.Success(result);
    }
    catch (Exception ex)
    {
        Logger.LogError(
            ex,
            "Failed to send message {MessageId}",
            message.Id);
        
        throw;
    }
}
```

### ✅ DO: Dispose Resources

```csharp
public class MyConnector : ChannelConnectorBase
{
    private HttpClient? _httpClient;
    private bool _disposed;

    protected override ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        
        return ValueTask.CompletedTask;
    }
}
```

### ❌ DON'T: Swallow Exceptions

```csharp
// ❌ Bad - hides errors
try
{
    await SendToProviderAsync(message);
    return OperationResult<SendResult>.Success(...);
}
catch
{
    return OperationResult<SendResult>.Failure(...);
}

// ✅ Good - preserve exception details
try
{
    await SendToProviderAsync(message);
    return OperationResult<SendResult>.Success(...);
}
catch (Exception ex)
{
    return OperationResult<SendResult>.Failure(
        ConnectorErrorCodes.SendFailed,
        MessagingErrorCodes.ErrorDomain,
        "Send failed",
        ex);
}
```

## See Also

- [Minimum Implementation](minimum-implementation.md) - Core connector methods
- [Timeouts](../connectors-configuration/timeouts.md) - Timeout configuration
- [Retry Policies](../connectors-configuration/retry-policies.md) - Retry configuration
- [Health Checks](../connectors-configuration/health-checks.md) - Health check integration
