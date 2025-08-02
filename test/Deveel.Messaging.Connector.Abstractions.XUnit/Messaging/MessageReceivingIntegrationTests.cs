using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Deveel.Messaging;

/// <summary>
/// Integration tests that demonstrate complete message receiving workflows,
/// including webhook handling, message processing pipelines, and error scenarios.
/// </summary>
public class MessageReceivingIntegrationTests
{
    [Fact]
    public async Task CompleteReceivingWorkflow_WebhookToMessageProcessing_WorksEndToEnd()
    {
        // Arrange
        var processedMessages = new List<IMessage>();
        
        var schema = new ChannelSchema("IntegrationTest", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.HandlerMessageState)
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
            {
                CanSend = true,
                CanReceive = true
            });

        var connector = new IntegrationTestConnector(schema, processedMessages);
        await connector.InitializeAsync(CancellationToken.None);

        // Simulate receiving multiple messages
        var webhookMessages = new[]
        {
            "MessageSid=SM1111111111&From=%2B1234567890&To=%2B1987654321&Body=Hello%20World&MessageStatus=received",
            "MessageSid=SM2222222222&From=%2B1234567891&To=%2B1987654321&Body=How%20are%20you%3F&MessageStatus=received",
            "MessageSid=SM3333333333&From=%2B1234567892&To=%2B1987654321&Body=Good%20morning%21&MessageStatus=received"
        };

        // Act - Process each webhook directly since MessageSource is no longer a ref struct
        var results = new List<ConnectorResult<ReceiveResult>>();
        foreach (var webhookData in webhookMessages)
        {
            var source = MessageSource.UrlPost(webhookData);
            var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.All(results, result => Assert.True(result.Successful));
        Assert.Equal(3, processedMessages.Count);
        
        // Verify message content
        Assert.Equal("Hello World", ((ITextContent)processedMessages[0].Content!).Text);
        Assert.Equal("How are you?", ((ITextContent)processedMessages[1].Content!).Text);
        Assert.Equal("Good morning!", ((ITextContent)processedMessages[2].Content!).Text);
        
        // Verify all senders are different
        var senders = processedMessages.Select(m => m.Sender?.Address).ToHashSet();
        Assert.Equal(3, senders.Count);
    }

    [Fact]
    public async Task MessageReceivingWithStatusTracking_FullLifecycle_TracksCorrectly()
    {
        // Arrange
        var messageStatuses = new Dictionary<string, List<MessageStatus>>();
        
        var schema = new ChannelSchema("StatusTracking", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.HandlerMessageState)
            .AddContentType(MessageContentType.PlainText);

        var connector = new StatusTrackingConnector(schema, messageStatuses);
        await connector.InitializeAsync(CancellationToken.None);

        var messageId = "SM1234567890";

        // Act - Simulate complete message lifecycle
        // 1. Receive initial message
        var messageWebhook = $"MessageSid={messageId}&From=%2B1234567890&To=%2B1987654321&Body=Test%20message&MessageStatus=received";
        var messageSource = MessageSource.UrlPost(messageWebhook);
        var receiveResult = await connector.ReceiveMessagesAsync(messageSource, CancellationToken.None);

        // 2. Receive status updates
        var statusUpdates = new[]
        {
            $"MessageSid={messageId}&MessageStatus=queued",
            $"MessageSid={messageId}&MessageStatus=sent",
            $"MessageSid={messageId}&MessageStatus=delivered"
        };

        foreach (var statusUpdate in statusUpdates)
        {
            var statusSource = MessageSource.UrlPost(statusUpdate);
            await connector.ReceiveMessageStatusAsync(statusSource, CancellationToken.None);
        }

        // Assert
        Assert.True(receiveResult.Successful);
        Assert.True(messageStatuses.ContainsKey(messageId));
        
        var statuses = messageStatuses[messageId];
        Assert.Equal(4, statuses.Count); // received + 3 status updates
        Assert.Contains(MessageStatus.Received, statuses);
        Assert.Contains(MessageStatus.Queued, statuses);
        Assert.Contains(MessageStatus.Sent, statuses);
        Assert.Contains(MessageStatus.Delivered, statuses);
    }

