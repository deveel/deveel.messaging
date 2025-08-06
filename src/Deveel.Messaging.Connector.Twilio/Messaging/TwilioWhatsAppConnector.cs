//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System;

namespace Deveel.Messaging
{
    /// <summary>
    /// A channel connector that implements WhatsApp Business messaging using the Twilio API.
    /// </summary>
    /// <remarks>
    /// This connector provides comprehensive support for Twilio WhatsApp Business API capabilities including
    /// sending messages, querying message status, template messaging, media attachments, health monitoring, 
    /// and webhook support for receiving messages and status updates.
    /// </remarks>
    [ChannelSchema(typeof(TwilioWhatsAppSchemaFactory))]
	public class TwilioWhatsAppConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ILogger<TwilioWhatsAppConnector>? _logger;
        private readonly ITwilioService _twilioService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        private string? _accountSid;
        private string? _authToken;
        private string? _webhookUrl;
        private string? _statusCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioWhatsAppConnector"/> class.
        /// </summary>
        /// <param name="schema">The channel schema that defines the connector's capabilities and configuration.</param>
        /// <param name="connectionSettings">The connection settings containing Twilio credentials and configuration.</param>
        /// <param name="twilioService">The Twilio service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when schema or connectionSettings is null.</exception>
        public TwilioWhatsAppConnector(IChannelSchema schema, ConnectionSettings connectionSettings, ITwilioService? twilioService = null, ILogger<TwilioWhatsAppConnector>? logger = null)
            : base(schema)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _twilioService = twilioService ?? new TwilioService();
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioWhatsAppConnector"/> class using the default WhatsApp schema.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing Twilio credentials and configuration.</param>
        /// <param name="twilioService">The Twilio service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
        public TwilioWhatsAppConnector(ConnectionSettings connectionSettings, ITwilioService? twilioService = null, ILogger<TwilioWhatsAppConnector>? logger = null)
            : this(TwilioChannelSchemas.TwilioWhatsApp, connectionSettings, twilioService, logger)
        {
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Initializing Twilio WhatsApp connector...");

                // Extract required parameters
                _accountSid = _connectionSettings.GetParameter("AccountSid") as string;
                _authToken = _connectionSettings.GetParameter("AuthToken") as string;

                // Extract optional parameters
                _webhookUrl = _connectionSettings.GetParameter("WebhookUrl") as string;
                _statusCallback = _connectionSettings.GetParameter("StatusCallback") as string;
                // ContentSid and ContentVariables are now extracted from ITemplateContent, not connection parameters

                // Perform custom validation logic
                if (string.IsNullOrWhiteSpace(_accountSid) || string.IsNullOrWhiteSpace(_authToken))
                {
                    return ConnectorResult<bool>.FailTask(TwilioErrorCodes.MissingCredentials, 
                        "Account SID and Auth Token are required");
                }

                // Validate connection settings against schema
                if (Schema is ChannelSchema channelSchema)
                {
                    var validationResults = channelSchema.ValidateConnectionSettings(_connectionSettings);
                    var validationErrors = validationResults.ToList();
                    if (validationErrors.Count > 0)
                    {
                        _logger?.LogError("Connection settings validation failed: {Errors}", 
                            string.Join(", ", validationErrors.Select(e => e.ErrorMessage)));
                        return ConnectorResult<bool>.ValidationFailedTask(TwilioErrorCodes.InvalidConnectionSettings, 
                            "Connection settings validation failed", validationErrors);
                    }
                }

                // Initialize Twilio client
                _twilioService.Initialize(_accountSid, _authToken);

                _logger?.LogInformation("Twilio WhatsApp connector initialized successfully");
                return ConnectorResult<bool>.SuccessTask(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Twilio WhatsApp connector");
                return ConnectorResult<bool>.FailTask(ConnectorErrorCodes.InitializationError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Testing Twilio WhatsApp connection...");

                // Test connection by fetching account information
                var account = await _twilioService.FetchAccountAsync(_accountSid!, cancellationToken);
                
                if (account == null)
                {
                    return ConnectorResult<bool>.Fail(TwilioErrorCodes.ConnectionFailed, 
                        "Unable to retrieve account information");
                }

                _logger?.LogDebug("WhatsApp connection test successful. Account: {AccountFriendlyName}", account.FriendlyName);
                return ConnectorResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "WhatsApp connection test failed");
                return ConnectorResult<bool>.Fail(TwilioErrorCodes.ConnectionTestFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Sending WhatsApp message {MessageId}", message.Id);

                // Validate message properties against schema (includes all validation through MessagePropertyConfiguration)
                    var validationResults = Schema.ValidateMessage(message);
                    var validationErrors = validationResults.ToList();
                    if (validationErrors.Count > 0)
                    {
                        _logger?.LogError("WhatsApp message properties validation failed: {Errors}", 
                            string.Join(", ", validationErrors.Select(e => e.ErrorMessage)));
                        return ConnectorResult<SendResult>.ValidationFailed(TwilioErrorCodes.InvalidMessage, 
                            "WhatsApp message properties validation failed", validationErrors);
                    }

                // Extract sender WhatsApp number from message.Sender
                var senderNumber = ExtractWhatsAppNumber(message.Sender);
                if (string.IsNullOrWhiteSpace(senderNumber))
                {
                    return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.MissingFromNumber, 
                        "Sender WhatsApp phone number is required and must be in format 'whatsapp:+1234567890'");
                }

                // Extract recipient phone number
                var toNumber = ExtractWhatsAppNumber(message.Receiver);
                if (string.IsNullOrWhiteSpace(toNumber))
                {
                    return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.InvalidRecipient, 
                        "Recipient WhatsApp phone number is required and must be in format 'whatsapp:+1234567890'");
                }

                // Build message creation options
                var createMessageOptions = new CreateMessageOptions(new PhoneNumber(toNumber))
                {
                    From = new PhoneNumber(senderNumber)
                };

                // Handle different content types
                if (message.Content?.ContentType == MessageContentType.Template && message.Content is ITemplateContent templateContent)
                {
                    // Template message - use ContentSid from TemplateId and ContentVariables from Parameters
                    var contentSid = templateContent.TemplateId;
                    if (!string.IsNullOrWhiteSpace(contentSid))
                    {
                        createMessageOptions.ContentSid = contentSid;
                        
                        // Convert template parameters to JSON string for ContentVariables
                        if (templateContent.Parameters != null && templateContent.Parameters.Count > 0)
                        {
                            try
                            {
                                var contentVariables = System.Text.Json.JsonSerializer.Serialize(templateContent.Parameters);
                                createMessageOptions.ContentVariables = contentVariables;
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Failed to serialize template parameters to JSON for message {MessageId}", message.Id);
                            }
                        }
                    }
                    else
                    {
                        return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.InvalidMessage, 
                            "Template content must have a valid TemplateId (ContentSid) for WhatsApp template messages");
                    }
                }
                else
                {
                    // Regular message - extract body and media
                    var messageBody = ExtractMessageBody(message);
                    if (!string.IsNullOrWhiteSpace(messageBody))
                    {
                        createMessageOptions.Body = messageBody;
                    }

                    // Add media URLs if present
                    var mediaUrls = ExtractMediaUrls(message);
                    if (mediaUrls?.Count > 0)
                    {
                        createMessageOptions.MediaUrl = mediaUrls;
                    }
                }

