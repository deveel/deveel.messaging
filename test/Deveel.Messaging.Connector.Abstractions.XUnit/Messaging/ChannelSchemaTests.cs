namespace Deveel.Messaging;

/// <summary>
/// Tests for the <see cref="ChannelSchema"/> class to verify correct implementation
/// of the <see cref="IChannelSchema"/> interface.
/// </summary>
public class ChannelSchemaTests
{
	[Fact]
	public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
	{
		// Arrange
		const string channelProvider = "TestProvider";
		const string channelType = "Email";
		const string version = "1.0.0";

		// Act
		var schema = new ChannelSchema(channelProvider, channelType, version);

		// Assert
		Assert.Equal(channelProvider, schema.ChannelProvider);
		Assert.Equal(channelType, schema.ChannelType);
		Assert.Equal(version, schema.Version);
		Assert.Null(schema.DisplayName);
		Assert.Equal(ChannelCapability.SendMessages, schema.Capabilities);
		Assert.NotNull(schema.Parameters);
		Assert.Empty(schema.Parameters);
		Assert.NotNull(schema.MessageProperties);
		Assert.Empty(schema.MessageProperties);
		Assert.NotNull(schema.ContentTypes);
		Assert.Empty(schema.ContentTypes);
		Assert.NotNull(schema.AuthenticationTypes);
		Assert.Empty(schema.AuthenticationTypes);
		Assert.NotNull(schema.Endpoints);
		Assert.Empty(schema.Endpoints);
	}

	[Theory]
	[InlineData(null, "Email", "1.0.0")]
	[InlineData("", "Email", "1.0.0")]
	[InlineData("   ", "Email", "1.0.0")]
	[InlineData("TestProvider", null, "1.0.0")]
	[InlineData("TestProvider", "", "1.0.0")]
	[InlineData("TestProvider", "   ", "1.0.0")]
	[InlineData("TestProvider", "Email", null)]
	[InlineData("TestProvider", "Email", "")]
	[InlineData("TestProvider", "Email", "   ")]
	public void Constructor_WithInvalidParameters_ThrowsArgumentException(
		string channelProvider, string channelType, string version)
	{
		// Act & Assert
		Assert.ThrowsAny<ArgumentException>(() => 
			new ChannelSchema(channelProvider, channelType, version));
	}

	[Fact]
	public void AddParameter_WithValidParameter_AddsToParametersList()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var parameter = new ChannelParameter("TestParam", DataType.String)
		{
			IsRequired = true,
			Description = "Test parameter"
		};

		// Act
		var result = schema.AddParameter(parameter);