    [Fact]
    public async Task BatchMessageReceiving_LargeVolume_HandlesEfficiently()
    {
        // Arrange
        var processedMessages = new List<IMessage>();
        
        var schema = new ChannelSchema("BatchProcessing", "Email", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .WithCapability(ChannelCapability.BulkMessaging)
            .AddContentType(MessageContentType.PlainText);

        var connector = new IntegrationTestConnector(schema, processedMessages);
        await connector.InitializeAsync(CancellationToken.None);

        // Create a large batch of messages
        var batchSize = 100;
        var batchMessages = Enumerable.Range(1, batchSize).Select(i => new
        {
            Id = $"msg-{i:D3}",
            From = $"sender{i}@test.com",
            To = "receiver@test.com",
            Body = $"Batch message {i}"
        }).ToArray();

        var batchPayload = new { Messages = batchMessages };
        var jsonPayload = JsonSerializer.Serialize(batchPayload);
        var source = MessageSource.Json(jsonPayload);

        var startTime = DateTime.UtcNow;

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        var endTime = DateTime.UtcNow;
        var processingTime = endTime - startTime;

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(batchSize, processedMessages.Count);
        Assert.True(processingTime.TotalSeconds < 5); // Should process quickly
        
        // Verify all messages were processed correctly
        for (int i = 1; i <= batchSize; i++)
        {
            var expectedId = $"msg-{i:D3}";
            Assert.Contains(processedMessages, m => m.Id == expectedId);
        }
    }

    [Fact]
    public async Task MessageReceivingWithValidation_InvalidMessages_HandledCorrectly()
    {
        // Arrange
        var schema = new ChannelSchema("ValidationTest", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
            {
                CanReceive = true
            });

        var connector = new ValidationTestConnector(schema);
        await connector.InitializeAsync(CancellationToken.None);

        // Test cases: valid and invalid messages
        var testCases = new[]
        {
            // Valid message
            new { Data = "MessageSid=SM1111111111&From=%2B1234567890&To=%2B1987654321&Body=Valid%20message", ShouldSucceed = true },
            // Missing MessageSid
            new { Data = "From=%2B1234567890&To=%2B1987654321&Body=Missing%20ID", ShouldSucceed = false },
            // Invalid phone number format
            new { Data = "MessageSid=SM2222222222&From=invalid-phone&To=%2B1987654321&Body=Invalid%20from", ShouldSucceed = false },
            // Empty body (should still succeed)
            new { Data = "MessageSid=SM3333333333&From=%2B1234567890&To=%2B1987654321&Body=", ShouldSucceed = true }
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var source = MessageSource.UrlPost(testCase.Data);
            var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

            if (testCase.ShouldSucceed)
            {
                Assert.True(result.Successful, $"Expected success for: {testCase.Data}");
            }
            else
            {
                Assert.False(result.Successful, $"Expected failure for: {testCase.Data}");
            }
        }
    }

    [Fact]
    public async Task MessageReceivingWithFiltering_ContentBasedFiltering_FiltersCorrectly()
    {
        // Arrange
        var filteredMessages = new List<IMessage>();
        
        var schema = new ChannelSchema("FilterTest", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .AddContentType(MessageContentType.PlainText);

        var connector = new FilteringTestConnector(schema, filteredMessages);
        await connector.InitializeAsync(CancellationToken.None);

        // Messages with different content types
        var messages = new[]
        {
            "MessageSid=SM1111111111&From=%2B1234567890&To=%2B1987654321&Body=URGENT%3A%20Important%20message",
            "MessageSid=SM2222222222&From=%2B1234567890&To=%2B1987654321&Body=Regular%20message",
            "MessageSid=SM3333333333&From=%2B1234567890&To=%2B1987654321&Body=SPAM%3A%20Buy%20now%21",
            "MessageSid=SM4444444444&From=%2B1234567890&To=%2B1987654321&Body=Another%20regular%20message"
        };

        // Act
        foreach (var messageData in messages)
        {
            var source = MessageSource.UrlPost(messageData);
            await connector.ReceiveMessagesAsync(source, CancellationToken.None);
        }

        // Assert - Only non-spam messages should be in filtered list
        Assert.Equal(3, filteredMessages.Count);
        Assert.DoesNotContain(filteredMessages, m => ((ITextContent)m.Content!).Text.Contains("SPAM"));
        Assert.Contains(filteredMessages, m => ((ITextContent)m.Content!).Text.Contains("URGENT"));
    }

    [Fact]
    public async Task MessageReceivingWithRetry_TransientFailures_RetriesSuccessfully()
    {
        // Arrange
        var attemptCount = 0;
        var schema = new ChannelSchema("RetryTest", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .AddContentType(MessageContentType.PlainText);

        var connector = new RetryTestConnector(schema, () => ++attemptCount);
        await connector.InitializeAsync(CancellationToken.None);

        var messageData = "MessageSid=SM1111111111&From=%2B1234567890&To=%2B1987654321&Body=Test%20message";
        var source = MessageSource.UrlPost(messageData);

        // Act - The connector will fail on first 2 attempts, succeed on 3rd
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Equal(3, attemptCount); // Should have made 3 attempts
    }

    [Fact]
    public async Task MessageReceivingWithTransformation_ContentTransformation_TransformsCorrectly()
    {
        // Arrange
        var transformedMessages = new List<IMessage>();
        
        var schema = new ChannelSchema("TransformTest", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .AddContentType(MessageContentType.PlainText);

        var connector = new TransformationTestConnector(schema, transformedMessages);
        await connector.InitializeAsync(CancellationToken.None);

        var messageData = "MessageSid=SM1111111111&From=%2B1234567890&To=%2B1987654321&Body=hello%20world%21";
        var source = MessageSource.UrlPost(messageData);

        // Act
        var result = await connector.ReceiveMessagesAsync(source, CancellationToken.None);

        // Assert
        Assert.True(result.Successful);
        Assert.Single(transformedMessages);
        
        var transformedMessage = transformedMessages.First();
        var content = ((ITextContent)transformedMessage.Content!).Text;
        
        // Should be transformed to uppercase
        Assert.Equal("HELLO WORLD!", content);
        
        // Should have additional properties
        Assert.NotNull(transformedMessage.Properties);
        Assert.True(transformedMessage.Properties.ContainsKey("Transformed"));
        Assert.Equal("true", transformedMessage.Properties["Transformed"].Value);
    }

    [Fact]
    public async Task ConcurrentMessageReceiving_HighThroughput_MaintainsDataIntegrity()
    {
        // Arrange
        var receivedMessages = new ConcurrentBag<IMessage>();
        var schema = new ChannelSchema("ConcurrencyTest", "SMS", "1.0.0")
            .WithCapability(ChannelCapability.ReceiveMessages)
            .AddContentType(MessageContentType.PlainText);

        var connector = new ConcurrentTestConnector(schema, receivedMessages);
        await connector.InitializeAsync(CancellationToken.None);

        var messageCount = 50;
        var semaphore = new SemaphoreSlim(10); // Limit concurrency

        // Act - Send many concurrent requests
        var tasks = Enumerable.Range(1, messageCount).Select(async i =>
        {
            await semaphore.WaitAsync();
            try
            {
                var messageData = $"MessageSid=SM{i:D10}&From=%2B123456789{i % 10}&To=%2B1987654321&Body=Concurrent%20message%20{i}";
                var source = MessageSource.UrlPost(messageData);
                return await connector.ReceiveMessagesAsync(source, CancellationToken.None);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Successful));
        Assert.Equal(messageCount, receivedMessages.Count);
        
        // Verify all message IDs are unique
        var messageIds = receivedMessages.Select(m => m.Id).ToHashSet();
        Assert.Equal(messageCount, messageIds.Count);
    }

    // Test connector implementations

    public class IntegrationTestConnector : ChannelConnectorBase
    {
        private readonly List<IMessage> _processedMessages;

        public IntegrationTestConnector(IChannelSchema schema, List<IMessage> processedMessages) 
            : base(schema)
        {
            _processedMessages = processedMessages;
        }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Integration Test Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            var messages = ParseMessages(source);
            _processedMessages.AddRange(messages);
            
            var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
            return Task.FromResult(ConnectorResult<ReceiveResult>.Success(result));
        }

        private List<IMessage> ParseMessages(MessageSource source)
        {
            var messages = new List<IMessage>();

            if (source.ContentType == MessageSource.UrlPostContentType)
            {
                var formData = source.AsUrlPostData();
                if (formData.TryGetValue("MessageSid", out var messageId))
                {
                    messages.Add(CreateMessage(formData));
                }
            }
            else if (source.ContentType == MessageSource.JsonContentType)
            {
                var jsonData = source.AsJson<JsonElement>();
                if (jsonData.TryGetProperty("Messages", out var messagesArray))
                {
                    foreach (var messageElement in messagesArray.EnumerateArray())
                    {
                        messages.Add(CreateMessageFromJson(messageElement));
                    }
                }
            }

            return messages;
        }

        private IMessage CreateMessage(IDictionary<string, string> formData)
        {
            return new Message
            {
                Id = formData.TryGetValue("MessageSid", out var sid) ? sid : Guid.NewGuid().ToString(),
                Sender = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("From", out var from) ? from : ""),
                Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                Content = new TextContent(formData.TryGetValue("Body", out var body) ? Uri.UnescapeDataString(body) : "")
            };
        }

        private IMessage CreateMessageFromJson(JsonElement jsonElement)
        {
            return new Message
            {
                Id = jsonElement.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString(),
                Sender = new Endpoint(EndpointType.EmailAddress, jsonElement.GetProperty("From").GetString() ?? ""),
                Receiver = new Endpoint(EndpointType.EmailAddress, jsonElement.GetProperty("To").GetString() ?? ""),
                Content = new TextContent(jsonElement.GetProperty("Body").GetString() ?? "")
            };
        }
    }

    public class StatusTrackingConnector : ChannelConnectorBase
    {
        private readonly Dictionary<string, List<MessageStatus>> _messageStatuses;

        public StatusTrackingConnector(IChannelSchema schema, Dictionary<string, List<MessageStatus>> messageStatuses) 
            : base(schema)
        {
            _messageStatuses = messageStatuses;
        }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Status Tracking Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            var formData = source.AsUrlPostData();
            var messageId = formData["MessageSid"];
            
            if (!_messageStatuses.ContainsKey(messageId))
                _messageStatuses[messageId] = new List<MessageStatus>();
            
            _messageStatuses[messageId].Add(MessageStatus.Received);

            var message = new Message
            {
                Id = messageId,
                Sender = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("From", out var from) ? from : ""),
                Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                Content = new TextContent(formData.TryGetValue("Body", out var body) ? Uri.UnescapeDataString(body) : "")
            };

            var result = new ReceiveResult(Guid.NewGuid().ToString(), new[] { message });
            return Task.FromResult(ConnectorResult<ReceiveResult>.Success(result));
        }

        protected override Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            var formData = source.AsUrlPostData();
            var messageId = formData["MessageSid"];
            var statusString = formData["MessageStatus"];
            
            var status = statusString.ToLowerInvariant() switch
            {
                "queued" => MessageStatus.Queued,
                "sent" => MessageStatus.Sent,
                "delivered" => MessageStatus.Delivered,
                "failed" => MessageStatus.DeliveryFailed,
                _ => MessageStatus.Unknown
            };

            if (!_messageStatuses.ContainsKey(messageId))
                _messageStatuses[messageId] = new List<MessageStatus>();
            
            _messageStatuses[messageId].Add(status);

            var statusResult = new StatusUpdateResult(messageId, status);
            return Task.FromResult(ConnectorResult<StatusUpdateResult>.Success(statusResult));
        }
    }

