//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using RestSharp;

using System.Text.Json;

namespace Deveel.Messaging
{
    /// <summary>
    /// Implements Facebook Graph API operations for Messenger messaging using RestSharp.
    /// This implementation follows Facebook Graph API best practices for authentication and validation.
    /// </summary>
    public class FacebookService : IFacebookService
    {
        private readonly RestClient _restClient;
        private string? _pageAccessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookService"/> class.
        /// </summary>
        /// <param name="restClient">Optional REST client for dependency injection. If null, a new one will be created.</param>
        public FacebookService(RestClient? restClient = null)
        {
            _restClient = restClient ?? new RestClient(FacebookConnectorConstants.GraphApiBaseUrl);
        }

        /// <inheritdoc/>
        public void Initialize(string pageAccessToken)
        {
            if (string.IsNullOrWhiteSpace(pageAccessToken))
                throw new ArgumentNullException(nameof(pageAccessToken), "Page Access Token cannot be null or empty");

            // Validate Facebook Page Access Token format
            if (!IsValidPageAccessToken(pageAccessToken))
                throw new ArgumentException("Invalid Page Access Token format", nameof(pageAccessToken));

            _pageAccessToken = pageAccessToken;
        }

        /// <inheritdoc/>
        public async Task<FacebookPageInfo?> FetchPageAsync(string pageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_pageAccessToken))
                throw new InvalidOperationException("Facebook service has not been initialized with an access token.");

            if (string.IsNullOrWhiteSpace(pageId))
                throw new ArgumentException("Page ID cannot be null or empty", nameof(pageId));

            try
            {
                var request = new RestRequest($"/{FacebookConnectorConstants.GraphApiVersion}/{pageId}", Method.Get);
                
                // Facebook Graph API requires specific parameters for page information
                request.AddParameter("fields", "id,name,category,access_token");
                request.AddParameter("access_token", _pageAccessToken);

                var response = await _restClient.ExecuteAsync(request, cancellationToken);

                if (!response.IsSuccessful)
                {
                    var errorDetails = ParseFacebookError(response);
                    throw new InvalidOperationException($"Facebook Graph API error: {errorDetails}");
                }

                if (string.IsNullOrEmpty(response.Content))
                    return null;

                var pageData = JsonSerializer.Deserialize<JsonElement>(response.Content);
                
                return new FacebookPageInfo
                {
                    Id = GetJsonStringProperty(pageData, "id") ?? pageId,
                    Name = GetJsonStringProperty(pageData, "name") ?? "",
                    Category = GetJsonStringProperty(pageData, "category") ?? ""
                };
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Error fetching Facebook page: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<FacebookMessageResponse> SendMessageAsync(FacebookMessageRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_pageAccessToken))
                throw new InvalidOperationException("Facebook service has not been initialized with an access token.");

            ArgumentNullException.ThrowIfNull(request, nameof(request));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(request.Recipient, nameof(request.Recipient));
            ArgumentNullException.ThrowIfNull(request.Message, nameof(request.Message));

            // Validate Facebook Messenger Platform requirements
            ValidateMessageRequest(request);

