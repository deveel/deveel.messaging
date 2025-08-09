//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System.Net;
using System;
using System.Text.Json;

namespace Deveel.Messaging
{
    /// <summary>
    /// A channel connector that implements email messaging using the SendGrid API.
    /// </summary>
    /// <remarks>
    /// This connector provides comprehensive support for SendGrid email capabilities including
    /// sending emails, querying message status, health monitoring, and webhook support
    /// for receiving email events and status updates.
    /// </remarks>
    public class SendGridEmailConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ISendGridService _sendGridService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        private string? _apiKey;
        private bool _sandboxMode;
        private string? _webhookUrl;
        private bool _trackingSettings;
        private string? _defaultFromName;
        private string? _defaultReplyTo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailConnector"/> class.
        /// </summary>
        /// <param name="schema">The channel schema that defines the connector's capabilities and configuration.</param>
        /// <param name="connectionSettings">The connection settings containing SendGrid credentials and configuration.</param>
        /// <param name="sendGridService">The SendGrid service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when schema or connectionSettings is null.</exception>
        public SendGridEmailConnector(IChannelSchema schema, ConnectionSettings connectionSettings, ISendGridService? sendGridService = null, ILogger<SendGridEmailConnector>? logger = null)
            : base(schema, logger)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _sendGridService = sendGridService ?? new SendGridService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailConnector"/> class using one of the predefined schemas.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing SendGrid credentials and configuration.</param>
        /// <param name="sendGridService">The SendGrid service for API operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionSettings is null.</exception>
        public SendGridEmailConnector(ConnectionSettings connectionSettings, ISendGridService? sendGridService = null, ILogger<SendGridEmailConnector>? logger = null)
            : this(SendGridChannelSchemas.SendGridEmail, connectionSettings, sendGridService, logger)
        {
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("Initializing SendGrid email connector...");

                // Extract required parameters first
                _apiKey = _connectionSettings.GetParameter("ApiKey") as string;

                // Extract optional parameters
                _sandboxMode = _connectionSettings.GetParameter("SandboxMode") as bool? ?? false;
                _webhookUrl = _connectionSettings.GetParameter("WebhookUrl") as string;
                _trackingSettings = _connectionSettings.GetParameter("TrackingSettings") as bool? ?? true;
                _defaultFromName = _connectionSettings.GetParameter("DefaultFromName") as string;
                _defaultReplyTo = _connectionSettings.GetParameter("DefaultReplyTo") as string;

                // Perform custom validation logic
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    return ConnectorResult<bool>.FailTask(SendGridErrorCodes.MissingApiKey, 
                        "SendGrid API Key is required");
                }

                // Validate connection settings against schema
                    var validationResults = Schema.ValidateConnectionSettings(_connectionSettings);
                    var validationErrors = validationResults.ToList();
                    if (validationErrors.Count > 0)
                    {
                        Logger.LogError("Connection settings validation failed: {Errors}", 
                            string.Join(", ", validationErrors.Select(e => e.ErrorMessage)));
                        return ConnectorResult<bool>.ValidationFailedTask(SendGridErrorCodes.InvalidConnectionSettings, 
                            "Connection settings validation failed", validationErrors);
                    }

                // Initialize SendGrid client
                _sendGridService.Initialize(_apiKey);

