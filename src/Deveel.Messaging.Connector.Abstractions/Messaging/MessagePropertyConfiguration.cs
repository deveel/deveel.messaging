//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides the configuration settings for a property contained within a message.
	/// </summary>
	public sealed class MessagePropertyConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessagePropertyConfiguration"/> class 
		/// with the specified property name.
		/// </summary>
		/// <param name="name">The name of the message property.</param>
		/// <param name="dataType">The type of data for the message property.</param>
		public MessagePropertyConfiguration(string name, ParameterType dataType)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
			Name = name;
			DataType = dataType;
		}

		/// <summary>
		/// Gets the of the message property to which this 
		/// configuration applies.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		public ParameterType DataType { get; }

		/// <summary>
		/// Gets or sets the display name of the property.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of the property, which 
		/// provides additional context or information.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets whether the property is required.
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the data 
		/// is considered sensitive.
		/// </summary>
		/// <remarks>
		/// A sensitive property typically contains information
		/// that should be handled with care, such as personal
		/// identification numbers or financial data.
		/// </remarks>
		public bool IsSensitive { get; set; }
	}
}
