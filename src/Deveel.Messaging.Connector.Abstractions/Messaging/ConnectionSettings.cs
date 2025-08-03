//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Globalization;

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents the settings for establishing a connection, including 
	/// parameters and an optional early schema validation.
	/// </summary>
	/// <remarks>
	/// This class provides mechanisms to manage connection parameters, 
	/// optionally validating them against a schema. 
	/// It supports setting and retrieving parameters by key, with 
	/// type-safe retrieval methods.
	/// </remarks>
	public class ConnectionSettings
	{
		private readonly IDictionary<string, object?> _parameters;
		private readonly IChannelSchema? _schema;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionSettings"/> class 
		/// with the specified target channel schema and a set of initial parameters.
		/// </summary>
		/// <param name="schema">
		/// The channel schema to be used for the connection. When this value is not provided
		/// the set of parameters is not triggering any validation.</param>
		/// <param name="parameters">
		/// A dictionary containing key-value pairs of parameters used for seeding the 
		/// connection settings.</param>
		public ConnectionSettings(IChannelSchema? schema, IDictionary<string, object?>? parameters)
		{
			_schema = schema;
			_parameters = parameters == null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(parameters);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionSettings"/> class 
		/// by copying the schema and parameters from an existing instance.
		/// </summary>
		/// <param name="settings">
		/// The existing <see cref="ConnectionSettings"/> instance from which to copy 
		/// the schema and parameters. Cannot be <see langword="null"/>.</param>
		public ConnectionSettings(ConnectionSettings settings)
			: this(settings._schema, settings._parameters)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionSettings"/> class 
		/// with a set of initial parameters.
		/// </summary>
		/// <param name="parameters">
		/// A dictionary containing configuration parameters for the connection. The keys 
		/// represent parameter names, and the values represent parameter values. 
		/// This can be <see langword="null"/> if no parameters are specified.</param>
		public ConnectionSettings(IDictionary<string, object?>? parameters)
			:this(null, parameters)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionSettings"/> class with 
		/// the specified channel schema used to validate the parameters.
		/// </summary>
		/// <param name="schema">
		/// The schema that defines the channel configuration. Can be <see langword="null"/> 
		/// if no specific schema is required.
		/// </param>
		public ConnectionSettings(IChannelSchema? schema)
			: this(schema, new Dictionary<string, object?>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionSettings"/> class with default settings.
		/// </summary>
		public ConnectionSettings()
			: this(new Dictionary<string, object?>())
		{
		}

		/// <summary>
		/// Gets a read-only dictionary of parameters.
		/// </summary>
		public IReadOnlyDictionary<string, object?> Parameters => _parameters.AsReadOnly();

		/// <summary>
		/// Gets or sets the parameter value associated with the specified key.
		/// </summary>
		/// <param name="key">
		/// The key of the parameter to get or set. Cannot be null or empty.
		/// </param>
		/// <returns>
		/// Returns the value of the parameter associated with the specified key,
		/// or <see langword="null"/> if the key does not exist.
		/// </returns>
		public object? this[string key]
		{
			get => GetParameter(key);
			set => SetParameter(key, value);
		}

		private ChannelParameter? FindParameter(string key)
		{
			if (_schema == null)
				return null;

			return _schema.Parameters.FirstOrDefault(x => String.Equals(x.Name, key, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Sets the specified parameter key to the given value in 
		/// the connection settings.
		/// </summary>
		/// <param name="key">The key of the parameter to set. Cannot be null or empty.</param>
		/// <param name="value">The value to associate with the specified key. Can be null.</param>
		/// <returns>
		/// Returns the current instance of <see cref="ConnectionSettings"/> to allow method chaining.
		/// </returns>
		public ConnectionSettings SetParameter(string key, object? value)
		{
			ValidateParameter(key, value);

			_parameters[key] = value;
			return this;
		}

		/// <summary>
		/// Retrieves the value associated with the specified key from 
		/// the parameter collection.
		/// </summary>
		/// <remarks>
		/// If the key is not found in the parameter collection and 
		/// a schema is defined, the method attempts to retrieve a 
		/// default value from the schema.
		/// </remarks>
		/// <param name="key">The key of the parameter to retrieve. 
		/// Cannot be null.</param>
		/// <returns>The value associated with the specified key if found; 
		/// otherwise, the default value from the schema if available; 
		/// otherwise, <see langword="null"/>.
		/// </returns>
		public object? GetParameter(string key)
		{
			if (_parameters.TryGetValue(key, out var value))
				return value;

			if (_schema != null)
			{
				var schemaParam = FindParameter(key);

				if (schemaParam != null && schemaParam.DefaultValue != null)
					value = schemaParam.DefaultValue;
			}

			return value;
		}

		/// <summary>
		/// Retrieves a parameter value associated with the specified key 
		/// and attempts to cast it to the specified type.
		/// </summary>
		/// <remarks>
		/// If the parameter value is not found and a schema is available, 
		/// the method attempts to find the parameter in the schema.
		/// </remarks>
		/// <typeparam name="T">The type to which the parameter value 
		/// should be cast.</typeparam>
		/// <param name="key">The key associated with the parameter to retrieve.
		/// Cannot be null or empty.</param>
		/// <returns>
		/// Returns the parameter value cast to the specified type <typeparamref name="T"/>
		/// .</returns>
		/// <exception cref="InvalidCastException">Thrown if the value associated with the specified key cannot be cast to the type <typeparamref name="T"/>.</exception>
		public T GetParameter<T>(string key)
		{
			var value = GetParameter(key);
			if (value == null && _schema != null)
			{
				var schemaParam = FindParameter(key);
			}

			if (value is T tValue)
				return tValue;

			throw new InvalidCastException($"The value for the key '{key}' cannot be cast to type '{typeof(T)}'.");
		}

		private void ValidateParameter(string key, object? value)
		{
			if (_schema == null)
				return;

			var schemaParam = FindParameter(key);
			if (schemaParam == null)
				throw new ArgumentException($"The parameter {key} is not supported by this schema");

			if (value == null && schemaParam.IsRequired)
				throw new ArgumentException($"The value of parameter {key} is required by this schema");

			if (!IsTypeCompatible(schemaParam.DataType, value))
				throw new ArgumentException($"The value provided foe the key '{key}' is not compatible with the type '{schemaParam.DataType}'");

			if (schemaParam.AllowedValues?.Any() ?? false)
			{
				if (!schemaParam.AllowedValues.Any(x => Equals(x, value)))
					throw new ArgumentException($"The value {value} is not allowed for the parameter {key}.");
			}
		}

		private bool IsTypeCompatible(DataType dataType, object? value)
		{
			return dataType switch
			{
				// TODO: support the case of strings "enabled" / "disabled"
				DataType.Boolean => value is bool,
				DataType.String => value is string,
				DataType.Integer => value is int || value is long || value is byte,
				DataType.Number => value is double || value is decimal || value is float,
				_ => false,
			};
		}

		private T ConvertTo<T>(DataType sourceType, object? value)
		{
			if (value is T tValue)
				return tValue;
			if (value == null)
				return default!;

			if (!CanConvertTo(sourceType, typeof(T)))
				throw new InvalidCastException($"Cannot convert value '{value}' from type '{sourceType}' to type '{typeof(T)}'.");

			try
			{
				return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
			}
			catch (InvalidCastException)
			{
				throw new InvalidCastException($"Cannot convert value '{value}' to type '{typeof(T)}'.");
			}
		}

		private bool CanConvertTo(DataType sourceType, Type destType)
		{
			return sourceType switch
			{
				DataType.Boolean => destType == typeof(bool) || destType == typeof(string),
				DataType.String => true, // optimistically assume any type can be converted to string
				DataType.Integer => destType == typeof(int) || destType == typeof(long) || destType == typeof(byte) ||
										 destType == typeof(short) || destType == typeof(sbyte) || destType == typeof(string),
				DataType.Number => destType == typeof(double) || destType == typeof(decimal) || destType == typeof(float) ||
										 destType == typeof(int) || destType == typeof(long) || destType == typeof(byte) ||
										 destType == typeof(short) || destType == typeof(sbyte) || destType == typeof(string),
				_ => false,
			};
		}
	}
}
