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
    /// A channel connector that implements SMS messaging using the Twilio API.
    /// </summary>
    /// <remarks>
    /// This connector provides comprehensive support for Twilio SMS capabilities including
    /// sending messages, querying message status, health monitoring, and webhook support
    /// for receiving messages and status updates.
    /// </remarks>
    [ChannelSchema(typeof(TwilioSmsSchemaFactory))]
	public class TwilioSmsConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ILogger<TwilioSmsConnector>? _logger;
        private readonly ITwilioService _twilioService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        private string? _accountSid;
        private string? _authToken;
        private string? _webhookUrl;
        private string? _statusCallback;
        private int? _validityPeriod;
        private decimal? _maxPrice;
        private string? _messagingServiceSid;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioSmsConnector"/> class.
        /// </summary>
        /// <param name="schema">The channel schema that defines the connector's capabilities and configuration.</param>
        /// <param name="connectionSettings">The connection settings containing Twilio credentials and configuration.</param>
        /// <param name="twilioService">The Twilio service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when schema or connectionSettings is null.</exception>
        public TwilioSmsConnector(IChannelSchema schema, ConnectionSettings connectionSettings, ITwilioService? twilioService = null, ILogger<TwilioSmsConnector>? logger = null)
            : base(schema)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _twilioService = twilioService ?? new TwilioService();
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioSmsConnector"/> class using one of the predefined schemas.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing Twilio credentials and configuration.</param>
        /// <param name="twilioService">The Twilio service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
        public TwilioSmsConnector(ConnectionSettings connectionSettings, ITwilioService? twilioService = null, ILogger<TwilioSmsConnector>? logger = null)
            : this(TwilioChannelSchemas.TwilioSms, connectionSettings, twilioService, logger)
        {
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Initializing Twilio SMS connector...");

                // Extract required parameters first - use nullable versions to avoid exceptions
                _accountSid = _connectionSettings.GetParameter("AccountSid") as string;
                _authToken = _connectionSettings.GetParameter("AuthToken") as string;

                // Extract optional parameters
                _webhookUrl = _connectionSettings.GetParameter("WebhookUrl") as string;
                _statusCallback = _connectionSettings.GetParameter("StatusCallback") as string;
                _validityPeriod = _connectionSettings.GetParameter("ValidityPeriod") as int?;
                _maxPrice = _connectionSettings.GetParameter("MaxPrice") as decimal?;
                _messagingServiceSid = _connectionSettings.GetParameter("MessagingServiceSid") as string;

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

                _logger?.LogInformation("Twilio SMS connector initialized successfully");
                return ConnectorResult<bool>.SuccessTask(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Twilio SMS connector");
                return ConnectorResult<bool>.FailTask(ConnectorErrorCodes.InitializationError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Testing Twilio connection...");

                // Test connection by fetching account information
                var account = await _twilioService.FetchAccountAsync(_accountSid!, cancellationToken);
                
                if (account == null)
                {
                    return ConnectorResult<bool>.Fail(TwilioErrorCodes.ConnectionFailed, 
                        "Unable to retrieve account information");
                }

                _logger?.LogDebug("Connection test successful. Account: {AccountFriendlyName}", account.FriendlyName);
                return ConnectorResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Connection test failed");
                return ConnectorResult<bool>.Fail(TwilioErrorCodes.ConnectionTestFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Sending SMS message {MessageId}", message.Id);

                // Extract and validate message properties before processing
                // var messageProperties = ExtractMessageProperties(message);

                // Validate message properties against schema (includes all validation through MessagePropertyConfiguration)
                    var validationResults = Schema.ValidateMessage(message);
                    var validationErrors = validationResults.ToList();
                    if (validationErrors.Count > 0)
                    {
                        _logger?.LogError("Message properties validation failed: {Errors}", 
                            string.Join(", ", validationErrors.Select(e => e.ErrorMessage)));
                        return ConnectorResult<SendResult>.ValidationFailed(TwilioErrorCodes.InvalidMessage, 
                            "Message properties validation failed", validationErrors);
                    }

                // Extract sender phone number from message.Sender
                var senderNumber = ExtractPhoneNumber(message.Sender);
                if (string.IsNullOrWhiteSpace(senderNumber) && string.IsNullOrWhiteSpace(_messagingServiceSid))
                {
                    return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.MissingFromNumber, 
                        "Sender phone number is required when MessagingServiceSid is not configured");
                }

                // Extract recipient phone number
                var toNumber = ExtractPhoneNumber(message.Receiver);
                if (string.IsNullOrWhiteSpace(toNumber))
                {
                    return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.InvalidRecipient, 
                        "Recipient phone number is required and must be in E.164 format");
                }

                // Extract message body
                var messageBody = ExtractMessageBody(message);

                // Build message creation options
                var createMessageOptions = new CreateMessageOptions(new PhoneNumber(toNumber));

                // Set sender (Sender phone number or MessagingServiceSid)
                if (!string.IsNullOrWhiteSpace(_messagingServiceSid))
                {
                    createMessageOptions.MessagingServiceSid = _messagingServiceSid;
                }
                else if (!string.IsNullOrWhiteSpace(senderNumber))
                {
                    createMessageOptions.From = new PhoneNumber(senderNumber);
                }

                // Set message content
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

                // Apply message-specific or connector-level settings
                ApplyMessageSettings(createMessageOptions, message);

                // Send the message
                var messageResource = await _twilioService.CreateMessageAsync(createMessageOptions, cancellationToken);

                _logger?.LogInformation("SMS message sent successfully. MessageSid: {MessageSid}, Status: {Status}", 
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

                if (!string.IsNullOrWhiteSpace(messageResource.Price))
                {
                    result.AdditionalData["Price"] = messageResource.Price;
                    result.AdditionalData["PriceUnit"] = messageResource.PriceUnit ?? "USD";
                }

                return ConnectorResult<SendResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send SMS message {MessageId}", message.Id);
                return ConnectorResult<SendResult>.Fail(TwilioErrorCodes.SendMessageFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusCoreAsync(string messageId, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Querying status for message {MessageId}", messageId);

                // Assume messageId is the Twilio SID
                var messageResource = await _twilioService.FetchMessageAsync(messageId, cancellationToken);
                var timestamp = messageResource.DateUpdated ?? messageResource.DateCreated ?? DateTime.UtcNow;
                var status = MapTwilioStatusToMessageStatus(messageResource.Status);

				var statusUpdate = new StatusUpdateResult(messageId, status, timestamp);

                statusUpdate.AdditionalData["TwilioStatus"] = messageResource.Status.ToString();
                statusUpdate.AdditionalData["ErrorCode"] = messageResource.ErrorCode ?? 0;
                statusUpdate.AdditionalData["ErrorMessage"] = messageResource.ErrorMessage ?? "";
                statusUpdate.AdditionalData["NumSegments"] = messageResource.NumSegments ?? "0";

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
                _logger?.LogError(ex, "Failed to query message status for {MessageId}", messageId);
                return ConnectorResult<StatusUpdatesResult>.Fail(TwilioErrorCodes.StatusQueryFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var statusInfo = new StatusInfo($"Twilio SMS Connector (Account: {_accountSid})");

                statusInfo.AdditionalData["AccountSid"] = _accountSid ?? "";
                statusInfo.AdditionalData["MessagingServiceSid"] = _messagingServiceSid ?? "";
                statusInfo.AdditionalData["State"] = State.ToString();
                statusInfo.AdditionalData["Uptime"] = DateTime.UtcNow - _startTime;

                return ConnectorResult<StatusInfo>.SuccessTask(statusInfo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get connector status");
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

        private string? ExtractPhoneNumber(IEndpoint? endpoint)
        {
            if (endpoint?.Type == EndpointType.PhoneNumber)
            {
                return endpoint.Address;
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

        private Dictionary<string, object?> ExtractMessageProperties(IMessage message)
        {
            var properties = new Dictionary<string, object?>();

            // Note: Sender and To are now handled as endpoints (message.Sender/message.Receiver)
            // not as message properties, so we don't extract them here
            // Body and MediaUrl are also extracted from content, not properties

            // Add properties from message.Properties if they exist
            if (message.Properties != null)
            {
                foreach (var property in message.Properties)
                {
                    properties[property.Key] = property.Value.Value;
                }
            }

            return properties;
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // E.164 format validation: should start with + followed by 1-15 digits
            // This is a basic validation - in a real implementation you might want to use a more sophisticated library
            var e164Pattern = @"^\+[1-9]\d{1,14}$";
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, e164Pattern);
        }

        private void ApplyMessageSettings(CreateMessageOptions options, IMessage message)
        {
            // Apply validity period
            if (_validityPeriod.HasValue)
            {
                options.ValidityPeriod = _validityPeriod.Value;
            }

            // Apply max price
            if (_maxPrice.HasValue)
            {
                options.MaxPrice = _maxPrice.Value;
            }

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
                        case "validityperiod":
                            if (int.TryParse(property.Value?.Value?.ToString(), out var validityPeriod))
                            {
                                options.ValidityPeriod = validityPeriod;
                            }
                            break;
                        case "maxprice":
                            if (decimal.TryParse(property.Value?.Value?.ToString(), out var maxPrice))
                            {
                                options.MaxPrice = maxPrice;
                            }
                            break;
                        case "providecallback":
                            if (bool.TryParse(property.Value?.Value?.ToString(), out var provideCallback) && 
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