                Logger.LogInformation("SendGrid email connector initialized successfully");
                return ConnectorResult<bool>.SuccessTask(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize SendGrid email connector");
                return ConnectorResult<bool>.FailTask(ConnectorErrorCodes.InitializationError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug("Testing SendGrid connection...");

                // Test connection by validating API key
                var isConnected = await _sendGridService.TestConnectionAsync(cancellationToken);
                
                if (!isConnected)
                {
                    return ConnectorResult<bool>.Fail(SendGridErrorCodes.ConnectionFailed, 
                        "Unable to connect to SendGrid API - please verify your API key");
                }

                Logger.LogDebug("Connection test successful");
                return ConnectorResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Connection test failed");
                return ConnectorResult<bool>.Fail(SendGridErrorCodes.ConnectionTestFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug("Sending email message {MessageId}", message.Id);

                // Note: Message validation is already performed by the base class in SendMessageAsync()
                // before calling this method, so we don't need to duplicate it here.

                // Extract and validate message properties before processing
                var messageProperties = ExtractMessageProperties(message);

                // Extract sender email
                var (senderEmail, senderName) = ExtractEmailFromEndpoint(message.Sender);
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    return ConnectorResult<SendResult>.Fail(SendGridErrorCodes.MissingSender, 
                        "Sender email address is required");
                }

                if (!IsValidEmailAddress(senderEmail))
                {
                    return ConnectorResult<SendResult>.Fail(SendGridErrorCodes.InvalidEmailAddress, 
                        "Sender email address is not valid");
                }

                // Extract recipient email
                var (recipientEmail, recipientName) = ExtractEmailFromEndpoint(message.Receiver);
                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    return ConnectorResult<SendResult>.Fail(SendGridErrorCodes.InvalidRecipient, 
                        "Recipient email address is required");
                }

                if (!IsValidEmailAddress(recipientEmail))
                {
                    return ConnectorResult<SendResult>.Fail(SendGridErrorCodes.InvalidRecipient, 
                        "Recipient email address is not valid");
                }

                // Extract subject
                var subject = messageProperties.TryGetValue("Subject", out var subjectValue) 
                    ? subjectValue?.ToString() 
                    : "No Subject";

                if (string.IsNullOrWhiteSpace(subject))
                {
                    return ConnectorResult<SendResult>.Fail(SendGridErrorCodes.MissingEmailContent, 
                        "Email subject is required");
                }

                // Create SendGrid message
                var from = new EmailAddress(senderEmail, senderName ?? _defaultFromName);
                var to = new EmailAddress(recipientEmail, recipientName);
                var sendGridMessage = MailHelper.CreateSingleEmail(from, to, subject, null, null);

                // Set content based on message content type
                await SetMessageContentAsync(sendGridMessage, message);

                // Apply message settings
                ApplyMessageSettings(sendGridMessage, message, messageProperties);

                // Apply connector settings
                ApplyConnectorSettings(sendGridMessage);

                // Send the message
                var response = await _sendGridService.SendEmailAsync(sendGridMessage, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var messageId = ExtractMessageIdFromResponse(response);
                    
                    Logger.LogInformation("Email message sent successfully. MessageId: {MessageId}, StatusCode: {StatusCode}", 
                        messageId ?? message.Id, response.StatusCode);

                    var result = new SendResult(message.Id, messageId ?? Guid.NewGuid().ToString())
                    {
                        Status = MessageStatus.Sent,
                        Timestamp = DateTime.UtcNow
                    };

                    // Add properties
                    result.AdditionalData["SendGridStatusCode"] = response.StatusCode.ToString();
                    result.AdditionalData["To"] = recipientEmail;
                    result.AdditionalData["From"] = senderEmail;
                    result.AdditionalData["Subject"] = subject;
                    result.AdditionalData["SandboxMode"] = _sandboxMode.ToString();

                    return ConnectorResult<SendResult>.Success(result);
                }
                else
                {
                    var errorMessage = await response.Body.ReadAsStringAsync();
                    Logger.LogError("Failed to send email. StatusCode: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorMessage);

                    var errorCode = response.StatusCode == HttpStatusCode.TooManyRequests 
                        ? SendGridErrorCodes.RateLimitExceeded 
                        : SendGridErrorCodes.SendMessageFailed;

                    return ConnectorResult<SendResult>.Fail(errorCode, 
                        $"Failed to send email: {response.StatusCode} - {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send email message {MessageId}", message.Id);
                return ConnectorResult<SendResult>.Fail(SendGridErrorCodes.SendMessageFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<StatusUpdatesResult>> GetMessageStatusCoreAsync(string messageId, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug("Querying status for message {MessageId}", messageId);

                // Note: SendGrid doesn't provide a direct message status API like Twilio
                // In a real implementation, you would need to:
                // 1. Set up Event Webhook to receive status updates
                // 2. Store status updates in your own database
                // 3. Query your database for the status
                
                // For this implementation, we'll simulate a basic status query
                var response = await _sendGridService.GetEmailActivityAsync(messageId, cancellationToken);
                
                var status = response.IsSuccessStatusCode ? MessageStatus.Delivered : MessageStatus.Unknown;
                var timestamp = DateTime.UtcNow;

                var statusUpdate = new StatusUpdateResult(messageId, status, timestamp);
                statusUpdate.AdditionalData["SendGridStatusCode"] = response.StatusCode.ToString();

                var result = new StatusUpdatesResult(messageId, new[] { statusUpdate });
                return ConnectorResult<StatusUpdatesResult>.Success(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to query message status for {MessageId}", messageId);
                return ConnectorResult<StatusUpdatesResult>.Fail(SendGridErrorCodes.StatusQueryFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var statusInfo = new StatusInfo($"SendGrid Email Connector");

                statusInfo.AdditionalData["ApiKeyConfigured"] = !string.IsNullOrEmpty(_apiKey);
                statusInfo.AdditionalData["SandboxMode"] = _sandboxMode;
                statusInfo.AdditionalData["TrackingSettings"] = _trackingSettings;
                statusInfo.AdditionalData["State"] = State.ToString();
                statusInfo.AdditionalData["Uptime"] = DateTime.UtcNow - _startTime;

                return ConnectorResult<StatusInfo>.SuccessTask(statusInfo);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get connector status");
                return ConnectorResult<StatusInfo>.FailTask(SendGridErrorCodes.StatusError, ex.Message);
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
                    // Test connectivity by validating API key
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

        private Dictionary<string, object?> ExtractMessageProperties(IMessage message)
        {
            var properties = new Dictionary<string, object?>();

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

        private Task SetMessageContentAsync(SendGridMessage sendGridMessage, IMessage message)
        {
            if (message.Content == null)
                return Task.CompletedTask;

            switch (message.Content.ContentType)
            {
                case MessageContentType.PlainText when message.Content is ITextContent textContent:
                    sendGridMessage.PlainTextContent = textContent.Text;
                    break;

                case MessageContentType.Html when message.Content is IHtmlContent htmlContent:
                    sendGridMessage.HtmlContent = htmlContent.Html;
                    break;

                case MessageContentType.Multipart when message.Content is IMultipartContent multipartContent:
                    foreach (var part in multipartContent.Parts)
                    {
                        if (part.ContentType == MessageContentType.PlainText && part is ITextContent textPart)
                        {
                            sendGridMessage.PlainTextContent = textPart.Text;
                        }
                        else if (part.ContentType == MessageContentType.Html && part is IHtmlContent htmlPart)
                        {
                            sendGridMessage.HtmlContent = htmlPart.Html;
                        }
                    }
                    break;

                case MessageContentType.Template when message.Content is ITemplateContent templateContent:
                    sendGridMessage.TemplateId = templateContent.TemplateId;
                    if (templateContent.Parameters != null && templateContent.Parameters.Any())
                    {
                        sendGridMessage.SetTemplateData(templateContent.Parameters);
                    }
                    break;

                default:
                    // Fallback to plain text if we can extract it
                    if (message.Content is ITextContent fallbackText)
                    {
                        sendGridMessage.PlainTextContent = fallbackText.Text;
                    }
                    break;
            }

            return Task.CompletedTask;
        }

        private void ApplyMessageSettings(SendGridMessage sendGridMessage, IMessage message, Dictionary<string, object?> properties)
        {
            // Apply priority if specified
            if (properties.TryGetValue("Priority", out var priorityValue))
            {
                var priority = priorityValue?.ToString()?.ToLowerInvariant();
                // SendGrid doesn't have direct priority setting, but we can use headers
                if (!string.IsNullOrEmpty(priority))
                {
                    var priorityNum = priority switch
                    {
                        "high" => "1",
                        "normal" => "3",
                        "low" => "5",
                        _ => "3"
                    };
                    sendGridMessage.AddHeader("X-Priority", priorityNum);
                }
            }

            // Apply categories
            if (properties.TryGetValue("Categories", out var categoriesValue))
            {
                var categories = categoriesValue?.ToString();
                if (!string.IsNullOrEmpty(categories))
                {
                    var categoryList = categories.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                    
                    sendGridMessage.Categories = categoryList;
                }
            }

            // Apply custom arguments
            if (properties.TryGetValue("CustomArgs", out var customArgsValue))
            {
                var customArgs = customArgsValue?.ToString();
                if (!string.IsNullOrEmpty(customArgs))
                {
                    try
                    {
                        var argsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(customArgs);
                        if (argsDict != null)
                        {
                            sendGridMessage.CustomArgs = argsDict;
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        Logger.LogWarning(ex, "Failed to parse CustomArgs as JSON: {CustomArgs}", customArgs);
                    }
                }
            }

            // Apply send at time
            if (properties.TryGetValue("SendAt", out var sendAtValue))
            {
                DateTime sendAt;
                if (sendAtValue is DateTime dateTime)
                {
                    sendAt = dateTime;
                }
                else if (sendAtValue is string dateString && DateTime.TryParse(dateString, out var parsedDate))
                {
                    sendAt = parsedDate;
                }
                else
                {
                    // Skip invalid SendAt values
                    goto skipSendAt;
                }

                var unixTimestamp = ((DateTimeOffset)sendAt.ToUniversalTime()).ToUnixTimeSeconds();
                sendGridMessage.SendAt = unixTimestamp;
            }
            skipSendAt:

            // Apply batch ID
            if (properties.TryGetValue("BatchId", out var batchIdValue))
            {
                var batchId = batchIdValue?.ToString();
                if (!string.IsNullOrEmpty(batchId))
                {
                    sendGridMessage.BatchId = batchId;
                }
            }

            // Apply unsubscribe group
            if (properties.TryGetValue("AsmGroupId", out var asmGroupIdValue))
            {
                if (int.TryParse(asmGroupIdValue?.ToString(), out var asmGroupId))
                {
                    sendGridMessage.Asm = new ASM { GroupId = asmGroupId };
                }
            }

            // Apply IP pool name
            if (properties.TryGetValue("IpPoolName", out var ipPoolValue))
            {
                var ipPoolName = ipPoolValue?.ToString();
                if (!string.IsNullOrEmpty(ipPoolName))
                {
                    sendGridMessage.IpPoolName = ipPoolName;
                }
            }

            // Apply reply-to
            if (!string.IsNullOrEmpty(_defaultReplyTo))
            {
                sendGridMessage.ReplyTo = new EmailAddress(_defaultReplyTo);
            }
        }

        private void ApplyConnectorSettings(SendGridMessage sendGridMessage)
        {
            // Apply sandbox mode
            if (_sandboxMode)
            {
                sendGridMessage.MailSettings = sendGridMessage.MailSettings ?? new MailSettings();
                sendGridMessage.MailSettings.SandboxMode = new SandboxMode { Enable = true };
            }

            // Apply tracking settings
            if (_trackingSettings)
            {
                sendGridMessage.TrackingSettings = sendGridMessage.TrackingSettings ?? new TrackingSettings();
                sendGridMessage.TrackingSettings.ClickTracking = new ClickTracking { Enable = true };
                sendGridMessage.TrackingSettings.OpenTracking = new OpenTracking { Enable = true };
            }
        }

        private string? ExtractMessageIdFromResponse(SendGrid.Response response)
        {
            try
            {
                // SendGrid includes the message ID in the X-Message-Id header
                if (response.Headers != null)
                {
                    var messageIdHeaders = response.Headers.GetValues("X-Message-Id");
                    if (messageIdHeaders != null && messageIdHeaders.Any())
                    {
                        return messageIdHeaders.First();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to extract message ID from SendGrid response");
            }

            return null;
        }

        /// <summary>
        /// Validates an email address format according to basic email validation rules.
        /// </summary>
        /// <param name="emailAddress">The email address to validate.</param>
        /// <returns>True if the email address is valid, false otherwise.</returns>
        private static bool IsValidEmailAddress(string? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return false;

            try
            {
                var emailAttribute = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                return emailAttribute.IsValid(emailAddress);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts the email address from an endpoint, handling name+email format.
        /// </summary>
        /// <param name="endpoint">The endpoint to extract email from.</param>
        /// <returns>A tuple containing the email address and optional name.</returns>
        private static (string? email, string? name) ExtractEmailFromEndpoint(IEndpoint? endpoint)
        {
            if (endpoint?.Type != EndpointType.EmailAddress || string.IsNullOrWhiteSpace(endpoint.Address))
                return (null, null);

            var address = endpoint.Address.Trim();

            // Check if it's in "Name <email@domain.com>" format
            var nameEmailPattern = @"^(.+?)\s*<(.+?)>$";
            var match = System.Text.RegularExpressions.Regex.Match(address, nameEmailPattern);

            if (match.Success)
            {
                var name = match.Groups[1].Value.Trim().Trim('"');
                var email = match.Groups[2].Value.Trim();
                return (email, name);
            }

            // Just email address
            return (address, null);
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<ReceiveResult>> ReceiveMessagesCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug("Receiving email message from SendGrid webhook");

                if (source.ContentType == MessageSource.JsonContentType)
                {
                    var messages = ParseSendGridWebhookJson(source);
                    
                    if (messages.Count == 0)
                    {
                        return ConnectorResult<ReceiveResult>.FailTask(SendGridErrorCodes.InvalidWebhookData, 
                            "No valid messages found in webhook JSON");
                    }

                    var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                    return ConnectorResult<ReceiveResult>.SuccessTask(result);
                }
                else if (source.ContentType == MessageSource.UrlPostContentType)
                {
                    var formData = source.AsUrlPostData();
                    var messages = ParseSendGridWebhookFormData(formData);
                    
                    if (messages.Count == 0)
                    {
                        return ConnectorResult<ReceiveResult>.FailTask(SendGridErrorCodes.InvalidWebhookData, 
                            "No valid messages found in webhook data");
                    }

                    var result = new ReceiveResult(Guid.NewGuid().ToString(), messages);
                    return ConnectorResult<ReceiveResult>.SuccessTask(result);
                }

                return ConnectorResult<ReceiveResult>.FailTask(SendGridErrorCodes.UnsupportedContentType, 
                    "Only JSON and form data are supported for SendGrid email receiving");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to receive email message from SendGrid webhook");
                return ConnectorResult<ReceiveResult>.FailTask(SendGridErrorCodes.ReceiveMessageFailed, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusUpdateResult>> ReceiveMessageStatusCoreAsync(MessageSource source, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug("Receiving email status update from SendGrid webhook");

                if (source.ContentType == MessageSource.JsonContentType)
                {
                    var statusResult = ParseSendGridStatusCallbackJson(source);
                    return ConnectorResult<StatusUpdateResult>.SuccessTask(statusResult);
                }
                else if (source.ContentType == MessageSource.UrlPostContentType)
                {
                    var formData = source.AsUrlPostData();
                    var statusResult = ParseSendGridStatusCallbackFormData(formData);
                    return ConnectorResult<StatusUpdateResult>.SuccessTask(statusResult);
                }

                return ConnectorResult<StatusUpdateResult>.FailTask(SendGridErrorCodes.UnsupportedContentType, 
                    "Only JSON and form data are supported for SendGrid status callbacks");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to receive email status from SendGrid webhook");
                return ConnectorResult<StatusUpdateResult>.FailTask(SendGridErrorCodes.ReceiveStatusFailed, ex.Message);
            }
        }

        private List<IMessage> ParseSendGridWebhookJson(MessageSource source)
        {
            var messages = new List<IMessage>();
            var jsonData = source.AsJson<System.Text.Json.JsonElement>();

            if (jsonData.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                // Batch events
                foreach (var eventElement in jsonData.EnumerateArray())
                {
                    var message = ParseSendGridJsonEvent(eventElement);
                    if (message != null)
                        messages.Add(message);
                }
            }
            else
            {
                // Single event
                var message = ParseSendGridJsonEvent(jsonData);
                if (message != null)
                    messages.Add(message);
            }

            return messages;
        }

        private IMessage? ParseSendGridJsonEvent(System.Text.Json.JsonElement eventData)
        {
            // SendGrid webhook events that represent received emails (inbound parse)
            if (!eventData.TryGetProperty("event", out var eventProperty))
                return null;

            var eventType = eventProperty.GetString();
            if (eventType != "inbound" && eventType != "processed")
                return null; // We only process inbound emails and processed events

            // Extract message ID
            var messageId = eventData.TryGetProperty("sg_message_id", out var idProp) ? 
                idProp.GetString() ?? Guid.NewGuid().ToString() : 
                Guid.NewGuid().ToString();

            // Extract email addresses
            var from = eventData.TryGetProperty("from", out var fromProp) ? fromProp.GetString() ?? "" : "";
            var to = eventData.TryGetProperty("to", out var toProp) ? toProp.GetString() ?? "" : "";

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return null;

            // Extract content
            var subject = eventData.TryGetProperty("subject", out var subjectProp) ? subjectProp.GetString() ?? "" : "";
            var text = eventData.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : "";
            var html = eventData.TryGetProperty("html", out var htmlProp) ? htmlProp.GetString() ?? "" : "";

            // Create message content (prefer HTML over plain text)
            MessageContent content;
            if (!string.IsNullOrEmpty(html))
            {
                content = new HtmlContent(html);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                content = new TextContent(text);
            }
            else
            {
                content = new TextContent("");
            }

            var message = new Message
            {
                Id = messageId,
                Sender = new Endpoint(EndpointType.EmailAddress, from),
                Receiver = new Endpoint(EndpointType.EmailAddress, to),
                Content = content,
                Properties = new Dictionary<string, MessageProperty>
                {
                    ["Subject"] = new MessageProperty("Subject", subject)
                }
            };

            // Add all other SendGrid event fields as message properties
            foreach (var property in eventData.EnumerateObject())
            {
                if (property.Name != "sg_message_id" && property.Name != "from" && 
                    property.Name != "to" && property.Name != "subject" && 
                    property.Name != "text" && property.Name != "html")
                {
                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetInt64().ToString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Array => property.Value.ToString(),
                        JsonValueKind.Object => property.Value.ToString(),
                        _ => property.Value.ToString()
                    };
                    message.Properties[property.Name] = new MessageProperty(property.Name, value);
                }
            }

            return message;
        }

        private List<IMessage> ParseSendGridWebhookFormData(IDictionary<string, string> formData)
        {
            var messages = new List<IMessage>();

            // Validate required fields for SendGrid inbound parse webhook
            if (!formData.TryGetValue("from", out var from) || string.IsNullOrEmpty(from))
            {
                throw new ArgumentException("from field is required for SendGrid webhooks");
            }

            if (!formData.TryGetValue("to", out var to) || string.IsNullOrEmpty(to))
            {
                throw new ArgumentException("to field is required for SendGrid webhooks");
            }

            // Extract message ID (use envelope info or generate)
            var messageId = formData.TryGetValue("envelope", out var envelope) ? 
                envelope : Guid.NewGuid().ToString();

            // Extract content
            var subject = formData.TryGetValue("subject", out var subjectValue) ? subjectValue : "";
            var text = formData.TryGetValue("text", out var textValue) ? textValue : "";
            var html = formData.TryGetValue("html", out var htmlValue) ? htmlValue : "";

            // Create message content (prefer HTML over plain text)
            MessageContent content;
            if (!string.IsNullOrEmpty(html))
            {
                content = new HtmlContent(html);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                content = new TextContent(text);
            }
            else
            {
                content = new TextContent("");
            }

            var message = new Message
            {
                Id = messageId,
                Sender = new Endpoint(EndpointType.EmailAddress, from),
                Receiver = new Endpoint(EndpointType.EmailAddress, to),
                Content = content,
                Properties = new Dictionary<string, MessageProperty>
                {
                    ["Subject"] = new MessageProperty("Subject", subject)
                }
            };

            // Add all other form fields as message properties
            foreach (var kvp in formData)
            {
                if (kvp.Key != "from" && kvp.Key != "to" && kvp.Key != "subject" && 
                    kvp.Key != "text" && kvp.Key != "html" && kvp.Key != "envelope")
                {
                    message.Properties[kvp.Key] = new MessageProperty(kvp.Key, kvp.Value);
                }
            }

            messages.Add(message);
            return messages;
        }

        private StatusUpdateResult ParseSendGridStatusCallbackJson(MessageSource source)
        {
            var jsonData = source.AsJson<System.Text.Json.JsonElement>();
            
            // For array of events, take the first one
            if (jsonData.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                if (jsonData.GetArrayLength() > 0)
                {
                    jsonData = jsonData.EnumerateArray().First();
                }
                else
                {
                    throw new ArgumentException("Empty events array in SendGrid webhook");
                }
            }

            var messageId = jsonData.TryGetProperty("sg_message_id", out var sidProp) ? 
                sidProp.GetString() ?? "unknown" : "unknown";
            var eventType = jsonData.TryGetProperty("event", out var eventProp) ? 
                eventProp.GetString() ?? "unknown" : "unknown";
            
            var messageStatus = MapSendGridEventToMessageStatus(eventType);
            var timestamp = DateTime.UtcNow;

            // Try to extract timestamp from the event
            if (jsonData.TryGetProperty("timestamp", out var timestampProp))
            {
                if (timestampProp.ValueKind == System.Text.Json.JsonValueKind.Number && 
                    timestampProp.TryGetInt64(out var unixTimestamp))
                {
                    timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                }
            }

            var statusResult = new StatusUpdateResult(messageId, messageStatus, timestamp);

            // Add additional JSON properties as additional data
            foreach (var property in jsonData.EnumerateObject())
            {
                if (property.Name != "sg_message_id")
                {
                    var value = property.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => property.Value.GetString() ?? "",
                        System.Text.Json.JsonValueKind.Number => property.Value.GetInt64().ToString(),
                        System.Text.Json.JsonValueKind.True => "true",
                        System.Text.Json.JsonValueKind.False => "false",
                        System.Text.Json.JsonValueKind.Array => property.Value.ToString(),
                        System.Text.Json.JsonValueKind.Object => property.Value.ToString(),
                        _ => property.Value.ToString()
                    };
                    statusResult.AdditionalData[property.Name] = value;
                }
            }

            // Mark as SendGrid email channel
            statusResult.AdditionalData["Channel"] = "Email";
            statusResult.AdditionalData["Provider"] = "SendGrid";

            return statusResult;
        }

        private StatusUpdateResult ParseSendGridStatusCallbackFormData(IDictionary<string, string> formData)
        {
            var messageId = formData.TryGetValue("sg_message_id", out var sid) ? sid : "unknown";
            var eventType = formData.TryGetValue("event", out var evt) ? evt : "unknown";
            
            var messageStatus = MapSendGridEventToMessageStatus(eventType);
            var timestamp = DateTime.UtcNow;

            // Try to extract timestamp from the event
            if (formData.TryGetValue("timestamp", out var timestampString) && 
                long.TryParse(timestampString, out var unixTimestamp))
            {
                timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
            }

            var statusResult = new StatusUpdateResult(messageId, messageStatus, timestamp);

            // Add additional form data as additional data
            foreach (var kvp in formData)
            {
                if (kvp.Key != "sg_message_id")
                {
                    statusResult.AdditionalData[kvp.Key] = kvp.Value;
                }
            }

            // Mark as SendGrid email channel
            statusResult.AdditionalData["Channel"] = "Email";
            statusResult.AdditionalData["Provider"] = "SendGrid";

            return statusResult;
        }

        private MessageStatus MapSendGridEventToMessageStatus(string eventType)
        {
            return eventType.ToLowerInvariant() switch
            {
                "processed" => MessageStatus.Queued,
                "deferred" => MessageStatus.Queued,
                "delivered" => MessageStatus.Delivered,
                "open" => MessageStatus.Delivered,
                "click" => MessageStatus.Delivered,
                "bounce" => MessageStatus.DeliveryFailed,
                "dropped" => MessageStatus.DeliveryFailed,
                "spamreport" => MessageStatus.DeliveryFailed,
                "unsubscribe" => MessageStatus.Delivered, // Still delivered, but user unsubscribed
                "group_unsubscribe" => MessageStatus.Delivered,
                "group_resubscribe" => MessageStatus.Delivered,
                "inbound" => MessageStatus.Received, // Inbound email received
                _ => MessageStatus.Unknown
            };
        }
    }
}