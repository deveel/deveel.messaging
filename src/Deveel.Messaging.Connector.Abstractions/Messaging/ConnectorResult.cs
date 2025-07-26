//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Describes the result of an operation performed by a connector,
	/// and encapsulates the outcome, any resulting value,
	/// error information, and additional provider data.
	/// </summary>
	/// <typeparam name="TValue">
	/// The type of the value returned by the connector operation.
	/// </typeparam>
	public readonly struct ConnectorResult<TValue>
	{
		private ConnectorResult(bool successful, TValue? value, IMessagingError? error, IDictionary<string, object>? providerData)
		{
			Successful = successful;
			Value = value;
			Error = error;
			ProviderData = providerData;
		}

		/// <summary>
		/// Gets a value indicating whether the operation was successful.
		/// </summary>
		public bool Successful { get; }

		/// <summary>
		/// Gets the value associated with the current instance.
		/// </summary>
		public TValue? Value { get; }

		/// <summary>
		/// Gets the error information associated with the 
		/// connector operation, if any.
		/// </summary>
		public IMessagingError? Error { get; }

		/// <summary>
		/// Gets the provider-specific data associated with the 
		/// current instance of the connector result.
		/// </summary>
		public IDictionary<string, object>? ProviderData { get; }

		/// <summary>
		/// Implicitly converts a value of type <typeparamref name="TValue"/> to 
		/// a <see cref="ConnectorResult{TValue}"/>.
		/// </summary>
		/// <param name="value">The value to be converted into a <see cref="ConnectorResult{TValue}"/>.</param>
		/// <remarks>
		/// This conversion assumes that the operation was successful,
		/// </remarks>
		public static implicit operator ConnectorResult<TValue>(TValue value) => new(true, value, null, null);

		/// <summary>
		/// Creates a successful result with the specified value and 
		/// optional additional data.
		/// </summary>
		/// <param name="value">The value associated with the successful result.</param>
		/// <param name="data">Optional. A dictionary containing additional data related to the result. Can be <see langword="null"/>.</param>
		/// <returns>A <see cref="ConnectorResult{TValue}"/> representing a successful operation with the specified value and
		/// additional data.</returns>
		public static ConnectorResult<TValue> Success(TValue value, IDictionary<string, object>? data = null)
			=> new ConnectorResult<TValue>(true, value, null, data);

		/// <summary>
		/// Creates a failed result with the specified error and optional additional data.
		/// </summary>
		/// <param name="error">The error that caused the failure. This parameter cannot be null.</param>
		/// <param name="data">Optional additional data associated with the failure. Can be null.</param>
		/// <returns>A <see cref="ConnectorResult{TValue}"/> representing a failed operation.</returns>
		public static ConnectorResult<TValue> Fail(IMessagingError error, IDictionary<string, object>? data = null)
			=> new ConnectorResult<TValue>(false, default, error, data);

		/// <summary>
		/// Creates a failed <see cref="ConnectorResult{TValue}"/> with the specified error code and optional error message
		/// and data.
		/// </summary>
		/// <param name="errorCode">The error code representing the failure reason. Cannot be null or empty.</param>
		/// <param name="errorMessage">An optional message providing additional details about the error. Can be null.</param>
		/// <param name="data">An optional dictionary containing additional data related to the error. Can be null.</param>
		/// <returns>A <see cref="ConnectorResult{TValue}"/> representing a failure, containing the specified error information.</returns>
		public static ConnectorResult<TValue> Fail(string errorCode, string? errorMessage = null, IDictionary<string, object>? data = null)
			=> Fail(new MessagingError(errorCode, errorMessage), data);

		/// <summary>
		/// Creates a <see cref="ConnectorResult{TValue}"/> that failed because of validation errors 
		/// on a message.
		/// </summary>
		/// <param name="errorCode">
		/// A code representing the validation error.
		/// </param>
		/// <param name="errorMessage">
		/// An optional message providing additional details about the validation error.
		/// </param>
		/// <param name="validationResults">
		/// A list of validation results associated with the error.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{TValue}"/> indicating that the validation failed,
		/// including the error code, message, and validation results.
		/// </returns>
		public static ConnectorResult<TValue> ValidationFailed(string errorCode, string? errorMessage = null, IEnumerable<ValidationResult>? validationResults = null)
			=> Fail(new MessageValidationError(errorCode, errorMessage, validationResults?.ToList() ?? new List<ValidationResult>()));

		/// <summary>
		/// Creates a <see cref="ConnectorResult{TValue}"/> that failed because of validation errors 
		/// on a message.
		/// </summary>
		/// <param name="errorCode">
		/// A code representing the validation error.
		/// </param>
		/// <param name="validationResults">
		/// A list of validation results associated with the error.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ConnectorResult{TValue}"/> indicating that the validation failed,
		/// including the error code, message, and validation results.
		/// </returns>
		public static ConnectorResult<TValue> ValidationFailed(string errorCode, IEnumerable<ValidationResult> validationResults)
			=> ValidationFailed(errorCode, null, validationResults);
	}
}
