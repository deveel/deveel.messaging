//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents an exception that is thrown when a 
	/// message to be sent fails validation.
	/// </summary>
	/// <remarks>
	/// This exception provides details about the validation errors
	/// encountered during message processing.
	/// </remarks>
	public sealed class MessageValidationException : MessagingException, IMessageValidationError
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageValidationException"/> class 
		/// with a specified error code, error message, and a list of validation results.
		/// </summary>
		/// <param name="errorCode">The error code associated with the validation exception.</param>
		/// <param name="message">The error message that describes the validation error.</param>
		/// <param name="validationResults">
		/// A read-only list of <see cref="ValidationResult"/> objects that provide details about 
		/// the validation errors.
		/// </param>
		public MessageValidationException(string errorCode, string? message, IReadOnlyList<ValidationResult> validationResults)
			: base(errorCode, message)
		{
			ValidationResults = validationResults;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageValidationException"/> class with 
		/// a specified error code and a collection of validation results.
		/// </summary>
		/// <param name="errorCode">The error code that represents the validation error.</param>
		/// <param name="validationResults">A read-only list of <see cref="ValidationResult"/> objects that contain details about the validation errors.</param>
		public MessageValidationException(string errorCode, IReadOnlyList<ValidationResult> validationResults)
			: base(errorCode)
		{
			ValidationResults = validationResults;
		}

		/// <summary>
		/// Gets the collection of validation results.
		/// </summary>
		public IReadOnlyList<ValidationResult> ValidationResults { get; }
	}
}
