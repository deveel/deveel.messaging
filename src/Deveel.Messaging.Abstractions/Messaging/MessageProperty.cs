namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a property of a message, which can be used to
	/// ransfer additional information along with the message.
	/// </summary>
	public class MessageProperty : IMessageProperty
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageProperty"/> class.
		/// </summary>
		public MessageProperty()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageProperty"/> class
		/// with the specified name and value.
		/// </summary>
		public MessageProperty(string name, object? value)
		{
			Name = name;
			Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageProperty"/> class using
		/// the specified message property.
		/// </summary>
		/// <param name="property">
		/// The message property from which to copy the name, value, 
		/// and sensitivity status.
		/// </param>
		public MessageProperty(IMessageProperty property)
		{
			Name = property?.Name ?? "";
			Value = property?.Value;
			IsSensitive = property?.IsSensitive ?? false;
		}

		/// <inheritdoc/>
		public string Name { get; set; } = string.Empty;

		/// <inheritdoc/>
		public object? Value { get; set; }

		/// <inheritdoc/>
		public bool IsSensitive { get; set; }

		/// <summary>
		/// Creates a new <see cref="MessageProperty"/> instance with 
		/// the specified name and value, marking it as sensitive.
		/// </summary>
		/// <param name="name">
		/// The name of the message property.
		/// </param>
		/// <param name="value">
		/// The value of the message property.
		/// </param>
		/// <returns>
		/// A <see cref="MessageProperty"/> object with the specified 
		/// name and value, marked as sensitive.
		/// </returns>
		public static MessageProperty Sensitive(string name, object? value)
		{
			return new MessageProperty(name, value) {
				IsSensitive = true
			};
		}
	}
}
