//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json.Serialization;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents an error encountered by a connector, including 
	/// an error code and an optional message.
	/// </summary>
	/// <remarks>
	/// This struct is used to encapsulate error information that 
	/// can be returned by connector operations.
	/// It includes a mandatory error code and an optional message 
	/// providing additional details about the error.
	/// </remarks>
	public readonly struct MessagingError : IMessagingError
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessagingError"/> 
		/// class with the specified error code and an optional
		/// message.
		/// </summary>
		/// <param name="code">
		/// The error code that identifies the type of error. This parameter 
		/// cannot be null or empty.
		/// </param>
		/// <param name="message">
		/// An optional message that provides additional details about the error.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="code"/> is null or empty.
		/// </exception>
		[JsonConstructor]
		public MessagingError(string code, string? message = null) : this()
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(code, nameof(code));
			ErrorCode = code;
			ErrorMessage = message;
		}

		/// <summary>
		/// Gets the error code that identifies the type of error
		/// on the provider.
		/// </summary>
		public string ErrorCode { get; }

		/// <summary>
		/// Gets the message associated with the current operation 
		/// or event.
		/// </summary>
		public string? ErrorMessage { get; }
	}
}
