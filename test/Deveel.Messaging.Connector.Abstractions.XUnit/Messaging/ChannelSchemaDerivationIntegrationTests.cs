namespace Deveel.Messaging;

/// <summary>
/// Integration tests for the <see cref="ChannelSchema"/> class derivation functionality 
/// demonstrating real-world use cases.
/// </summary>
public class ChannelSchemaDerivationIntegrationTests
{
	[Fact]
	public void TwilioSmsSchema_CopyForCustomer_RestrictsCorrectly()
	{
		// Arrange - Create a comprehensive Twilio base schema
		var twilioBaseSchema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.WithDisplayName("Twilio SMS Base Connector")
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
				Description = "Sender phone number",
				DefaultValue = "+1234567890"
			})
			.AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String)
			{
				IsRequired = false,
				Description = "URL for receiving webhooks"
			})
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Media)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url))
			.AddMessageProperty(new MessagePropertyConfiguration("PhoneNumber", ParameterType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String) { IsRequired = false })
			.AddMessageProperty(new MessagePropertyConfiguration("IsUrgent", ParameterType.Boolean) { IsRequired = false });

		// Act - Create a restricted copy with specific restrictions for a customer
		// Note: ChannelProvider, ChannelType, and Version remain the same as base (logical identity)
		var customerSmsSchema = new ChannelSchema(twilioBaseSchema, "Customer SMS Notifications")
			.RemoveCapability(ChannelCapability.ReceiveMessages) // Outbound only
			.RemoveCapability(ChannelCapability.BulkMessaging)   // Single messages only
			.RemoveParameter("WebhookUrl")                       // No webhook needed
			.RemoveContentType(MessageContentType.Media)        // Text only
			.RemoveEndpoint(EndpointType.Url)                    // Phone numbers only
			.RemoveMessageProperty("IsUrgent")                   // No urgency levels
			.UpdateMessageProperty("PhoneNumber", prop => 
			{
				prop.Description = "Customer phone number in E.164 format";
			})
			.UpdateEndpoint(EndpointType.PhoneNumber, endpoint => 
			{
				endpoint.CanReceive = false; // Outbound only
				endpoint.IsRequired = true;  // Must specify phone number
			});

		// Assert - Core properties must match base (logical identity)
		Assert.Equal("Twilio", customerSmsSchema.ChannelProvider);
		Assert.Equal("SMS", customerSmsSchema.ChannelType);
		Assert.Equal("1.0.0", customerSmsSchema.Version);
		Assert.Equal("Customer SMS Notifications", customerSmsSchema.DisplayName);
		
		// Verify logical compatibility
		Assert.True(twilioBaseSchema.IsCompatibleWith(customerSmsSchema));
		Assert.Equal(twilioBaseSchema.GetLogicalIdentity(), customerSmsSchema.GetLogicalIdentity());

		// Verify capability restriction
		Assert.Equal(ChannelCapability.SendMessages | ChannelCapability.MessageStatusQuery, customerSmsSchema.Capabilities);
		Assert.False(customerSmsSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

		// Verify parameter changes
		Assert.Equal(3, customerSmsSchema.Parameters.Count); // WebhookUrl removed
		Assert.DoesNotContain(customerSmsSchema.Parameters, p => p.Name == "WebhookUrl");
		
		var fromNumberParam = customerSmsSchema.Parameters.First(p => p.Name == "FromNumber");
		Assert.Equal("+1234567890", fromNumberParam.DefaultValue);

		// Verify content type restriction
		Assert.Single(customerSmsSchema.ContentTypes);
		Assert.Contains(MessageContentType.PlainText, customerSmsSchema.ContentTypes);
		Assert.DoesNotContain(MessageContentType.Media, customerSmsSchema.ContentTypes);

		// Verify endpoint changes
		Assert.Single(customerSmsSchema.Endpoints); // webhook removed
		var smsEndpoint = customerSmsSchema.Endpoints.First();
		Assert.Equal(EndpointType.PhoneNumber, smsEndpoint.Type);
		Assert.True(smsEndpoint.CanSend);
		Assert.False(smsEndpoint.CanReceive);
		Assert.True(smsEndpoint.IsRequired);

		// Verify message property changes
		Assert.Equal(2, customerSmsSchema.MessageProperties.Count); // IsUrgent removed
		Assert.DoesNotContain(customerSmsSchema.MessageProperties, p => p.Name == "IsUrgent");
		
		var phoneNumberProp = customerSmsSchema.MessageProperties.First(p => p.Name == "PhoneNumber");
		Assert.Equal("Customer phone number in E.164 format", phoneNumberProp.Description);

		// Verify base schema is unchanged
		Assert.Equal(4, twilioBaseSchema.Parameters.Count);
		Assert.Equal(2, twilioBaseSchema.ContentTypes.Count);
		Assert.Equal(2, twilioBaseSchema.Endpoints.Count);
		Assert.Equal(3, twilioBaseSchema.MessageProperties.Count);
		Assert.True(twilioBaseSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

		// Verify restriction validation
		var restrictionValidation = customerSmsSchema.ValidateAsRestrictionOf(twilioBaseSchema);
		Assert.Empty(restrictionValidation);
	}

	[Fact]
	public void EmailSchema_DepartmentSpecificCopies_WorkIndependently()
	{
		// Arrange - Base email schema with full capabilities
		var baseEmailSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
			.WithDisplayName("Corporate SMTP Connector")
			.WithCapabilities(
				ChannelCapability.SendMessages | 
				ChannelCapability.Templates | 
				ChannelCapability.MediaAttachments |
				ChannelCapability.BulkMessaging)
			.AddParameter(new ChannelParameter("SmtpHost", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("SmtpPort", ParameterType.Integer) { IsRequired = true, DefaultValue = 587 })
			.AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("Password", ParameterType.String) { IsRequired = true, IsSensitive = true })
			.AddParameter(new ChannelParameter("EnableSsl", ParameterType.Boolean) { DefaultValue = true })
			.AddParameter(new ChannelParameter("MaxAttachmentSize", ParameterType.Integer) { DefaultValue = 25 })
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Multipart)
			.AddContentType(MessageContentType.Media)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.ApplicationId))
			.AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer) { IsRequired = false })
			.AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean) { IsRequired = false });

		// Act - Create HR Department schema (restricted copy)
		var hrEmailSchema = new ChannelSchema(baseEmailSchema, "HR Secure Email")
			.RemoveCapability(ChannelCapability.MediaAttachments) // No attachments for HR
			.RemoveCapability(ChannelCapability.BulkMessaging) // No bulk emails
			.RestrictContentTypes(MessageContentType.PlainText) // Plain text only
			.RemoveParameter("MaxAttachmentSize") // Not needed without attachments
			.UpdateParameter("EnableSsl", param => 
			{
				param.IsRequired = true; // Force SSL for HR
				param.DefaultValue = true;
			})
			.RemoveMessageProperty("IsHtml"); // No HTML emails

		// Act - Create Marketing Department schema (enhanced copy)
		var marketingEmailSchema = new ChannelSchema(baseEmailSchema, "Marketing Bulk Email")
			.UpdateParameter("MaxAttachmentSize", param =>
			{
				param.DefaultValue = 10; // Smaller attachments for bulk emails
				param.Description = "Maximum attachment size in MB for bulk emails";
			})
			.AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
			{
				IsRequired = true,
				Description = "Marketing campaign identifier"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("SegmentId", ParameterType.String)
			{
				IsRequired = false,
				Description = "Target segment identifier"
			});

		// Assert HR Schema - Core properties must match base (logical identity)
		Assert.Equal("SMTP", hrEmailSchema.ChannelProvider);
		Assert.Equal("Email", hrEmailSchema.ChannelType);
		Assert.Equal("2.0.0", hrEmailSchema.Version);
		Assert.Equal("HR Secure Email", hrEmailSchema.DisplayName);
		Assert.True(baseEmailSchema.IsCompatibleWith(hrEmailSchema));
		Assert.False(hrEmailSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.False(hrEmailSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
		Assert.True(hrEmailSchema.Capabilities.HasFlag(ChannelCapability.SendMessages));
		Assert.Single(hrEmailSchema.ContentTypes);
		Assert.Contains(MessageContentType.PlainText, hrEmailSchema.ContentTypes);
		Assert.Equal(5, hrEmailSchema.Parameters.Count); // MaxAttachmentSize removed
		Assert.Equal(2, hrEmailSchema.MessageProperties.Count); // IsHtml removed

		// Assert Marketing Schema - Core properties must match base (logical identity)
		Assert.Equal("SMTP", marketingEmailSchema.ChannelProvider);
		Assert.Equal("Email", marketingEmailSchema.ChannelType);
		Assert.Equal("2.0.0", marketingEmailSchema.Version);
		Assert.Equal("Marketing Bulk Email", marketingEmailSchema.DisplayName);
		Assert.True(baseEmailSchema.IsCompatibleWith(marketingEmailSchema));
		Assert.True(marketingEmailSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
		Assert.Equal(4, marketingEmailSchema.ContentTypes.Count); // All content types preserved
		Assert.Equal(6, marketingEmailSchema.Parameters.Count); // All parameters preserved
		Assert.Equal(5, marketingEmailSchema.MessageProperties.Count); // 2 new properties added

		var maxAttachmentParam = marketingEmailSchema.Parameters.First(p => p.Name == "MaxAttachmentSize");
		Assert.Equal(10, maxAttachmentParam.DefaultValue);
		Assert.Equal("Maximum attachment size in MB for bulk emails", maxAttachmentParam.Description);

		// Verify all schemas have the same logical identity
		Assert.Equal(baseEmailSchema.GetLogicalIdentity(), hrEmailSchema.GetLogicalIdentity());
		Assert.Equal(baseEmailSchema.GetLogicalIdentity(), marketingEmailSchema.GetLogicalIdentity());

		// Verify base schema is unchanged
		Assert.Equal(6, baseEmailSchema.Parameters.Count);
		Assert.Equal(4, baseEmailSchema.ContentTypes.Count);
		Assert.Equal(3, baseEmailSchema.MessageProperties.Count);
		Assert.True(baseEmailSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(baseEmailSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));

		// Verify restriction validations
		var hrRestrictionValidation = hrEmailSchema.ValidateAsRestrictionOf(baseEmailSchema);
		Assert.Empty(hrRestrictionValidation);
		
		// Marketing schema is not a restriction (it adds properties)
		var marketingRestrictionValidation = marketingEmailSchema.ValidateAsRestrictionOf(baseEmailSchema).ToList();
		Assert.NotEmpty(marketingRestrictionValidation); // Should have validation errors for added properties
	}

	[Fact]
	public void MultiChannelSchema_ChannelSpecificCopies_RestrictEndpointsCorrectly()
	{
		// Arrange - Universal messaging base schema
		var baseMessagingSchema = new ChannelSchema("Universal", "MultiChannel", "2.0.0")
			.WithDisplayName("Universal Multi-Channel Connector")
			.AddAuthenticationType(AuthenticationType.Token)
			.AddAuthenticationType(AuthenticationType.ApiKey)
			.WithCapabilities(
				ChannelCapability.SendMessages |
				ChannelCapability.ReceiveMessages |
				ChannelCapability.MessageStatusQuery |
				ChannelCapability.BulkMessaging |
				ChannelCapability.Templates |
				ChannelCapability.MediaAttachments |
				ChannelCapability.HealthCheck)
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Multipart)
			.AddContentType(MessageContentType.Media)
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url))
			.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.ApplicationId))
			.AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.Integer))
			.AddMessageProperty(new MessagePropertyConfiguration("Category", ParameterType.String))
			.AddMessageProperty(new MessagePropertyConfiguration("MessageType", ParameterType.String));
