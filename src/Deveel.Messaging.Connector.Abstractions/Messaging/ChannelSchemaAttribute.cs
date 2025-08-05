//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Specifies the reference schema for a channel connector implementation.
	/// </summary>
	/// <remarks>
	/// This attribute is used to decorate channel connector classes to define their 
	/// reference schema.
	/// The schema instance referenced by this attribute serves as the 
	/// authoritative definition of the connector's capabilities, parameters, and constraints.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public sealed class ChannelSchemaAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelSchemaAttribute"/> class
		/// with the specified schema factory type.
		/// </summary>
		/// <param name="schemaType">
		/// The type that implements <see cref="IChannelSchema"/> or <see cref="IChannelSchemaFactory"/> and provides the schema.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="schemaType"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="schemaType"/> does not implement <see cref="IChannelSchemaFactory"/> or
		/// <see cref="IChannelSchema"/> contracts.</exception>
		public ChannelSchemaAttribute(Type schemaType)
		{
			ArgumentNullException.ThrowIfNull(schemaType, nameof(schemaType));

			if (!typeof(IChannelSchema).IsAssignableFrom(schemaType) &&
				!typeof(IChannelSchemaFactory).IsAssignableFrom(schemaType))
			{
				throw new ArgumentException($"Type '{schemaType.Name}' must be a {nameof(IChannelSchema)} or implement {nameof(IChannelSchemaFactory)}.", nameof(schemaType));
			}

			SchemaType = schemaType;
		}

		/// <summary>
		/// Gets the type that implements a <see cref="IChannelSchema"/> or <see cref="IChannelSchemaFactory"/> 
		/// and provides the reference schema of a connector.
		/// </summary>
		public Type SchemaType { get; }
	}
}