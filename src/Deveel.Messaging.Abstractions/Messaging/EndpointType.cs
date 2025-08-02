//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Specifies the type of endpoint used 
	/// in messaging contexts.
	/// </summary>
	/// <remarks>
	/// This enumeration is used to categorize different types of endpoints, 
	/// such as phone numbers, email addresses, URLs, and various identifiers.
	/// </remarks>
	public enum EndpointType
	{
		/// <summary>
		/// Represents a phone number with country code, area code, 
		/// and local number components.
		/// </summary>
		PhoneNumber,

		/// <summary>
		/// Represents an email address.
		/// </summary>
		EmailAddress,

		/// <summary>
		/// An URL endpoint, which can be used to identify 
		/// web resources or services.
		/// </summary>
		Url,

		/// <summary>
		/// The endpoint is a topic within a queue system
		/// </summary>
		Topic,

		/// <summary>
		/// The endpoint is a unique identifier (typically of the 
		/// endpoint itself) used within the messaging system.
		/// </summary>
		Id,

		/// <summary>
		/// A user identifier within a platform or system,
		/// </summary>
		UserId,

		/// <summary>
		/// The identifier for an application that can send or 
		/// receive messages.
		/// </summary>
		ApplicationId,

		/// <summary>
		/// A unique identifier for a device that can send or
		/// receive messages.
		/// </summary>
		DeviceId,

		/// <summary>
		/// An alpha-numeric label that can be used to send
		/// messages to a specific user or entity.
		/// </summary>
		Label,

		/// <summary>
		/// Any type of endpoint
		/// </summary>
		Any = 122
	}
}
