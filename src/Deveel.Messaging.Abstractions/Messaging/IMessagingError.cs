//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents an error that occurred in the context
	/// of messaging operations
	/// </summary>
	public interface IMessagingError
	{
		/// <summary>
		/// Gets the code that represents the error.
		/// </summary>
		string ErrorCode { get; }

		/// <summary>
		/// Gets a descriptive message that details the error.
		/// </summary>
		string? ErrorMessage { get; }
	}
}
