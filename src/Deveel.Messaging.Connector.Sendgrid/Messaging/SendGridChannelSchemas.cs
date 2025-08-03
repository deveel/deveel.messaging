//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides pre-configured channel schemas for SendGrid email messaging services.
    /// </summary>
    public static class SendGridChannelSchemas
    {
        /// <summary>
        /// Gets the comprehensive base schema for SendGrid email messaging that supports
        /// all available capabilities and configurations.
        /// </summary>
        /// <remarks>
        /// This schema includes all SendGrid email capabilities including sending,
        /// status queries, templates, and webhook support. It can be used as-is
        /// or derived to create more restrictive configurations for specific use cases.
        /// </remarks>
        public static ChannelSchema SendGridEmail => new ChannelSchema("SendGrid", "Email", "1.0.0")
            .WithDisplayName("SendGrid Email Connector")
            .WithCapabilities(
                ChannelCapability.SendMessages | 
                ChannelCapability.MessageStatusQuery |
                ChannelCapability.BulkMessaging |
                ChannelCapability.Templates |
                ChannelCapability.MediaAttachments |
                ChannelCapability.HealthCheck)
            .AddParameter(new ChannelParameter("ApiKey", ParameterType.String)
            {
                IsRequired = true,
                IsSensitive = true,
                Description = "SendGrid API Key - found in your SendGrid Dashboard under Settings > API Keys"
            })
            .AddParameter(new ChannelParameter("SandboxMode", ParameterType.Boolean)
            {
                IsRequired = false,
                DefaultValue = false,
                Description = "Enable sandbox mode for testing without actually sending emails"
            })
            .AddParameter(new ChannelParameter("WebhookUrl", ParameterType.String)
            {
                IsRequired = false,
                Description = "URL to receive webhook notifications for email events and status updates"
            })
            .AddParameter(new ChannelParameter("TrackingSettings", ParameterType.Boolean)
            {
                IsRequired = false,
                DefaultValue = true,
                Description = "Enable tracking for opens, clicks, and other email engagement metrics"
            })
            .AddParameter(new ChannelParameter("DefaultFromName", ParameterType.String)
            {
                IsRequired = false,
                Description = "Default sender name to use when not specified in the message"
            })
            .AddParameter(new ChannelParameter("DefaultReplyTo", ParameterType.String)
            {
                IsRequired = false,
                Description = "Default reply-to email address"
            })
            .AddContentType(MessageContentType.PlainText)
            .AddContentType(MessageContentType.Html)
            .AddContentType(MessageContentType.Template)
            .AddContentType(MessageContentType.Multipart)
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress)
            {
                CanSend = true,
                CanReceive = true, // Email addresses can be both senders and receivers
                IsRequired = true // Email address is required for sending
            })
            .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
            {
                CanSend = false,
                CanReceive = true // For webhooks
            })
            .AddAuthenticationType(AuthenticationType.ApiKey)
            .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String)
            {
                IsRequired = true,
                Description = "Email subject line"
            }.Configure()
                .WithMaxLength(998) // RFC 2822 limit
                .WithCustomValidator(value => 
                {
                    if (value != null && string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        return new[]
                        {
                            new ValidationResult(
                                "Subject cannot be empty",
                                new[] { "Subject" })
                        };
                    }
                    return Enumerable.Empty<ValidationResult>();
                })
                .Build())
            .AddMessageProperty(new MessagePropertyConfiguration("Priority", ParameterType.String)
            {
                IsRequired = false,
                Description = "Email priority (low, normal, high)"
            }.Configure()
                .WithAllowedValues("low", "normal", "high")
                .Build())
            .AddMessageProperty(new MessagePropertyConfiguration("Categories", ParameterType.String)
            {
                IsRequired = false,
                Description = "Comma-separated list of categories for tracking and organization"
            }.Configure()
                .WithCustomValidator(ValidateCategories)
                .Build())
            .AddMessageProperty(new MessagePropertyConfiguration("CustomArgs", ParameterType.String)
            {
                IsRequired = false,
                Description = "JSON object containing custom arguments to attach to the email"
            }.Configure()
                .WithCustomValidator(ValidateJsonContent)
                .Build())
            .AddMessageProperty(new MessagePropertyConfiguration("SendAt", ParameterType.String)
            {
                IsRequired = false,
                Description = "Schedule the email to be sent at a specific time (ISO 8601 format or DateTime)"
            }.Configure()
                .WithCustomValidator(ValidateSendAtTime)
                .Build())
            .AddMessageProperty(new MessagePropertyConfiguration("BatchId", ParameterType.String)
            {
                IsRequired = false,
                Description = "Batch ID for grouping emails together for batch operations"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("IpPoolName", ParameterType.String)
            {
                IsRequired = false,
                Description = "IP pool name to use for sending this email"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("AsmGroupId", ParameterType.Integer)
            {
                IsRequired = false,
                Description = "Unsubscribe group ID for subscription management"
            }.Configure()
                .WithMinValue(1)
                .Build());

        /// <summary>
        /// Gets a simplified email-only schema for basic email messaging use cases.
        /// This schema removes webhooks, templates, and advanced features.
        /// </summary>
        public static ChannelSchema SimpleEmail => new ChannelSchema(SendGridEmail, "SendGrid Simple Email")
            .RemoveCapability(ChannelCapability.BulkMessaging)
            .RemoveCapability(ChannelCapability.Templates)
            .RemoveCapability(ChannelCapability.MediaAttachments)
            .RemoveParameter("WebhookUrl")
            .RemoveParameter("TrackingSettings")
            .RemoveContentType(MessageContentType.Template)
            .RemoveContentType(MessageContentType.Multipart)
            .RemoveMessageProperty("Categories")
            .RemoveMessageProperty("CustomArgs")
            .RemoveMessageProperty("SendAt")
            .RemoveMessageProperty("BatchId")
            .RemoveMessageProperty("IpPoolName")
            .RemoveMessageProperty("AsmGroupId");

        /// <summary>
        /// Gets a transactional email schema optimized for automated notifications and receipts.
        /// This schema includes tracking and delivery confirmation but removes bulk capabilities.
        /// </summary>
        public static ChannelSchema TransactionalEmail => new ChannelSchema(SendGridEmail, "SendGrid Transactional Email")
            .RemoveCapability(ChannelCapability.BulkMessaging)
            .RemoveCapability(ChannelCapability.Templates)
            .RemoveParameter("WebhookUrl")
            .UpdateParameter("TrackingSettings", param => param.DefaultValue = true)
            .RemoveContentType(MessageContentType.Template)
            .RemoveMessageProperty("SendAt")
            .RemoveMessageProperty("BatchId")
            .RemoveMessageProperty("IpPoolName");

        /// <summary>
        /// Gets a marketing email schema optimized for campaigns and newsletters.
        /// This schema includes all tracking, templates, and bulk messaging capabilities.
        /// </summary>
        public static ChannelSchema MarketingEmail => new ChannelSchema(SendGridEmail, "SendGrid Marketing Email")
            .UpdateParameter("TrackingSettings", param => param.DefaultValue = true)
            .AddMessageProperty(new MessagePropertyConfiguration("ListId", ParameterType.String)
            {
                IsRequired = false,
                Description = "Marketing list ID for campaign tracking"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("CampaignId", ParameterType.String)
            {
                IsRequired = false,
                Description = "Campaign ID for grouping and tracking marketing emails"
            });

        /// <summary>
        /// Gets a template-focused email schema optimized for dynamic content using SendGrid templates.
        /// This schema focuses on template messaging capabilities with webhook support for tracking.
        /// </summary>
        public static ChannelSchema TemplateEmail => new ChannelSchema(SendGridEmail, "SendGrid Template Email")
            .RemoveCapability(ChannelCapability.MediaAttachments)
            .RemoveContentType(MessageContentType.PlainText)
            .RemoveContentType(MessageContentType.Html)
            .RemoveContentType(MessageContentType.Multipart)
            .AddMessageProperty(new MessagePropertyConfiguration("TemplateId", ParameterType.String)
            {
                IsRequired = true,
                Description = "SendGrid template ID to use for the email"
            }.Configure()
                .NotEmpty()
                .Build())
            .AddMessageProperty(new MessagePropertyConfiguration("TemplateData", ParameterType.String)
            {
                IsRequired = false,
                Description = "JSON object containing template variable substitutions"
            }.Configure()
                .WithCustomValidator(ValidateJsonContent)
                .Build());

        /// <summary>
        /// Gets a bulk email schema optimized for high-volume email campaigns.
        /// This schema includes batch processing and advanced delivery options.
        /// </summary>
        public static ChannelSchema BulkEmail => new ChannelSchema(SendGridEmail, "SendGrid Bulk Email")
            .UpdateParameter("TrackingSettings", param => param.DefaultValue = true)
            .AddMessageProperty(new MessagePropertyConfiguration("MailBatchId", ParameterType.String)
            {
                IsRequired = false,
                Description = "Mail batch ID for bulk operations and tracking"
            })
            .AddMessageProperty(new MessagePropertyConfiguration("UnsubscribeGroupId", ParameterType.Integer)
            {
                IsRequired = false,
                Description = "Unsubscribe group ID for bulk email compliance"
            }.Configure()
                .WithMinValue(1)
                .Build());

        /// <summary>
        /// Validates that categories property contains at most 10 categories with max 255 chars each.
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateCategories(object? value)
        {
            if (value == null) yield break;

            var categories = value.ToString();
            if (string.IsNullOrEmpty(categories)) yield break;

            var categoryList = categories.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            if (categoryList.Length > 10)
            {
                yield return new ValidationResult(
                    "Cannot specify more than 10 categories",
                    new[] { "Categories" });
            }

            foreach (var category in categoryList)
            {
                if (category.Trim().Length > 255)
                {
                    yield return new ValidationResult(
                        "Category name cannot exceed 255 characters",
                        new[] { "Categories" });
                    break;
                }
            }
        }

        /// <summary>
        /// Validates that the property contains valid JSON content.
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateJsonContent(object? value)
        {
            if (value == null) 
                return Enumerable.Empty<ValidationResult>();

            var jsonContent = value.ToString();
            if (string.IsNullOrEmpty(jsonContent)) 
                return Enumerable.Empty<ValidationResult>();

            try
            {
                System.Text.Json.JsonDocument.Parse(jsonContent);
                return Enumerable.Empty<ValidationResult>();
            }
            catch (System.Text.Json.JsonException)
            {
                return new[]
                {
                    new ValidationResult(
                        "CustomArgs must be valid JSON",
                        new[] { "CustomArgs" })
                };
            }
        }

        /// <summary>
        /// Validates that SendAt time is in the future and within SendGrid's 72-hour limit.
        /// Handles both DateTime and string values.
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateSendAtTime(object? value)
        {
            if (value == null) 
                yield break;

            DateTime sendAt;
            
            // Handle DateTime objects directly (don't treat them as type mismatches)
            if (value is DateTime dateTime)
            {
                sendAt = dateTime;
            }
            else if (value is string dateString)
            {
                if (!DateTime.TryParse(dateString, out sendAt))
                {
                    yield return new ValidationResult(
                        "SendAt must be a valid date and time",
                        new[] { "SendAt" });
                    yield break;
                }
            }
            else
            {
                yield return new ValidationResult(
                    "SendAt must be a valid date and time or DateTime object",
                    new[] { "SendAt" });
                yield break;
            }

            if (sendAt <= DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "SendAt must be a future date and time",
                    new[] { "SendAt" });
            }
            else if (sendAt > DateTime.UtcNow.AddDays(72)) // SendGrid limit
            {
                yield return new ValidationResult(
                    "SendAt cannot be more than 72 hours in the future",
                    new[] { "SendAt" });
            }
        }
    }
}