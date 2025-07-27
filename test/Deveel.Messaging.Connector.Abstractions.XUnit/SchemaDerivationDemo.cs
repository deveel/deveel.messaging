//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging;

/// <summary>
/// Demonstration of ChannelSchema logical identity and compatibility functionality.
/// </summary>
public class SchemaDerivationDemo
{
	[Fact]
	public void DemonstrateSchemaLogicalIdentityAndCompatibility()
	{
		// Example demonstrating ChannelSchema logical identity and compatibility functionality
		
		// 1. Create a comprehensive Twilio SMS base schema
		var twilioBaseSchema = new ChannelSchema("Twilio", "SMS", "2.1.0")
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
			.AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String)
			{
				IsRequired = false,
				Description = "Webhook URL for receiving messages"
			})
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
			.AddMessageProperty(new MessagePropertyConfiguration("IsUrgent", ParameterType.Boolean)
			{
				IsRequired = false,
				Description = "Whether message requires urgent delivery"
			});

		// Verify base schema properties
		Assert.Equal("Twilio", twilioBaseSchema.ChannelProvider);
		Assert.Equal("SMS", twilioBaseSchema.ChannelType);
		Assert.Equal("2.1.0", twilioBaseSchema.Version);
		Assert.Equal("Twilio/SMS/2.1.0", twilioBaseSchema.GetLogicalIdentity());
		Assert.Equal(4, twilioBaseSchema.Parameters.Count);
		Assert.Equal(2, twilioBaseSchema.ContentTypes.Count);
		Assert.Equal(2, twilioBaseSchema.Endpoints.Count);
		Assert.Equal(3, twilioBaseSchema.MessageProperties.Count);
		Assert.True(twilioBaseSchema.Capabilities.HasFlag(ChannelCapability.ReceiveMessages));

		// 2. Create a restricted schema with the same logical identity
		// Note: ChannelProvider, ChannelType, and Version remain the same as base
		var customerSmsSchema = new ChannelSchema(twilioBaseSchema, "Customer Corp Restricted SMS")
			.RestrictCapabilities(ChannelCapability.SendMessages) // Remove receiving capabilities
			.RemoveParameter("WebhookUrl") // Remove webhook support
			.RestrictContentTypes(MessageContentType.PlainText) // Only plain text messages
			.RemoveEndpoint(EndpointType.Url) // Remove webhook endpoint
			.UpdateParameter("FromNumber", param => 
			{
				param.DefaultValue = "+1234567890"; // Set a default number
				param.Description = "Customer's designated sender number";
			})
			.UpdateMessageProperty("PhoneNumber", prop =>
			{
				prop.Description = "Customer phone number in E.164 format";
			})
			.RemoveMessageProperty("IsUrgent") // Remove urgency property
			.UpdateEndpoint(EndpointType.PhoneNumber, endpoint =>
			{
				endpoint.CanReceive = false; // Make it send-only
				endpoint.IsRequired = true;
			});

		// Verify schemas have the same logical identity
		Assert.Equal("Twilio", customerSmsSchema.ChannelProvider);
		Assert.Equal("SMS", customerSmsSchema.ChannelType);
		Assert.Equal("2.1.0", customerSmsSchema.Version);
		Assert.Equal("Twilio/SMS/2.1.0", customerSmsSchema.GetLogicalIdentity());
		Assert.Equal("Customer Corp Restricted SMS", customerSmsSchema.DisplayName);
		
		// Verify restriction effects
		Assert.Equal(3, customerSmsSchema.Parameters.Count); // WebhookUrl removed
		Assert.Single(customerSmsSchema.ContentTypes); // Only PlainText
		Assert.Single(customerSmsSchema.Endpoints); // Only sms
		Assert.Equal(2, customerSmsSchema.MessageProperties.Count); // IsUrgent removed
		Assert.Equal(ChannelCapability.SendMessages, customerSmsSchema.Capabilities);

		// 3. Demonstrate logical compatibility
		Assert.True(twilioBaseSchema.IsCompatibleWith(customerSmsSchema));
		Assert.True(customerSmsSchema.IsCompatibleWith(twilioBaseSchema));
		
		// 4. Demonstrate validation as restriction (before adding new parameters)
		var restrictionValidation = customerSmsSchema.ValidateAsRestrictionOf(twilioBaseSchema);
		Assert.Empty(restrictionValidation); // Should be valid restriction
		
		// 5. Show that modifications are independent by adding a new parameter
		customerSmsSchema.AddParameter(new ChannelParameter("CustomerId", ParameterType.String)
		{
			IsRequired = true,
			Description = "Customer identifier for tracking"
		});

		// Base schema should be unchanged
		Assert.Equal(4, twilioBaseSchema.Parameters.Count);
		Assert.Equal(4, customerSmsSchema.Parameters.Count); // Added CustomerId

		// 6. Demonstrate validation with restricted schema
		
		// Valid settings for the restricted schema
		var validSettings = new ConnectionSettings();
		validSettings.SetParameter("AccountSid", "AC123456789");
		validSettings.SetParameter("AuthToken", "auth_token_secret");
		validSettings.SetParameter("FromNumber", "+1234567890");
		validSettings.SetParameter("CustomerId", "CUST001");

		var validationResults = customerSmsSchema.ValidateConnectionSettings(validSettings);
		Assert.Empty(validationResults);

		// Invalid settings (trying to use removed parameter)
		var invalidSettings = new ConnectionSettings();
		invalidSettings.SetParameter("AccountSid", "AC123456789");
		invalidSettings.SetParameter("AuthToken", "auth_token_secret");
		invalidSettings.SetParameter("FromNumber", "+1234567890");
		invalidSettings.SetParameter("WebhookUrl", "https://example.com/webhook"); // This was removed!
		invalidSettings.SetParameter("CustomerId", "CUST001");

		var invalidResults = customerSmsSchema.ValidateConnectionSettings(invalidSettings).ToList();
		Assert.Single(invalidResults);
		Assert.Contains("Unknown parameter 'WebhookUrl'", invalidResults[0].ErrorMessage);

		// 7. Test message property validation
		var validMessageProps = new Dictionary<string, object?>
		{
			{ "PhoneNumber", "+9876543210" },
			{ "MessageType", "transactional" }
			// Note: IsUrgent was removed in restricted schema
		};

		var messageValidation = customerSmsSchema.ValidateMessageProperties(validMessageProps);
		Assert.Empty(messageValidation);

		// Try to use removed property
		var invalidMessageProps = new Dictionary<string, object?>
		{
			{ "PhoneNumber", "+9876543210" },
			{ "IsUrgent", true } // This was removed!
		};

		var invalidMessageValidation = customerSmsSchema.ValidateMessageProperties(invalidMessageProps).ToList();
		Assert.Single(invalidMessageValidation);
		Assert.Contains("Unknown message property 'IsUrgent'", invalidMessageValidation[0].ErrorMessage);

		// 8. Show how adding parameters affects restriction validation
		// Now it's no longer a valid restriction (it has extra parameters)
		var restrictionValidationAfterAddition = customerSmsSchema.ValidateAsRestrictionOf(twilioBaseSchema).ToList();
		Assert.Single(restrictionValidationAfterAddition);
		Assert.Contains("Parameter 'CustomerId' is not defined in target schema", restrictionValidationAfterAddition[0].ErrorMessage);

		// Try with incompatible schema
		var incompatibleSchema = new ChannelSchema("DifferentProvider", "SMS", "2.1.0");
		var incompatibleValidation = customerSmsSchema.ValidateAsRestrictionOf(incompatibleSchema).ToList();
		Assert.Single(incompatibleValidation);
		Assert.Contains("Schema is not compatible", incompatibleValidation[0].ErrorMessage);

		// 9. Demonstrate that logical identity determines relationship
		Assert.Equal(twilioBaseSchema.GetLogicalIdentity(), customerSmsSchema.GetLogicalIdentity());
		
		// But configurations can be different
		Assert.NotEqual(twilioBaseSchema.DisplayName, customerSmsSchema.DisplayName);
		Assert.NotEqual(twilioBaseSchema.Capabilities, customerSmsSchema.Capabilities);
		
		// Parameter counts can be different due to restrictions
		Assert.Equal(4, twilioBaseSchema.Parameters.Count); // Base has all parameters
		Assert.Equal(4, customerSmsSchema.Parameters.Count); // Restricted has removed one, added one
		
		// But the specific parameters are different
		Assert.Contains(twilioBaseSchema.Parameters, p => p.Name == "WebhookUrl");
		Assert.DoesNotContain(customerSmsSchema.Parameters, p => p.Name == "WebhookUrl");
		Assert.DoesNotContain(twilioBaseSchema.Parameters, p => p.Name == "CustomerId");
		Assert.Contains(customerSmsSchema.Parameters, p => p.Name == "CustomerId");
	}
}