                // Apply message-specific settings
                ApplyMessageSettings(createMessageOptions, message);

                // Send the message
                var messageResource = await _twilioService.CreateMessageAsync(createMessageOptions, cancellationToken);

                _logger?.LogInformation("WhatsApp message sent successfully. MessageSid: {MessageSid}, Status: {Status}", 
                    messageResource.Sid, messageResource.Status);

                var result = new SendResult(message.Id, messageResource.Sid)
                {
                    Status = MapTwilioStatusToMessageStatus(messageResource.Status),
                    Timestamp = messageResource.DateCreated ?? DateTime.UtcNow
                };

                // Add properties
                result.AdditionalData["TwilioSid"] = messageResource.Sid;
                result.AdditionalData["TwilioStatus"] = messageResource.Status.ToString();
                result.AdditionalData["To"] = messageResource.To;
                result.AdditionalData["From"] = messageResource.From ?? senderNumber ?? "";
                result.AdditionalData["NumSegments"] = messageResource.NumSegments ?? "0";
                result.AdditionalData["Channel"] = "WhatsApp";

                if (!string.IsNullOrWhiteSpace(messageResource.Price))
                {
                    result.AdditionalData["Price"] = messageResource.Price;
                    result.AdditionalData["PriceUnit"] = messageResource.PriceUnit ?? "USD";
                }

