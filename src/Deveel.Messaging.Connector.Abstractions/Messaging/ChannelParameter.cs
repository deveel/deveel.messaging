//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text.Json.Serialization;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a parameter used to describe a channel specification, 
	/// including its name, data type, and additional metadata.
	/// </summary>
	/// <remarks>
	/// This class is used to define the parameters required by a channel, 
	/// specifying details such as whether the parameter is required, sensitive, 
	/// or has a default value. 
	/// It also allows for defining a set of allowed values for the parameter.
	/// </remarks>
	public sealed class ChannelParameter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelParameter"/> class with 
		/// the specified name and data type.
		/// </summary>
		/// <param name="name">
		/// The name of the connector parameter. This value cannot be null or empty.
		/// </param>
		/// <param name="dataType">
		/// The data type of the connector parameter, represented by the 
		/// <see cref="ParameterType"/> enumeration.
		/// </param>
		[JsonConstructor]
		public ChannelParameter(string name, ParameterType dataType)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

			Name = name;
			DataType = dataType;
		}

		/// <summary>
		/// Gets the name of the connector parameter.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets or sets the display name of the entity.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets the type of the parameter.
		/// </summary>
		public ParameterType DataType { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the 
		/// field is required.
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the 
		/// data is considered sensitive.
		/// </summary>
		public bool IsSensitive { get; set; }

		/// <summary>
		/// Gets or sets a description of the parameter
		/// scope and usage.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the default value for the property,
		/// used when the parameter is not provided by the user.
		/// </summary>
		public object? DefaultValue { get; set; }

		/// <summary>
		/// Gets or sets a set of allowed values for the parameter.
		/// </summary>
		public object[]? AllowedValues { get; set; }
	}
}
