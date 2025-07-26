//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents an error that occurs during message validation, 
	/// including an error code, an optional error message, and
	/// a collection of validation results.
	/// </summary>
	/// <remarks>
	/// This object is used to encapsulate details about validation errors 
	/// encountered when processing messages. 
	/// </remarks>
	public readonly struct MessageValidationError : IMessageValidationError
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageValidationError"/> class 
		/// with the specified error code, error message, and validation results.
		/// </summary>
		/// <param name="errorCode">The code representing the specific validation error.</param>
		/// <param name="errorMessage">The message describing the validation error.</param>
		/// <param name="validationResults">A read-only list of <see cref="ValidationResult"/> objects 
		/// detailing the results of the validation process.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="errorCode"/> is null or whitespace, or when
		/// the <paramref name="validationResults"/> is null.
		/// </exception>
		public MessageValidationError(string errorCode, string? errorMessage, IReadOnlyList<ValidationResult> validationResults)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
			ArgumentNullException.ThrowIfNull(validationResults, nameof(validationResults));

			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
			ValidationResults = validationResults;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageValidationError"/> class with the 
		/// specified error code and validation results.
		/// </summary>
		/// <param name="errorCode">
		/// The error code associated with the validation error.
		/// </param>
		/// <param name="validationResults">
		/// A read-only list of <see cref="ValidationResult"/> objects that detail the validation errors.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="errorCode"/> is null or whitespace, or when
		/// the <paramref name="validationResults"/> is null.
		/// </exception>
		public MessageValidationError(string errorCode, IReadOnlyList<ValidationResult> validationResults)
			: this(errorCode, null, validationResults)
		{
		}

		/// <summary>
		/// Gets the collection of validation results.
		/// </summary>
		public IReadOnlyList<ValidationResult> ValidationResults { get; }

		/// <summary>
		/// Gets the error code associated with the validation error.
		/// </summary>
		public string ErrorCode { get; }

		/// <summary>
		/// Gets the error message associated with the current operation.
		/// </summary>
		public string? ErrorMessage { get; }
	}
}
