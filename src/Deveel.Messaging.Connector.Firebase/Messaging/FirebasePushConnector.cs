//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Deveel.Messaging
{
    /// <summary>
    /// A channel connector that implements Firebase Cloud Messaging (FCM) push notifications.
    /// </summary>
    /// <remarks>
    /// This connector provides comprehensive support for Firebase Cloud Messaging capabilities including
    /// sending push notifications, bulk messaging, health monitoring, and various notification features
    /// such as images, actions, and platform-specific configurations.
    /// </remarks>
    [ChannelSchema(typeof(FirebasePushSchemaFactory))]
    public class FirebasePushConnector : ChannelConnectorBase
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly IFirebaseService _firebaseService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        private string? _projectId;
        private string? _serviceAccountKey;
        private bool _dryRun;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebasePushConnector"/> class.
        /// </summary>
        /// <param name="schema">The channel schema that defines the connector's capabilities and configuration.</param>
        /// <param name="connectionSettings">The connection settings containing Firebase credentials and configuration.</param>
        /// <param name="firebaseService">The Firebase service for FCM operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <param name="authenticationManager">Optional authentication manager for handling authentication flows.</param>
        /// <exception cref="ArgumentNullException">Thrown when schema or connectionSettings is null.</exception>
        public FirebasePushConnector(IChannelSchema schema, ConnectionSettings connectionSettings, IFirebaseService? firebaseService = null, ILogger<FirebasePushConnector>? logger = null, IAuthenticationManager? authenticationManager = null)
            : base(schema, logger, authenticationManager)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _firebaseService = firebaseService ?? new FirebaseService();

            // Register Firebase-specific authentication provider
            AuthenticationManager.RegisterProvider(new FirebaseServiceAccountAuthenticationProvider());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebasePushConnector"/> class using the default Firebase push schema.
        /// </summary>
        /// <param name="connectionSettings">The connection settings containing Firebase credentials and configuration.</param>
        /// <param name="firebaseService">The Firebase service for FCM operations.</param>
        /// <param name="logger">Optional logger for diagnostic and operational logging.</param>
        /// <param name="authenticationManager">Optional authentication manager for handling authentication flows.</param>
        public FirebasePushConnector(ConnectionSettings connectionSettings, IFirebaseService? firebaseService = null, ILogger<FirebasePushConnector>? logger = null, IAuthenticationManager? authenticationManager = null)
            : this(FirebaseChannelSchemas.FirebasePush, connectionSettings, firebaseService, logger, authenticationManager)
        {
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<bool>> InitializeConnectorAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInitializingConnector();

                // Perform authentication first
                var authResult = await AuthenticateAsync(_connectionSettings, cancellationToken);
                if (!authResult.Successful)
                {
                    return authResult;
                }

                // Extract configuration from connection settings with proper handling of missing values
                var projectIdParam = _connectionSettings.GetParameter("ProjectId");
                var dryRunParam = _connectionSettings.GetParameter("DryRun");

                _projectId = projectIdParam?.ToString();
                _dryRun = dryRunParam != null && Convert.ToBoolean(dryRunParam);

                if (string.IsNullOrWhiteSpace(_projectId))
                {
                    return ConnectorResult<bool>.Fail(ConnectorErrorCodes.InitializationError, "ProjectId is required");
                }

                // Get the service account key from the authenticated credential
                if (AuthenticationCredential?.AuthenticationType == AuthenticationType.Certificate)
                {
                    _serviceAccountKey = AuthenticationCredential.CredentialValue;
                }
                else
                {
                    return ConnectorResult<bool>.Fail(ConnectorErrorCodes.InitializationError, "Service account authentication is required for Firebase");
                }

                // Initialize Firebase service
                await _firebaseService.InitializeAsync(_serviceAccountKey, _projectId);

                Logger.LogConnectorInitialized(_projectId);
                return ConnectorResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                Logger.LogInitializationFailed(ex);
                return ConnectorResult<bool>.Fail(ConnectorErrorCodes.InitializationError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<bool>> TestConnectorConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogTestingConnection();

                var isConnected = await _firebaseService.TestConnectionAsync(cancellationToken);
                
                if (isConnected)
                {
                    Logger.LogConnectionTestSuccessful();
                    return ConnectorResult<bool>.Success(true);
                }
                else
                {
                    Logger.LogConnectionTestFailed();
                    return ConnectorResult<bool>.Fail(ConnectorErrorCodes.ConnectionTestError, "Firebase connection test failed");
                }
            }
            catch (Exception ex)
            {
                Logger.LogConnectionTestException(ex);
                return ConnectorResult<bool>.Fail(ConnectorErrorCodes.ConnectionTestError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<SendResult>> SendMessageCoreAsync(IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogSendingPushNotification(message.Receiver?.Address);

                var firebaseMessage = await BuildFirebaseMessageAsync(message, cancellationToken);
                var messageId = await _firebaseService.SendAsync(firebaseMessage, _dryRun, cancellationToken);

                var result = new SendResult(message.Id, messageId);
                result.AdditionalData["MessageId"] = messageId;
                result.AdditionalData["ProjectId"] = _projectId!;
                result.AdditionalData["DryRun"] = _dryRun;

                Logger.LogPushNotificationSent(messageId);
                return ConnectorResult<SendResult>.Success(result);
            }
            catch (Exception ex)
            {
                Logger.LogPushNotificationSendFailed(ex);
                return ConnectorResult<SendResult>.Fail(ConnectorErrorCodes.SendMessageError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<BatchSendResult>> SendBatchCoreAsync(IMessageBatch batch, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogSendingBatch(batch.Messages.Count());

                var batchId = Guid.NewGuid().ToString();
                var messages = new List<FirebaseAdmin.Messaging.Message>();

                // Build Firebase messages
                foreach (var message in batch.Messages)
                {
                    var firebaseMessage = await BuildFirebaseMessageAsync(message, cancellationToken);
                    messages.Add(firebaseMessage);
                }

                // Check if we can use multicast (all messages to device tokens with same notification)
                var deviceTokenMessages = messages.Where(m => !string.IsNullOrEmpty(m.Token)).ToList();
                var topicMessages = messages.Where(m => !string.IsNullOrEmpty(m.Topic)).ToList();

                var results = new Dictionary<string, SendResult>();

                // Send multicast messages if possible
                if (deviceTokenMessages.Count > 1 && CanUseMulticast(deviceTokenMessages))
                {
                    await SendMulticastMessagesAsync(deviceTokenMessages, batch.Messages, results, cancellationToken);
                }
                else if (deviceTokenMessages.Count > 0)
                {
                    await SendIndividualMessagesAsync(deviceTokenMessages, batch.Messages, results, cancellationToken);
                }

                // Send topic messages individually
                if (topicMessages.Count > 0)
                {
                    await SendIndividualMessagesAsync(topicMessages, batch.Messages, results, cancellationToken);
                }

                var batchResult = new BatchSendResult(batchId, batchId, results);
                
                Logger.LogBatchSent(batchId, results.Count);
                
                return ConnectorResult<BatchSendResult>.Success(batchResult);
            }
            catch (Exception ex)
            {
                Logger.LogBatchSendFailed(ex);
                return ConnectorResult<BatchSendResult>.Fail(ConnectorErrorCodes.SendBatchError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override Task<ConnectorResult<StatusInfo>> GetConnectorStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var status = new StatusInfo("Firebase connector operational", $"Project: {_projectId ?? "Unknown"}");
                status.AdditionalData["ProjectId"] = _projectId ?? "Unknown";
                status.AdditionalData["IsInitialized"] = _firebaseService.IsInitialized;
                status.AdditionalData["DryRun"] = _dryRun;
                status.AdditionalData["Uptime"] = DateTime.UtcNow - _startTime;

                return ConnectorResult<StatusInfo>.SuccessTask(status);
            }
            catch (Exception ex)
            {
                return ConnectorResult<StatusInfo>.FailTask(ConnectorErrorCodes.GetStatusError, ex.Message);
            }
        }

        /// <inheritdoc/>
        protected override async Task<ConnectorResult<ConnectorHealth>> GetConnectorHealthAsync(CancellationToken cancellationToken)
        {
            var health = new ConnectorHealth
            {
                State = State,
                IsHealthy = State == ConnectorState.Ready && _firebaseService.IsInitialized,
                LastHealthCheck = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - _startTime
            };

            health.Metrics["ProjectId"] = _projectId ?? "Unknown";
            health.Metrics["IsInitialized"] = _firebaseService.IsInitialized;
            health.Metrics["DryRun"] = _dryRun;

            if (!health.IsHealthy)
            {
                if (State != ConnectorState.Ready)
                {
                    health.Issues.Add($"Connector is in {State} state");
                }
                if (!_firebaseService.IsInitialized)
                {
                    health.Issues.Add("Firebase service is not initialized");
                }
            }

            // Test connection if healthy
            if (health.IsHealthy)
            {
                try
                {
                    var connectionTest = await TestConnectorConnectionAsync(cancellationToken);
                    if (!connectionTest.Successful)
                    {
                        health.IsHealthy = false;
                        health.Issues.Add("Firebase connection test failed");
                    }
                }
                catch (Exception ex)
                {
                    health.IsHealthy = false;
                    health.Issues.Add($"Connection test error: {ex.Message}");
                }
            }

            return ConnectorResult<ConnectorHealth>.Success(health);
        }

        /// <summary>
        /// Builds a Firebase message from the messaging framework message.
        /// </summary>
        private async Task<FirebaseAdmin.Messaging.Message> BuildFirebaseMessageAsync(IMessage message, CancellationToken cancellationToken)
        {
            var messageBuilder = new FirebaseAdmin.Messaging.Message();

            // Set target (device token or topic)
            if (message.Receiver?.Type == EndpointType.DeviceId)
            {
                messageBuilder.Token = message.Receiver.Address;
            }
            else if (message.Receiver?.Type == EndpointType.Topic)
            {
                messageBuilder.Topic = message.Receiver.Address;
            }
            else
            {
                throw new ArgumentException("Message receiver must be a DeviceId or Topic endpoint");
            }

            // Build notification from content and properties
            var notification = BuildNotification(message);
            if (notification != null)
            {
                messageBuilder.Notification = notification;
            }

            // Build data payload
            var data = BuildDataPayload(message);
            if (data.Count > 0)
            {
                messageBuilder.Data = data;
            }

            // Set Android-specific configuration
            var androidConfig = BuildAndroidConfig(message);
            if (androidConfig != null)
            {
                messageBuilder.Android = androidConfig;
            }

            // Set iOS-specific configuration  
            var apnsConfig = BuildApnsConfig(message);
            if (apnsConfig != null)
            {
                messageBuilder.Apns = apnsConfig;
            }

            // Set web push configuration
            var webPushConfig = BuildWebPushConfig(message);
            if (webPushConfig != null)
            {
                messageBuilder.Webpush = webPushConfig;
            }

            await Task.CompletedTask; // Suppress async warning
            return messageBuilder;
        }

        /// <summary>
        /// Builds the notification payload from message content and properties.
        /// </summary>
        private Notification? BuildNotification(IMessage message)
        {
            var title = GetMessageProperty(message, "Title");
            var body = (message.Content as ITextContent)?.Text ?? 
                      GetMessageProperty(message, "Body");
            var imageUrl = GetMessageProperty(message, "ImageUrl");

            // If no title or body, return null (data-only message)
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
            {
                return null;
            }

            var notification = new Notification
            {
                Title = title,
                Body = body
            };

            if (!string.IsNullOrEmpty(imageUrl))
            {
                notification.ImageUrl = imageUrl;
            }

            return notification;
        }

        /// <summary>
        /// Builds the data payload from message content and custom data.
        /// </summary>
        private Dictionary<string, string> BuildDataPayload(IMessage message)
        {
            var data = new Dictionary<string, string>();

            // Add custom data if provided
            var customData = GetMessageProperty(message, "CustomData");
            if (!string.IsNullOrEmpty(customData))
            {
                try
                {
                    using var document = JsonDocument.Parse(customData);
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        data[property.Name] = property.Value.ToString();
                    }
                }
                catch (JsonException)
                {
                    // Invalid JSON, add as single field
                    data["customData"] = customData;
                }
            }

            // Add message ID
            if (!string.IsNullOrEmpty(message.Id))
            {
                data["messageId"] = message.Id;
            }

            return data;
        }

        /// <summary>
        /// Builds Android-specific configuration.
        /// </summary>
        private AndroidConfig? BuildAndroidConfig(IMessage message)
        {
            var hasAndroidConfig = false;
            var androidConfig = new AndroidConfig();

            // Set notification configuration
            var androidNotification = new AndroidNotification();

            var color = GetMessageProperty(message, "Color");
            if (!string.IsNullOrEmpty(color))
            {
                androidNotification.Color = color;
                hasAndroidConfig = true;
            }

            var sound = GetMessageProperty(message, "Sound");
            if (!string.IsNullOrEmpty(sound))
            {
                androidNotification.Sound = sound;
                hasAndroidConfig = true;
            }

            var tag = GetMessageProperty(message, "Tag");
            if (!string.IsNullOrEmpty(tag))
            {
                androidNotification.Tag = tag;
                hasAndroidConfig = true;
            }

            var clickAction = GetMessageProperty(message, "ClickAction");
            if (!string.IsNullOrEmpty(clickAction))
            {
                androidNotification.ClickAction = clickAction;
                hasAndroidConfig = true;
            }

            if (hasAndroidConfig)
            {
                androidConfig.Notification = androidNotification;
            }

            // Set priority
            var priority = GetMessageProperty(message, "Priority");
            if (!string.IsNullOrEmpty(priority))
            {
                androidConfig.Priority = priority.ToLowerInvariant() == "high" ? Priority.High : Priority.Normal;
                hasAndroidConfig = true;
            }

            // Set TTL
            var timeToLiveStr = GetMessageProperty(message, "TimeToLive");
            if (!string.IsNullOrEmpty(timeToLiveStr) && int.TryParse(timeToLiveStr, out var ttlSeconds))
            {
                androidConfig.TimeToLive = TimeSpan.FromSeconds(ttlSeconds);
                hasAndroidConfig = true;
            }

            // Set collapse key
            var collapseKey = GetMessageProperty(message, "CollapseKey");
            if (!string.IsNullOrEmpty(collapseKey))
            {
                androidConfig.CollapseKey = collapseKey;
                hasAndroidConfig = true;
            }

            // Set restricted package name
            var restrictedPackageName = GetMessageProperty(message, "RestrictedPackageName");
            if (!string.IsNullOrEmpty(restrictedPackageName))
            {
                androidConfig.RestrictedPackageName = restrictedPackageName;
                hasAndroidConfig = true;
            }

            return hasAndroidConfig ? androidConfig : null;
        }

        /// <summary>
        /// Builds iOS-specific APNS configuration.
        /// </summary>
        private ApnsConfig? BuildApnsConfig(IMessage message)
        {
            var hasApnsConfig = false;
            var apnsConfig = new ApnsConfig();
            var apsPayload = new Aps();

            // Set badge
            var badgeStr = GetMessageProperty(message, "Badge");
            if (!string.IsNullOrEmpty(badgeStr) && int.TryParse(badgeStr, out var badgeCount))
            {
                apsPayload.Badge = badgeCount;
                hasApnsConfig = true;
            }

            // Set sound
            var sound = GetMessageProperty(message, "Sound");
            if (!string.IsNullOrEmpty(sound))
            {
                apsPayload.Sound = sound;
                hasApnsConfig = true;
            }

            // Set content-available
            var contentAvailableStr = GetMessageProperty(message, "ContentAvailable");
            if (!string.IsNullOrEmpty(contentAvailableStr) && bool.TryParse(contentAvailableStr, out var isContentAvailable) && isContentAvailable)
            {
                apsPayload.ContentAvailable = true;
                hasApnsConfig = true;
            }

            // Set mutable-content
            var mutableContentStr = GetMessageProperty(message, "MutableContent");
            if (!string.IsNullOrEmpty(mutableContentStr) && bool.TryParse(mutableContentStr, out var isMutableContent) && isMutableContent)
            {
                apsPayload.MutableContent = true;
                hasApnsConfig = true;
            }

            // Set thread-id
            var threadId = GetMessageProperty(message, "ThreadId");
            if (!string.IsNullOrEmpty(threadId))
            {
                apsPayload.ThreadId = threadId;
                hasApnsConfig = true;
            }

            if (hasApnsConfig)
            {
                apnsConfig.Aps = apsPayload;
            }

            return hasApnsConfig ? apnsConfig : null;
        }

        /// <summary>
        /// Builds web push configuration.
        /// </summary>
        private WebpushConfig? BuildWebPushConfig(IMessage message)
        {
            // For now, return null as web push specific configuration is minimal
            // This can be extended based on requirements
            return null;
        }

        /// <summary>
        /// Gets a message property value as a string.
        /// </summary>
        private string? GetMessageProperty(IMessage message, string propertyName)
        {
            if (message.Properties?.TryGetValue(propertyName, out var property) == true)
            {
                return property.Value?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Checks if messages can use multicast (same notification content).
        /// </summary>
        private bool CanUseMulticast(List<FirebaseAdmin.Messaging.Message> messages)
        {
            if (messages.Count <= 1) return false;

            var first = messages[0];
            return messages.All(m => 
                AreNotificationsEqual(m.Notification, first.Notification) &&
                AreDataPayloadsEqual(m.Data, first.Data));
        }

        /// <summary>
        /// Compares two notifications for equality.
        /// </summary>
        private bool AreNotificationsEqual(Notification? n1, Notification? n2)
        {
            if (n1 == null && n2 == null) return true;
            if (n1 == null || n2 == null) return false;
            
            return n1.Title == n2.Title && 
                   n1.Body == n2.Body && 
                   n1.ImageUrl == n2.ImageUrl;
        }

        /// <summary>
        /// Compares two data payloads for equality.
        /// </summary>
        private bool AreDataPayloadsEqual(IReadOnlyDictionary<string, string>? d1, IReadOnlyDictionary<string, string>? d2)
        {
            if (d1 == null && d2 == null) return true;
            if (d1 == null || d2 == null) return false;
            if (d1.Count != d2.Count) return false;

            return d1.All(kvp => d2.TryGetValue(kvp.Key, out var value) && value == kvp.Value);
        }

        /// <summary>
        /// Sends messages using multicast for efficiency.
        /// </summary>
        private async Task SendMulticastMessagesAsync(List<FirebaseAdmin.Messaging.Message> messages, IEnumerable<IMessage> originalMessages, Dictionary<string, SendResult> results, CancellationToken cancellationToken)
        {
            var tokens = messages.Select(m => m.Token!).ToList();
            var template = messages[0];

            // Split into chunks of maximum allowed tokens
            var chunks = tokens.Chunk(FirebaseConnectorConstants.MaxMulticastTokens);

            foreach (var chunk in chunks)
            {
                var multicastMessage = new MulticastMessage
                {
                    Tokens = chunk.ToList(),
                    Notification = template.Notification,
                    Data = template.Data,
                    Android = template.Android,
                    Apns = template.Apns
                };

                var batchResponse = await _firebaseService.SendMulticastAsync(multicastMessage, _dryRun, cancellationToken);

                // Process individual responses
                for (int i = 0; i < chunk.Count(); i++)
                {
                    var token = chunk.ElementAt(i);
                    var response = batchResponse.Responses[i];
                    
                    // Find the original message ID for this token
                    var originalMessage = originalMessages.FirstOrDefault(m => m.Receiver?.Address == token);
                    var messageId = originalMessage?.Id ?? $"multicast-{token}";
                    
                    var result = new SendResult(messageId, response.MessageId ?? "unknown");
                    result.AdditionalData["Token"] = token;
                    result.AdditionalData["ProjectId"] = _projectId!;
                    result.AdditionalData["DryRun"] = _dryRun;

                    if (response.IsSuccess)
                    {
                        result.AdditionalData["MessageId"] = response.MessageId;
                    }
                    else
                    {
                        result.AdditionalData["Error"] = response.Exception?.Message ?? "Unknown error";
                    }

                    // Use the original message ID as key
                    results[messageId] = result;
                }
            }
        }

        /// <summary>
        /// Sends messages individually.
        /// </summary>
        private async Task SendIndividualMessagesAsync(List<FirebaseAdmin.Messaging.Message> messages, IEnumerable<IMessage> originalMessages, Dictionary<string, SendResult> results, CancellationToken cancellationToken)
        {
            var batchResponse = await _firebaseService.SendEachAsync(messages, _dryRun, cancellationToken);

            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                var response = batchResponse.Responses[i];
                
                // Find the original message ID
                var originalMessage = originalMessages.ElementAtOrDefault(i);
                var messageId = originalMessage?.Id ?? $"message-{Guid.NewGuid()}";
                
                var result = new SendResult(messageId, response.MessageId ?? "unknown");
                result.AdditionalData["Token"] = message.Token;
                result.AdditionalData["Topic"] = message.Topic;
                result.AdditionalData["ProjectId"] = _projectId!;
                result.AdditionalData["DryRun"] = _dryRun;

                if (response.IsSuccess)
                {
                    result.AdditionalData["MessageId"] = response.MessageId;
                }
                else
                {
                    result.AdditionalData["Error"] = response.Exception?.Message ?? "Unknown error";
                }

                // Use the original message ID as key
                results[messageId] = result;
            }
        }
    }
}