;

		// Act - Create SMS-only copy
		var smsOnlySchema = new ChannelSchema(baseMessagingSchema, "SMS Only Messaging")
			.RemoveCapability(ChannelCapability.MediaAttachments) // SMS doesn't support large media
			.RestrictContentTypes(MessageContentType.PlainText) // SMS is text only
			.RestrictAuthenticationTypes(AuthenticationType.Token) // Restrict to Token auth (subset of base)
			.RemoveEndpoint(EndpointType.EmailAddress)
			.RemoveEndpoint(EndpointType.ApplicationId)
			.UpdateEndpoint(EndpointType.PhoneNumber, endpoint => { endpoint.IsRequired = true; })
			.UpdateMessageProperty("MessageType", e =>
			{
				e.IsRequired = false;
				e.Description = "SMS message type (transactional, promotional)";
			});

		// Act - Create Email-only copy with enhanced features
		var emailOnlySchema = new ChannelSchema(baseMessagingSchema, "Email Only Service")
			.RemoveEndpoint(EndpointType.PhoneNumber)
			.RemoveEndpoint(EndpointType.Url)
			.RemoveEndpoint(EndpointType.ApplicationId)
			.RemoveCapability(ChannelCapability.MediaAttachments)
			.RestrictContentTypes(MessageContentType.PlainText, MessageContentType.Html)
			.AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
			{
				IsRequired = true,
				Description = "Email subject line"
			})
			.AddMessageProperty(new MessagePropertyConfiguration("IsHtml", ParameterType.Boolean)
			{
				IsRequired = false,
				Description = "Indicates if the email is HTML formatted"
			});

		// Assert multiple derived schemas don't affect each other
		var smsOnlyDerivedSchema = new ChannelSchema(baseMessagingSchema, "SMS Only Service")
			.RemoveEndpoint(EndpointType.EmailAddress)
			.RemoveEndpoint(EndpointType.Url)
			.RemoveEndpoint(EndpointType.ApplicationId)
			.RestrictContentTypes(MessageContentType.PlainText);

		// Assert SMS Schema - Core properties must match base (logical identity)
		Assert.Equal("Universal", smsOnlySchema.ChannelProvider);
		Assert.Equal("MultiChannel", smsOnlySchema.ChannelType);
		Assert.Equal("2.0.0", smsOnlySchema.Version);
		Assert.Equal("SMS Only Messaging", smsOnlySchema.DisplayName);
		Assert.True(baseMessagingSchema.IsCompatibleWith(smsOnlySchema));
		Assert.False(smsOnlySchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.Single(smsOnlySchema.ContentTypes);
		Assert.Contains(MessageContentType.PlainText, smsOnlySchema.ContentTypes);
		Assert.Single(smsOnlySchema.AuthenticationTypes); // Restricted to Token only
		Assert.Contains(AuthenticationType.Token, smsOnlySchema.AuthenticationTypes);
		Assert.Equal(2, smsOnlySchema.Endpoints.Count); // sms and webhook only
		Assert.Contains(smsOnlySchema.Endpoints, e => e.Type == EndpointType.PhoneNumber && e.IsRequired);
		Assert.DoesNotContain(smsOnlySchema.Endpoints, e => e.Type == EndpointType.EmailAddress);
		Assert.Equal(3, smsOnlySchema.MessageProperties.Count); // Same count as base (Priority, MessageType) + 1 (MessageType)

		// Assert Email Schema - Core properties must match base (logical identity)
		Assert.Equal("Universal", emailOnlySchema.ChannelProvider);
		Assert.Equal("MultiChannel", emailOnlySchema.ChannelType);
		Assert.Equal("2.0.0", emailOnlySchema.Version);
		Assert.Equal("Email Only Service", emailOnlySchema.DisplayName);
		Assert.True(baseMessagingSchema.IsCompatibleWith(emailOnlySchema));
		Assert.False(emailOnlySchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(emailOnlySchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
		Assert.Equal(2, emailOnlySchema.ContentTypes.Count); // Plain Text and Html
		Assert.Single(emailOnlySchema.Endpoints); // email only
		Assert.Contains(emailOnlySchema.Endpoints, e => e.Type == EndpointType.EmailAddress);
		Assert.DoesNotContain(emailOnlySchema.Endpoints, e => e.Type == EndpointType.PhoneNumber);
		Assert.Equal(5, emailOnlySchema.MessageProperties.Count); // Base 3 + 2 added (Subject, IsHtml)

		// Verify both schemas are independent but have same logical identity
		Assert.Equal(baseMessagingSchema.GetLogicalIdentity(), smsOnlySchema.GetLogicalIdentity());
		Assert.Equal(baseMessagingSchema.GetLogicalIdentity(), emailOnlySchema.GetLogicalIdentity());
		
		var smsMessageType = smsOnlySchema.MessageProperties.First(p => p.Name == "MessageType");
		Assert.Equal("SMS message type (transactional, promotional)", smsMessageType.Description);

		// Verify base schema is unchanged
		Assert.Equal(4, baseMessagingSchema.Endpoints.Count);
		Assert.Equal(3, baseMessagingSchema.MessageProperties.Count); // Recipient, Priority, MessageType
		Assert.Equal(2, baseMessagingSchema.AuthenticationTypes.Count); // Base has both Token and ApiKey
		Assert.True(baseMessagingSchema.Capabilities.HasFlag(ChannelCapability.BulkMessaging));
		Assert.True(baseMessagingSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));

		// Verify restriction validations
		var smsRestrictionValidation = smsOnlySchema.ValidateAsRestrictionOf(baseMessagingSchema);
		Assert.Empty(smsRestrictionValidation); // SMS is now a valid restriction
		
		// Email schema is not a restriction (it adds capabilities and properties)
		var emailRestrictionValidation = emailOnlySchema.ValidateAsRestrictionOf(baseMessagingSchema).ToList();
		Assert.NotEmpty(emailRestrictionValidation); // Should have validation errors for added properties
	}

	[Fact]
	public void DerivedSchema_ValidationWorks_WithRestrictedProperties()
	{
		// Arrange
		var baseSchema = new ChannelSchema("Base", "Test", "1.0.0")
			.AddParameter(new ChannelParameter("RequiredParam", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("OptionalParam", ParameterType.String) { IsRequired = false })
			.AddParameter(new ChannelParameter("RemovedParam", ParameterType.String) { IsRequired = false })
			.AddMessageProperty(new MessagePropertyConfiguration("RequiredProp", ParameterType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("OptionalProp", ParameterType.String) { IsRequired = false })
			.AddMessageProperty(new MessagePropertyConfiguration("RemovedProp", ParameterType.String) { IsRequired = false });

		var derivedSchema = new ChannelSchema(baseSchema, "Restricted Schema")
			.RemoveParameter("RemovedParam")
			.RemoveMessageProperty("RemovedProp")
			.UpdateParameter("OptionalParam", param => param.IsRequired = true); // Make it required

		// Verify core properties match
		Assert.Equal(baseSchema.ChannelProvider, derivedSchema.ChannelProvider);
		Assert.Equal(baseSchema.ChannelType, derivedSchema.ChannelType);
		Assert.Equal(baseSchema.Version, derivedSchema.Version);

		// Act & Assert - Valid connection settings
		var validConnectionSettings = new ConnectionSettings();
		validConnectionSettings.SetParameter("RequiredParam", "value1");
		validConnectionSettings.SetParameter("OptionalParam", "value2");

		var connectionValidationResults = derivedSchema.ValidateConnectionSettings(validConnectionSettings);
		Assert.Empty(connectionValidationResults);

		// Act & Assert - Invalid connection settings (missing required parameter)
		var invalidConnectionSettings = new ConnectionSettings();
		invalidConnectionSettings.SetParameter("RequiredParam", "value1");
		// Missing OptionalParam which is now required in derived schema

		var invalidConnectionResults = derivedSchema.ValidateConnectionSettings(invalidConnectionSettings).ToList();
		Assert.Single(invalidConnectionResults);
		Assert.Contains("Required parameter 'OptionalParam' is missing", invalidConnectionResults[0].ErrorMessage);

		// Act & Assert - Unknown parameter (removed in derived schema)
		var unknownParamSettings = new ConnectionSettings();
		unknownParamSettings.SetParameter("RequiredParam", "value1");
		unknownParamSettings.SetParameter("OptionalParam", "value2");
		unknownParamSettings.SetParameter("RemovedParam", "value3"); // This was removed

		var unknownParamResults = derivedSchema.ValidateConnectionSettings(unknownParamSettings).ToList();
		Assert.Single(unknownParamResults);
		Assert.Contains("Unknown parameter 'RemovedParam' is not supported", unknownParamResults[0].ErrorMessage);

		// Act & Assert - Valid message properties
		var validMessageProperties = new Dictionary<string, object?>
		{
			{ "RequiredProp", "value1" },
			{ "OptionalProp", "value2" }
		};

		var messageValidationResults = derivedSchema.ValidateMessageProperties(validMessageProperties);
		Assert.Empty(messageValidationResults);

		// Act & Assert - Unknown message property (removed in derived schema)
		var unknownPropMessage = new Dictionary<string, object?>
		{
			{ "RequiredProp", "value1" },
			{ "RemovedProp", "value2" } // This was removed
		};

		var unknownPropResults = derivedSchema.ValidateMessageProperties(unknownPropMessage).ToList();
		Assert.Single(unknownPropResults);
		Assert.Contains("Unknown message property 'RemovedProp' is not supported", unknownPropResults[0].ErrorMessage);
	}

	[Fact]
	public void DerivedSchema_MultipleGenerations_WorkCorrectly()
	{
		// Arrange - Create a three-generation schema hierarchy
		var grandparentSchema = new ChannelSchema("Base", "Multi", "1.0.0")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | 
							 ChannelCapability.Templates | ChannelCapability.MediaAttachments)
			.AddParameter(new ChannelParameter("Param1", ParameterType.String))
			.AddParameter(new ChannelParameter("Param2", ParameterType.String))
			.AddParameter(new ChannelParameter("Param3", ParameterType.String))
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Media);

		var parentSchema = new ChannelSchema(grandparentSchema, "Parent Restricted")
			.RemoveCapability(ChannelCapability.MediaAttachments)
			.RemoveParameter("Param3")
			.RemoveContentType(MessageContentType.Media);

		var childSchema = new ChannelSchema(parentSchema, "Child Very Restricted")
			.RemoveCapability(ChannelCapability.Templates)
			.RemoveParameter("Param2")
			.RestrictContentTypes(MessageContentType.PlainText);

		// Act & Assert
		// All schemas should have the same logical identity
		Assert.Equal("Base", grandparentSchema.ChannelProvider);
		Assert.Equal("Multi", grandparentSchema.ChannelType);
		Assert.Equal("1.0.0", grandparentSchema.Version);
		
		Assert.Equal("Base", parentSchema.ChannelProvider);
		Assert.Equal("Multi", parentSchema.ChannelType);
		Assert.Equal("1.0.0", parentSchema.Version);
		
		Assert.Equal("Base", childSchema.ChannelProvider);
		Assert.Equal("Multi", childSchema.ChannelType);
		Assert.Equal("1.0.0", childSchema.Version);

		// Verify logical compatibility
		Assert.True(grandparentSchema.IsCompatibleWith(parentSchema));
		Assert.True(grandparentSchema.IsCompatibleWith(childSchema));
		Assert.True(parentSchema.IsCompatibleWith(childSchema));
		
		Assert.Equal(grandparentSchema.GetLogicalIdentity(), parentSchema.GetLogicalIdentity());
		Assert.Equal(grandparentSchema.GetLogicalIdentity(), childSchema.GetLogicalIdentity());

		// Grandparent should have everything
		Assert.True(grandparentSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(grandparentSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.Equal(3, grandparentSchema.Parameters.Count);
		Assert.Equal(3, grandparentSchema.ContentTypes.Count);

		// Parent should have restrictions from first copy
		Assert.False(parentSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(parentSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.Equal(2, parentSchema.Parameters.Count);
		Assert.Equal(2, parentSchema.ContentTypes.Count);
		Assert.DoesNotContain(parentSchema.Parameters, p => p.Name == "Param3");

		// Child should have restrictions from both copies
		Assert.False(childSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.False(childSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.Single(childSchema.Parameters);
		Assert.Single(childSchema.ContentTypes);
		Assert.DoesNotContain(childSchema.Parameters, p => p.Name == "Param2");
		Assert.DoesNotContain(childSchema.Parameters, p => p.Name == "Param3");
		Assert.Contains(MessageContentType.PlainText, childSchema.ContentTypes);

		// Verify restriction validations
		var parentRestrictionValidation = parentSchema.ValidateAsRestrictionOf(grandparentSchema);
		Assert.Empty(parentRestrictionValidation);
		
		var childRestrictionValidation = childSchema.ValidateAsRestrictionOf(parentSchema);
		Assert.Empty(childRestrictionValidation);
		
		var childFromGrandparentValidation = childSchema.ValidateAsRestrictionOf(grandparentSchema);
		Assert.Empty(childFromGrandparentValidation);
	}
	
	[Fact]
	public void CopiedSchema_ValidationWorks_WithRestrictedProperties()
	{
		// Arrange
		var baseSchema = new ChannelSchema("Base", "Test", "1.0.0")
			.AddParameter(new ChannelParameter("RequiredParam", ParameterType.String) { IsRequired = true })
			.AddParameter(new ChannelParameter("OptionalParam", ParameterType.String) { IsRequired = false })
			.AddParameter(new ChannelParameter("RemovedParam", ParameterType.String) { IsRequired = false })
			.AddMessageProperty(new MessagePropertyConfiguration("RequiredProp", ParameterType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("OptionalProp", ParameterType.String) { IsRequired = false })
			.AddMessageProperty(new MessagePropertyConfiguration("RemovedProp", ParameterType.String) { IsRequired = false });

		var restrictedSchema = new ChannelSchema(baseSchema, "Restricted Schema")
			.RemoveParameter("RemovedParam")
			.RemoveMessageProperty("RemovedProp")
			.UpdateParameter("OptionalParam", param => param.IsRequired = true); // Make it required

		// Verify logical identity is maintained
		Assert.Equal(baseSchema.ChannelProvider, restrictedSchema.ChannelProvider);
		Assert.Equal(baseSchema.ChannelType, restrictedSchema.ChannelType);
		Assert.Equal(baseSchema.Version, restrictedSchema.Version);
		Assert.True(baseSchema.IsCompatibleWith(restrictedSchema));
		Assert.Equal(baseSchema.GetLogicalIdentity(), restrictedSchema.GetLogicalIdentity());

		// Act & Assert - Valid connection settings
		var validConnectionSettings = new ConnectionSettings();
		validConnectionSettings.SetParameter("RequiredParam", "value1");
		validConnectionSettings.SetParameter("OptionalParam", "value2");

		var connectionValidationResults = restrictedSchema.ValidateConnectionSettings(validConnectionSettings);
		Assert.Empty(connectionValidationResults);

		// Act & Assert - Invalid connection settings (missing required parameter)
		var invalidConnectionSettings = new ConnectionSettings();
		invalidConnectionSettings.SetParameter("RequiredParam", "value1");
		// Missing OptionalParam which is now required in restricted schema

		var invalidConnectionResults = restrictedSchema.ValidateConnectionSettings(invalidConnectionSettings).ToList();
		Assert.Single(invalidConnectionResults);
		Assert.Contains("Required parameter 'OptionalParam' is missing", invalidConnectionResults[0].ErrorMessage);

		// Act & Assert - Unknown parameter (removed in restricted schema)
		var unknownParamSettings = new ConnectionSettings();
		unknownParamSettings.SetParameter("RequiredParam", "value1");
		unknownParamSettings.SetParameter("OptionalParam", "value2");
		unknownParamSettings.SetParameter("RemovedParam", "value3"); // This was removed

		var unknownParamResults = restrictedSchema.ValidateConnectionSettings(unknownParamSettings).ToList();
		Assert.Single(unknownParamResults);
		Assert.Contains("Unknown parameter 'RemovedParam' is not supported", unknownParamResults[0].ErrorMessage);

		// Act & Assert - Valid message properties
		var validMessageProperties = new Dictionary<string, object?>
		{
			{ "RequiredProp", "value1" },
			{ "OptionalProp", "value2" }
		};

		var messageValidationResults = restrictedSchema.ValidateMessageProperties(validMessageProperties);
		Assert.Empty(messageValidationResults);

		// Act & Assert - Unknown message property (removed in restricted schema)
		var unknownPropMessage = new Dictionary<string, object?>
		{
			{ "RequiredProp", "value1" },
			{ "RemovedProp", "value2" } // This was removed
		};

		var unknownPropResults = restrictedSchema.ValidateMessageProperties(unknownPropMessage).ToList();
		Assert.Single(unknownPropResults);
		Assert.Contains("Unknown message property 'RemovedProp' is not supported", unknownPropResults[0].ErrorMessage);

		// Verify restriction validation
		var restrictionValidation = restrictedSchema.ValidateAsRestrictionOf(baseSchema);
		Assert.Empty(restrictionValidation); // Should be a valid restriction
	}

	[Fact]
	public void CopiedSchema_MultipleGenerations_WorkCorrectly()
	{
		// Arrange - Create a three-generation schema hierarchy through copying
		var grandparentSchema = new ChannelSchema("Base", "Multi", "1.0.0")
			.WithCapabilities(ChannelCapability.SendMessages | ChannelCapability.ReceiveMessages | 
							 ChannelCapability.Templates | ChannelCapability.MediaAttachments)
			.AddParameter(new ChannelParameter("Param1", ParameterType.String))
			.AddParameter(new ChannelParameter("Param2", ParameterType.String))
			.AddParameter(new ChannelParameter("Param3", ParameterType.String))
			.AddContentType(MessageContentType.PlainText)
			.AddContentType(MessageContentType.Html)
			.AddContentType(MessageContentType.Media);

		var parentSchema = new ChannelSchema(grandparentSchema, "Parent Restricted")
			.RemoveCapability(ChannelCapability.MediaAttachments)
			.RemoveParameter("Param3")
			.RemoveContentType(MessageContentType.Media);

		var childSchema = new ChannelSchema(parentSchema, "Child Very Restricted")
			.RemoveCapability(ChannelCapability.Templates)
			.RemoveParameter("Param2")
			.RestrictContentTypes(MessageContentType.PlainText);

		// Act & Assert
		// All schemas should have the same logical identity
		Assert.Equal("Base", grandparentSchema.ChannelProvider);
		Assert.Equal("Multi", grandparentSchema.ChannelType);
		Assert.Equal("1.0.0", grandparentSchema.Version);
		
		Assert.Equal("Base", parentSchema.ChannelProvider);
		Assert.Equal("Multi", parentSchema.ChannelType);
		Assert.Equal("1.0.0", parentSchema.Version);
		
		Assert.Equal("Base", childSchema.ChannelProvider);
		Assert.Equal("Multi", childSchema.ChannelType);
		Assert.Equal("1.0.0", childSchema.Version);

		// Verify logical compatibility
		Assert.True(grandparentSchema.IsCompatibleWith(parentSchema));
		Assert.True(grandparentSchema.IsCompatibleWith(childSchema));
		Assert.True(parentSchema.IsCompatibleWith(childSchema));
		
		Assert.Equal(grandparentSchema.GetLogicalIdentity(), parentSchema.GetLogicalIdentity());
		Assert.Equal(grandparentSchema.GetLogicalIdentity(), childSchema.GetLogicalIdentity());

		// Grandparent should have everything
		Assert.True(grandparentSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(grandparentSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.Equal(3, grandparentSchema.Parameters.Count);
		Assert.Equal(3, grandparentSchema.ContentTypes.Count);

		// Parent should have restrictions from first copy
		Assert.False(parentSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.True(parentSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.Equal(2, parentSchema.Parameters.Count);
		Assert.Equal(2, parentSchema.ContentTypes.Count);
		Assert.DoesNotContain(parentSchema.Parameters, p => p.Name == "Param3");

		// Child should have restrictions from both copies
		Assert.False(childSchema.Capabilities.HasFlag(ChannelCapability.MediaAttachments));
		Assert.False(childSchema.Capabilities.HasFlag(ChannelCapability.Templates));
		Assert.Single(childSchema.Parameters);
		Assert.Single(childSchema.ContentTypes);
		Assert.DoesNotContain(childSchema.Parameters, p => p.Name == "Param2");
		Assert.DoesNotContain(childSchema.Parameters, p => p.Name == "Param3");
		Assert.Contains(MessageContentType.PlainText, childSchema.ContentTypes);

		// Verify restriction validations
		var parentRestrictionValidation = parentSchema.ValidateAsRestrictionOf(grandparentSchema);
		Assert.Empty(parentRestrictionValidation);
		
		var childRestrictionValidation = childSchema.ValidateAsRestrictionOf(parentSchema);
		Assert.Empty(childRestrictionValidation);
		
		var childFromGrandparentValidation = childSchema.ValidateAsRestrictionOf(grandparentSchema);
		Assert.Empty(childFromGrandparentValidation);
	}
}