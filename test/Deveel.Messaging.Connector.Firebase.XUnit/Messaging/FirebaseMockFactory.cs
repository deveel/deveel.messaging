//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Moq;

namespace Deveel.Messaging
{
    /// <summary>
    /// Factory class for creating mock Firebase service instances and test data.
    /// </summary>
    public static class FirebaseMockFactory
    {
        /// <summary>
        /// Creates a valid connection settings object for Firebase testing.
        /// </summary>
        public static ConnectionSettings CreateValidConnectionSettings()
        {
            var settings = new ConnectionSettings();
            settings.SetParameter("ProjectId", "test-project-123");
            settings.SetParameter("ServiceAccountKey", CreateTestServiceAccountKey());
            settings.SetParameter("DryRun", true);
            return settings;
        }

        /// <summary>
        /// Creates minimal connection settings for basic testing.
        /// </summary>
        public static ConnectionSettings CreateMinimalConnectionSettings()
        {
            var settings = new ConnectionSettings();
            settings.SetParameter("ProjectId", "test-project");
            settings.SetParameter("ServiceAccountKey", CreateTestServiceAccountKey());
            return settings;
        }

        /// <summary>
        /// Creates connection settings with missing required fields.
        /// </summary>
        public static ConnectionSettings CreateInvalidConnectionSettings()
        {
            var settings = new ConnectionSettings();
            settings.SetParameter("DryRun", true);
            // Missing ProjectId and ServiceAccountKey
            return settings;
        }

        /// <summary>
        /// Creates a mock Firebase service that simulates successful operations.
        /// </summary>
        public static Mock<IFirebaseService> CreateMockFirebaseService()
        {
            var mock = new Mock<IFirebaseService>();
            
            mock.SetupGet(x => x.IsInitialized).Returns(true);
            mock.SetupGet(x => x.App).Returns((FirebaseApp?)null);
            
            mock.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            mock.Setup(x => x.SendAsync(It.IsAny<FirebaseAdmin.Messaging.Message>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test-message-id-123");
            
            mock.Setup(x => x.SendEachAsync(It.IsAny<IEnumerable<FirebaseAdmin.Messaging.Message>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<FirebaseAdmin.Messaging.Message> messages, bool dryRun, CancellationToken ct) =>
                {
                    var responses = messages.Select(m => CreateMockSendResponse($"msg-{Guid.NewGuid()}", true)).ToList();
                    return CreateMockBatchResponse(responses);
                });
            
            mock.Setup(x => x.SendMulticastAsync(It.IsAny<MulticastMessage>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MulticastMessage msg, bool dryRun, CancellationToken ct) =>
                {
                    var responses = msg.Tokens.Select(token => CreateMockSendResponse($"msg-{Guid.NewGuid()}", true)).ToList();
                    return CreateMockBatchResponse(responses);
                });
            
            return mock;
        }

        /// <summary>
        /// Creates a mock Firebase service that simulates initialization failure.
        /// </summary>
        public static Mock<IFirebaseService> CreateFailingFirebaseService()
        {
            var mock = new Mock<IFirebaseService>();
            
            mock.SetupGet(x => x.IsInitialized).Returns(false);
            mock.SetupGet(x => x.App).Returns((FirebaseApp?)null);
            
            mock.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Firebase initialization failed"));
            
            mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            return mock;
        }

        /// <summary>
        /// Creates a test device token message.
        /// </summary>
        public static IMessage CreateDeviceTokenMessage(string deviceToken = "test-device-token")
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Receiver = new Endpoint(EndpointType.DeviceId, deviceToken),
                Content = new TextContent("Hello from Firebase!")
            };
            
            // Add some properties using the fluent API
            message.With("Title", "Test Notification")
                   .With("Priority", "high");
            
            return message;
        }

        /// <summary>
        /// Creates a test topic message.
        /// </summary>
        public static IMessage CreateTopicMessage(string topic = "test-topic")
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Receiver = new Endpoint(EndpointType.Topic, topic),
                Content = new TextContent("Hello topic subscribers!")
            };
            
            message.With("Title", "Topic Notification");
            
            return message;
        }

        /// <summary>
        /// Creates a message batch with multiple device tokens.
        /// </summary>
        public static IMessageBatch CreateDeviceTokenBatch(int messageCount = 3)
        {
            var messages = new List<IMessage>();
            
            for (int i = 0; i < messageCount; i++)
            {
                var message = CreateDeviceTokenMessage($"device-token-{i}");
                messages.Add(message);
            }
            
            return new TestMessageBatch(messages);
        }

        /// <summary>
        /// Creates a test service account key JSON string.
        /// </summary>
        public static string CreateTestServiceAccountKey()
        {
            return """
            {
              "type": "service_account",
              "project_id": "test-project-123",
              "private_key_id": "test-key-id",
              "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKB\ntest-private-key-content\n-----END PRIVATE KEY-----\n",
              "client_email": "test@test-project-123.iam.gserviceaccount.com",
              "client_id": "123456789012345678901",
              "auth_uri": "https://accounts.google.com/o/oauth2/auth",
              "token_uri": "https://oauth2.googleapis.com/token",
              "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
              "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/test%40test-project-123.iam.gserviceaccount.com"
            }
            """;
        }

        /// <summary>
        /// Creates a mock SendResponse for testing.
        /// </summary>
        private static SendResponse CreateMockSendResponse(string messageId, bool isSuccess)
        {
            // Since SendResponse properties are read-only, we need to use reflection or create a mock
            var mock = new Mock<SendResponse>();
            mock.SetupGet(x => x.MessageId).Returns(messageId);
            mock.SetupGet(x => x.IsSuccess).Returns(isSuccess);
            // Don't set Exception property for now to avoid nullable issues
            return mock.Object;
        }

        /// <summary>
        /// Creates a mock BatchResponse for testing.
        /// </summary>
        private static BatchResponse CreateMockBatchResponse(IList<SendResponse> responses)
        {
            var mock = new Mock<BatchResponse>();
            mock.SetupGet(x => x.Responses).Returns((IReadOnlyList<SendResponse>)responses.ToList().AsReadOnly());
            mock.SetupGet(x => x.SuccessCount).Returns(responses.Count(r => r.IsSuccess));
            mock.SetupGet(x => x.FailureCount).Returns(responses.Count(r => !r.IsSuccess));
            return mock.Object;
        }
    }

    /// <summary>
    /// Simple message batch implementation for testing.
    /// </summary>
    public class TestMessageBatch : IMessageBatch
    {
        public TestMessageBatch(IEnumerable<IMessage> messages)
        {
            Id = Guid.NewGuid().ToString();
            Messages = messages;
        }

        public string Id { get; }
        public IDictionary<string, object>? Properties { get; set; }
        public IEnumerable<IMessage> Messages { get; }
    }
}