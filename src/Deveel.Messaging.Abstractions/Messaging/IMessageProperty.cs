namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a property of a message, which can be 
	/// used to store additional metadata or information.
	/// </summary>
	public interface IMessageProperty
	{
		/// <summary>
		/// Gets the name associated with the current instance.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the current value of the property.
		/// </summary>
		object? Value { get; }

		/// <summary>
		/// Gets a value indicating whether the data is 
		/// considered sensitive.
		/// </summary>
		/// <remarks>
		/// A sensitive property typically contains information
		/// that should be handled with care, such as personal
		/// identification numbers or financial data.
		/// </remarks>
		bool IsSensitive { get; }
	}
}