                return ConnectorResult<SendResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send WhatsApp message {MessageId}", message.Id);
                return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.SendMessageFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusCoreAsync(string messageId, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Querying WhatsApp message status for {MessageId}", messageId);

                // Assume messageId is the Twilio SID
                var messageResource = await _twilioService.FetchMessageAsync(messageId, cancellationToken);
                var timestamp = messageResource.DateUpdated ?? messageResource.DateCreated ?? DateTime.UtcNow;
                var status = MapTwilioStatusToMessageStatus(messageResource.Status);

                var statusUpdate = new StatusUpdateResult(messageId, status, timestamp);

                statusUpdate.AdditionalData["TwilioStatus"] = messageResource.Status.ToString();
                statusUpdate.AdditionalData["ErrorCode"] = messageResource.ErrorCode ?? 0;
                statusUpdate.AdditionalData["ErrorMessage"] = messageResource.ErrorMessage ?? "";
                statusUpdate.AdditionalData["NumSegments"] = messageResource.NumSegments ?? "0";
                statusUpdate.AdditionalData["Channel"] = "WhatsApp";

                if (!string.IsNullOrWhiteSpace(messageResource.Price))
                {
                    statusUpdate.AdditionalData["Price"] = messageResource.Price;
                    statusUpdate.AdditionalData["PriceUnit"] = messageResource.PriceUnit ?? "USD";
                }

                var result = new StatusUpdatesResult(messageId, new[] { statusUpdate });
                return ConnectorResult<StatusUpdatesResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to query WhatsApp message status for {MessageId}", messageId);
                return ConnectorResult<StatusUpdatesResult>.Fail(TwilioErrorCodes.StatusQueryFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var statusInfo = new StatusInfo($"Twilio WhatsApp Connector (Account: {_accountSid})");

                statusInfo.AdditionalData["AccountSid"] = _accountSid ?? "";
                // ContentSid is now extracted from template content, not stored as connection setting
                statusInfo.AdditionalData["State"] = State.ToString();
                statusInfo.AdditionalData["Uptime"] = DateTime.UtcNow - _startTime;
                statusInfo.AdditionalData["Channel"] = "WhatsApp";

                return ConnectorResult<StatusInfo>.SuccessTask(statusInfo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get WhatsApp connector status");
                return ConnectorResult<StatusInfo>.FailTask(TwilioErrorCodes.StatusError, ex.Message);
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

            if (State == ConnectorState.Ready)
            {
                try
                {
                    // Test connectivity by fetching account info
                    var testResult = await TestConnectorConnectionAsync(cancellationToken);
                    if (!testResult.Successful)
                    {
                        health.IsHealthy = false;
                        health.Issues.Add($"Connection test failed: {testResult.Error?.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    health.IsHealthy = false;
                    health.Issues.Add($"Health check failed: {ex.Message}");
                }
            }
            else
            {
                health.Issues.Add($"Connector is in {State} state");
            }

            return ConnectorResult<ConnectorHealth>.Success(health);
        }

        private string? ExtractWhatsAppNumber(IEndpoint? endpoint)
        {
            if (endpoint?.Type == EndpointType.PhoneNumber)
            {
                var number = endpoint.Address;
                if (!string.IsNullOrWhiteSpace(number))
                {
                    // Ensure WhatsApp format
                    return number.StartsWith("whatsapp:") ? number : $"whatsapp:{number}";
                }
            }
            return null;
        }

        private string? ExtractMessageBody(IMessage message)
        {
            if (message.Content?.ContentType == MessageContentType.PlainText && message.Content is ITextContent textContent)
            {
                return textContent.Text;
            }
            return null;
        }

        private List<Uri>? ExtractMediaUrls(IMessage message)
        {
            if (message.Content?.ContentType == MessageContentType.Media && message.Content is IMediaContent mediaContent)
            {
                if (!string.IsNullOrWhiteSpace(mediaContent.FileUrl))
                {
                    try
                    {
                        return new List<Uri> { new Uri(mediaContent.FileUrl) };
                    }
                    catch (UriFormatException ex)
                    {
                        _logger?.LogWarning(ex, "Invalid media URL format: {MediaUrl}", mediaContent.FileUrl);
                        return null;
                    }
                }
            }
            return null;
        }

        private void ApplyMessageSettings(CreateMessageOptions options, IMessage message)
        {
            // Apply status callback
            if (!string.IsNullOrWhiteSpace(_statusCallback))
            {
                options.StatusCallback = new Uri(_statusCallback);
            }

            // Apply message-specific properties if available
            if (message.Properties != null)
            {
                foreach (var property in message.Properties)
                {
                    switch (property.Key.ToLowerInvariant())
                    {
                        case "providecallback":
                            if (bool.TryParse(property.Value?.ToString(), out var provideCallback) && 
                                provideCallback && !string.IsNullOrWhiteSpace(_statusCallback))
                            {
                                options.StatusCallback = new Uri(_statusCallback);
                            }
                            break;
                    }
                }
            }
        }

        private MessageStatus MapTwilioStatusToMessageStatus(MessageResource.StatusEnum twilioStatus)
        {
            if (twilioStatus == MessageResource.StatusEnum.Accepted || twilioStatus == MessageResource.StatusEnum.Queued)
                return MessageStatus.Queued;
            if (twilioStatus == MessageResource.StatusEnum.Sending || twilioStatus == MessageResource.StatusEnum.Sent)
                return MessageStatus.Sent;
            if (twilioStatus == MessageResource.StatusEnum.Delivered)
                return MessageStatus.Delivered;
            if (twilioStatus == MessageResource.StatusEnum.Undelivered || twilioStatus == MessageResource.StatusEnum.Failed)
                return MessageStatus.DeliveryFailed;
            if (twilioStatus == MessageResource.StatusEnum.Received)
                return MessageStatus.Received;
            
            return MessageStatus.Unknown;
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Receiving WhatsApp message from Twilio webhook");

                if (source.ContentType == MessageSource.UrlPostContentType)
                {
                    var formData = source.AsUrlPostData();
                    var messages = ParseTwilioWebhookFormData(formData);
                    
                    if (messages.Count == 0)
                    {
                        return ConnectorResult<ReceiveResult>.FailTask(TwilioErrorCodes.InvalidWebhookData, 
                            "No valid messages found in webhook data");
                    }

                    var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                    return ConnectorResult<ReceiveResult>.SuccessTask(result);
                }
                else if (source.ContentType == MessageSource.JsonContentType)
                {
                    var messages = ParseTwilioWebhookJson(source);
                    
                    if (messages.Count == 0)
                    {
                        return ConnectorResult<ReceiveResult>.FailTask(TwilioErrorCodes.InvalidWebhookData, 
                            "No valid messages found in webhook JSON");
                    }

                    var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                    return ConnectorResult<ReceiveResult>.SuccessTask(result);
                }

                return ConnectorResult<ReceiveResult>.FailTask(TwilioErrorCodes.UnsupportedContentType, 
                    "Only form data and JSON are supported for Twilio WhatsApp message receiving");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to receive WhatsApp message from Twilio webhook");
                return ConnectorResult<ReceiveResult>.FailTask(TwilioErrorCodes.ReceiveMessageFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Receiving WhatsApp message status update from Twilio webhook");

                if (source.ContentType == MessageSource.UrlPostContentType)
                {
                    var formData = source.AsUrlPostData();
                    var statusResult = ParseTwilioStatusCallbackFormData(formData);
                    
                    return ConnectorResult<StatusUpdateResult>.SuccessTask(statusResult);
                }
                else if (source.ContentType == MessageSource.JsonContentType)
                {
                    var statusResult = ParseTwilioStatusCallbackJson(source);
                    return ConnectorResult<StatusUpdateResult>.SuccessTask(statusResult);
                }

                return ConnectorResult<StatusUpdateResult>.FailTask(TwilioErrorCodes.UnsupportedContentType, 
                    "Only form data and JSON are supported for Twilio WhatsApp status callbacks");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to receive WhatsApp message status from Twilio webhook");
                return ConnectorResult<StatusUpdateResult>.FailTask(TwilioErrorCodes.ReceiveStatusFailed, ex.Message);
            }
        }

        private List<IMessage> ParseTwilioWebhookFormData(IDictionary<string, string> formData)
        {
            var messages = new List<IMessage>();

            // Validate required fields for Twilio WhatsApp webhook
            if (!formData.TryGetValue("MessageSid", out var messageSid) || string.IsNullOrEmpty(messageSid))
            {
                throw new ArgumentException("MessageSid is required for Twilio WhatsApp webhooks");
            }

            if (!formData.TryGetValue("From", out var from) || string.IsNullOrEmpty(from))
            {
                throw new ArgumentException("From field is required for Twilio WhatsApp webhooks");
            }

            if (!formData.TryGetValue("To", out var to) || string.IsNullOrEmpty(to))
            {
                throw new ArgumentException("To field is required for Twilio WhatsApp webhooks");
            }

            // Body is optional for WhatsApp messages (e.g., button responses, media messages)
            var body = formData.TryGetValue("Body", out var bodyValue) ? bodyValue : "";

            // Create the message
            var message = new Message
            {
                Id = messageSid,
                Sender = new Endpoint(GetTwilioEndpointType(from), from),
                Receiver = new Endpoint(GetTwilioEndpointType(to), to),
                Content = new TextContent(body),
                Properties = new Dictionary<string, MessageProperty>()
            };

            // Add all other Twilio webhook fields as message properties
            foreach (var kvp in formData)
            {
                if (kvp.Key != "MessageSid" && kvp.Key != "From" && kvp.Key != "To" && kvp.Key != "Body")
                {
                    message.Properties[kvp.Key] = new MessageProperty(kvp.Key, kvp.Value);
                }
            }

            messages.Add(message);
            return messages;
        }

        private List<IMessage> ParseTwilioWebhookJson(MessageSource source)
        {
            var messages = new List<IMessage>();
            var jsonData = source.AsJson<System.Text.Json.JsonElement>();

            if (jsonData.TryGetProperty("Messages", out var messagesArray))
            {
                // Batch messages
                foreach (var messageElement in messagesArray.EnumerateArray())
                {
                    var message = ParseTwilioJsonMessage(messageElement);
                    if (message != null)
                        messages.Add(message);
                }
            }
            else
            {
                // Single message
                var message = ParseTwilioJsonMessage(jsonData);
                if (message != null)
                    messages.Add(message);
            }

            return messages;
        }

        private IMessage? ParseTwilioJsonMessage(System.Text.Json.JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("MessageSid", out var sidProperty))
                return null;

            var messageSid = sidProperty.GetString();
            if (string.IsNullOrEmpty(messageSid))
                return null;

            var from = jsonData.TryGetProperty("From", out var fromProp) ? fromProp.GetString() ?? "" : "";
            var to = jsonData.TryGetProperty("To", out var toProp) ? toProp.GetString() ?? "" : "";
            var body = jsonData.TryGetProperty("Body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return null;

            return new Message
            {
                Id = messageSid,
                Sender = new Endpoint(GetTwilioEndpointType(from), from),
                Receiver = new Endpoint(GetTwilioEndpointType(to), to),
                Content = new TextContent(body)
            };
        }

        private StatusUpdateResult ParseTwilioStatusCallbackFormData(IDictionary<string, string> formData)
        {
            var messageId = formData.TryGetValue("MessageSid", out var sid) ? sid : "unknown";
            var statusString = formData.TryGetValue("MessageStatus", out var status) ? status : "unknown";
            
            var messageStatus = MapTwilioStatusStringToMessageStatus(statusString);
            var statusResult = new StatusUpdateResult(messageId, messageStatus);

            // Add additional Twilio status data
            if (formData.TryGetValue("MessagePrice", out var price))
                statusResult.AdditionalData["MessagePrice"] = price;
            if (formData.TryGetValue("MessagePriceUnit", out var priceUnit))
                statusResult.AdditionalData["MessagePriceUnit"] = priceUnit;
            if (formData.TryGetValue("ErrorCode", out var errorCode))
                statusResult.AdditionalData["ErrorCode"] = errorCode;
            if (formData.TryGetValue("ErrorMessage", out var errorMessage))
                statusResult.AdditionalData["ErrorMessage"] = errorMessage;
            if (formData.TryGetValue("To", out var to))
                statusResult.AdditionalData["To"] = to;
            if (formData.TryGetValue("From", out var from))
                statusResult.AdditionalData["From"] = from;
            if (formData.TryGetValue("AccountSid", out var accountSid))
                statusResult.AdditionalData["AccountSid"] = accountSid;
            
            // Add WhatsApp-specific fields
            if (formData.TryGetValue("ProfileName", out var profileName))
                statusResult.AdditionalData["ProfileName"] = profileName;
            if (formData.TryGetValue("ButtonText", out var buttonText))
                statusResult.AdditionalData["ButtonText"] = buttonText;
            if (formData.TryGetValue("ButtonPayload", out var buttonPayload))
                statusResult.AdditionalData["ButtonPayload"] = buttonPayload;
            
            // Mark as WhatsApp channel
            statusResult.AdditionalData["Channel"] = "WhatsApp";

            return statusResult;
        }

        private StatusUpdateResult ParseTwilioStatusCallbackJson(MessageSource source)
        {
            var jsonData = source.AsJson<System.Text.Json.JsonElement>();
            
            var messageId = jsonData.TryGetProperty("MessageSid", out var sidProp) ? sidProp.GetString() ?? "unknown" : "unknown";
            var statusString = jsonData.TryGetProperty("MessageStatus", out var statusProp) ? statusProp.GetString() ?? "unknown" : "unknown";
            
            var messageStatus = MapTwilioStatusStringToMessageStatus(statusString);
            var statusResult = new StatusUpdateResult(messageId, messageStatus);

            // Add additional JSON properties as additional data
            foreach (var property in jsonData.EnumerateObject())
            {
                if (property.Name != "MessageSid" && property.Name != "MessageStatus")
                {
                    statusResult.AdditionalData[property.Name] = property.Value.GetString() ?? "";
                }
            }

            // Mark as WhatsApp channel
            statusResult.AdditionalData["Channel"] = "WhatsApp";

            return statusResult;
        }

        private MessageStatus MapTwilioStatusStringToMessageStatus(string statusString)
        {
            return statusString.ToLowerInvariant() switch
            {
                "delivered" => MessageStatus.Delivered,
                "sent" => MessageStatus.Sent,
                "failed" => MessageStatus.DeliveryFailed,
                "undelivered" => MessageStatus.DeliveryFailed,
                "received" => MessageStatus.Received,
                "queued" => MessageStatus.Queued,
                "accepted" => MessageStatus.Queued,
                "sending" => MessageStatus.Sent,
                "read" => MessageStatus.Delivered, // WhatsApp-specific read status
                _ => MessageStatus.Unknown
            };
        }

        private static EndpointType GetTwilioEndpointType(string address)
        {
            if (string.IsNullOrEmpty(address))
                return EndpointType.Id;

            if (address.StartsWith("whatsapp:"))
                return EndpointType.PhoneNumber;

            if (address.StartsWith("+"))
                return EndpointType.PhoneNumber;

            if (address.Contains("@"))
                return EndpointType.EmailAddress;

            return EndpointType.Id;
        }
    }
}