//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using FirebaseAdmin.Messaging;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Deveel.Messaging
{
    /// <summary>
    /// Tests for the <see cref="FirebasePushConnector"/> class to verify
    /// its functionality and integration with the Firebase Cloud Messaging API.
    /// </summary>
    public class FirebasePushConnectorTests
    {
        [Fact]
        public void Constructor_WithValidSchemaAndSettings_CreatesConnector()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();

            // Act
            var connector = new FirebasePushConnector(schema, connectionSettings);

            // Assert
            Assert.Same(schema, connector.Schema);
            Assert.Equal(ConnectorState.Uninitialized, connector.State);
        }

        [Fact]
        public void Constructor_WithConnectionSettingsOnly_UsesDefaultSchema()
        {
            // Arrange
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();

            // Act
            var connector = new FirebasePushConnector(connectionSettings);

            // Assert
            Assert.Equal(FirebaseConnectorConstants.Provider, connector.Schema.ChannelProvider);
            Assert.Equal(FirebaseConnectorConstants.PushChannel, connector.Schema.ChannelType);
            Assert.Equal(ConnectorState.Uninitialized, connector.State);
        }

        [Fact]
        public void Constructor_WithNullConnectionSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FirebasePushConnector(schema, null!));
            Assert.Throws<ArgumentNullException>(() => new FirebasePushConnector(null!));
        }

        [Fact]
        public void Constructor_WithLogger_StoresLogger()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();

            // Act
            var connector = new FirebasePushConnector(schema, connectionSettings, null, NullLogger<FirebasePushConnector>.Instance);

            // Assert
            Assert.Same(schema, connector.Schema);
            Assert.Equal(ConnectorState.Uninitialized, connector.State);
        }

        [Fact]
        public async Task InitializeAsync_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            var schema = FirebaseTestSchemas.TestSimplePush; // Use test simple schema instead of production
            var connectionSettings = FirebaseMockFactory.CreateMinimalConnectionSettings();
            var mockFirebaseService = FirebaseMockFactory.CreateMockFirebaseService();
            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);

            // Act
            var result = await connector.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful initialization but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            Assert.Equal(ConnectorState.Ready, connector.State);
            
            // Verify Firebase service was initialized
            mockFirebaseService.Verify(x => x.InitializeAsync(It.IsAny<string>(), "test-project"), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WithMissingProjectId_ReturnsFailure()
        {
            // Arrange
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production
            var connectionSettings = new ConnectionSettings();
            connectionSettings.SetParameter("ServiceAccountKey", FirebaseMockFactory.CreateTestServiceAccountKey());
            var connector = new FirebasePushConnector(schema, connectionSettings);

            // Act
            var result = await connector.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            Assert.Equal(ConnectorErrorCodes.InitializationError, result.Error?.ErrorCode);
            Assert.Contains("ProjectId is required", result.Error?.ErrorMessage);
            Assert.Equal(ConnectorState.Error, connector.State);
        }

        [Fact]
        public async Task InitializeAsync_WithMissingServiceAccountKey_ReturnsFailure()
        {
            // Arrange
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production
            var connectionSettings = new ConnectionSettings();
            connectionSettings.SetParameter("ProjectId", "test-project");
            var connector = new FirebasePushConnector(schema, connectionSettings);

            // Act
            var result = await connector.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            // With the new authentication mechanism, missing ServiceAccountKey returns AUTHENTICATION_FAILED
            // which is more accurate than the generic INITIALIZATION_ERROR
            Assert.Equal("AUTHENTICATION_FAILED", result.Error?.ErrorCode);
            // The error message will be about no suitable authentication configuration found
            Assert.Contains("authentication", result.Error?.ErrorMessage?.ToLower());
            Assert.Equal(ConnectorState.Error, connector.State);
        }

        [Fact]
        public async Task InitializeAsync_WithFirebaseServiceFailure_ReturnsFailure()
        {
            // Arrange
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var mockFirebaseService = FirebaseMockFactory.CreateFailingFirebaseService();
            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);

            // Act
            var result = await connector.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            Assert.Equal(ConnectorErrorCodes.InitializationError, result.Error?.ErrorCode);
            Assert.Equal(ConnectorState.Error, connector.State);
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidConnection_ReturnsSuccess()
        {
            // Arrange
            var connector = await CreateInitializedConnectorAsync();

            // Act
            var result = await connector.TestConnectionAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidConnection_ReturnsFailure()
        {
            // Arrange
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var mockFirebaseService = new Mock<IFirebaseService>();
            mockFirebaseService.SetupGet(x => x.IsInitialized).Returns(true);
            mockFirebaseService.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockFirebaseService.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);
            await connector.InitializeAsync(CancellationToken.None);

            // Act
            var result = await connector.TestConnectionAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Successful);
            Assert.Equal(ConnectorErrorCodes.ConnectionTestError, result.Error?.ErrorCode);
        }

        [Fact]
        public async Task SendMessageAsync_WithDeviceToken_ReturnsSuccess()
        {
            // Arrange
            var mockFirebaseService = CreateRealMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = FirebaseMockFactory.CreateDeviceTokenMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            Assert.NotNull(result.Value);
            Assert.Equal(message.Id, result.Value.MessageId);
            Assert.NotNull(result.Value.RemoteMessageId);

            // Verify Firebase service was called
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Token == "test-device-token" &&
                    m.Notification != null
                ), 
                true,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithTopic_ReturnsSuccess()
        {
            // Arrange
            var mockFirebaseService = CreateRealMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            var message = FirebaseMockFactory.CreateTopicMessage();

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            Assert.NotNull(result.Value);
            Assert.Equal(message.Id, result.Value.MessageId);

            // Verify Firebase service was called
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Topic == "test-topic" &&
                    m.Notification != null
                ), 
                true,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }


        [Fact]
        public async Task GetStatusAsync_ReturnsStatusWithProjectInfo()
        {
            // Arrange
            var connector = await CreateInitializedConnectorAsync();

            // Act
            var result = await connector.GetStatusAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.Contains("ProjectId", result.Value.AdditionalData.Keys);
            Assert.Contains("IsInitialized", result.Value.AdditionalData.Keys);
        }

        [Fact]
        public async Task GetHealthAsync_WithHealthyConnector_ReturnsHealthyStatus()
        {
            // Arrange
            var connector = await CreateInitializedConnectorAsync();

            // Act
            var result = await connector.GetHealthAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.IsHealthy);
            Assert.Equal(ConnectorState.Ready, result.Value.State);
            Assert.Empty(result.Value.Issues);
        }

        [Fact]
        public async Task GetHealthAsync_WithUnhealthyConnector_ReturnsUnhealthyStatus()
        {
            // Arrange
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var mockFirebaseService = new Mock<IFirebaseService>();
            mockFirebaseService.SetupGet(x => x.IsInitialized).Returns(false);
            
            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);

            // Act
            var result = await connector.GetHealthAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Successful);
            Assert.NotNull(result.Value);
            Assert.False(result.Value.IsHealthy);
            Assert.Contains("Firebase service is not initialized", result.Value.Issues);
        }

        [Fact]
        public void Schema_ValidatesDeviceIdEndpoint()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Act
            var deviceEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.DeviceId);

            // Assert
            Assert.NotNull(deviceEndpoint);
            Assert.True(deviceEndpoint.CanSend);
            Assert.False(deviceEndpoint.CanReceive);
            Assert.True(deviceEndpoint.IsRequired);
        }

        [Fact]
        public void Schema_ValidatesTopicEndpoint()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Act
            var topicEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Topic);

            // Assert
            Assert.NotNull(topicEndpoint);
            Assert.True(topicEndpoint.CanSend);
            Assert.False(topicEndpoint.CanReceive);
            Assert.False(topicEndpoint.IsRequired);
        }

        [Fact]
        public void Schema_ValidatesRequiredParameters()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Act
            var projectIdParam = schema.Parameters.FirstOrDefault(p => p.Name == "ProjectId");
            var serviceAccountParam = schema.Parameters.FirstOrDefault(p => p.Name == "ServiceAccountKey");

            // Assert
            Assert.NotNull(projectIdParam);
            Assert.True(projectIdParam.IsRequired);
            Assert.NotNull(serviceAccountParam);
            Assert.True(serviceAccountParam.IsRequired);
            Assert.True(serviceAccountParam.IsSensitive);
        }

        [Fact]
        public void Schema_ValidatesCapabilities()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Act & Assert
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
        }

        [Fact]
        public void SimplePushSchema_RemovesAdvancedFeatures()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.SimplePush;

            // Act & Assert
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            Assert.Null(schema.Parameters.FirstOrDefault(p => p.Name == "DryRun"));
            Assert.Null(schema.MessageProperties.FirstOrDefault(p => p.Name == "ImageUrl"));
            Assert.Null(schema.MessageProperties.FirstOrDefault(p => p.Name == "CustomData"));
        }

        [Fact]
        public void BulkPushSchema_IncludesBulkFeatures()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.BulkPush;

            // Act & Assert
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            Assert.NotNull(schema.MessageProperties.FirstOrDefault(p => p.Name == "ConditionExpression"));
            Assert.NotNull(schema.MessageProperties.FirstOrDefault(p => p.Name == "BatchId"));
        }

        [Fact]
        public void RichPushSchema_IncludesRichFeatures()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.RichPush;

            // Act & Assert
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            Assert.NotNull(schema.MessageProperties.FirstOrDefault(p => p.Name == "Actions"));
            Assert.NotNull(schema.MessageProperties.FirstOrDefault(p => p.Name == "Category"));
            Assert.NotNull(schema.MessageProperties.FirstOrDefault(p => p.Name == "Subtitle"));
        }

        /// <summary>
        /// Helper method to create an initialized connector for testing.
        /// </summary>
        private async Task<FirebasePushConnector> CreateInitializedConnectorAsync()
        {
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production schema
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var mockFirebaseService = FirebaseMockFactory.CreateMockFirebaseService();
            
            var connector = new FirebasePushConnector(schema, connectionSettings, mockFirebaseService.Object);
            var result = await connector.InitializeAsync(CancellationToken.None);
            
            Assert.True(result.Successful, $"Failed to initialize connector: {result.Error?.ErrorMessage}");
            return connector;
        }

        /// <summary>
        /// Helper method to create an initialized connector with a specific Firebase service.
        /// </summary>
        private async Task<FirebasePushConnector> CreateInitializedConnectorAsync(IFirebaseService firebaseService)
        {
            var schema = FirebaseTestSchemas.TestFirebasePush; // Use test schema instead of production schema
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            
            var connector = new FirebasePushConnector(schema, connectionSettings, firebaseService);
            var result = await connector.InitializeAsync(CancellationToken.None);
            
            Assert.True(result.Successful, $"Failed to initialize connector: {result.Error?.ErrorMessage}");
            return connector;
        }

        /// <summary>
        /// Helper method to create an initialized bulk connector.
        /// </summary>
        private async Task<FirebasePushConnector> CreateInitializedBulkConnectorAsync(IFirebaseService firebaseService)
        {
            var schema = FirebaseTestSchemas.TestBulkPush; // Use test bulk schema instead of production schema
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            
            var connector = new FirebasePushConnector(schema, connectionSettings, firebaseService);
            var result = await connector.InitializeAsync(CancellationToken.None);
            
            Assert.True(result.Successful, $"Failed to initialize bulk connector: {result.Error?.ErrorMessage}");
            return connector;
        }

        /// <summary>
        /// Creates a real mock Firebase service that actually works for testing.
        /// </summary>
        private Mock<IFirebaseService> CreateRealMockFirebaseService()
        {
            var mock = new Mock<IFirebaseService>();
            
            mock.SetupGet(x => x.IsInitialized).Returns(true);
            mock.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            
            // Setup SendAsync
            mock.Setup(x => x.SendAsync(It.IsAny<FirebaseAdmin.Messaging.Message>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FirebaseAdmin.Messaging.Message msg, bool dryRun, CancellationToken ct) => $"firebase-msg-{Guid.NewGuid()}");
            
            // Setup SendEachAsync
            mock.Setup(x => x.SendEachAsync(It.IsAny<IEnumerable<FirebaseAdmin.Messaging.Message>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<FirebaseAdmin.Messaging.Message> messages, bool dryRun, CancellationToken ct) =>
                {
                    var responses = messages.Select(m => CreateMockSendResponse($"firebase-{Guid.NewGuid()}", true)).ToList();
                    return CreateMockBatchResponse(responses);
                });
            
            // Setup SendMulticastAsync
            mock.Setup(x => x.SendMulticastAsync(It.IsAny<MulticastMessage>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MulticastMessage msg, bool dryRun, CancellationToken ct) =>
                {
                    var responses = msg.Tokens.Select(token => CreateMockSendResponse($"firebase-{Guid.NewGuid()}", true)).ToList();
                    return CreateMockBatchResponse(responses);
                });
            
            return mock;
        }

        /// <summary>
        /// Creates a mock SendResponse for testing.
        /// </summary>
        private SendResponse CreateMockSendResponse(string messageId, bool isSuccess)
        {
            var mock = new Mock<SendResponse>();
            mock.SetupGet(x => x.MessageId).Returns(messageId);
            mock.SetupGet(x => x.IsSuccess).Returns(isSuccess);
            return mock.Object;
        }

        /// <summary>
        /// Creates a mock BatchResponse for testing.
        /// </summary>
        private BatchResponse CreateMockBatchResponse(IList<SendResponse> responses)
        {
            var mock = new Mock<BatchResponse>();
            mock.SetupGet(x => x.Responses).Returns((IReadOnlyList<SendResponse>)responses.ToList().AsReadOnly());
            mock.SetupGet(x => x.SuccessCount).Returns(responses.Count(r => r.IsSuccess));
            mock.SetupGet(x => x.FailureCount).Returns(responses.Count(r => !r.IsSuccess));
            return mock.Object;
        }
    }
}