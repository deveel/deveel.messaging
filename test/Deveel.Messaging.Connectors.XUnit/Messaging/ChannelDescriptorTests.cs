//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;
using Deveel.Messaging;
using Xunit;

namespace Deveel.Messaging.XUnit
{
	/// <summary>
	/// Tests for the ConnectorDescriptor class functionality.
	/// </summary>
	public class ConnectorDescriptorTests
	{
		[Fact]
		public void Constructor_WithValidParameters_SetsProperties()
		{
			// Arrange
			var connectorType = typeof(TestConnector);
			var schema = CreateTestSchema();

			// Act
			var descriptor = new ConnectorDescriptor(connectorType, schema);

			// Assert
			Assert.Equal(connectorType, descriptor.ConnectorType);
			Assert.Equal(schema, descriptor.Schema);
			Assert.Equal("TestProvider", descriptor.ChannelProvider);
			Assert.Equal("TestType", descriptor.ChannelType);
		}

		[Fact]
		public void Constructor_WithNullConnectorType_ThrowsException()
		{
			// Arrange
			var schema = CreateTestSchema();

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => new ConnectorDescriptor(null!, schema));
		}

		[Fact]
		public void Constructor_WithNullSchema_ThrowsException()
		{
			// Arrange
			var connectorType = typeof(TestConnector);

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => new ConnectorDescriptor(connectorType, null!));
		}

		[Fact]
		public void DisplayName_WhenSchemaHasDisplayName_ReturnsSchemaDisplayName()
		{
			// Arrange
			var connectorType = typeof(TestConnector);
			var schema = new ChannelSchema("TestProvider", "TestType", "1.0.0")
				.WithDisplayName("Custom Display Name")
				.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
				.HandlesMessageEndpoint(EndpointType.PhoneNumber)
				.AddContentType(MessageContentType.PlainText)
				.AddAuthenticationType(AuthenticationType.Basic);
			var descriptor = new ConnectorDescriptor(connectorType, schema);

			// Act
			var displayName = descriptor.DisplayName;

			// Assert
			Assert.Equal("Custom Display Name", displayName);
		}

		[Fact]
		public void DisplayName_WhenSchemaHasNoDisplayName_ReturnsConnectorTypeName()
		{
			// Arrange
			var connectorType = typeof(TestConnector);
			var schema = CreateTestSchema();
			var descriptor = new ConnectorDescriptor(connectorType, schema);

			// Act
			var displayName = descriptor.DisplayName;

			// Assert
			Assert.Equal("TestConnector", displayName);
		}

		[Fact]
		public void SupportsCapability_WithSupportedCapability_ReturnsTrue()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsCapability(ChannelCapability.SendMessages);

			// Assert
			Assert.True(supports);
		}

		[Fact]
		public void SupportsCapability_WithUnsupportedCapability_ReturnsFalse()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsCapability(ChannelCapability.Templates);

			// Assert
			Assert.False(supports);
		}

		[Fact]
		public void SupportsAnyCapability_WithOneSupportedCapability_ReturnsTrue()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsAnyCapability(ChannelCapability.SendMessages | ChannelCapability.Templates);

			// Assert
			Assert.True(supports);
		}

		[Fact]
		public void SupportsAnyCapability_WithNoSupportedCapabilities_ReturnsFalse()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsAnyCapability(ChannelCapability.Templates | ChannelCapability.MediaAttachments);

			// Assert
			Assert.False(supports);
		}

		[Fact]
		public void SupportsAllCapabilities_WithAllSupportedCapabilities_ReturnsTrue()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsAllCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages);

			// Assert
			Assert.True(supports);
		}

		[Fact]
		public void SupportsAllCapabilities_WithPartiallySupported_ReturnsFalse()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsAllCapabilities(ChannelCapability.SendMessages | ChannelCapability.Templates);

			// Assert
			Assert.False(supports);
		}

		[Fact]
		public void SupportsContentType_WithSupportedContentType_ReturnsTrue()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsContentType(MessageContentType.PlainText);

			// Assert
			Assert.True(supports);
		}

		[Fact]
		public void SupportsContentType_WithUnsupportedContentType_ReturnsFalse()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsContentType(MessageContentType.Html);

			// Assert
			Assert.False(supports);
		}

		[Fact]
		public void SupportsEndpointType_WithSupportedEndpointType_ReturnsTrue()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsEndpointType(EndpointType.PhoneNumber);

			// Assert
			Assert.True(supports);
		}

		[Fact]
		public void SupportsEndpointType_WithUnsupportedEndpointType_ReturnsFalse()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsEndpointType(EndpointType.EmailAddress);

			// Assert
			Assert.False(supports);
		}

		[Fact]
		public void SupportsAuthenticationType_WithSupportedAuthenticationType_ReturnsTrue()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsAuthenticationType(AuthenticationType.Basic);

			// Assert
			Assert.True(supports);
		}

		[Fact]
		public void SupportsAuthenticationType_WithUnsupportedAuthenticationType_ReturnsFalse()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var supports = descriptor.SupportsAuthenticationType(AuthenticationType.ApiKey);

			// Assert
			Assert.False(supports);
		}

		[Fact]
		public void GetLogicalIdentity_ReturnsCorrectFormat()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var identity = descriptor.GetLogicalIdentity();

			// Assert
			Assert.Equal("TestProvider/TestType/1.0.0", identity);
		}

		[Fact]
		public void ToString_ReturnsFormattedString()
		{
			// Arrange
			var descriptor = CreateTestDescriptor();

			// Act
			var result = descriptor.ToString();

			// Assert
			Assert.Equal("TestConnector (TestProvider/TestType/1.0.0)", result);
		}

		[Fact]
		public void Equals_WithSameConnectorType_ReturnsTrue()
		{
			// Arrange
			var descriptor1 = CreateTestDescriptor();
			var descriptor2 = CreateTestDescriptor();

			// Act & Assert
			Assert.True(descriptor1.Equals(descriptor2));
			Assert.Equal(descriptor1.GetHashCode(), descriptor2.GetHashCode());
		}

		[Fact]
		public void Equals_WithDifferentConnectorType_ReturnsFalse()
		{
			// Arrange
			var descriptor1 = CreateTestDescriptor();
			var descriptor2 = new ConnectorDescriptor(typeof(string), CreateTestSchema());

			// Act & Assert
			Assert.False(descriptor1.Equals(descriptor2));
		}

		private static ConnectorDescriptor CreateTestDescriptor()
		{
			return new ConnectorDescriptor(typeof(TestConnector), CreateTestSchema());
		}

		private static IChannelSchema CreateTestSchema()
		{
			return new ChannelSchema("TestProvider", "TestType", "1.0.0")
				.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
				.HandlesMessageEndpoint(EndpointType.PhoneNumber)
				.AddContentType(MessageContentType.PlainText)
				.AddAuthenticationType(AuthenticationType.Basic);
		}

		// Simple test connector class that doesn't need to implement IChannelConnector fully
		private class TestConnector
		{
		}
	}
}