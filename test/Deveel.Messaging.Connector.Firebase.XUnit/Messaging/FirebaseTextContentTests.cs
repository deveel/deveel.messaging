//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using Moq;

namespace Deveel.Messaging
{
    /// <summary>
    /// Tests for Firebase TextContent integration, ensuring that notification body
    /// text is properly sourced from message TextContent instead of Body property.
    /// </summary>
    public class FirebaseTextContentTests
    {
        [Fact]
        public async Task SendMessageAsync_WithTextContent_UsesTextForNotificationBody()
        {
            // Arrange
            var mockFirebaseService = CreateInspectingMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            
            var message = new Message
            {
                Id = "text-content-test",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("This text should be the notification body")
            };
            
            // Only add title - body should come from TextContent
            message.With("Title", "Test Notification");

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Notification != null &&
                    m.Notification.Title == "Test Notification" &&
                    m.Notification.Body == "This text should be the notification body"
                ), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithTextContentAndBodyProperty_PrefersTextContent()
        {
            // Arrange
            var mockFirebaseService = CreateInspectingMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            
            var message = new Message
            {
                Id = "priority-test",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("Text from TextContent")
            };
            
            message.With("Title", "Priority Test");
            // Note: Body property is no longer part of the schema, so we test TextContent only

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Notification != null &&
                    m.Notification.Body == "Text from TextContent"
                ), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyTextContent_CreatesDataOnlyMessage()
        {
            // Arrange
            var mockFirebaseService = CreateInspectingMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            
            var message = new Message
            {
                Id = "empty-content-test",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("") // Empty TextContent
            };
            
            message.With("Title", "Empty Content Test");
            // Add some data to make it a valid data-only message
            message.With("CustomData", @"{""action"":""silent""}");

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            
            // Should create a data-only message with title but no body
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Data != null &&
                    m.Data.ContainsKey("action") &&
                    m.Data["action"] == "silent"
                ), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithNullTextContent_CreatesDataOnlyMessage()
        {
            // Arrange
            var mockFirebaseService = CreateInspectingMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            
            var message = new Message
            {
                Id = "null-content-test",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent(null) // Null text
            };
            
            message.With("Title", "Null Content Test");
            message.With("CustomData", @"{""type"":""silent""}");

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            
            // Should create a data-only message
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Data != null &&
                    m.Data.ContainsKey("type") &&
                    m.Data["type"] == "silent"
                ), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithNonTextContent_CreatesDataOnlyMessage()
        {
            // Arrange
            var mockFirebaseService = CreateInspectingMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            
            var message = new Message
            {
                Id = "json-content-test",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new JsonContent(@"{""key"":""value""}")
            };
            
            message.With("Title", "JSON Content Test");
            message.With("CustomData", @"{""source"":""json""}");

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            
            // Should create a data-only message since JsonContent doesn't provide notification body
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Data != null &&
                    m.Data.ContainsKey("source") &&
                    m.Data["source"] == "json"
                ), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithOnlyTextContent_CreatesNotificationWithOnlyBody()
        {
            // Arrange
            var mockFirebaseService = CreateInspectingMockFirebaseService();
            var connector = await CreateInitializedConnectorAsync(mockFirebaseService.Object);
            
            var message = new Message
            {
                Id = "body-only-test",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("Only body text, no title")
            };
            
            // Don't add any properties - only content

            // Act
            var result = await connector.SendMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.True(result.Successful, $"Expected successful send but got: {result.Error?.ErrorCode} - {result.Error?.ErrorMessage}");
            
            mockFirebaseService.Verify(x => x.SendAsync(
                It.Is<FirebaseAdmin.Messaging.Message>(m => 
                    m.Notification != null &&
                    m.Notification.Title == null &&
                    m.Notification.Body == "Only body text, no title"
                ), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        #region Helper Methods

        private async Task<FirebasePushConnector> CreateInitializedConnectorAsync(IFirebaseService firebaseService)
        {
            var schema = FirebaseTestSchemas.TestFirebasePush;
            var connectionSettings = FirebaseMockFactory.CreateValidConnectionSettings();
            var connector = new FirebasePushConnector(schema, connectionSettings, firebaseService);
            
            var result = await connector.InitializeAsync(CancellationToken.None);
            Assert.True(result.Successful, $"Failed to initialize connector: {result.Error?.ErrorMessage}");
            
            return connector;
        }

        private Mock<IFirebaseService> CreateInspectingMockFirebaseService()
        {
            var mock = new Mock<IFirebaseService>();
            
            mock.SetupGet(x => x.IsInitialized).Returns(true);
            mock.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            
            mock.Setup(x => x.SendAsync(It.IsAny<FirebaseAdmin.Messaging.Message>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FirebaseAdmin.Messaging.Message msg, bool dryRun, CancellationToken ct) => $"msg-{Guid.NewGuid()}");
            
            return mock;
        }

        #endregion
    }
}