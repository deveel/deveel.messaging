//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// An exception that is thrown when an error occurs
	/// in the messaging operations.
	/// </summary>
	public class MessagingException : Exception, IMessagingError
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessagingException"/> class.
		/// </summary>
		/// <param name="errorCode">
		/// The error code that identifies the type of error.
		/// </param>
		public MessagingException(string errorCode)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
			ErrorCode = errorCode;
		}

		/// <summary>
		/// Constructs a new instance of the <see cref="MessagingException"/> class
		/// with a specified error message.
		/// </summary>
		/// <param name="errorCode">
		/// The error code that identifies the type of error.
		/// </param>
		/// <param name="message">The message that describes the error.</param>
		public MessagingException(string errorCode, string? message) : base(message)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
			ErrorCode = errorCode;
		}

		/// <summary>
		/// Constructs a new instance of the <see cref="MessagingException"/> class 
		/// with a specified error message and a reference to the inner exception 
		/// that is the cause of this exception.
		/// </summary>
		/// <param name="errorCode">
		/// The error code that identifies the type of error.
		/// </param>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
		public MessagingException(string errorCode, string? message, Exception? innerException) 
			: base(message, innerException)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
			ErrorCode = errorCode;
		}

		/// <summary>
		/// Gets the error code associated with the current operation.
		/// </summary>
		public string ErrorCode { get; }

		string? IMessagingError.ErrorMessage => Message;
	}
}
