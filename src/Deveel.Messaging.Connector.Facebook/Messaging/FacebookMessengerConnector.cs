//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Deveel.Messaging
{
    /// <summary>
    /// A channel connector that implements messaging using the Facebook Messenger Platform API.
    /// </summary>
    /// <remarks>
    /// This connector provides comprehensive support for Facebook Messenger capabilities including
    /// sending messages, receiving messages via webhooks, media attachments, and health monitoring.
    /// </remarks>
    [ChannelSchema(typeof(FacebookMessengerSchemaFactory))]
    public class FacebookMessengerConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ILogger<FacebookMessengerConnector>? _logger;
        private readonly IFacebookService _facebookService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        private string? _pageAccessToken;
        private string? _pageId;
        private string? _webhookUrl;
        private string? _verifyToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookMessengerConnector"/> class.
        /// </summary>
        /// <param name="schema">The channel schema that defines the connector's capabilities and configuration.</param>
        /// <param name="connectionSettings">The connection settings containing Facebook credentials and configuration.</param>
        /// <param name="facebookService">The Facebook service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when schema or connectionSettings is null.</exception>
        public FacebookMessengerConnector(IChannelSchema schema, ConnectionSettings connectionSettings, IFacebookService? facebookService = null, ILogger<FacebookMessengerConnector>? logger = null)
            : base(schema)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _facebookService = facebookService ?? new FacebookService();
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookMessengerConnector"/> class using one of the predefined schemas.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing Facebook credentials and configuration.</param>
        /// <param name="facebookService">The Facebook service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
        public FacebookMessengerConnector(ConnectionSettings connectionSettings, IFacebookService? facebookService = null, ILogger<FacebookMessengerConnector>? logger = null)
            : this(FacebookChannelSchemas.FacebookMessenger, connectionSettings, facebookService, logger)
        {
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInitializingConnector();

                // Extract required parameters
                _pageAccessToken = _connectionSettings.GetParameter("PageAccessToken") as string;
                _pageId = _connectionSettings.GetParameter("PageId") as string;

                // Extract optional parameters
                _webhookUrl = _connectionSettings.GetParameter("WebhookUrl") as string;
                _verifyToken = _connectionSettings.GetParameter("VerifyToken") as string;

                // Perform custom validation logic
                if (string.IsNullOrWhiteSpace(_pageAccessToken))
                {
                    return ConnectorResult<bool>.FailTask(FacebookErrorCodes.MissingCredentials,
                        "Page Access Token is required");
                }

                if (string.IsNullOrWhiteSpace(_pageId))
                {
                    return ConnectorResult<bool>.FailTask(FacebookErrorCodes.MissingPageId,
                        "Page ID is required");
                }

                // Initialize Facebook service with authentication validation
                _facebookService.Initialize(_pageAccessToken);

                _logger?.LogConnectorInitialized();
                return ConnectorResult<bool>.SuccessTask(true);
            } catch (ArgumentException ex)
            {
                _logger?.LogAuthenticationValidationFailed(ex.Message, ex);
                return ConnectorResult<bool>.FailTask(FacebookErrorCodes.MissingCredentials,
                    $"Facebook authentication error: {ex.Message}");
            } catch (Exception ex)
            {
                _logger?.LogInitializationFailed(ex);
                return ConnectorResult<bool>.FailTask(ConnectorErrorCodes.InitializationError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogTestingConnection();

                // Test connection by fetching page information using Graph API
                var page = await _facebookService.FetchPageAsync(_pageId!, cancellationToken);

                if (page == null)
                {
                    return ConnectorResult<bool>.Fail(FacebookErrorCodes.ConnectionFailed,
                        "Unable to retrieve page information - page may not exist or access token may be invalid");
                }

                _logger?.LogConnectionTestSuccessful(page.Name, page.Category);
                return ConnectorResult<bool>.Success(true);
            } catch (InvalidOperationException ex) when (ex.Message.Contains("Facebook Graph API error"))
            {
                _logger?.LogConnectionTestGraphApiError(ex.Message, ex);
                return ConnectorResult<bool>.Fail(FacebookErrorCodes.ConnectionTestFailed, ex.Message);
            } catch (Exception ex)
            {
                _logger?.LogConnectionTestFailed(ex);
                return ConnectorResult<bool>.Fail(FacebookErrorCodes.ConnectionTestFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogSendingMessage(message.Id);

                // Extract recipient User ID first to get the right error code
                var recipientId = ExtractUserId(message.Receiver);
                if (string.IsNullOrWhiteSpace(recipientId))
                {
                    return ConnectorResult<SendResult>.Fail(FacebookErrorCodes.InvalidRecipient,
                        "Recipient User ID is required and must be a valid Facebook PSID");
                }

                // Build Facebook message request with Graph API validation
                var request = BuildMessageRequest(message, recipientId);

                // Send the message with Facebook Graph API requirements
                var response = await _facebookService.SendMessageAsync(request, cancellationToken);

                _logger?.LogMessageSent(message.Id, response.MessageId);

                var result = new SendResult(message.Id, response.MessageId)
                {
                    Status = MessageStatus.Sent,
                    Timestamp = DateTime.UtcNow
                };

                // Add enhanced properties with Graph API information
                result.AdditionalData["FacebookMessageId"] = response.MessageId;
                result.AdditionalData["RecipientId"] = response.RecipientId;
                result.AdditionalData["PageId"] = _pageId ?? "";
                result.AdditionalData["HttpClient"] = "RestSharp";
                result.AdditionalData["ApiVersion"] = FacebookConnectorConstants.GraphApiVersion;

                return ConnectorResult<SendResult>.Success(result);
            } catch (ArgumentException ex)
            {
                _logger?.LogMessageValidationError(message.Id, ex.Message, ex);
                return ConnectorResult<SendResult>.Fail(FacebookErrorCodes.SendMessageFailed,
                    $"Facebook validation error: {ex.Message}");
            } catch (InvalidOperationException ex) when (ex.Message.Contains("Facebook Graph API error"))
            {
                _logger?.LogMessageGraphApiError(message.Id, ex.Message, ex);
                return ConnectorResult<SendResult>.Fail(FacebookErrorCodes.SendMessageFailed, ex.Message);
            } catch (Exception ex)
            {
                _logger?.LogMessageSendFailed(message.Id, ex);
                return ConnectorResult<SendResult>.Fail(FacebookErrorCodes.SendMessageFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var statusText = $"Facebook Messenger Connector (Page: {_pageId})";
                var statusInfo = new StatusInfo("Ready", statusText);

                statusInfo.AdditionalData["PageId"] = _pageId ?? "";
                statusInfo.AdditionalData["State"] = State.ToString();
                statusInfo.AdditionalData["Uptime"] = DateTime.UtcNow - _startTime;
                statusInfo.AdditionalData["ApiVersion"] = FacebookConnectorConstants.GraphApiVersion;
                statusInfo.AdditionalData["GraphApiCompliance"] = "Full";

                return ConnectorResult<StatusInfo>.SuccessTask(statusInfo);
            } catch (Exception ex)
            {
                _logger?.LogGetStatusFailed(ex);
                return ConnectorResult<StatusInfo>.FailTask(FacebookErrorCodes.StatusError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<ConnectorHealth>> GetConnectorHealthAsync(CancellationToken cancellationToken)
        {
            var health = new ConnectorHealth
            {
                State = State,
                IsHealthy = State == ConnectorState.Ready,
                LastHealthCheck = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - _startTime
            };

            // Add RestSharp and Graph API specific health metrics
            health.Metrics["ApiVersion"] = FacebookConnectorConstants.GraphApiVersion;
            health.Metrics["PageId"] = _pageId ?? "";
            health.Metrics["GraphApiCompliance"] = "Full";

            if (State == ConnectorState.Ready)
            {
                try
                {
                    // Test connectivity by fetching page info using Graph API
                    var testResult = await TestConnectorConnectionAsync(cancellationToken);
                    if (!testResult.Successful)
                    {
                        health.IsHealthy = false;
                        health.Issues.Add($"Connection test failed: {testResult.Error?.ErrorMessage}");
                    } else
                    {
                        health.Metrics["LastSuccessfulApiCall"] = DateTime.UtcNow;
                    }
                } catch (Exception ex)
                {
                    health.IsHealthy = false;
                    health.Issues.Add($"Health check failed: {ex.Message}");
                }
            } else
            {
                health.IsHealthy = false;
                health.Issues.Add($"Health check failed: Connector is in {State} state");
            }

            return ConnectorResult<ConnectorHealth>.Success(health);
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogReceivingMessage();

                if (source.ContentType != MessageSource.JsonContentType)
                {
                    return ConnectorResult<ReceiveResult>.FailTask(FacebookErrorCodes.UnsupportedContentType,
                        "Only JSON content type is supported for Facebook webhooks");
                }

                var messages = ParseFacebookWebhook(source);

                if (messages.Count == 0)
                {
                    return ConnectorResult<ReceiveResult>.FailTask(FacebookErrorCodes.InvalidWebhookData,
                        "No valid messages found in webhook data");
                }

                var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                return ConnectorResult<ReceiveResult>.SuccessTask(result);
            } catch (Exception ex)
            {
                _logger?.LogReceiveMessageFailed(ex);
                return ConnectorResult<ReceiveResult>.FailTask(FacebookErrorCodes.ReceiveMessageFailed, ex.Message);
            }
        }

        private string? ExtractUserId(IEndpoint? endpoint)
        {
            if (endpoint?.Type == EndpointType.UserId)
            {
                return endpoint.Address;
            }
            return null;
        }

        private FacebookMessageRequest BuildMessageRequest(IMessage message, string recipientId)
        {
            var request = new FacebookMessageRequest
            {
                Recipient = recipientId
            };

            // Apply message properties
            if (message.Properties != null)
            {
                foreach (var property in message.Properties)
                {
                    switch (property.Key.ToLowerInvariant())
                    {
                        case "messagingtype":
                            request.MessagingType = property.Value?.Value?.ToString() ?? "RESPONSE";
                            break;
                        case "notificationtype":
                            request.NotificationType = property.Value?.Value?.ToString() ?? "REGULAR";
                            break;
                        case "tag":
                            request.Tag = property.Value?.Value?.ToString();
                            break;
                    }
                }
            }

            // Build Facebook message based on content type
            request.Message = BuildFacebookMessage(message);

            return request;
        }

        private FacebookMessage BuildFacebookMessage(IMessage message)
        {
            var fbMessage = new FacebookMessage();

            switch (message.Content?.ContentType)
            {
                case MessageContentType.PlainText when message.Content is ITextContent textContent:
                    fbMessage.Text = textContent.Text;
                    break;

                case MessageContentType.Media when message.Content is IMediaContent mediaContent:
                    fbMessage.Attachment = new FacebookAttachment
                    {
                        Type = GetFacebookAttachmentType(mediaContent.MediaType.ToString() ?? "file"),
                        Payload = new FacebookPayload
                        {
                            Url = mediaContent.FileUrl,
                            IsReusable = true
                        }
                    };
                    break;
            }

            // Add quick replies if specified
            if (message.Properties?.TryGetValue("QuickReplies", out var quickRepliesProperty) == true)
            {
                var quickRepliesJson = quickRepliesProperty.Value?.ToString();
                if (!string.IsNullOrEmpty(quickRepliesJson))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                        };
                        fbMessage.QuickReplies = JsonSerializer.Deserialize<List<FacebookQuickReply>>(quickRepliesJson, options);
                    } catch (JsonException ex)
                    {
                        _logger?.LogQuickRepliesParsingFailed(quickRepliesJson, ex);
                    }
                }
            }

            return fbMessage;
        }

        private string GetFacebookAttachmentType(string mediaType)
        {
            return mediaType.ToLowerInvariant() switch
            {
                "image" => "image",
                "audio" => "audio",
                "video" => "video",
                _ => "file"
            };
        }

        private List<IMessage> ParseFacebookWebhook(MessageSource source)
        {
            var messages = new List<IMessage>();
            var jsonData = source.AsJson<JsonElement>();

            if (!jsonData.TryGetProperty("object", out var objectProperty) ||
                objectProperty.GetString() != "page")
            {
                return messages;
            }

            if (!jsonData.TryGetProperty("entry", out var entryArray))
            {
                return messages;
            }

            foreach (var entry in entryArray.EnumerateArray())
            {
                if (entry.TryGetProperty("messaging", out var messagingArray))
                {
                    foreach (var messagingEvent in messagingArray.EnumerateArray())
                    {
                        var message = ParseMessagingEvent(messagingEvent);
                        if (message != null)
                            messages.Add(message);
                    }
                }
            }

            return messages;
        }

        private IMessage? ParseMessagingEvent(JsonElement messagingEvent)
        {
            // Check if this is a message event (not postback, delivery, etc.)
            if (!messagingEvent.TryGetProperty("message", out var messageProperty))
                return null;

            // Extract sender and recipient
            if (!messagingEvent.TryGetProperty("sender", out var senderProperty) ||
                !messagingEvent.TryGetProperty("recipient", out var recipientProperty))
                return null;

            var senderId = senderProperty.GetProperty("id").GetString();
            var recipientId = recipientProperty.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(recipientId))
                return null;

            // Extract message ID and timestamp
            var messageId = messageProperty.GetProperty("mid").GetString() ?? Guid.NewGuid().ToString();
            var timestamp = messageProperty.TryGetProperty("timestamp", out var timestampProperty)
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestampProperty.GetInt64()).DateTime
                : DateTime.UtcNow;

            // Extract message content
            MessageContent? content = null;
            if (messageProperty.TryGetProperty("text", out var textProperty))
            {
                content = new TextContent(textProperty.GetString() ?? "");
            } else if (messageProperty.TryGetProperty("attachments", out var attachmentsProperty))
            {
                // Handle attachments (simplified - just take the first one)
                var firstAttachment = attachmentsProperty.EnumerateArray().FirstOrDefault();
                if (firstAttachment.ValueKind != JsonValueKind.Undefined)
                {
                    var type = GetMediaType(firstAttachment.GetProperty("type").GetString() ?? "file");
                    var payload = firstAttachment.GetProperty("payload");
                    var url = payload.GetProperty("url").GetString() ?? "";

                    content = new MediaContent(type, "", url);
                }
            }

            if (content == null)
                return null;

            return new Message
            {
                Id = messageId,
                Sender = new Endpoint(EndpointType.UserId, senderId),
                Receiver = new Endpoint(EndpointType.UserId, recipientId),
                Content = content,
                Properties = new Dictionary<string, MessageProperty>
                {
                    { "Timestamp", new MessageProperty("Timestamp", timestamp.ToString()) },
                    { "PageId", new MessageProperty("PageId", _pageId ?? "") }
                }
            };
        }

        private MediaType GetMediaType(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "image" => MediaType.Image,
                "video" => MediaType.Video,
                "audio" => MediaType.Audio,
                _ => MediaType.File
            };
        }

        /// <inheritdoc/>
        protected override async IAsyncEnumerable<ValidationResult> ValidateMessageCoreAsync(IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Don't validate recipient here - let EmailAddress pass so we can catch it in SendMessageCoreAsync
            // This allows us to return INVALID_RECIPIENT instead of MESSAGE_VALIDATION_FAILED

            // Run base validation for other checks
            var hasErrors = false;
            await foreach (var result in base.ValidateMessageCoreAsync(message, cancellationToken))
            {
                if (result != ValidationResult.Success)
                {
                    hasErrors = true;
                    yield return result;
                }
            }

            // Facebook-specific validations (except recipient which we handle in core method)
            if (message.Content?.ContentType == MessageContentType.PlainText && message.Content is ITextContent textContent)
            {
                if (!string.IsNullOrEmpty(textContent.Text) && textContent.Text.Length > 2000)
                {
                    hasErrors = true;
                    yield return new ValidationResult("Message text exceeds Facebook's 2000 character limit");
                }
            }

            // If no validation errors were found, yield success
            if (!hasErrors)
            {
                yield return ValidationResult.Success!;
            }
        }
    }
}