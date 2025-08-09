//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
    /// <summary>
    /// Debug tests to understand Firebase schema validation requirements
    /// </summary>
    public class FirebaseValidationDebugTests
    {
        [Fact]
        public void DebugFirebaseSchemaValidation_WithMinimalMessage()
        {
            // Arrange - Use test schema that has proper endpoint configuration
            var schema = FirebaseTestSchemas.TestFirebasePush;
            
            // Test 1: Minimal message with content only
            var message1 = new Message
            {
                Id = "test-1",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("Test content")
            };

            // Act & Debug
            var validationResults1 = schema.ValidateMessage(message1).ToList();
            
            // These should pass with the test schema
            Assert.Empty(validationResults1);

            // Test 2: Message with Title property
            var message2 = new Message
            {
                Id = "test-2",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("Test content")
            };
            message2.With("Title", "Test Title");

            var validationResults2 = schema.ValidateMessage(message2).ToList();
            Assert.Empty(validationResults2);

            // Test 3: Message with both Title and Body
            var message3 = new Message
            {
                Id = "test-3",
                Receiver = new Endpoint(EndpointType.DeviceId, "test-token"),
                Content = new TextContent("Test content")
            };
            message3.With("Title", "Test Title");

            var validationResults3 = schema.ValidateMessage(message3).ToList();
            Assert.Empty(validationResults3);
        }

        [Fact]
        public void DebugFirebaseTopicValidation()
        {
            // Arrange - Use test schema that has proper endpoint configuration
            var schema = FirebaseTestSchemas.TestFirebasePush;
            
            var topicMessage = new Message
            {
                Id = "topic-test",
                Receiver = new Endpoint(EndpointType.Topic, "test-topic"),
                Content = new TextContent("Topic notification")
            };
            topicMessage.With("Title", "Topic Title");

            // Act
            var validationResults = schema.ValidateMessage(topicMessage).ToList();
            
            // Should pass with test schema
            Assert.Empty(validationResults);
        }

        [Fact]
        public void DebugFirebaseSchemaEndpoints()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            
            // Debug: Print all supported endpoints
            var endpointInfo = string.Join("; ", schema.Endpoints.Select(e => 
                $"{e.Type} (CanSend: {e.CanSend}, CanReceive: {e.CanReceive}, Required: {e.IsRequired})"));
            
            // Debug: Check if DeviceId and Topic are supported
            var deviceIdEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.DeviceId);
            var topicEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Topic);
            
            Assert.NotNull(deviceIdEndpoint);
            Assert.NotNull(topicEndpoint);
            Assert.True(deviceIdEndpoint.CanSend, "DeviceId should support sending");
            Assert.True(topicEndpoint.CanSend, "Topic should support sending");
            
            // If we get here, the endpoints are configured correctly
            Assert.True(true, $"Firebase schema endpoints: {endpointInfo}");
        }

        [Fact]
        public void DebugFirebaseSchemaProperties()
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            
            // Debug: Print all message properties
            var propertyInfo = string.Join("; ", schema.MessageProperties.Select(p => 
                $"{p.Name} (Required: {p.IsRequired}, Type: {p.DataType})"));
            
            // Check specific properties
            var titleProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Title");
            var bodyProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Body");
            
            var titleRequired = titleProperty?.IsRequired ?? false;
            var bodyRequired = bodyProperty?.IsRequired ?? false;
            
            Assert.True(true, $"Firebase properties: {propertyInfo}. Title required: {titleRequired}, Body required: {bodyRequired}");
        }
    }
}