//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents an error that occurs during message validation, 
	/// providing details about the validation failures.
	/// </summary>
	/// <remarks>
	/// This interface extends <see cref="IMessagingError"/> to include 
	/// validation-specific error information.
	/// </remarks>
	public interface IMessageValidationError : IMessagingError
	{
		/// <summary>
		/// Gets a read-only list of validation results.
		/// </summary>
		IReadOnlyList<ValidationResult> ValidationResults { get; }
	}
}
