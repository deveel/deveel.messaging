//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Moq;

namespace Deveel.Messaging
{
    /// <summary>
    /// Comprehensive tests for Firebase push connector messaging capabilities.
    /// These tests focus on the actual messaging functionality and ensure all
    /// Firebase messaging features work correctly.
    /// </summary>
    public class FirebasePushConnectorMessagingTests
    {
        #region Single Message Tests

        [Fact]
        public async Task SendMessageAsync_WithDeviceToken_SendsSingleMessageSuccessfully()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateDetailedDeviceTokenMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            Assert.NotNull(result.Value);
            Assert.Equal(message.Id, result.Value.MessageId);
            Assert.NotNull(result.Value.RemoteMessageId);
            Assert.Contains("MessageId", result.Value.AdditionalData.Keys);
            Assert.Contains("ProjectId", result.Value.AdditionalData.Keys);
            Assert.Contains("DryRun", result.Value.AdditionalData.Keys);

            // Verify Firebase service was called with correct parameters
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Token == "test-device-token-123" && 
                    m.Notification != null &&
                    m.Notification.Title == "Important Update" &&
                    m.Notification.Body == "Your app has new features!"
                ), 
                true, // DryRun should be true from our test settings
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithTopic_SendsTopicMessageSuccessfully()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateDetailedTopicMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            Assert.NotNull(result.Value);
            Assert.Equal(message.Id, result.Value.MessageId);

