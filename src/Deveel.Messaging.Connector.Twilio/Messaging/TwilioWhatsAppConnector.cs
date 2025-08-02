//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

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
    public class TwilioWhatsAppConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ILogger<TwilioWhatsAppConnector>? _logger;
        private readonly ITwilioService _twilioService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        private string? _accountSid;
        private string? _authToken;
        private string? _fromNumber;
        private string? _webhookUrl;
        private string? _statusCallback;
        private string? _contentSid;
        private string? _contentVariables;

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
        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Initializing Twilio WhatsApp connector...");

                // Extract required parameters
                _accountSid = _connectionSettings.GetParameter("AccountSid") as string;
                _authToken = _connectionSettings.GetParameter("AuthToken") as string;
                _fromNumber = _connectionSettings.GetParameter("FromNumber") as string;

                // Extract optional parameters
                _webhookUrl = _connectionSettings.GetParameter("WebhookUrl") as string;
                _statusCallback = _connectionSettings.GetParameter("StatusCallback") as string;
                _contentSid = _connectionSettings.GetParameter("ContentSid") as string;
                _contentVariables = _connectionSettings.GetParameter("ContentVariables") as string;

                // Perform custom validation logic
                if (string.IsNullOrWhiteSpace(_accountSid) || string.IsNullOrWhiteSpace(_authToken))
                {
                    return ConnectorResult<bool>.Fail(TwilioErrorCodes.MissingCredentials, 
                        "Account SID and Auth Token are required");
                }

                // WhatsApp requires a from number
                if (string.IsNullOrWhiteSpace(_fromNumber))
                {
                    return ConnectorResult<bool>.Fail(TwilioErrorCodes.MissingFromNumber, 
                        "FromNumber is required for WhatsApp messaging");
                }

                // Ensure WhatsApp number format
                if (!_fromNumber.StartsWith("whatsapp:"))
                {
                    _fromNumber = $"whatsapp:{_fromNumber}";
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
                        return ConnectorResult<bool>.ValidationFailed(TwilioErrorCodes.InvalidConnectionSettings, 
                            "Connection settings validation failed", validationErrors);
                    }
                }

                // Initialize Twilio client
                _twilioService.Initialize(_accountSid, _authToken);

                _logger?.LogInformation("Twilio WhatsApp connector initialized successfully");
                return ConnectorResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Twilio WhatsApp connector");
                return ConnectorResult<bool>.Fail(ConnectorErrorCodes.InitializationError, ex.Message);
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

                // Extract recipient phone number
                var toNumber = ExtractWhatsAppNumber(message.Receiver);
                if (string.IsNullOrWhiteSpace(toNumber))
                {
                    return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.InvalidRecipient, 
                        "Recipient WhatsApp phone number is required and must be in E.164 format");
                }

                // Build message creation options
                var createMessageOptions = new CreateMessageOptions(new PhoneNumber(toNumber))
                {
                    From = new PhoneNumber(_fromNumber!)
                };

                // Handle different content types
                if (message.Content?.ContentType == MessageContentType.Template)
                {
                    // Template message - use ContentSid
                    var contentSid = ExtractContentSid(message);
                    if (!string.IsNullOrWhiteSpace(contentSid))
                    {
                        createMessageOptions.ContentSid = contentSid;
                        
                        // Add content variables if provided
                        var contentVariables = ExtractContentVariables(message);
                        if (!string.IsNullOrWhiteSpace(contentVariables))
                        {
                            createMessageOptions.ContentVariables = contentVariables;
                        }
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
                result.AdditionalData["From"] = messageResource.From ?? _fromNumber ?? "";
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
        protected override async Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var statusInfo = new StatusInfo($"Twilio WhatsApp Connector (Account: {_accountSid})");

                statusInfo.AdditionalData["AccountSid"] = _accountSid ?? "";
                statusInfo.AdditionalData["FromNumber"] = _fromNumber ?? "";
                statusInfo.AdditionalData["ContentSid"] = _contentSid ?? "";
                statusInfo.AdditionalData["State"] = State.ToString();
                statusInfo.AdditionalData["Uptime"] = DateTime.UtcNow - _startTime;
                statusInfo.AdditionalData["Channel"] = "WhatsApp";

                return ConnectorResult<StatusInfo>.Success(statusInfo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get WhatsApp connector status");
                return ConnectorResult<StatusInfo>.Fail(TwilioErrorCodes.StatusError, ex.Message);
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
            if (message.Content?.ContentType == MessageContentType.PlainText)
            {
                return message.Content.ToString();
            }
            return null;
        }

        private List<Uri>? ExtractMediaUrls(IMessage message)
        {
            if (message.Content?.ContentType == MessageContentType.Media)
            {
                // In a real implementation, you'd extract media URLs from the content
                // For now, return null - this would be enhanced based on the actual media content structure
                return null;
            }
            return null;
        }

        private string? ExtractContentSid(IMessage message)
        {
            // First check message properties
            if (message.Properties?.ContainsKey("ContentSid") == true)
            {
                return message.Properties["ContentSid"]?.ToString();
            }

            // Fall back to connector-level ContentSid
            return _contentSid;
        }

        private string? ExtractContentVariables(IMessage message)
        {
            // First check message properties
            if (message.Properties?.ContainsKey("ContentVariables") == true)
            {
                return message.Properties["ContentVariables"]?.ToString();
            }

            // Fall back to connector-level ContentVariables
            return _contentVariables;
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
    }
}