		// Assert
		Assert.Same(schema, result);
		Assert.Contains(parameter, schema.Parameters);
		Assert.Single(schema.Parameters);
	}

	[Fact]
	public void AddParameter_WithNullParameter_ThrowsArgumentNullException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => schema.AddParameter(null!));
	}

	[Fact]
	public void AddContentType_WithValidContentType_AddsToContentTypesList()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		const MessageContentType contentType = MessageContentType.Html;

		// Act
		var result = schema.AddContentType(contentType);

		// Assert
		Assert.Same(schema, result);
		Assert.Contains(contentType, schema.ContentTypes);
		Assert.Single(schema.ContentTypes);
	}

	[Fact]
	public void AddContentType_WithMultipleContentTypes_AddsAllToList()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		schema.AddContentType(MessageContentType.PlainText)
			  .AddContentType(MessageContentType.Html)
			  .AddContentType(MessageContentType.Json);

		// Assert
		Assert.Equal(3, schema.ContentTypes.Count);
		Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
		Assert.Contains(MessageContentType.Html, schema.ContentTypes);
		Assert.Contains(MessageContentType.Json, schema.ContentTypes);
	}

	[Fact]
	public void AddAuthenticationType_WithValidAuthenticationType_AddsToAuthenticationTypesList()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		const AuthenticationType authType = AuthenticationType.Basic;

		// Act
		var result = schema.AddAuthenticationType(authType);

		// Assert
		Assert.Same(schema, result);
		Assert.Contains(authType, schema.AuthenticationTypes);
		Assert.Single(schema.AuthenticationTypes);
	}

	#region Endpoint Configuration Tests

	[Fact]
	public void HandlesMessageEndpoint_WithValidEndpoint_AddsToEndpointsList()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var endpoint = new ChannelEndpointConfiguration(EndpointType.EmailAddress)
		{
			CanSend = true,
			CanReceive = false,
			IsRequired = true
		};

		// Act
		var result = schema.HandlesMessageEndpoint(endpoint);

		// Assert
		Assert.Same(schema, result);
		Assert.Contains(endpoint, schema.Endpoints);
		Assert.Single(schema.Endpoints);
	}

	[Fact]
	public void HandlesMessageEndpoint_WithNullEndpoint_ThrowsArgumentNullException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => schema.HandlesMessageEndpoint(null!));
	}

	[Fact]
	public void HandlesMessageEndpoint_WithValidType_AddsEndpointWithDefaults()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		var result = schema.HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
		{
			e.CanSend = true;
			e.CanReceive = true;
		});

		// Assert
		Assert.Same(schema, result);
		Assert.Single(schema.Endpoints);
		
		var endpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.PhoneNumber, endpoint.Type);
		Assert.True(endpoint.CanSend);
		Assert.True(endpoint.CanReceive);
		Assert.False(endpoint.IsRequired); // Default should be false
	}

	[Fact]
	public void HandlesMessageEndpoint_WithCustomFlags_AddsEndpointWithSpecifiedFlags()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		var result = schema.HandlesMessageEndpoint(EndpointType.Url, e =>
		{
			e.CanSend = false;
			e.CanReceive = true;
		});

		// Assert
		Assert.Same(schema, result);
		Assert.Single(schema.Endpoints);
		
		var endpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.Url, endpoint.Type);
		Assert.False(endpoint.CanSend);
		Assert.True(endpoint.CanReceive);
	}

	[Theory]
	[InlineData(EndpointType.EmailAddress)]
	[InlineData(EndpointType.PhoneNumber)]
	[InlineData(EndpointType.Url)]
	public void HandlesMessageEndpoint_WithDuplicateType_ThrowsArgumentException(EndpointType endpointType)
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		
		// First add a valid endpoint
		schema.HandlesMessageEndpoint(endpointType);

		// Act & Assert - Adding the same type again should throw
		Assert.Throws<InvalidOperationException>(() => schema.HandlesMessageEndpoint(endpointType));
	}

	[Fact]
	public void AllowsAnyMessageEndpoint_AddsWildcardEndpoint()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		var result = schema.AllowsAnyMessageEndpoint();

		// Assert
		Assert.Same(schema, result);
		Assert.Single(schema.Endpoints);
		
		var endpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.Any, endpoint.Type);
		Assert.True(endpoint.CanSend);
		Assert.True(endpoint.CanReceive);
	}

	[Fact]
	public void EndpointConfiguration_FluentChaining_AddsMultipleEndpoints()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		var result = schema
			.HandlesMessageEndpoint(EndpointType.EmailAddress, e =>
			{
				e.CanSend = true;
				e.CanReceive = false;
			})
			.HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
			{
				e.CanSend = false;
				e.CanReceive = true;
			})
			.HandlesMessageEndpoint(EndpointType.Url, e =>
			{
				e.CanSend = true;
				e.CanReceive = true;
				e.IsRequired = true;
			});

		// Assert
		Assert.Same(schema, result);
		Assert.Equal(3, schema.Endpoints.Count);
		
		// Verify email endpoint
		var emailEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.EmailAddress);
		Assert.NotNull(emailEndpoint);
		Assert.True(emailEndpoint.CanSend);
		Assert.False(emailEndpoint.CanReceive);
		
		// Verify SMS endpoint
		var smsEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.PhoneNumber);
		Assert.NotNull(smsEndpoint);
		Assert.False(smsEndpoint.CanSend);
		Assert.True(smsEndpoint.CanReceive);
		
		// Verify webhook endpoint
		var webhookEndpoint = schema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
		Assert.NotNull(webhookEndpoint);
		Assert.True(webhookEndpoint.CanSend);
		Assert.True(webhookEndpoint.CanReceive);
		Assert.True(webhookEndpoint.IsRequired);
	}

	[Fact]
	public void EndpointConfiguration_IntegrationWithOtherMethods_WorksInFluentChain()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithDisplayName("Test Schema")
			.WithCapability(ChannelCapability.ReceiveMessages)
			.HandlesMessageEndpoint(EndpointType.EmailAddress)
			.AddContentType(MessageContentType.PlainText)
			.HandlesMessageEndpoint(EndpointType.PhoneNumber, e =>
			{
				e.CanSend = false;
				e.CanReceive = true;
			})
			.WithCapability(ChannelCapability.Templates);

		// Assert
		Assert.Equal("Test Schema", schema.DisplayName);
		Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
		Assert.Equal(2, schema.Endpoints.Count);
		
		var expectedCapabilities = ChannelCapability.SendMessages |
								   ChannelCapability.ReceiveMessages |
								   ChannelCapability.Templates;
		
		Assert.Equal(expectedCapabilities, schema.Capabilities);
	}

	[Fact]
	public void EndpointConfiguration_MultipleEndpointsOfSameType_ThrowsInvalidOperationException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act - Add first endpoint configuration
		schema.HandlesMessageEndpoint(EndpointType.EmailAddress, e =>
		{
			e.CanSend = true;
			e.CanReceive = false;
		});

		// Assert - Adding another endpoint with the same type should throw
		var exception = Assert.Throws<InvalidOperationException>(() => 
			schema.HandlesMessageEndpoint(EndpointType.EmailAddress, e =>
			{
				e.CanSend = false;
				e.CanReceive = true;
			}));
		
		Assert.Contains("An endpoint configuration with type 'EmailAddress' already exists", exception.Message);
		Assert.Single(schema.Endpoints);
	}

	[Fact]
	public void AllowsAnyMessageEndpoint_WithExistingWildcardEndpoint_ThrowsInvalidOperationException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act - Add first wildcard endpoint
		schema.AllowsAnyMessageEndpoint();

		// Assert - Adding another wildcard endpoint should throw
		var exception = Assert.Throws<InvalidOperationException>(() => 
			schema.AllowsAnyMessageEndpoint());
		
		Assert.Contains("An endpoint configuration with type 'Any' already exists", exception.Message);
		Assert.Single(schema.Endpoints);
	}

	[Fact]
	public void AllowsMessageEndpoint_AfterAllowsAnyMessageEndpoint_ThrowsInvalidOperationException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act - Add wildcard endpoint first
		schema.AllowsAnyMessageEndpoint();

		// Assert - Adding a specific endpoint after wildcard should throw
		var exception = Assert.Throws<InvalidOperationException>(() =>
			schema.HandlesMessageEndpoint(EndpointType.Any));
		
		Assert.Contains("An endpoint configuration with type 'Any' already exists", exception.Message);
		Assert.Single(schema.Endpoints);
	}

	#region Message Property Configuration Tests

	[Fact]
	public void AddMessageProperty_WithValidProperty_AddsToMessagePropertiesList()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var property = new MessagePropertyConfiguration("TestProperty", DataType.String)
		{
			IsRequired = true,
			Description = "Test message property"
		};

		// Act
		var result = schema.AddMessageProperty(property);

		// Assert
		Assert.Same(schema, result);
		Assert.Contains(property, schema.MessageProperties);
		Assert.Single(schema.MessageProperties);
	}

	[Fact]
	public void AddMessageProperty_WithNullProperty_ThrowsArgumentNullException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => schema.AddMessageProperty(null!));
	}

	[Fact]
	public void AddMessageProperty_WithDuplicateName_ThrowsInvalidOperationException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var firstProperty = new MessagePropertyConfiguration("Priority", DataType.Integer)
		{
			IsRequired = true
		};
		var duplicateProperty = new MessagePropertyConfiguration("Priority", DataType.String)
		{
			IsRequired = false
		};

		// Act - Add first property
		schema.AddMessageProperty(firstProperty);

		// Assert - Adding another property with the same name should throw
		var exception = Assert.Throws<InvalidOperationException>(() => 
			schema.AddMessageProperty(duplicateProperty));
		
		Assert.Contains("A message property configuration with name 'Priority' already exists", exception.Message);
		Assert.Single(schema.MessageProperties);
	}

	[Fact]
	public void AddMessageProperty_CaseInsensitiveDuplicateName_ThrowsInvalidOperationException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var firstProperty = new MessagePropertyConfiguration("PRIORITY", DataType.Integer);
		var duplicateProperty = new MessagePropertyConfiguration("priority", DataType.String);

		// Act - Add first property
		schema.AddMessageProperty(firstProperty);

		// Assert - Adding another property with different case should throw
		var exception = Assert.Throws<InvalidOperationException>(() => 
			schema.AddMessageProperty(duplicateProperty));
		
		Assert.Contains("A message property configuration with name 'priority' already exists", exception.Message);
		Assert.Single(schema.MessageProperties);
	}

	[Fact]
	public void AddMessageProperty_FluentChaining_AddsMultipleProperties()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		var result = schema
			.AddMessageProperty("Priority", DataType.Integer, p =>
			{
				p.IsRequired = true;
				p.Description = "Message priority";
			})
			.AddMessageProperty("Category", DataType.String, p =>
			{
				p.Description = "Message category";
			})
			.AddMessageProperty("Timestamp", DataType.String, p =>
			{
				p.IsRequired = true;
				p.Description = "Message timestamp";
			});

		// Assert
		Assert.Same(schema, result);
		Assert.Equal(3, schema.MessageProperties.Count);
		
		// Verify each property
		var priorityProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Priority");
		Assert.NotNull(priorityProperty);
		Assert.Equal(DataType.Integer, priorityProperty.DataType);
		Assert.True(priorityProperty.IsRequired);
		
		var categoryProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Category");
		Assert.NotNull(categoryProperty);
		Assert.Equal(DataType.String, categoryProperty.DataType);
		Assert.False(categoryProperty.IsRequired);
		
		var timestampProperty = schema.MessageProperties.FirstOrDefault(p => p.Name == "Timestamp");
		Assert.NotNull(timestampProperty);
		Assert.Equal(DataType.String, timestampProperty.DataType);
		Assert.True(timestampProperty.IsRequired);
	}

	[Fact]
	public void AddMessageProperty_IntegrationWithOtherMethods_WorksInFluentChain()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithDisplayName("Test Schema")
			.AddMessageProperty("Priority", DataType.Integer, p => p.IsRequired = true)
			.AddContentType(MessageContentType.PlainText)
			.AddMessageProperty("Category", DataType.String)
			.HandlesMessageEndpoint(EndpointType.EmailAddress);

		// Assert
		Assert.Equal("Test Schema", schema.DisplayName);
		Assert.Equal(2, schema.MessageProperties.Count);
		Assert.Contains(MessageContentType.PlainText, schema.ContentTypes);
		Assert.Single(schema.Endpoints);
	}

	#endregion

	#region Message Validation Tests

	[Fact]
	public void ValidateMessage_WithNullMessage_ThrowsArgumentNullException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => schema.ValidateMessage(null!));
	}

	[Fact]
	public void ValidateMessage_WithEmptyProperties_ReturnsEmptyWhenNoRequiredProperties()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("OptionalProperty", DataType.String);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>());

		// Act
		var results = schema.ValidateMessage(message);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateMessage_WithMissingRequiredProperty_ReturnsValidationError()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("RequiredProperty", DataType.String, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>());

		// Act
		var results = schema.ValidateMessage(message).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Required message property 'RequiredProperty' is missing.", results[0].ErrorMessage);
		Assert.Contains("RequiredProperty", results[0].MemberNames);
	}

	[Fact]
	public void ValidateMessage_WithAllRequiredProperties_ReturnsEmpty()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("RequiredProperty1", DataType.String, p => p.IsRequired = true)
			.AddMessageProperty("RequiredProperty2", DataType.Integer, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "RequiredProperty1", "test" },
			{ "RequiredProperty2", 123 }
		});

		// Act
		var results = schema.ValidateMessage(message);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateMessage_WithIncompatibleType_ReturnsValidationError()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("StringProperty", DataType.String, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "StringProperty", 123 } // Wrong type: should be string
		});

		// Act
		var results = schema.ValidateMessage(message).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Message property 'StringProperty' has an incompatible type. Expected: String, Actual: Int32.", results[0].ErrorMessage);
		Assert.Contains("StringProperty", results[0].MemberNames);
	}

	[Fact]
	public void ValidateMessage_WithUnknownProperty_ReturnsValidationError()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("KnownProperty", DataType.String);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "KnownProperty", "test" },
			{ "UnknownProperty", "value" }
		});

		// Act
		var results = schema.ValidateMessage(message).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Unknown message property 'UnknownProperty' is not supported by this schema.", results[0].ErrorMessage);
		Assert.Contains("UnknownProperty", results[0].MemberNames);
	}

	[Fact]
	public void ValidateMessage_WithMultipleErrors_ReturnsAllValidationErrors()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("RequiredProperty", DataType.String, p => p.IsRequired = true)
			.AddMessageProperty("TypedProperty", DataType.Boolean, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "TypedProperty", "not_a_boolean" }, // Wrong type
			{ "UnknownProperty", "value" } // Unknown property
			// Missing RequiredProperty
		});

		// Act
		var results = schema.ValidateMessage(message).ToList();

		// Assert
		Assert.Equal(3, results.Count);
		Assert.Contains(results, r => r.ErrorMessage!.Contains("Required message property 'RequiredProperty' is missing"));
		Assert.Contains(results, r => r.ErrorMessage!.Contains("Message property 'TypedProperty' has an incompatible type"));
		Assert.Contains(results, r => r.ErrorMessage!.Contains("Unknown message property 'UnknownProperty' is not supported"));
	}

	[Theory]
	[InlineData(DataType.Boolean, true)]
	[InlineData(DataType.Boolean, false)]
	[InlineData(DataType.String, "test")]
	[InlineData(DataType.Integer, 123)]
	[InlineData(DataType.Integer, (long)456)]
	[InlineData(DataType.Integer, (byte)78)]
	[InlineData(DataType.Number, 123.45)]
	[InlineData(DataType.Number, 678.90f)]
	[InlineData(DataType.Number, 100)]
	public void ValidateMessage_WithCompatibleTypes_ReturnsEmpty(DataType propertyType, object value)
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("TestProperty", propertyType, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "TestProperty", value }
		});

		// Act
		var results = schema.ValidateMessage(message);

		// Assert
		Assert.Empty(results);
	}

	[Theory]
	[InlineData(DataType.Boolean, "not_boolean")]
	[InlineData(DataType.Boolean, 123)]
	[InlineData(DataType.String, true)]
	[InlineData(DataType.Integer, "not_number")]
	[InlineData(DataType.Integer, 123.45)]
	[InlineData(DataType.Number, "not_number")]
	public void ValidateMessage_WithIncompatibleTypes_ReturnsValidationError(DataType propertyType, object value)
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("TestProperty", propertyType, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "TestProperty", value }
		});

		// Act
		var results = schema.ValidateMessage(message).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains($"Message property 'TestProperty' has an incompatible type. Expected: {propertyType}", results[0].ErrorMessage);
		Assert.Contains("TestProperty", results[0].MemberNames);
	}

	[Fact]
	public void ValidateMessage_WithComplexEmailScenario_ValidatesCorrectly()
	{
		// Arrange
		var emailSchema = new ChannelSchema("SMTP", "Email", "1.0.0")
			.AddMessageProperty("Priority", DataType.Integer, p =>
			{
				p.IsRequired = true;
				p.Description = "Email priority level";
			})
			.AddMessageProperty("Category", DataType.String, p =>
			{
				p.Description = "Email category";
			})
			.AddMessageProperty("IsHtml", DataType.Boolean, p =>
			{
				p.IsRequired = true;
				p.Description = "Whether email content is HTML";
			})
			.AddMessageProperty("Sensitivity", DataType.String, p =>
			{
				p.IsSensitive = true;
				p.Description = "Email sensitivity level";
			});

		var validMessage = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "Priority", 1 },
			{ "IsHtml", true },
			{ "Category", "Newsletter" }
		});

		var invalidMessage = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "Priority", "not_a_number" }, // Wrong type
			{ "IsHtml", true },
			// Missing required Priority (overridden by wrong type above)
			{ "UnknownProperty", "value" } // Unknown property
		});

		// Act
		var validResults = emailSchema.ValidateMessage(validMessage);
		var invalidResults = emailSchema.ValidateMessage(invalidMessage).ToList();

		// Assert
		Assert.Empty(validResults);

		Assert.Equal(2, invalidResults.Count);
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Message property 'Priority' has an incompatible type"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Unknown message property 'UnknownProperty' is not supported"));
	}

	[Fact]
	public void ValidateMessage_CaseInsensitivePropertyNames_HandlesCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty("TestProperty", DataType.String, p => p.IsRequired = true);

		var message = CreateTestMessage(properties: new Dictionary<string, object?>
		{
			{ "testproperty", "value" } // Different case
		});

		// Act
		var results = schema.ValidateMessage(message).ToList();

		// Assert
		// The validation should handle case-insensitive property names correctly
		// and not report the required property as missing since "testproperty" 
		// matches "TestProperty" case-insensitively
		Assert.Empty(results);
	}

	#endregion

	#region Helper Methods

	private static IMessage CreateTestMessage(
		string id = "test-message-id",
		IEndpoint? sender = null,
		IEndpoint? receiver = null,
		IMessageContent? content = null,
		IDictionary<string, object?>? properties = null)
	{
		return new Message
		{
			Id = id,
			Sender = Endpoint.Create(sender),
			Receiver = Endpoint.Create(receiver),
			Content = content == null ? new TextContent("Test message") : MessageContent.Create(content),
			Properties = properties?.ToDictionary(
				kvp => kvp.Key,
				kvp => new MessageProperty(kvp.Key, kvp.Value),
				StringComparer.OrdinalIgnoreCase)
		};
	}

	#endregion

	[Fact]
	public void EndpointConfiguration_WithReceiveOnlyEndpoint_ConfiguredCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		schema.HandlesMessageEndpoint(EndpointType.Id, e =>
		{
			e.CanSend = false;
			e.CanReceive = true;
		});

		// Assert
		Assert.Single(schema.Endpoints);
		
		var endpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.Id, endpoint.Type);
		Assert.False(endpoint.CanSend);
		Assert.True(endpoint.CanReceive);
	}

	[Fact]
	public void EndpointConfiguration_WithSendOnlyEndpoint_ConfiguredCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		schema.HandlesMessageEndpoint(EndpointType.Label, e =>
		{
			e.CanSend = true;
			e.CanReceive = false;
		});

		// Assert
		Assert.Single(schema.Endpoints);
		
		var endpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.Label, endpoint.Type);
		Assert.False(endpoint.CanReceive);
		Assert.True(endpoint.CanSend);
	}

	[Fact]
	public void EndpointConfiguration_WithBiDirectionalEndpoint_ConfiguredCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		schema.HandlesMessageEndpoint(EndpointType.Topic, e =>
		{
			e.CanSend = true;
			e.CanReceive = true;
		});

		// Assert
		Assert.Single(schema.Endpoints);
		
		var endpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.Topic, endpoint.Type);
		Assert.True(endpoint.CanSend);
		Assert.True(endpoint.CanReceive);
	}

	#endregion
}