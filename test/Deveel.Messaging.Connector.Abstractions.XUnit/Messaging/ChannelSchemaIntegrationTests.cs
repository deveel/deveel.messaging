namespace Deveel.Messaging;

/// <summary>
/// Integration tests that verify the ChannelSchema works correctly
/// in realistic scenarios and with complex configurations.
/// </summary>
public class ChannelSchemaIntegrationTests
{
	[Fact]
	public void EmailConnectorSchema_ConfiguredCorrectly()
	{
		// Arrange & Act
		var emailSchema = new ChannelSchema("SMTP", "Email", "1.2.0")
			.WithDisplayName("SMTP Email Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.Templates | 
				ChannelCapability.MediaAttachments |
				ChannelCapability.HealthCheck)
			.AddParameter(new ChannelParameter("Host", ParameterType.String)
			{
				IsRequired = true,
				Description = "SMTP server hostname"
			})
			.AddParameter(new ChannelParameter("Port", ParameterType.Integer)
			{
				IsRequired = true,
				DefaultValue = 587,
				Description = "SMTP server port"
			})
			.AddParameter(new ChannelParameter("Username", ParameterType.String)
			{
				IsRequired = true,
				Description = "SMTP authentication username"
			})
			.AddParameter(new ChannelParameter("Password", ParameterType.String)
			{
				IsRequired = true,
				IsSensitive = true,
				Description = "SMTP authentication password"
			})
			.AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean)
			{
				DefaultValue = true,
				Description = "Enable SSL/TLS encryption"
			})
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Multipart)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress)
			{
				CanSend = true,
				CanReceive = false
			})
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
			{
				CanSend = true,
				CanReceive = false
			})
			.AddAuthenticationType(AuthenticationType.Basic);

		// Assert
		Assert.Equal("SMTP", emailSchema.ChannelProvider);
		Assert.Equal("Email", emailSchema.ChannelType);
		Assert.Equal("1.2.0", emailSchema.Version);
		Assert.Equal("SMTP Email Connector", emailSchema.DisplayName);

		// Verify capabilities
		Assert.True(emailSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
		Assert.True(emailSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.True(emailSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(emailSchema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
		Assert.False(emailSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

		// Verify parameters
		Assert.Equal(5, emailSchema.Parameters.Count);
		AssertParameterExists(emailSchema, "Host", ParameterType.String, isRequired: true);
		AssertParameterExists(emailSchema, "Port", ParameterType.Integer, isRequired: true, defaultValue: 587);
		AssertParameterExists(emailSchema, "Username", ParameterType.String, isRequired: true);
		AssertParameterExists(emailSchema, "Password", ParameterType.String, isRequired: true, isSensitive: true);
		AssertParameterExists(emailSchema, "EnableSsl", ParameterType.Boolean, defaultValue: true);

		// Verify content types
		Assert.Equal(3, emailSchema.ContentTypes.Count);
		Assert.Contains(MessageContentType.PlainText, emailSchema.ContentTypes);
		Assert.Contains(MessageContentType.Html, emailSchema.ContentTypes);
		Assert.Contains(MessageContentType.Multipart, emailSchema.ContentTypes);

		// Verify endpoints
		Assert.Equal(2, emailSchema.Endpoints.Count);
		Assert.Contains(emailSchema.Endpoints, e => e.Type == EndpointType.EmailAddress && e.CanSend && !e.CanReceive);
		Assert.Contains(emailSchema.Endpoints, e => e.Type == EndpointType.PhoneNumber && e.CanSend && !e.CanReceive);

		// Verify authentication types
		Assert.Single(emailSchema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.Basic, emailSchema.AuthenticationTypes);
	}

	[Fact]
	public void SmsConnectorSchema_ConfiguredCorrectly()
	{
		// Arrange & Act
		var smsSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
			.WithDisplayName("Twilio SMS Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.ReceiveMessages |
				ChannelCapability.MessageStatusQuery |
				ChannelCapability.BulkMessaging)
			.AddParameter(new ChannelParameter("AccountSid", ParameterType.String)
			{
				IsRequired = true,
				Description = "Twilio Account SID"
			})
			.AddParameter(new ChannelParameter("AuthToken", ParameterType.String)
			{
				IsRequired = true,
				IsSensitive = true,
				Description = "Twilio Auth Token"
			})
			.AddParameter(new ChannelParameter("FromNumber", ParameterType.String)
			{
				IsRequired = true,
				Description = "Sender phone number"
			})
			.AddContentType(MessageContentType.PlainText)
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
			.AddAuthenticationType(AuthenticationType.Token);

		// Assert
		Assert.Equal("Twilio", smsSchema.ChannelProvider);
		Assert.Equal("SMS", smsSchema.ChannelType);
		Assert.Equal("2.1.0", smsSchema.Version);
		Assert.Equal("Twilio SMS Connector", smsSchema.DisplayName);

		// Verify bi-directional capabilities
		Assert.True(smsSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
		Assert.True(smsSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
		Assert.True(smsSchema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
		Assert.True(smsSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));

		Assert.Equal(3, smsSchema.Parameters.Count);
		Assert.Single(smsSchema.ContentTypes);
		Assert.Equal(2, smsSchema.Endpoints.Count);
		Assert.Single(smsSchema.AuthenticationTypes);

		// Verify endpoints
		var smsEndpoint = smsSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.PhoneNumber);
		Assert.NotNull(smsEndpoint);
		Assert.True(smsEndpoint.CanSend);
		Assert.True(smsEndpoint.CanReceive);

		var webhookEndpoint = smsSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
		Assert.NotNull(webhookEndpoint);
		Assert.False(webhookEndpoint.CanSend);
		Assert.True(webhookEndpoint.CanReceive);
	}

	[Fact]
	public void MultipleAuthenticationTypesSchema_ConfiguredCorrectly()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Generic", "API", "1.0.0")
			.AllowsAnyMessageEndpoint()
			.AddAuthenticationType(AuthenticationType.None)
			.AddAuthenticationType(AuthenticationType.Basic)
			.AddAuthenticationType(AuthenticationType.Token)
			.AddAuthenticationType(AuthenticationType.ClientCredentials)
			.AddAuthenticationType(AuthenticationType.Certificate)
			.AddAuthenticationType(AuthenticationType.Custom);

		// Assert
		Assert.Equal(6, schema.AuthenticationTypes.Count);
		Assert.Contains(AuthenticationType.None, schema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.Basic, schema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.Token, schema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.ClientCredentials, schema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.Certificate, schema.AuthenticationTypes);
		Assert.Contains(AuthenticationType.Custom, schema.AuthenticationTypes);

		// Verify wildcard endpoint
		Assert.Single(schema.Endpoints);
		var anyEndpoint = schema.Endpoints.First();
		Assert.Equal(EndpointType.Any, anyEndpoint.Type);
		Assert.True(anyEndpoint.CanSend);
		Assert.True(anyEndpoint.CanReceive);
	}

	[Fact]
	public void AllCapabilities_CanBeSetAndVerified()
	{
		// Arrange
		var allCapabilities = ChannelCapability.SendMessages |
		                     ChannelCapability.ReceiveMessages |
		                     ChannelCapability.MessageStatusQuery |
		                     ChannelCapability.HandlerMessageState |
		                     ChannelCapability.MediaAttachments |
		                     ChannelCapability.Templates |
		                     ChannelCapability.BulkMessaging |
		                     ChannelCapability.HealthCheck;

		// Act
		var schema = new ChannelSchema("Universal", "Multi", "1.0.0")
			.WithCapabilities(allCapabilities);

		// Assert
		Assert.Equal(allCapabilities, schema.Capabilities);
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.SendMessages));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MessageStatusQuery));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HandlerMessageState));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
		Assert.True(schema.Capabilities.HasFlag(ChannelCapability.HealthCheck));
	}

	[Fact]
	public void ComplexParameterConfiguration_WithAllParameterTypes()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Complex", "Test", "1.0.0")
			.AddParameter(new ChannelParameter("BoolParam", ParameterType.Boolean)
			{
				DefaultValue = false,
				Description = "Boolean parameter"
			})
			.AddParameter(new ChannelParameter("IntParam", ParameterType.Integer)
			{
				IsRequired = true,
				AllowedValues = new object[] { 1, 2, 3, 4, 5 },
				Description = "Integer parameter with allowed values"
			})
			.AddParameter(new ChannelParameter("NumberParam", ParameterType.Number)
			{
				DefaultValue = 3.14,
				Description = "Decimal number parameter"
			})
			.AddParameter(new ChannelParameter("StringParam", ParameterType.String)
			{
				IsRequired = true,
				IsSensitive = true,
				AllowedValues = new object[] { "dev", "test", "prod" },
				Description = "String parameter with environment values"
			});

		// Assert
		Assert.Equal(4, schema.Parameters.Count);
		
		var boolParam = schema.Parameters.First(p => p.Name == "BoolParam");
		Assert.Equal(ParameterType.Boolean, boolParam.DataType);
		Assert.Equal(false, boolParam.DefaultValue);
		Assert.False(boolParam.IsRequired);

		var intParam = schema.Parameters.First(p => p.Name == "IntParam");
		Assert.Equal(ParameterType.Integer, intParam.DataType);
		Assert.True(intParam.IsRequired);
		Assert.NotNull(intParam.AllowedValues);
		Assert.Equal(5, intParam.AllowedValues.Length);

		var numberParam = schema.Parameters.First(p => p.Name == "NumberParam");
		Assert.Equal(ParameterType.Number, numberParam.DataType);
		Assert.Equal(3.14, numberParam.DefaultValue);

		var stringParam = schema.Parameters.First(p => p.Name == "StringParam");
		Assert.Equal(ParameterType.String, stringParam.DataType);
		Assert.True(stringParam.IsRequired);
		Assert.True(stringParam.IsSensitive);
		Assert.NotNull(stringParam.AllowedValues);
		Assert.Contains("dev", stringParam.AllowedValues);
		Assert.Contains("test", stringParam.AllowedValues);
		Assert.Contains("prod", stringParam.AllowedValues);
	}

	[Fact]
	public void SchemaAsInterface_CanBeUsedPolymorphically()
	{
		// Arrange
		var schemas = new List<IChannelSchema>
		{
			new ChannelSchema("Provider1", "Email", "1.0.0"),
			new ChannelSchema("Provider2", "SMS", "2.0.0"),
			new ChannelSchema("Provider3", "Push", "1.5.0")
		};

		// Act & Assert
		foreach (var schema in schemas)
		{
			Assert.NotNull(schema.ChannelProvider);
			Assert.NotNull(schema.ChannelType);
			Assert.NotNull(schema.Version);
			Assert.NotNull(schema.Parameters);
			Assert.NotNull(schema.ContentTypes);
			Assert.NotNull(schema.AuthenticationTypes);
			Assert.NotNull(schema.Endpoints);
		}

		// Verify different types
		Assert.Equal("Email", schemas[0].ChannelType);
		Assert.Equal("SMS", schemas[1].ChannelType);
		Assert.Equal("Push", schemas[2].ChannelType);
	}

	[Fact]
	public void AllMessageContentTypes_CanBeAdded()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Universal", "Multi", "1.0.0")
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Multipart)
			.AddContentType(MessageContentType.Template)
			.AddContentType(MessageContentType.Media)
			.AddContentType(MessageContentType.Json)
			.AddContentType(MessageContentType.Binary);

		// Assert
		Assert.Equal(7, schema.ContentTypes.Count);
		
		// Verify all enum values are supported
		var expectedContentTypes = new[]
		{
			MessageContentType.PlainText,
			MessageContentType.Html,
			MessageContentType.Multipart,
			MessageContentType.Template,
			MessageContentType.Media,
			MessageContentType.Json,
			MessageContentType.Binary
		};

		foreach (var expectedType in expectedContentTypes)
		{
			Assert.Contains(expectedType, schema.ContentTypes);
		}
	}

	[Fact]
	public void WebApiConnectorSchema_WithComplexEndpointConfiguration()
	{
		// Arrange & Act
		var webApiSchema = new ChannelSchema("RestAPI", "WebAPI", "3.0.0")
			.WithDisplayName("REST API Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.ReceiveMessages |
				ChannelCapability.MessageStatusQuery |
				ChannelCapability.HealthCheck)
			.AddParameter(new ChannelParameter("BaseUrl", ParameterType.String)
			{
				IsRequired = true,
				Description = "Base URL for the API"
			})
			.AddParameter(new ChannelParameter("Timeout", ParameterType.Integer)
			{
				DefaultValue = 30,
				Description = "Request timeout in seconds"
			})
			.AddContentType(MessageContentType.Json)
			.AddContentType(MessageContentType.PlainText)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
			{
				CanSend = false,
				CanReceive = true,
				IsRequired = true
			})
			.AddAuthenticationType(AuthenticationType.Token)
			.AddAuthenticationType(AuthenticationType.Basic);

		// Assert
		Assert.Equal("RestAPI", webApiSchema.ChannelProvider);
		Assert.Equal("WebAPI", webApiSchema.ChannelType);
		Assert.Equal("3.0.0", webApiSchema.Version);
		Assert.Equal("REST API Connector", webApiSchema.DisplayName);

		// Verify endpoints
		Assert.Single(webApiSchema.Endpoints);
		
		var callbackEndpoint = webApiSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Url);
		Assert.NotNull(callbackEndpoint);
		Assert.False(callbackEndpoint.CanSend);
		Assert.True(callbackEndpoint.CanReceive);
		Assert.True(callbackEndpoint.IsRequired);

		// Verify other properties
		Assert.Equal(2, webApiSchema.Parameters.Count);
		Assert.Equal(2, webApiSchema.ContentTypes.Count);
		Assert.Equal(2, webApiSchema.AuthenticationTypes.Count);
	}

	[Fact]
	public void MessageQueueConnectorSchema_WithBidirectionalEndpoints()
	{
		// Arrange & Act
		var queueSchema = new ChannelSchema("RabbitMQ", "Queue", "2.0.0")
			.WithDisplayName("RabbitMQ Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.ReceiveMessages |
				ChannelCapability.BulkMessaging)
			.AddParameter(new ChannelParameter("ConnectionString", ParameterType.String)
			{
				IsRequired = true,
				IsSensitive = true,
				Description = "RabbitMQ connection string"
			})
			.AddContentType(MessageContentType.Json)
			.AddContentType(MessageContentType.Binary)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Topic)
			{
				CanSend = true,
				CanReceive = true
			})
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Id)
			{
				CanSend = true,
				CanReceive = false
			})
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Label)
			{
				CanSend = true,
				CanReceive = true
			})
			.AddAuthenticationType(AuthenticationType.Basic);

		// Assert
		Assert.Equal("RabbitMQ", queueSchema.ChannelProvider);
		Assert.Equal("Queue", queueSchema.ChannelType);
		Assert.Equal(3, queueSchema.Endpoints.Count);

		// Verify bidirectional queue endpoint (using Topic for queue-like behavior)
		var queueEndpoint = queueSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Topic);
		Assert.NotNull(queueEndpoint);
		Assert.True(queueEndpoint.CanSend);
		Assert.True(queueEndpoint.CanReceive);

		// Verify send-only exchange endpoint (using Id for exchange-like behavior)
		var exchangeEndpoint = queueSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Id);
		Assert.NotNull(exchangeEndpoint);
		Assert.True(exchangeEndpoint.CanSend);
		Assert.False(exchangeEndpoint.CanReceive);

		// Verify bidirectional topic endpoint (using Label for secondary topic behavior)
		var topicEndpoint = queueSchema.Endpoints.FirstOrDefault(e => e.Type == EndpointType.Label);
		Assert.NotNull(topicEndpoint);
		Assert.True(topicEndpoint.CanSend);
		Assert.True(topicEndpoint.CanReceive);
	}

	[Fact]
	public void FlexibleConnectorSchema_WithAnyEndpointConfiguration()
	{
		// Arrange & Act
		var flexibleSchema = new ChannelSchema("Universal", "Flexible", "1.0.0")
			.WithDisplayName("Universal Flexible Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.ReceiveMessages |
				ChannelCapability.MessageStatusQuery |
				ChannelCapability.Templates |
				ChannelCapability.MediaAttachments)
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Json)
			.AddContentType(MessageContentType.Binary)
			.AllowsAnyMessageEndpoint()
			.AddAuthenticationType(AuthenticationType.None)
			.AddAuthenticationType(AuthenticationType.Token);

		// Assert
		Assert.Equal("Universal", flexibleSchema.ChannelProvider);
		Assert.Equal("Flexible", flexibleSchema.ChannelType);
		Assert.Single(flexibleSchema.Endpoints);

		var anyEndpoint = flexibleSchema.Endpoints.First();
		Assert.Equal(EndpointType.Any, anyEndpoint.Type);
		Assert.True(anyEndpoint.CanSend);
		Assert.True(anyEndpoint.CanReceive);
		Assert.False(anyEndpoint.IsRequired); // Default should be false

		Assert.Equal(4, flexibleSchema.ContentTypes.Count);
		Assert.Equal(2, flexibleSchema.AuthenticationTypes.Count);
	}

	[Fact]
	public void MessagePropertyConfiguration_EmailConnectorWithProperties_ConfiguredCorrectly()
	{
		// Arrange & Act
		var emailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
			.WithDisplayName("Advanced SMTP Email Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.Templates | 
				ChannelCapability.MediaAttachments |
				ChannelCapability.HealthCheck)
			.AddParameter(new ChannelParameter("Host", ParameterType.String)
			{
				IsRequired = true,
				Description = "SMTP server hostname"
			})
			.AddParameter(new ChannelParameter("Port", ParameterType.Integer)
			{
				IsRequired = true,
				DefaultValue = 587,
				Description = "SMTP server port"
			})
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Multipart)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress)
			{
				CanSend = true,
				CanReceive = false
			})
			.AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer)
			{
				IsRequired = true,
				Description = "Email priority level (1-5)"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
			{
				IsRequired = true,
				Description = "Email subject line"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean)
			{
				IsRequired = false,
				Description = "Whether email content is HTML formatted"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("Sensitivity", ParameterType.String)
			{
				IsRequired = false,
				IsSensitive = true,
				Description = "Email sensitivity level for compliance"
			})
			.AddAuthenticationType(AuthenticationType.Basic);

		// Assert
		Assert.Equal("SMTP", emailSchema.ChannelProvider);
		Assert.Equal("Email", emailSchema.ChannelType);
		Assert.Equal("2.0.0", emailSchema.Version);
		Assert.Equal("Advanced SMTP Email Connector", emailSchema.DisplayName);

		// Verify message properties
		Assert.Equal(4, emailSchema.MessageProperties.Count);
		
		var priorityProperty = emailSchema.MessageProperties.FirstOrDefault(p => p.Name == "Priority");
		Assert.NotNull(priorityProperty);
		Assert.Equal(ParameterType.Integer, priorityProperty.DataType);
		Assert.True(priorityProperty.IsRequired);
		Assert.False(priorityProperty.IsSensitive);
		
		var subjectProperty = emailSchema.MessageProperties.FirstOrDefault(p => p.Name == "Subject");
		Assert.NotNull(subjectProperty);
		Assert.Equal(ParameterType.String, subjectProperty.DataType);
		Assert.True(subjectProperty.IsRequired);
		
		var isHtmlProperty = emailSchema.MessageProperties.FirstOrDefault(p => p.Name == "IsHtml");
		Assert.NotNull(isHtmlProperty);
		Assert.Equal(ParameterType.Boolean, isHtmlProperty.DataType);
		Assert.False(isHtmlProperty.IsRequired);
		
		var sensitivityProperty = emailSchema.MessageProperties.FirstOrDefault(p => p.Name == "Sensitivity");
		Assert.NotNull(sensitivityProperty);
		Assert.Equal(ParameterType.String, sensitivityProperty.DataType);
		Assert.False(sensitivityProperty.IsRequired);
		Assert.True(sensitivityProperty.IsSensitive);

		// Verify other properties
		Assert.Equal(2, emailSchema.Parameters.Count);
		Assert.Equal(3, emailSchema.ContentTypes.Count);
		Assert.Single(emailSchema.Endpoints);
		Assert.Single(emailSchema.AuthenticationTypes);
	}

	[Fact]
	public void MessagePropertyValidation_EmailConnectorScenario_ValidatesCorrectly()
	{
		// Arrange
		var emailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
			.AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer)
			{
				IsRequired = true,
				Description = "Email priority level"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
			{
				IsRequired = true,
				Description = "Email subject line"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean)
			{
				IsRequired = false,
				Description = "Whether email content is HTML"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("Category", ParameterType.String)
			{
				IsRequired = false,
				Description = "Email category"
			});

		// Valid message properties
		var validProperties = new Dictionary<string, object?>
		{
			{ "Priority", 2 },
			{ "Subject", "Important Update" },
			{ "IsHtml", true },
			{ "Category", "Newsletter" }
		};

		// Invalid message properties - various errors
		var invalidProperties = new Dictionary<string, object?>
		{
			{ "Priority", "high" }, // Wrong type - should be integer
			// Missing required "Subject"
			{ "IsHtml", true },
			{ "UnknownProperty", "value" }, // Unknown property
			{ "Category", "Marketing" }
		};

		// Missing required properties
		var missingRequiredProperties = new Dictionary<string, object?>
		{
			{ "IsHtml", false },
			{ "Category", "Info" }
			// Missing both Priority and Subject
		};

		// Act
		var validResults = emailSchema.ValidateMessageProperties(validProperties);
		var invalidResults = emailSchema.ValidateMessageProperties(invalidProperties).ToList();
		var missingRequiredResults = emailSchema.ValidateMessageProperties(missingRequiredProperties).ToList();

		// Assert
		// Valid properties should pass validation
		Assert.Empty(validResults);

		// Invalid properties should have 3 errors: wrong type, missing required, unknown property
		Assert.Equal(3, invalidResults.Count);
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Message property 'Priority' has an incompatible type"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Required message property 'Subject' is missing"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Unknown message property 'UnknownProperty' is not supported"));

		// Missing required properties should have 2 errors
		Assert.Equal(2, missingRequiredResults.Count);
		Assert.Contains(missingRequiredResults, r => r.ErrorMessage!.Contains("Required message property 'Priority' is missing"));
		Assert.Contains(missingRequiredResults, r => r.ErrorMessage!.Contains("Required message property 'Subject' is missing"));
	}

	[Fact]
	public void SmsConnectorWithMessageProperties_ConfiguredAndValidatedCorrectly()
	{
		// Arrange & Act
		var smsSchema = new ChannelSchema("Twilio", "SMS", "3.0.0")
			.WithDisplayName("Enhanced Twilio SMS Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.ReceiveMessages |
				ChannelCapability.MessageStatusQuery |
				ChannelCapability.BulkMessaging)
			.AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String)
			{
				IsRequired = true,
				Description = "Recipient phone number in E.164 format"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String)
			{
				IsRequired = false,
				Description = "Type of SMS message (transactional, promotional, etc.)"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("DeliveryAttempts", ParameterType.Integer)
			{
				IsRequired = false,
				Description = "Number of delivery attempts"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("IsUrgent", ParameterType.Boolean)
			{
				IsRequired = false,
				Description = "Whether message requires urgent delivery"
			})
			.AddContentType(MessageContentType.PlainText)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
			{
				CanSend = true,
				CanReceive = true
			});

		// Test valid message properties
		var validSmsProperties = new Dictionary<string, object?>
		{
			{ "PhoneNumber", "+1234567890" },
			{ "MessageType", "transactional" },
			{ "DeliveryAttempts", 3 },
			{ "IsUrgent", false }
		};

		// Test minimal valid properties (only required)
		var minimalSmsProperties = new Dictionary<string, object?>
		{
			{ "PhoneNumber", "+9876543210" }
		};

		// Test invalid properties
		var invalidSmsProperties = new Dictionary<string, object?>
		{
			{ "PhoneNumber", 1234567890 }, // Wrong type - should be string
			{ "DeliveryAttempts", "three" }, // Wrong type - should be integer
			{ "InvalidProperty", "test" }    // Unknown property
		};

		// Act
		var validResults = smsSchema.ValidateMessageProperties(validSmsProperties);
		var minimalResults = smsSchema.ValidateMessageProperties(minimalSmsProperties);
		var invalidResults = smsSchema.ValidateMessageProperties(invalidSmsProperties).ToList();

		// Assert
		Assert.Equal("Enhanced Twilio SMS Connector", smsSchema.DisplayName);
		Assert.Equal(4, smsSchema.MessageProperties.Count);

		// Valid and minimal properties should pass
		Assert.Empty(validResults);
		Assert.Empty(minimalResults);

		// Invalid properties should have 3 errors
		Assert.Equal(3, invalidResults.Count);
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Message property 'PhoneNumber' has an incompatible type"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Message property 'DeliveryAttempts' has an incompatible type"));
		Assert.Contains(invalidResults, r => r.ErrorMessage!.Contains("Unknown message property 'InvalidProperty' is not supported"));
	}

	private static void AssertParameterExists(
		ChannelSchema schema, 
		string name, 
		ParameterType dataType, 
		bool isRequired = false, 
		bool isSensitive = false, 
		object? defaultValue = null)
	{
		var parameter = schema.Parameters.FirstOrDefault(p => p.Name == name);
		Assert.NotNull(parameter);
		Assert.Equal(dataType, parameter.DataType);
		Assert.Equal(isRequired, parameter.IsRequired);
		Assert.Equal(isSensitive, parameter.IsSensitive);
		
		if (defaultValue != null)
		{
			Assert.Equal(defaultValue, parameter.DefaultValue);
		}
	}
}