    public class ValidationTestConnector : ChannelConnectorBase
    {
        public ValidationTestConnector(IChannelSchema schema) : base(schema) { }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Validation Test Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            var formData = source.AsUrlPostData();

            // Validate required fields
            if (!formData.TryGetValue("MessageSid", out var messageId) || string.IsNullOrEmpty(messageId))
            {
                return Task.FromResult(ConnectorResult<ReceiveResult>.Fail("MISSING_MESSAGE_ID", "MessageSid is required"));
            }

            if (!formData.TryGetValue("From", out var from) || !IsValidPhoneNumber(from))
            {
                return Task.FromResult(ConnectorResult<ReceiveResult>.Fail("INVALID_FROM", "Valid From phone number is required"));
            }

            var message = new Message
            {
                Id = messageId,
                Sender = new Endpoint(EndpointType.PhoneNumber, from),
                Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                Content = new TextContent(formData.TryGetValue("Body", out var body) ? Uri.UnescapeDataString(body) : "")
            };

            var result = new ReceiveResult(Guid.NewGuid().ToString(), new[] { message });
            return Task.FromResult(ConnectorResult<ReceiveResult>.Success(result));
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            return !string.IsNullOrEmpty(phoneNumber) && phoneNumber.StartsWith("+") && phoneNumber.Length > 5;
        }
    }

    public class FilteringTestConnector : ChannelConnectorBase
    {
        private readonly List<IMessage> _filteredMessages;

        public FilteringTestConnector(IChannelSchema schema, List<IMessage> filteredMessages) 
            : base(schema)
        {
            _filteredMessages = filteredMessages;
        }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Filtering Test Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            var formData = source.AsUrlPostData();
            var body = formData.TryGetValue("Body", out var b) ? Uri.UnescapeDataString(b) : "";

            var message = new Message
            {
                Id = formData.TryGetValue("MessageSid", out var sid) ? sid : Guid.NewGuid().ToString(),
                Sender = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("From", out var from) ? from : ""),
                Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                Content = new TextContent(body)
            };

            // Filter out spam messages
            if (!body.Contains("SPAM"))
            {
                _filteredMessages.Add(message);
            }

            var result = new ReceiveResult(Guid.NewGuid().ToString(), new[] { message });
            return Task.FromResult(ConnectorResult<ReceiveResult>.Success(result));
        }
    }

    public class RetryTestConnector : ChannelConnectorBase
    {
        private readonly Func<int> _getAttemptCount;

        public RetryTestConnector(IChannelSchema schema, Func<int> getAttemptCount) 
            : base(schema)
        {
            _getAttemptCount = getAttemptCount;
        }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Retry Test Connector")));

        protected override async Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            // Implement retry logic within the connector itself
            ConnectorResult<ReceiveResult> result;
            int attempts = 0;
            
            do
            {
                attempts = _getAttemptCount();
                
                // Fail on first 2 attempts, succeed on 3rd
                if (attempts < 3)
                {
                    result = ConnectorResult<ReceiveResult>.Fail("TRANSIENT_ERROR", $"Simulated failure on attempt {attempts}");
                    // Simulate delay before retry
                    await Task.Delay(10, cancellationToken);
                    continue;
                }

                var formData = source.AsUrlPostData();
                var message = new Message
                {
                    Id = formData.TryGetValue("MessageSid", out var sid) ? sid : Guid.NewGuid().ToString(),
                    Sender = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("From", out var from) ? from : ""),
                    Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                    Content = new TextContent(formData.TryGetValue("Body", out var body) ? Uri.UnescapeDataString(body) : "")
                };

                var receiveResult = new ReceiveResult(Guid.NewGuid().ToString(), new[] { message });
                result = ConnectorResult<ReceiveResult>.Success(receiveResult);
                break;
                
            } while (attempts < 3);

            return result;
        }
    }

    public class TransformationTestConnector : ChannelConnectorBase
    {
        private readonly List<IMessage> _transformedMessages;

        public TransformationTestConnector(IChannelSchema schema, List<IMessage> transformedMessages) 
            : base(schema)
        {
            _transformedMessages = transformedMessages;
        }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Transformation Test Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            var formData = source.AsUrlPostData();
            var originalBody = formData.TryGetValue("Body", out var body) ? Uri.UnescapeDataString(body) : "";

            // Transform the message content
            var transformedBody = originalBody.ToUpperInvariant();

            var message = new Message
            {
                Id = formData.TryGetValue("MessageSid", out var sid) ? sid : Guid.NewGuid().ToString(),
                Sender = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("From", out var from) ? from : ""),
                Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                Content = new TextContent(transformedBody),
                Properties = new Dictionary<string, MessageProperty>
                {
                    { "Transformed", new MessageProperty("Transformed", "true") },
                    { "OriginalBody", new MessageProperty("OriginalBody", originalBody) }
                }
            };

            _transformedMessages.Add(message);

            var result = new ReceiveResult(Guid.NewGuid().ToString(), new[] { message });
            return Task.FromResult(ConnectorResult<ReceiveResult>.Success(result));
        }
    }

    public class ConcurrentTestConnector : ChannelConnectorBase
    {
        private readonly ConcurrentBag<IMessage> _receivedMessages;

        public ConcurrentTestConnector(IChannelSchema schema, ConcurrentBag<IMessage> receivedMessages) 
            : base(schema)
        {
            _receivedMessages = receivedMessages;
        }

        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<bool>.Success(true));

        protected override Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<SendResult>.Success(new SendResult(message.Id, $"remote-{message.Id}")));

        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(ConnectorResult<StatusInfo>.Success(new StatusInfo("Concurrent Test Connector")));

        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            // Simulate some processing time and then process synchronously
            return Task.Run(async () =>
            {
                await Task.Delay(10, cancellationToken);

                var formData = source.AsUrlPostData();
                var message = new Message
                {
                    Id = formData.TryGetValue("MessageSid", out var sid) ? sid : Guid.NewGuid().ToString(),
                    Sender = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("From", out var from) ? from : ""),
                    Receiver = new Endpoint(EndpointType.PhoneNumber, formData.TryGetValue("To", out var to) ? to : ""),
                    Content = new TextContent(formData.TryGetValue("Body", out var body) ? Uri.UnescapeDataString(body) : "")
                };

                _receivedMessages.Add(message);

                var result = new ReceiveResult(Guid.NewGuid().ToString(), new[] { message });
                return ConnectorResult<ReceiveResult>.Success(result);
            });
        }
    }
}

// Note: This integration test file now uses the default implementations from Deveel.Messaging.Abstractions:
// - Message instead of TestMessage
// - Endpoint instead of TestEndpoint  
// - TextContent instead of TestTextContent
// - MessageProperty instead of TestMessageProperty
//
// This eliminates code duplication and ensures consistency across the codebase while
// maintaining all the comprehensive integration test scenarios.