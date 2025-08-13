//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides pre-configured channel schemas for Firebase Cloud Messaging (FCM) services.
    /// </summary>
    public static class FirebaseChannelSchemas
    {
        /// <summary>
        /// Gets the comprehensive base schema for Firebase Cloud Messaging that supports
        /// all available capabilities and configurations.
        /// </summary>
        /// <remarks>
        /// This schema includes all Firebase FCM capabilities including sending push notifications,
        /// status queries, and health monitoring. It can be used as-is or derived to create
        /// more restrictive configurations for specific use cases.
        /// </remarks>
        public static ChannelSchema FirebasePush => new ChannelSchema(FirebaseConnectorConstants.Provider, FirebaseConnectorConstants.PushChannel, "1.0.0")
            .WithDisplayName("Firebase Cloud Messaging (FCM) Connector")
            .WithCapabilities(
                ChannelCapability.SendMessages | 
                ChannelCapability.BulkMessaging |
                ChannelCapability.HealthCheck)
            .AddParameter(new ChannelParameter("ProjectId", DataType.String)
            {
                IsRequired = true,
                Description = "Firebase project ID - found in your Firebase Console project settings"
            })
            .AddParameter(new ChannelParameter("ServiceAccountKey", DataType.String)
            {
                IsRequired = true,
                IsSensitive = true,
                Description = "Firebase service account key JSON - download from Firebase Console > Project Settings > Service Accounts"
            })
            .AddParameter(new ChannelParameter("DryRun", DataType.Boolean)
            {
                IsRequired = false,
                DefaultValue = false,
                Description = "Enable dry run mode for testing without actually sending push notifications"
            })
            .AddContentType(MessageContentType.Json)
            .AddContentType(MessageContentType.PlainText)
            .HandlesMessageEndpoint(EndpointType.DeviceId, e =>
            {
                e.CanSend = false; // Push notifications are send-only
				e.CanReceive = true; 
                e.IsRequired = true; // Device token is required for sending
            })
            .HandlesMessageEndpoint(EndpointType.Topic, e =>
            {
                e.CanSend = false; // Topic notifications are send-only
				e.CanReceive = true; 
                e.IsRequired = false; // Alternative to device tokens
            })
            .AddAuthenticationConfiguration(new AuthenticationConfiguration(AuthenticationType.Certificate, "Firebase Service Account Authentication")
                .WithRequiredField("ServiceAccountKey", DataType.String, field =>
                {
                    field.DisplayName = "Service Account Key";
                    field.Description = "Firebase service account key JSON or file path";
                    field.AuthenticationRole = "Certificate";
                    field.IsSensitive = true;
                })
                .WithOptionalField("ProjectId", DataType.String, field =>
                {
                    field.DisplayName = "Project ID";
                    field.Description = "Firebase project ID (can be extracted from service account key)";
                    field.AuthenticationRole = "ProjectId";
                }))
            .AddMessageProperty("Title", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Notification title";
                p.MaxLength = FirebaseConnectorConstants.MaxTitleLength;
            })
            // Body text now comes from TextContent instead of a message property
            .AddMessageProperty("ImageUrl", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "URL of an image to display in the notification";
                p.CustomValidator = ValidateImageUrl;
            })
            .AddMessageProperty("Sound", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Sound to play when the notification is received";
            })
            .AddMessageProperty("Badge", DataType.Integer, p =>
            {
                p.IsRequired = false;
                p.Description = "Badge count for iOS applications";
                p.MinValue = 0;
            })
            .AddMessageProperty("ClickAction", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Action to perform when the notification is clicked";
            })
            .AddMessageProperty("Color", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Notification color in #rrggbb format for Android";
                p.CustomValidator = ValidateHexColor;
            })
            .AddMessageProperty("Tag", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Notification tag for Android grouping";
            })
            .AddMessageProperty("Priority", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Message priority (normal, high)";
                p.AllowedValues = new[] { "normal", "high" };
            })
            .AddMessageProperty("TimeToLive", DataType.Integer, p =>
            {
                p.IsRequired = false;
                p.Description = "Time to live in seconds (0 to 2,419,200 seconds - 4 weeks)";
                p.MinValue = 0;
                p.MaxValue = 2419200; // 4 weeks
            })
            .AddMessageProperty("CollapseKey", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Collapse key for message grouping";
            })
            .AddMessageProperty("RestrictedPackageName", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Package name of the Android app to restrict delivery to";
            })
            .AddMessageProperty("MutableContent", DataType.Boolean, p =>
            {
                p.IsRequired = false;
                p.Description = "Enable mutable content for iOS notification service extensions";
            })
            .AddMessageProperty("ContentAvailable", DataType.Boolean, p =>
            {
                p.IsRequired = false;
                p.Description = "Enable content-available for iOS background app refresh";
            })
            .AddMessageProperty("ThreadId", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Thread ID for iOS notification grouping";
            })
            .AddMessageProperty("CustomData", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Custom data payload as JSON string";
                p.CustomValidator = ValidateJsonContent;
            });

        /// <summary>
        /// Gets a simplified push notification schema for basic messaging use cases.
        /// This schema removes advanced features and focuses on simple text notifications.
        /// </summary>
        public static ChannelSchema SimplePush => new ChannelSchema(FirebasePush, "Firebase Simple Push")
            .RemoveCapability(ChannelCapability.BulkMessaging)
            .RemoveParameter("DryRun")
            .RemoveMessageProperty("ImageUrl")
            .RemoveMessageProperty("Sound")
            .RemoveMessageProperty("Badge")
            .RemoveMessageProperty("ClickAction")
            .RemoveMessageProperty("Color")
            .RemoveMessageProperty("Tag")
            .RemoveMessageProperty("Priority")
            .RemoveMessageProperty("TimeToLive")
            .RemoveMessageProperty("CollapseKey")
            .RemoveMessageProperty("RestrictedPackageName")
            .RemoveMessageProperty("MutableContent")
            .RemoveMessageProperty("ContentAvailable")
            .RemoveMessageProperty("ThreadId")
            .RemoveMessageProperty("CustomData");

        /// <summary>
        /// Gets a bulk push notification schema optimized for high-volume campaigns.
        /// This schema includes all bulk messaging capabilities and advanced targeting options.
        /// </summary>
        public static ChannelSchema BulkPush => new ChannelSchema(FirebasePush, "Firebase Bulk Push")
            .AddMessageProperty("ConditionExpression", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "FCM condition expression for advanced targeting";
                p.CustomValidator = ValidateConditionExpression;
            })
            .AddMessageProperty("BatchId", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "Batch identifier for grouping related messages";
            });

        /// <summary>
        /// Gets a rich push notification schema optimized for interactive and media-rich notifications.
        /// This schema includes advanced notification features and customization options.
        /// </summary>
        public static ChannelSchema RichPush => new ChannelSchema(FirebasePush, "Firebase Rich Push")
            .RemoveCapability(ChannelCapability.BulkMessaging)
            .AddMessageProperty("Actions", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "JSON array of notification actions/buttons";
                p.CustomValidator = ValidateActionsJson;
            })
            .AddMessageProperty("Category", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "iOS notification category for action buttons";
            })
            .AddMessageProperty("Subtitle", DataType.String, p =>
            {
                p.IsRequired = false;
                p.Description = "iOS notification subtitle";
                p.MaxLength = 256;
            });

        /// <summary>
        /// Validates that the image URL is a valid HTTP/HTTPS URL.
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateImageUrl(object? value)
        {
            if (value == null) yield break;

            var url = value.ToString();
            if (string.IsNullOrEmpty(url)) yield break;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                yield return new ValidationResult(
                    "ImageUrl must be a valid HTTP or HTTPS URL",
                    new[] { "ImageUrl" });
            }
        }

        /// <summary>
        /// Validates that the color is in valid hexadecimal format (#rrggbb or #aarrggbb).
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateHexColor(object? value)
        {
            if (value == null) yield break;

            var color = value.ToString();
            if (string.IsNullOrEmpty(color)) yield break;

            if (!color.StartsWith('#') || 
                (color.Length != 7 && color.Length != 9) ||
                !color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                yield return new ValidationResult(
                    "Color must be in hexadecimal format (#rrggbb or #aarrggbb)",
                    new[] { "Color" });
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
                        "CustomData must be valid JSON",
                        new[] { "CustomData" })
                };
            }
        }

        /// <summary>
        /// Validates FCM condition expressions for topic-based targeting.
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateConditionExpression(object? value)
        {
            if (value == null) yield break;

            var condition = value.ToString();
            if (string.IsNullOrEmpty(condition)) yield break;

            // Basic validation for FCM condition syntax
            // More comprehensive validation would require FCM SDK parsing
            if (condition.Length > 1000)
            {
                yield return new ValidationResult(
                    "Condition expression cannot exceed 1000 characters",
                    new[] { "ConditionExpression" });
            }

            // Check for valid operators and basic syntax
            var validOperators = new[] { "&&", "||", "(", ")", "'" };
            var containsValidSyntax = condition.Contains("'") || 
                                    validOperators.Any(op => condition.Contains(op));

            if (!containsValidSyntax)
            {
                yield return new ValidationResult(
                    "Condition expression must contain valid FCM syntax with topic names in single quotes",
                    new[] { "ConditionExpression" });
            }
        }

        /// <summary>
        /// Validates that the actions property contains valid JSON array for notification actions.
        /// </summary>
        private static IEnumerable<ValidationResult> ValidateActionsJson(object? value)
        {
            if (value == null) 
                return Enumerable.Empty<ValidationResult>();

            var jsonContent = value.ToString();
            if (string.IsNullOrEmpty(jsonContent)) 
                return Enumerable.Empty<ValidationResult>();

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(jsonContent);
                
                if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    return new[]
                    {
                        new ValidationResult(
                            "Actions must be a JSON array",
                            new[] { "Actions" })
                    };
                }

                // Validate each action has required properties
                foreach (var action in document.RootElement.EnumerateArray())
                {
                    if (!action.TryGetProperty("action", out _))
                    {
                        return new[]
                        {
                            new ValidationResult(
                                "Each action must have an 'action' property",
                                new[] { "Actions" })
                        };
                    }
                }

                return Enumerable.Empty<ValidationResult>();
            }
            catch (System.Text.Json.JsonException)
            {
                return new[]
                {
                    new ValidationResult(
                        "Actions must be valid JSON",
                        new[] { "Actions" })
                };
            }
        }
    }
}