            try
            {
                var restRequest = new RestRequest($"/{FacebookConnectorConstants.GraphApiVersion}/me/messages", Method.Post);
                
                // Facebook Graph API authentication
                restRequest.AddParameter("access_token", _pageAccessToken);
                
                // Build message payload according to Facebook Messenger Platform API specification
                var messagePayload = BuildFacebookMessagePayload(request);
                restRequest.AddJsonBody(messagePayload);

                var response = await _restClient.ExecuteAsync(restRequest, cancellationToken);

                if (!response.IsSuccessful)
                {
                    var errorDetails = ParseFacebookError(response);
                    throw new InvalidOperationException($"Facebook Graph API error: {errorDetails}");
                }

                if (string.IsNullOrEmpty(response.Content))
                    throw new InvalidOperationException("Facebook API returned empty response");

                var responseData = JsonSerializer.Deserialize<JsonElement>(response.Content);
                
                return new FacebookMessageResponse
                {
                    MessageId = GetJsonStringProperty(responseData, "message_id") ?? "",
                    RecipientId = request.Recipient
                };
            }
            catch (Exception ex) when (!(ex is InvalidOperationException || ex is ArgumentException))
            {
                throw new InvalidOperationException($"Error sending Facebook message: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates Facebook Page Access Token format according to Facebook requirements.
        /// </summary>
        private static bool IsValidPageAccessToken(string token)
        {
            // Facebook Page Access Tokens typically start with specific patterns
            // and have minimum length requirements
            return !string.IsNullOrWhiteSpace(token) && 
                   token.Length >= 20 && 
                   !token.Contains(" ") &&
                   (token.StartsWith("EAA") || token.StartsWith("EAAG") || token.Contains("|"));
        }

        /// <summary>
        /// Validates message request according to Facebook Messenger Platform requirements.
        /// </summary>
        private static void ValidateMessageRequest(FacebookMessageRequest request)
        {
            // Facebook Messenger Platform validation requirements
            if (string.IsNullOrEmpty(request.Message.Text) && request.Message.Attachment == null)
                throw new ArgumentException("Message must contain either text or attachment");

            if (!string.IsNullOrEmpty(request.Message.Text) && request.Message.Text.Length > 2000)
                throw new ArgumentException("Message text cannot exceed 2000 characters (Facebook limit)");

            // Check quick replies from the message object
            if (request.Message.QuickReplies?.Count > 13)
                throw new ArgumentException("Maximum 13 quick replies allowed (Facebook limit)");

            // Also check from the request level (if it exists)
            if (request.QuickReplies?.Count > 13)
                throw new ArgumentException("Maximum 13 quick replies allowed (Facebook limit)");

            // Validate messaging type
            var validMessagingTypes = new[] { "RESPONSE", "UPDATE", "MESSAGE_TAG", "NON_PROMOTIONAL_SUBSCRIPTION" };
            if (!validMessagingTypes.Contains(request.MessagingType))
                throw new ArgumentException($"Invalid messaging type. Must be one of: {string.Join(", ", validMessagingTypes)}");

            // Validate notification type
            var validNotificationTypes = new[] { "REGULAR", "SILENT_PUSH", "NO_PUSH" };
            if (!string.IsNullOrEmpty(request.NotificationType) && !validNotificationTypes.Contains(request.NotificationType))
                throw new ArgumentException($"Invalid notification type. Must be one of: {string.Join(", ", validNotificationTypes)}");
        }

        /// <summary>
        /// Builds Facebook Messenger Platform message payload according to API specification.
        /// </summary>
        private static object BuildFacebookMessagePayload(FacebookMessageRequest request)
        {
            var payload = new Dictionary<string, object>
            {
                ["recipient"] = new { id = request.Recipient },
                ["messaging_type"] = request.MessagingType,
                ["message"] = BuildMessageContent(request.Message)
            };

            // Add optional parameters only if they have meaningful values
            if (!string.IsNullOrEmpty(request.NotificationType) && request.NotificationType != "REGULAR")
            {
                payload["notification_type"] = request.NotificationType;
            }

            if (!string.IsNullOrEmpty(request.Tag))
            {
                payload["tag"] = request.Tag;
            }

            return payload;
        }

        /// <summary>
        /// Builds message content according to Facebook Messenger Platform specification.
        /// </summary>
        private static object BuildMessageContent(FacebookMessage message)
        {
            var content = new Dictionary<string, object>();

            // Add text content
            if (!string.IsNullOrEmpty(message.Text))
            {
                content["text"] = message.Text;
            }

            // Add attachment
            if (message.Attachment != null)
            {
                content["attachment"] = new
                {
                    type = message.Attachment.Type,
                    payload = new
                    {
                        url = message.Attachment.Payload.Url,
                        is_reusable = message.Attachment.Payload.IsReusable
                    }
                };
            }

            // Add quick replies with Facebook specification compliance
            if (message.QuickReplies != null && message.QuickReplies.Count > 0)
            {
                content["quick_replies"] = message.QuickReplies.Select(qr => 
                {
                    var quickReply = new Dictionary<string, object>
                    {
                        ["content_type"] = qr.ContentType,
                        ["title"] = qr.Title,
                        ["payload"] = qr.Payload
                    };

                    if (!string.IsNullOrEmpty(qr.ImageUrl))
                    {
                        quickReply["image_url"] = qr.ImageUrl;
                    }

                    return quickReply;
                }).ToArray();
            }

            return content;
        }

        /// <summary>
        /// Parses Facebook API error response for detailed error information.
        /// </summary>
        private static string ParseFacebookError(RestResponse response)
        {
            try
            {
                if (string.IsNullOrEmpty(response.Content))
                    return $"HTTP {(int)response.StatusCode} {response.StatusCode}: {response.ErrorMessage}";

                var errorData = JsonSerializer.Deserialize<JsonElement>(response.Content);
                
                if (errorData.TryGetProperty("error", out var error))
                {
                    var message = GetJsonStringProperty(error, "message") ?? "Unknown error";
                    var code = GetJsonStringProperty(error, "code") ?? "";
                    var subcode = GetJsonStringProperty(error, "error_subcode") ?? "";
                    
                    return !string.IsNullOrEmpty(subcode) 
                        ? $"Code {code} (Subcode {subcode}): {message}"
                        : $"Code {code}: {message}";
                }

                return response.Content;
            }
            catch
            {
                return $"HTTP {(int)response.StatusCode} {response.StatusCode}: {response.ErrorMessage}";
            }
        }

        /// <summary>
        /// Safely extracts string property from JSON element.
        /// </summary>
        private static string? GetJsonStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
        }
    }
}