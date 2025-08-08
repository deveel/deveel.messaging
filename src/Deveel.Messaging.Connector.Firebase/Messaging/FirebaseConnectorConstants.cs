//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
    /// <summary>
    /// Provides constants used throughout the Firebase connector implementation.
    /// </summary>
    public static class FirebaseConnectorConstants
    {
        /// <summary>
        /// The provider identifier for Firebase connectors.
        /// </summary>
        public const string Provider = "Firebase";

        /// <summary>
        /// The channel type identifier for Firebase Cloud Messaging (FCM) push notifications.
        /// </summary>
        public const string PushChannel = "FCM";

        /// <summary>
        /// The default Firebase project URL template.
        /// </summary>
        public const string DefaultProjectUrlTemplate = "https://fcm.googleapis.com/v1/projects/{0}/messages:send";

        /// <summary>
        /// The Firebase Admin SDK service account key file parameter name.
        /// </summary>
        public const string ServiceAccountKeyParameter = "ServiceAccountKey";

        /// <summary>
        /// The Firebase project ID parameter name.
        /// </summary>
        public const string ProjectIdParameter = "ProjectId";

        /// <summary>
        /// The maximum number of tokens that can be sent in a single multicast message.
        /// </summary>
        public const int MaxMulticastTokens = 1000;

        /// <summary>
        /// The maximum size of the notification payload in bytes.
        /// </summary>
        public const int MaxPayloadSize = 4096;

        /// <summary>
        /// The maximum length of notification title.
        /// </summary>
        public const int MaxTitleLength = 256;

        /// <summary>
        /// The maximum length of notification body.
        /// </summary>
        public const int MaxBodyLength = 4000;
    }
}