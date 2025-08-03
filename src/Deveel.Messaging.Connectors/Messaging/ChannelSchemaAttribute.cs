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
	/// master schema. The schema instance referenced by this attribute serves as the 
	/// authoritative definition of the connector's capabilities, parameters, and constraints.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public sealed class ChannelSchemaAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelSchemaAttribute"/> class
		/// with the specified schema factory type.
		/// </summary>
		/// <param name="schemaFactoryType">
		/// The type that implements <see cref="IChannelSchemaFactory"/> and provides the master schema.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown when schemaFactoryType is null.</exception>
		/// <exception cref="ArgumentException">Thrown when schemaFactoryType does not implement IChannelSchemaFactory.</exception>
		public ChannelSchemaAttribute(Type schemaFactoryType)
		{
			ArgumentNullException.ThrowIfNull(schemaFactoryType, nameof(schemaFactoryType));

			if (!typeof(IChannelSchemaFactory).IsAssignableFrom(schemaFactoryType))
			{
				throw new ArgumentException($"Type '{schemaFactoryType.Name}' must implement {nameof(IChannelSchemaFactory)}.", nameof(schemaFactoryType));
			}

			SchemaFactoryType = schemaFactoryType;
		}

		/// <summary>
		/// Gets the type that implements <see cref="IChannelSchemaFactory"/> and provides the master schema.
		/// </summary>
		public Type SchemaFactoryType { get; }

		/// <summary>
		/// Creates and returns the master schema instance for the associated connector.
		/// </summary>
		/// <returns>The master schema instance.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the schema factory cannot be instantiated or created.</exception>
		public IChannelSchema CreateSchema()
		{
			try
			{
				var factory = Activator.CreateInstance(SchemaFactoryType) as IChannelSchemaFactory;
				if (factory == null)
				{
					throw new InvalidOperationException($"Failed to create instance of schema factory '{SchemaFactoryType.Name}'.");
				}

				return factory.CreateSchema();
			}
			catch (Exception ex) when (!(ex is InvalidOperationException))
			{
				throw new InvalidOperationException($"Failed to create schema using factory '{SchemaFactoryType.Name}': {ex.Message}", ex);
			}
		}
	}

	/// <summary>
	/// Defines the contract for creating channel schemas.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface are used by the <see cref="ChannelSchemaAttribute"/>
	/// to provide master schemas for channel connectors.
	/// </remarks>
	public interface IChannelSchemaFactory
	{
		/// <summary>
		/// Creates and returns the master schema for a channel connector.
		/// </summary>
		/// <returns>The master schema instance.</returns>
		IChannelSchema CreateSchema();
	}
}