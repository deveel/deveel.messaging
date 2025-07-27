//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="ChannelSchema"/> class copy and logical identity functionality.
/// </summary>
public class ChannelSchemaDerivedTests
{
	[Fact]
	public void Constructor_WithSourceSchema_CreatesCorrectCopy()
	{
		// Arrange
		var sourceSchema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.WithDisplayName("Twilio SMS Base")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | ChannelCapability.MessageStatusQuery)
			.AddParameter(new ChannelParameter("AccountSid", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("AuthToken", ParameterType.String) { IsRequired = true, IsSensitive = true })
			.AddParameter(new ChannelParameter("FromNumber", ParameterType.String) { IsRequired = true })
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Media)
			.AddAuthenticationType(AuthenticationType.Token)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
			{
				CanSend = true,
				CanReceive = true
			})
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
			{
				CanSend = false,
				CanReceive = true
			})
			.AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String) { IsRequired = false });

		// Act
		var copiedSchema = new ChannelSchema(sourceSchema, "Custom Twilio SMS");

		// Assert - Core properties must match source (logical identity)
		Assert.Equal("Twilio", copiedSchema.ChannelProvider);
		Assert.Equal("SMS", copiedSchema.ChannelType);
		Assert.Equal("1.0.0", copiedSchema.Version);
		Assert.Equal("Custom Twilio SMS", copiedSchema.DisplayName);
		Assert.Equal(sourceSchema.Capabilities, copiedSchema.Capabilities);
		
		// Verify logical compatibility
		Assert.True(sourceSchema.IsCompatibleWith(copiedSchema));
		Assert.True(copiedSchema.IsCompatibleWith(sourceSchema));
		Assert.Equal(sourceSchema.GetLogicalIdentity(), copiedSchema.GetLogicalIdentity());

		// Verify all collections are copied
		Assert.Equal(sourceSchema.Parameters.Count, copiedSchema.Parameters.Count);
		Assert.Equal(sourceSchema.MessageProperties.Count, copiedSchema.MessageProperties.Count);
		Assert.Equal(sourceSchema.ContentTypes.Count, copiedSchema.ContentTypes.Count);
		Assert.Equal(sourceSchema.AuthenticationTypes.Count, copiedSchema.AuthenticationTypes.Count);
		Assert.Equal(sourceSchema.Endpoints.Count, copiedSchema.Endpoints.Count);

		// Verify parameters are copied
		Assert.Contains(copiedSchema.Parameters, p => p.Name == "AccountSid" && p.IsRequired);
		Assert.Contains(copiedSchema.Parameters, p => p.Name == "AuthToken" && p.IsSensitive);
		Assert.Contains(copiedSchema.Parameters, p => p.Name == "FromNumber");

		// Verify content types are copied
		Assert.Contains(MessageContentType.PlainText, copiedSchema.ContentTypes);
		Assert.Contains(MessageContentType.Media, copiedSchema.ContentTypes);

		// Verify authentication types are copied
		Assert.Contains(AuthenticationType.Token, copiedSchema.AuthenticationTypes);

		// Verify endpoints are copied
		Assert.Contains(copiedSchema.Endpoints, e => e.Type == EndpointType.PhoneNumber && e.CanSend && e.CanReceive);
		Assert.Contains(copiedSchema.Endpoints, e => e.Type == EndpointType.Url && !e.CanSend && e.CanReceive);

		// Verify message properties are copied
		Assert.Contains(copiedSchema.MessageProperties, p => p.Name == "PhoneNumber" && p.IsRequired);
		Assert.Contains(copiedSchema.MessageProperties, p => p.Name == "MessageType" && !p.IsRequired);
	}

	[Fact]
	public void Constructor_WithSourceSchemaAndNoDisplayName_CreatesDefaultDisplayName()
	{
		// Arrange
		var sourceSchema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.WithDisplayName("Twilio SMS Base");

		// Act
		var copiedSchema = new ChannelSchema(sourceSchema);

		// Assert
		Assert.Equal("Twilio SMS Base (Copy)", copiedSchema.DisplayName);
		Assert.Equal(sourceSchema.ChannelProvider, copiedSchema.ChannelProvider);
		Assert.Equal(sourceSchema.ChannelType, copiedSchema.ChannelType);
		Assert.Equal(sourceSchema.Version, copiedSchema.Version);
		Assert.Equal(sourceSchema.GetLogicalIdentity(), copiedSchema.GetLogicalIdentity());
	}

	[Fact]
	public void Constructor_WithNullSourceSchema_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => 
			new ChannelSchema(null!, "Custom Display Name"));
	}

	[Fact]
	public void CopiedSchema_ModificationsAreIndependent_FromSourceSchema()
	{
		// Arrange
		var sourceSchema = new ChannelSchema("Base", "Base", "1.0.0")
			.AddParameter(new ChannelParameter("SharedParam", ParameterType.String))
			.AddContentType(MessageContentType.PlainText)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress));

		var copiedSchema = new ChannelSchema(sourceSchema, "Modified Schema");

		// Act - Modify the copied schema
		copiedSchema.AddParameter(new ChannelParameter("NewParam", ParameterType.Integer));
		copiedSchema.AddContentType(MessageContentType.Html);
		copiedSchema.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber));

		// Assert - Source schema should remain unchanged
		Assert.Single(sourceSchema.Parameters);
		Assert.Single(sourceSchema.ContentTypes);
		Assert.Single(sourceSchema.Endpoints);

		// Copied schema should have the new items
		Assert.Equal(2, copiedSchema.Parameters.Count);
		Assert.Equal(2, copiedSchema.ContentTypes.Count);
		Assert.Equal(2, copiedSchema.Endpoints.Count);

		// Core properties should match (logical identity)
		Assert.Equal(sourceSchema.ChannelProvider, copiedSchema.ChannelProvider);
		Assert.Equal(sourceSchema.ChannelType, copiedSchema.ChannelType);
		Assert.Equal(sourceSchema.Version, copiedSchema.Version);
		Assert.True(sourceSchema.IsCompatibleWith(copiedSchema));
	}

	[Fact]
	public void BaseSchema_CreatedDirectly_HasCorrectProperties()
	{
		// Arrange & Act
		var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Assert
		Assert.Equal("Provider/Type/1.0.0", baseSchema.GetLogicalIdentity());
		Assert.Equal("Provider", baseSchema.ChannelProvider);
		Assert.Equal("Type", baseSchema.ChannelType);
		Assert.Equal("1.0.0", baseSchema.Version);
	}

	[Fact]
	public void CopiedSchema_CorePropertiesMatchSource()
	{
		// Arrange
		var sourceSchema = new ChannelSchema("MyProvider", "MyType", "2.0.0")
			.WithDisplayName("Source Schema");

		// Act
		var copiedSchema = new ChannelSchema(sourceSchema, "Copy Schema");

		// Assert - Core identifying properties must match
		Assert.Equal(sourceSchema.ChannelProvider, copiedSchema.ChannelProvider);
		Assert.Equal(sourceSchema.ChannelType, copiedSchema.ChannelType);
		Assert.Equal(sourceSchema.Version, copiedSchema.Version);
		Assert.Equal(sourceSchema.GetLogicalIdentity(), copiedSchema.GetLogicalIdentity());
		
		// Display name can be different
		Assert.NotEqual(sourceSchema.DisplayName, copiedSchema.DisplayName);
		Assert.Equal("Copy Schema", copiedSchema.DisplayName);
	}

	[Fact]
	public void CopiedSchema_CanRestrictSourceCapabilities()
	{
		// Arrange
		var sourceSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | ChannelCapability.Templates);

		// Act
		var copiedSchema = new ChannelSchema(sourceSchema, "Restricted Schema")
			.RestrictCapabilities(ChannelCapability.SendMessages);

		// Assert
		Assert.True(sourceSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
		Assert.True(sourceSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		
		Assert.True(copiedSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
		Assert.False(copiedSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
		Assert.False(copiedSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		
		// Core properties should still match
		Assert.Equal(sourceSchema.ChannelProvider, copiedSchema.ChannelProvider);
		Assert.Equal(sourceSchema.ChannelType, copiedSchema.ChannelType);
		Assert.Equal(sourceSchema.Version, copiedSchema.Version);
		Assert.True(sourceSchema.IsCompatibleWith(copiedSchema));
	}

	[Fact]
	public void IsCompatibleWith_SameLogicalIdentity_ReturnsTrue()
	{
		// Arrange
		var schema1 = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithDisplayName("Schema 1");
		var schema2 = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithDisplayName("Schema 2");

		// Act & Assert
		Assert.True(schema1.IsCompatibleWith(schema2));
		Assert.True(schema2.IsCompatibleWith(schema1));
		Assert.Equal(schema1.GetLogicalIdentity(), schema2.GetLogicalIdentity());
	}

	[Fact]
	public void IsCompatibleWith_DifferentLogicalIdentity_ReturnsFalse()
	{
		// Arrange
		var schema1 = new ChannelSchema("Provider1", "Type", "1.0.0");
		var schema2 = new ChannelSchema("Provider2", "Type", "1.0.0");
		var schema3 = new ChannelSchema("Provider1", "Type", "2.0.0");

		// Act & Assert
		Assert.False(schema1.IsCompatibleWith(schema2));
		Assert.False(schema1.IsCompatibleWith(schema3));
		Assert.NotEqual(schema1.GetLogicalIdentity(), schema2.GetLogicalIdentity());
		Assert.NotEqual(schema1.GetLogicalIdentity(), schema3.GetLogicalIdentity());
	}

	[Fact]
	public void ValidateAsRestrictionOf_ValidRestriction_ReturnsEmpty()
	{
		// Arrange
		var baseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
			.AddParameter(new ChannelParameter("Param1", ParameterType.String))
			.AddParameter(new ChannelParameter("Param2", ParameterType.String))
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html);

		var restrictedSchema = new ChannelSchema(baseSchema, "Restricted")
			.RestrictCapabilities(ChannelCapability.SendMessages)
			.RemoveParameter("Param2")
			.RestrictContentTypes(MessageContentType.PlainText);

		// Act
		var validationResults = restrictedSchema.ValidateAsRestrictionOf(baseSchema);

		// Assert
		Assert.Empty(validationResults);
	}

	[Fact]
	public void ValidateAsRestrictionOf_IncompatibleSchema_ReturnsValidationError()
	{
		// Arrange
		var schema1 = new ChannelSchema("Provider1", "Type", "1.0.0");
		var schema2 = new ChannelSchema("Provider2", "Type", "1.0.0");

		// Act
		var validationResults = schema1.ValidateAsRestrictionOf(schema2).ToList();

		// Assert
		Assert.Single(validationResults);
		Assert.Contains("Schema is not compatible", validationResults[0].ErrorMessage);
	}

	[Fact]
	public void DerivedSchema_CanRemoveSourceCapabilities()
	{
		// Arrange
		var baseSchema = new ChannelSchema("TestProvider", "SMS", "1.0.0")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url))
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Media);

		// Act - Create a derived schema that restricts capabilities
		var derivedSchema = new ChannelSchema(baseSchema, "SMS Send Only")
			.RemoveCapability(ChannelCapability.ReceiveMessages)
			.RemoveContentType(MessageContentType.Media);

		// Assert - Verify logical identity is preserved
		Assert.Equal("TestProvider", derivedSchema.ChannelProvider);
		Assert.Equal("SMS", derivedSchema.ChannelType);
		Assert.Equal("1.0.0", derivedSchema.Version);
		Assert.Equal("SMS Send Only", derivedSchema.DisplayName);

		// Verify capability restriction
		Assert.Equal(ChannelCapability.SendMessages, derivedSchema.Capabilities);
		Assert.False(derivedSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

		// Verify content type restriction
		Assert.Single(derivedSchema.ContentTypes);
		Assert.Contains(MessageContentType.PlainText, derivedSchema.ContentTypes);
		Assert.DoesNotContain(MessageContentType.Media, derivedSchema.ContentTypes);

		// Verify endpoints are copied
		Assert.Equal(2, derivedSchema.Endpoints.Count);
		Assert.Equal(baseSchema.Endpoints.Count, derivedSchema.Endpoints.Count);
	}

	[Fact]
	public void DerivedSchema_CanUpdateSourceParameter()
	{
		// Arrange
		var baseSchema = new ChannelSchema("TestProvider", "Multi", "1.0.0")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress))
			.AddContentType(MessageContentType.PlainText)
			.AddParameter(new ChannelParameter("TestParam", ParameterType.String) { IsRequired = true });

		// Act - Update parameter configuration
		var derivedSchema = new ChannelSchema(baseSchema, "Modified Schema")
			.UpdateParameter("TestParam", param => 
			{
				param.IsRequired = false;
				param.DefaultValue = "default";
			})
			.RemoveEndpoint(EndpointType.EmailAddress);

		// Assert - Verify logical identity is preserved
		Assert.Equal("TestProvider", derivedSchema.ChannelProvider);
		Assert.Equal("Multi", derivedSchema.ChannelType);
		Assert.Equal("1.0.0", derivedSchema.Version);
		Assert.Equal("Modified Schema", derivedSchema.DisplayName);

		// Verify parameter is updated
		var testParam = derivedSchema.Parameters.Single(p => p.Name == "TestParam");
		Assert.False(testParam.IsRequired);
		Assert.Equal("default", testParam.DefaultValue);

		// Verify endpoint is removed
		Assert.Empty(derivedSchema.Endpoints);
	}
}