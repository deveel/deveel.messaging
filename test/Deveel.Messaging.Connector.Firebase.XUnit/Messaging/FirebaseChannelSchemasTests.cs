//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
    /// <summary>
    /// Tests for the Firebase channel schemas to verify their configurations and validations.
    /// </summary>
    public class FirebaseChannelSchemasTests
    {
        [Fact]
        public void FirebasePush_HasCorrectProviderAndType()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            Assert.Equal(FirebaseConnectorConstants.Provider, schema.ChannelProvider);
            Assert.Equal(FirebaseConnectorConstants.PushChannel, schema.ChannelType);
            Assert.Equal("1.0.0", schema.Version);
            Assert.Equal("Firebase Cloud Messaging (FCM) Connector", schema.DisplayName);
        }

        [Fact]
        public void FirebasePush_HasCorrectCapabilities()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.HandleMessageState));
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.Templates));
        }

        [Fact]
        public void FirebasePush_HasRequiredParameters()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            var projectIdParam = schema.Parameters.FirstOrDefault(p => p.Name == "ProjectId");
            Assert.NotNull(projectIdParam);
            Assert.True(projectIdParam.IsRequired);
            Assert.Equal(DataType.String, projectIdParam.DataType);

            var serviceAccountParam = schema.Parameters.FirstOrDefault(p => p.Name == "ServiceAccountKey");
            Assert.NotNull(serviceAccountParam);
            Assert.True(serviceAccountParam.IsRequired);
            Assert.True(serviceAccountParam.IsSensitive);
            Assert.Equal(DataType.String, serviceAccountParam.DataType);

            var dryRunParam = schema.Parameters.FirstOrDefault(p => p.Name == "DryRun");
            Assert.NotNull(dryRunParam);
            Assert.False(dryRunParam.IsRequired);
            Assert.Equal(DataType.Boolean, dryRunParam.DataType);
            Assert.Equal(false, dryRunParam.DefaultValue);
        }

        [Fact]
        public void FirebasePush_HasCorrectEndpoints()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            var deviceEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.DeviceId);
            Assert.NotNull(deviceEndpoint);
            Assert.True(deviceEndpoint.CanSend);
            Assert.False(deviceEndpoint.CanReceive);
            Assert.True(deviceEndpoint.IsRequired);

            var topicEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Topic);
            Assert.NotNull(topicEndpoint);
            Assert.True(topicEndpoint.CanSend);
            Assert.False(topicEndpoint.CanReceive);
            Assert.False(topicEndpoint.IsRequired);
        }

        [Fact]
        public void FirebasePush_HasCorrectContentTypes()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            Assert.Contains(MessageContentType.Json, schema.ContentTypes);
            Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
        }

        [Fact]
        public void FirebasePush_HasCorrectAuthenticationType()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            Assert.Single(schema.AuthenticationConfigurations);
            Assert.Equal(AuthenticationType.ApiKey, schema.AuthenticationConfigurations.First().AuthenticationType);
        }

        [Fact]
        public void FirebasePush_ValidatesMessageProperties()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;

            // Assert
            var titleProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Title");
            Assert.NotNull(titleProp);
            Assert.Equal(DataType.String, titleProp.DataType);
            Assert.Equal(FirebaseConnectorConstants.MaxTitleLength, titleProp.MaxLength);

            var bodyProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Body");
            Assert.NotNull(bodyProp);
            Assert.Equal(DataType.String, bodyProp.DataType);
            Assert.Equal(FirebaseConnectorConstants.MaxBodyLength, bodyProp.MaxLength);

            var priorityProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Priority");
            Assert.NotNull(priorityProp);
            Assert.Contains("normal", priorityProp.AllowedValues!);
            Assert.Contains("high", priorityProp.AllowedValues!);
        }

        [Fact]
        public void SimplePush_DerivesFromFirebasePush()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.SimplePush;

            // Assert
            Assert.Equal(FirebaseConnectorConstants.Provider, schema.ChannelProvider);
            Assert.Equal(FirebaseConnectorConstants.PushChannel, schema.ChannelType);
            Assert.Equal("Firebase Simple Push", schema.DisplayName);
            
            // Should have removed bulk messaging capability
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            
            // Should have removed advanced properties
            Assert.Null(schema.Parameters.FirstOrDefault(p => p.Name == "DryRun"));
            Assert.Null(schema.MessageProperties.FirstOrDefault(p => p.Name == "ImageUrl"));
            Assert.Null(schema.MessageProperties.FirstOrDefault(p => p.Name == "CustomData"));
        }

        [Fact]
        public void BulkPush_HasAdditionalBulkProperties()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.BulkPush;

            // Assert
            Assert.Equal("Firebase Bulk Push", schema.DisplayName);
            Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            
            var conditionProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "ConditionExpression");
            Assert.NotNull(conditionProp);
            Assert.Equal(DataType.String, conditionProp.DataType);

            var batchIdProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "BatchId");
            Assert.NotNull(batchIdProp);
            Assert.Equal(DataType.String, batchIdProp.DataType);
        }

        [Fact]
        public void RichPush_HasRichMediaProperties()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.RichPush;

            // Assert
            Assert.Equal("Firebase Rich Push", schema.DisplayName);
            Assert.False(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
            
            var actionsProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Actions");
            Assert.NotNull(actionsProp);
            Assert.Equal(DataType.String, actionsProp.DataType);

            var categoryProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Category");
            Assert.NotNull(categoryProp);
            Assert.Equal(DataType.String, categoryProp.DataType);

            var subtitleProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Subtitle");
            Assert.NotNull(subtitleProp);
            Assert.Equal(DataType.String, subtitleProp.DataType);
            Assert.Equal(256, subtitleProp.MaxLength);
        }

        [Theory]
        [InlineData("http://example.com/image.png", true)]
        [InlineData("https://example.com/image.png", true)]
        [InlineData("ftp://example.com/image.png", false)]
        [InlineData("not-a-url", false)]
        [InlineData("", true)] // Empty should be valid (optional field)
        [InlineData(null, true)] // Null should be valid (optional field)
        public void ImageUrl_Validation_WorksCorrectly(string? imageUrl, bool shouldBeValid)
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            var imageUrlProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "ImageUrl");
            Assert.NotNull(imageUrlProp);

            // Act
            var validationResults = imageUrlProp.Validate(imageUrl).ToList();

            // Assert
            if (shouldBeValid)
            {
                Assert.Empty(validationResults);
            }
            else
            {
                Assert.NotEmpty(validationResults);
            }
        }

        [Theory]
        [InlineData("#ff0000", true)]
        [InlineData("#FF0000", true)]
        [InlineData("#00ff00aa", true)]
        [InlineData("ff0000", false)]
        [InlineData("#gg0000", false)]
        [InlineData("#ff00", false)]
        [InlineData("", true)] // Empty should be valid (optional field)
        [InlineData(null, true)] // Null should be valid (optional field)
        public void Color_Validation_WorksCorrectly(string? color, bool shouldBeValid)
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            var colorProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Color");
            Assert.NotNull(colorProp);

            // Act
            var validationResults = colorProp.Validate(color).ToList();

            // Assert
            if (shouldBeValid)
            {
                Assert.Empty(validationResults);
            }
            else
            {
                Assert.NotEmpty(validationResults);
            }
        }

        [Theory]
        [InlineData("{\"key\": \"value\"}", true)]
        [InlineData("{\"number\": 123}", true)]
        [InlineData("invalid json", false)]
        [InlineData("", true)] // Empty should be valid (optional field)
        [InlineData(null, true)] // Null should be valid (optional field)
        public void CustomData_JsonValidation_WorksCorrectly(string? jsonData, bool shouldBeValid)
        {
            // Arrange
            var schema = FirebaseChannelSchemas.FirebasePush;
            var customDataProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "CustomData");
            Assert.NotNull(customDataProp);

            // Act
            var validationResults = customDataProp.Validate(jsonData).ToList();

            // Assert
            if (shouldBeValid)
            {
                Assert.Empty(validationResults);
            }
            else
            {
                Assert.NotEmpty(validationResults);
            }
        }

        [Theory]
        [InlineData("'topicA' && 'topicB'", true)]
        [InlineData("'topicA' || 'topicB'", true)]
        [InlineData("('topicA' && 'topicB') || 'topicC'", true)]
        [InlineData("invalid condition", false)]
        [InlineData("", true)] // Empty should be valid (optional field)
        [InlineData(null, true)] // Null should be valid (optional field)
        public void ConditionExpression_Validation_WorksCorrectly(string? condition, bool shouldBeValid)
        {
            // Arrange
            var schema = FirebaseChannelSchemas.BulkPush;
            var conditionProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "ConditionExpression");
            Assert.NotNull(conditionProp);

            // Act
            var validationResults = conditionProp.Validate(condition).ToList();

            // Assert
            if (shouldBeValid)
            {
                Assert.Empty(validationResults);
            }
            else
            {
                Assert.NotEmpty(validationResults);
            }
        }

        [Theory]
        [InlineData("[{\"action\": \"view\", \"title\": \"View\"}]", true)]
        [InlineData("[{\"action\": \"share\"}, {\"action\": \"dismiss\"}]", true)]
        [InlineData("[{\"title\": \"No Action\"}]", false)] // Missing action property
        [InlineData("not an array", false)]
        [InlineData("", true)] // Empty should be valid (optional field)
        [InlineData(null, true)] // Null should be valid (optional field)
        public void Actions_JsonValidation_WorksCorrectly(string? actionsJson, bool shouldBeValid)
        {
            // Arrange
            var schema = FirebaseChannelSchemas.RichPush;
            var actionsProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Actions");
            Assert.NotNull(actionsProp);

            // Act
            var validationResults = actionsProp.Validate(actionsJson).ToList();

            // Assert
            if (shouldBeValid)
            {
                Assert.Empty(validationResults);
            }
            else
            {
                Assert.NotEmpty(validationResults);
            }
        }

        [Fact]
        public void TimeToLive_HasCorrectRange()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;
            var ttlProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "TimeToLive");

            // Assert
            Assert.NotNull(ttlProp);
            Assert.Equal(DataType.Integer, ttlProp.DataType);
            Assert.Equal(0, ttlProp.MinValue);
            Assert.Equal(2419200, ttlProp.MaxValue); // 4 weeks in seconds
        }

        [Fact]
        public void Badge_HasCorrectRange()
        {
            // Arrange & Act
            var schema = FirebaseChannelSchemas.FirebasePush;
            var badgeProp = schema.MessageProperties.FirstOrDefault(p => p.Name == "Badge");

            // Assert
            Assert.NotNull(badgeProp);
            Assert.Equal(DataType.Integer, badgeProp.DataType);
            Assert.Equal(0, badgeProp.MinValue);
        }
    }
}