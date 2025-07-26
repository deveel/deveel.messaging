//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// Describes an error that is associated to a message.
	/// </summary>
	public interface IMessageError {
		/// <summary>
		/// Gets the error code that is assigned
		/// by the messaging system.
		/// </summary>
		string Code { get; }

		/// <summary>
		/// Gets a descriptive message of the error.
		/// </summary>
		string? Message { get; }

		/// <summary>
		/// Gets an optional error that is the cause
		/// of this error.
		/// </summary>
		/// <remarks>
		/// In a messaging system, this type of error is
		/// typically the error that is received from a remote
		/// message channel.
		/// </remarks>
		IMessageError? InnerError { get; }
	}
}