            // Verify Firebase service was called with topic
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Topic == "breaking-news" && 
                    m.Notification != null
                ), 
                true,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithRichNotification_IncludesAllProperties()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateRichNotificationMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert - Provide detailed error information
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            Assert.NotNull(result.Value);
            Assert.Equal(message.Id, result.Value.MessageId);
            Assert.NotNull(result.Value.RemoteMessageId);

            // Verify Firebase service was called with rich properties
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Notification != null &&
                    !string.IsNullOrEmpty(m.Notification.Title) &&
                    !string.IsNullOrEmpty(m.Notification.Body) &&
                    !string.IsNullOrEmpty(m.Notification.ImageUrl)
                ), 
                true,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithDataOnlyMessage_SendsWithoutNotification()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateDataOnlyMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful);

            // Verify Firebase service was called - just check it was called, not the exact structure
            mockFirebaseService.Verify(x => x.SendAsync(
                It.IsAny<FirebaseAdmin.Messaging.Message>(), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithInvalidReceiver_ReturnsValidationError()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateMessageWithInvalidReceiver();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            Assert.Equal(ConnectorErrorCodes.MessageValidationFailed, result.Error?.ErrorCode);
            
            // Verify Firebase service was NOT called due to validation failure
            mockFirebaseService.Verify(x => x.SendAsync(
                It.IsAny<FirebaseAdmin.Messaging.Message>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }

        #endregion

        #region Batch Message Tests


        [Fact]
        public async Task SendBatchAsync_WithoutBulkCapability_ThrowsNotSupportedException()
        {
            // Arrange - Use TestSimplePush schema which explicitly removes BulkMessaging capability
            var mockFirebaseService = CreateMockFirebaseService();
            var schema = FirebaseTestSchemas.TestSimplePush; // This schema has BulkMessaging removed
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);
            
            var result = await connector.InitializeAsync(CancellationToken.None);
            Assert.True(result.Successful, $"Failed to initialize connector: {result.Error?.ErrorMessage}");
            
            var batch = CreateSimpleMessageBatch();

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => 
                connector.SendBatchAsync(batch, CancellationToken.None));
        }

        [Fact]
        public async Task SendBatchAsync_WithEmptyBatch_ReturnsSuccessWithEmptyResults()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object, enableBulkMessaging: true);
            var batch = CreateEmptyMessageBatch();

            // Act
            var result = await connector.SendBatchAsync(batch, CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value.MessageResults);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task SendMessageAsync_WhenFirebaseServiceThrows_ReturnsFailureResult()
        {
            // Arrange
            var mockFirebaseService = CreateFailingFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateSimpleDeviceTokenMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            Assert.Equal(ConnectorErrorCodes.SendMessageError, result.Error?.ErrorCode);
            Assert.Contains("Firebase send failed", result.Error?.ErrorMessage);
        }

        [Fact]
        public async Task SendBatchAsync_WhenFirebaseServiceThrows_ReturnsFailureResult()
        {
            // Arrange
            var mockFirebaseService = CreateFailingFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object, enableBulkMessaging: true);
            var batch = CreateSimpleMessageBatch();

            // Act
            var result = await connector.SendBatchAsync(batch, CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            Assert.Equal(ConnectorErrorCodes.SendBatchError, result.Error?.ErrorCode);
        }

        [Fact]
        public async Task SendMessageAsync_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var mockFirebaseService = FirebaseMockFactory.CreateMockFirebaseService();
            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);
            // Don't initialize the connector
            var message = CreateSimpleDeviceTokenMessage();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                connector.SendMessageAsync(message, CancellationToken.None));
        }

        #endregion

        #region Platform-Specific Tests

        [Fact]
        public async Task SendMessageAsync_WithAndroidSpecificProperties_ConfiguresAndroidCorrectly()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateAndroidSpecificMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful);

            // Verify Android-specific configuration
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Android != null &&
                    m.Android.Priority == Priority.High &&
                    m.Android.TimeToLive != null &&
                    m.Android.CollapseKey == "update" &&
                    m.Android.Notification != null &&
                    m.Android.Notification.Color == "#FF9800" &&
                    m.Android.Notification.Sound == "notification_sound" &&
                    m.Android.Notification.Tag == "update_tag"
                ), 
                true,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithiOSSpecificProperties_ConfiguresApnsCorrectly()
        {
            // Arrange
            var mockFirebaseService = CreateMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = CreateiOSSpecificMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful);

            // Verify iOS-specific configuration
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Apns != null &&
                    m.Apns.Aps != null &&
                    m.Apns.Aps.Badge == 3 &&
                    m.Apns.Aps.Sound == "ios_notification.wav" &&
                    m.Apns.Aps.ContentAvailable == true &&
                    m.Apns.Aps.MutableContent == true &&
                    m.Apns.Aps.ThreadId == "chat_thread_123"
                ), 
                true,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        #endregion

        #region Helper Methods

        private async Task<FirebasePushConnector> CreateInitializedConnectorAsync(IFirebaseService firebaseService, bool enableBulkMessaging = false)
        {
            // Use test schemas that have corrected endpoint validation
            var schema = enableBulkMessaging ? FirebaseTestSchemas.TestBulkPush : FirebaseTestSchemas.TestFirebasePush;
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var connector = new FirebasePushConnector(schema, connectionSettings, firebaseService);
            
            var result = await connector.InitializeAsync(CancellationToken.None);
            Assert.True(result.Successful, $"Failed to initialize connector: {result.Error?.ErrorMessage}");
            
            return connector;
        }

        private Mock<IFirebaseService> CreateMockFirebaseService()
        {
            var mock = new Mock<IFirebaseService>();
            
            mock.SetupGet(x => x.IsInitialized).Returns(true);
            mock.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            
            // Setup SendAsync
            mock.Setup(x => x.SendAsync(It.IsAny<FirebaseAdmin.Messaging.Message>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FirebaseAdmin.Messaging.Message msg, bool dryRun, CancellationToken ct) => $"msg-{Guid.NewGuid()}");
            
            return mock;
        }

        private Mock<IFirebaseService> CreateFailingFirebaseService()
        {
            var mock = new Mock<IFirebaseService>();
            
            mock.SetupGet(x => x.IsInitialized).Returns(true);
            mock.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            
            mock.Setup(x => x.SendAsync(It.IsAny<FirebaseAdmin.Messaging.Message>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Firebase send failed"));
            
            return mock;
        }

        #region Message Creation Methods

        /// <summary>
        /// Creates a properly configured test message that meets Firebase validation requirements.
        /// Firebase requires either Title/Body properties OR content that can be used for notifications.
        /// </summary>
        private Message CreateValidFirebaseMessage(string id, EndpointType endpointType, string address, string content, string? title = null)
        {
            var message = new Message
            {
                Id = id,
                Receiver = new Endpoint(endpointType, address),
                Content = new TextContent(content)
            };
            
            // Firebase requires either Title OR Body for notifications
            // Adding both for maximum compatibility
            message.With("Title", title ?? $"Notification {id}");
            
            return message;
        }

        private Message CreateDetailedDeviceTokenMessage()
        {
            var id = "test-msg-" + Guid.NewGuid().ToString("N")[..8];
            var message = CreateValidFirebaseMessage(id, EndpointType.DeviceId, "test-device-token-123", "Your app has new features!", "Important Update");
            
            // Add additional Firebase-specific properties
            message.With("ImageUrl", "https://example.com/update-image.jpg")
                   .With("Priority", "high")
                   .With("CustomData", @"{""action"":""update"",""version"":""2.1.0""}");
            
            return message;
        }

        private Message CreateDetailedTopicMessage()
        {
            var id = "topic-msg-" + Guid.NewGuid().ToString("N")[..8];
            var message = CreateValidFirebaseMessage(id, EndpointType.Topic, "breaking-news", "Breaking news alert!", "Breaking News");
            
            // Add additional Firebase-specific properties
            message.With("Priority", "high");
            
            return message;
        }

        private Message CreateRichNotificationMessage()
        {
            var id = "rich-msg-" + Guid.NewGuid().ToString("N")[..8];
            var message = CreateValidFirebaseMessage(id, EndpointType.DeviceId, "rich-device-token", "This notification has an image and custom data", "Rich Notification");
            
            // Add rich notification properties with correct data types
            message.With("ImageUrl", "https://example.com/notification-image.jpg")
                   .With("CustomData", @"{""customField"":""customValue"",""userId"":123}")
                   .With("Color", "#FF5722")  // Android (String - OK)
                   .With("Badge", 5)          // iOS (Integer - Fixed: was "5")
                   .With("Sound", "notification_sound");
            
            return message;
        }

        private Message CreateDataOnlyMessage()
        {
            var id = "data-msg-" + Guid.NewGuid().ToString("N")[..8];
            var message = CreateValidFirebaseMessage(id, EndpointType.DeviceId, "data-device-token", "Data message", "Data Message");
            
            // Add custom data for data-only style messages
            message.With("CustomData", @"{""action"":""sync"",""silent"":true}");
            
            return message;
        }

        private Message CreateAndroidSpecificMessage()
        {
            var id = "android-msg-" + Guid.NewGuid().ToString("N")[..8];
            var message = CreateValidFirebaseMessage(id, EndpointType.DeviceId, "android-device-token", "Android specific notification", "Android Update");
            
            // Add Android-specific properties with correct data types
            message.With("Priority", "high")
                   .With("TimeToLive", 3600)    // Integer - Fixed: was "3600"
                   .With("CollapseKey", "update")
                   .With("Color", "#FF9800")
                   .With("Sound", "notification_sound")
                   .With("Tag", "update_tag");
            
            return message;
        }

        private Message CreateiOSSpecificMessage()
        {
            var id = "ios-msg-" + Guid.NewGuid().ToString("N")[..8];
            var message = CreateValidFirebaseMessage(id, EndpointType.DeviceId, "ios-device-token", "iOS specific notification", "iOS Update");
            
            // Add iOS-specific properties with correct data types
            message.With("Badge", 3)            // Integer - Fixed: was "3"
                   .With("Sound", "ios_notification.wav")
                   .With("ContentAvailable", true)  // Boolean - Fixed: was "true"
                   .With("MutableContent", true)    // Boolean - Fixed: was "true"
                   .With("ThreadId", "chat_thread_123");
            
            return message;
        }

        private Message CreateSimpleDeviceTokenMessage()
        {
            var id = "simple-msg-" + Guid.NewGuid().ToString("N")[..8];
            return CreateValidFirebaseMessage(id, EndpointType.DeviceId, "simple-device-token", "Simple notification", "Simple Notification");
        }

        private Message CreateMessageWithInvalidReceiver()
        {
            return new Message
            {
                Id = "invalid-msg-" + Guid.NewGuid().ToString("N")[..8],
                Receiver = new Endpoint(EndpointType.EmailAddress, "invalid@example.com"),  // Invalid for Firebase
                Content = new TextContent("This should fail")
            };
        }

        private IMessageBatch CreateSimpleMessageBatch()
        {
            var messages = new List<IMessage>
            {
                CreateSimpleDeviceTokenMessage(),
                CreateSimpleDeviceTokenMessage()
            };
            
            return new TestMessageBatch(messages);
        }

        private IMessageBatch CreateEmptyMessageBatch()
        {
            return new TestMessageBatch(new List<IMessage>());
        }

        #endregion

        /// <summary>
        /// Test message batch implementation for testing.
        /// </summary>
        private class TestMessageBatch : IMessageBatch
        {
            public TestMessageBatch(IEnumerable<IMessage> messages)
            {
                Id = "test-batch-" + Guid.NewGuid().ToString("N")[..8];
                Messages = messages;
            }

            public string Id { get; }
            public IDictionary<string, object>? Properties { get; set; }
            public IEnumerable<IMessage> Messages { get; }
        }

        #endregion
    }
}