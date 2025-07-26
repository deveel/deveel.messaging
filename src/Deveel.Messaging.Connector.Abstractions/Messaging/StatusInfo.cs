//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents the status information of the connector.
	/// </summary>
	/// <remarks>
	/// This class is immutable with respect to the <see cref="Status"/> 
	/// property, which is set at initialization. 
	/// The <see cref="Timestamp"/> property defaults to the current 
	/// UTC time if not specified.
	/// </remarks>
	public readonly struct StatusInfo
	{
		/// <summary>
		/// Constructs a new instance of the <see cref="StatusInfo"/> 
		/// class with the specified status and an optional timestamp.
		/// </summary>
		/// <param name="status">The status message. This parameter cannot be null or whitespace.</param>
		/// <param name="description">An optional description of the status.</param>
		/// <param name="timestamp">The optional timestamp associated with the status. If not provided, the current UTC time is used.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the status parameter is null or whitespace.
		/// </exception>
		public StatusInfo(string status, string? description = null, DateTimeOffset? timestamp = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(status, nameof(status));

			Status = status;
			Timestamp = timestamp ?? DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Gets the current status of the operation.
		/// </summary>
		public string Status { get; }

		/// <summary>
		/// Gets or sets the description text.
		/// </summary>
		public string? Description { get; }

		/// <summary>
		/// Gets or sets the timestamp indicating when the event occurred.
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Gets or sets a collection of additional data associated with the status.
		/// </summary>
		public IDictionary<string, object> AdditionalData { get; } = new Dictionary<string, object>();
	}
}
