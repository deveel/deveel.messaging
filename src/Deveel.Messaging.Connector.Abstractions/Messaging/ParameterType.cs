//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Specifies the type of a parameter to a messaging channel
	/// or 
	/// for configuration of the messaging system.
	/// </summary>
	/// <remarks>
	/// This enumeration is used to define the expected data type 
	/// of a parameter, allowing for type-specific processing or 
	/// validation.
	/// </remarks>
	public enum ParameterType
	{
		/// <summary>
		/// Represents a Boolean (true or false) value.
		/// </summary>
		Boolean,

		/// <summary>
		/// Represents an integer value.
		/// </summary>
		Integer,

		/// <summary>
		/// Represents a numerical value and provides methods for 
		/// basic arithmetic operations.
		/// </summary>
		Number,

		/// <summary>
		/// Represents text as a series of Unicode characters.
		/// </summary>
		String,
	}
}
