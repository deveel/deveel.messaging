//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging {
	/// <summary>
	/// A part of a messaging system that can be used to 
	/// send or receive messages.
	/// </summary>
	public class Endpoint : IEndpoint {
		/// <summary>
		/// Constructs the endpoint with the given type and address.
		/// </summary>
		/// <param name="type">
		/// The type of the endpoint.
		/// </param>
		/// <param name="address">
		/// The address of the endpoint, specific to its type.
		/// </param>
		public Endpoint(EndpointType type, string address) {
			Type = type;
			Address = address;
		}

		/// <summary>
		/// Constructs the endpoint with the given type and address.
		/// </summary>
		/// <param name="type">
		/// The type of the endpoint as a string.
		/// </param>
		/// <param name="address">
		/// The address of the endpoint, specific to its type.
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown when the type string cannot be converted to a valid EndpointType.
		/// </exception>
		public Endpoint(string type, string address) {
			Type = ParseEndpointType(type);
			Address = address;
		}

		/// <summary>
		/// Constructs the endpoint with no properties set.
		/// </summary>
		public Endpoint() {
		}

		/// <summary>
		/// Constructs the endpoint from the given instance.
		/// </summary>
		/// <param name="endpoint">
		/// The source instance of <see cref="IEndpoint"/> that is used
		/// to initialize the properties of this instance.
		/// </param>
		public Endpoint(IEndpoint endpoint) 
			: this(endpoint.Type, endpoint.Address) {
		}

		/// <inheritdoc/>
		public EndpointType Type { get; set; }

		/// <inheritdoc/>
		public string Address { get; set; } = "";

		/// <summary>
		/// Creates a new instance of <see cref="Endpoint"/> with the given
		/// type and address.
		/// </summary>
		/// <param name="type">
		/// The type of the endpoint to create
		/// </param>
		/// <param name="address">
		/// The address of the endpoint, specific to its type.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// the endpoint with the given type and address.
		/// </returns>
		/// <exception cref="ArgumentException"></exception>
		public static Endpoint Create(EndpointType type, string address) {
			ArgumentException.ThrowIfNullOrWhiteSpace(address, nameof(address));
			return new Endpoint(type, address);
		}

		/// <summary>
		/// Creates a new instance of <see cref="Endpoint"/> with the given
		/// type and address.
		/// </summary>
		/// <param name="type">
		/// The type of the endpoint to create as a string.
		/// </param>
		/// <param name="address">
		/// The address of the endpoint, specific to its type.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// the endpoint with the given type and address.
		/// </returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static Endpoint Create(string type, string address) {
			ArgumentNullException.ThrowIfNull(type, nameof(type));
			ArgumentException.ThrowIfNullOrWhiteSpace(address, nameof(address));
			return new Endpoint(type, address);
		}

		/// <summary>
		/// Creates a new endpoint that represents an identifier
		/// to a service endpoint.
		/// </summary>
		/// <param name="endpointId">
		/// The identifier of the service endpoint.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// a reference to a service endpoint.
		/// </returns>
		/// <seealso cref="EndpointType.Id"/>
		public static Endpoint Id(string endpointId)
			=> Create(EndpointType.Id, endpointId);

		/// <summary>
		/// Create a new endpoint that represents an 
		/// email address.
		/// </summary>
		/// <param name="address">
		/// The address of the email.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// an email address.
		/// </returns>
		/// <seealso cref="EndpointType.EmailAddress"/>
		public static Endpoint EmailAddress(string address)
			=> Create(EndpointType.EmailAddress, address);

		/// <summary>
		/// Creates a new endpoint that represents a phone number.
		/// </summary>
		/// <param name="number">
		/// The phone number of the endpoint.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// a phone number.
		/// </returns>
		public static Endpoint PhoneNumber(string number) 
			=> Create(EndpointType.PhoneNumber, number);

		/// <summary>
		/// Creates a new endpoint that represents a URL address.
		/// </summary>
		/// <param name="address">
		/// The URL address of the endpoint.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// a URL address.
		/// </returns>
		public static Endpoint Url(string address) 
			=> Create(EndpointType.Url, address);

		/// <summary>
		/// Creates a new endpoint that represents an application
		/// identifier.
		/// </summary>
		/// <param name="appId">
		/// The identifier of the application.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// a endpoint for an application.
		/// </returns>
		public static Endpoint Application(string appId)
			=> Create(EndpointType.ApplicationId, appId);

		/// <summary>
		/// Creates a new endpoint that represents a user identifier.
		/// </summary>
		/// <param name="userId">
		/// The identifier of the user.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// the endpoint for a user in a system.
		/// </returns>
		public static Endpoint User(string userId)
			=> Create(EndpointType.UserId, userId);

		/// <summary>
		/// Creates a new endpoint that represents a device identifier.
		/// </summary>
		/// <param name="deviceId">
		/// The identifier of the device.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Endpoint"/> that represents
		/// a endpoint for a device.
		/// </returns>
		public static Endpoint Device(string deviceId)
			=> Create(EndpointType.DeviceId, deviceId);

		/// <summary>
		/// Creates an endpoint with a specified alphanumeric label.
		/// </summary>
		/// <param name="label">
		/// The alphanumeric label to associate with the endpoint.
		/// </param>
		/// <returns>
		/// Returns an <see cref="Endpoint"/> object configured with the 
		/// specified label.
		/// </returns>
		public static Endpoint AlphaNumeric(string label)
			=> Create(EndpointType.Label, label);

		/// <summary>
		/// Creates an instance of <see cref="Endpoint"/> from an existing
		/// abstraction of <see cref="IEndpoint"/>.
		/// </summary>
		/// <param name="endpoint">
		/// The source instance of <see cref="IEndpoint"/> to convert.
		/// </param>
		/// <returns>
		/// Returns a new instance of <see cref="Endpoint"/> that is created
		/// from the provided <paramref name="endpoint"/>.
		/// </returns>
		public static Endpoint? Create(IEndpoint? endpoint)
		{
			if (endpoint == null)
				return null;
			if (endpoint is Endpoint)
				return (Endpoint) endpoint;

			return new Endpoint(endpoint.Type, endpoint.Address);
		}

		/// <summary>
		/// Parses a string representation of an endpoint type to an EndpointType enum value.
		/// </summary>
		/// <param name="type">The string representation of the endpoint type.</param>
		/// <returns>The corresponding EndpointType enum value.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the type string is not a valid endpoint type.
		/// </exception>
		private static EndpointType ParseEndpointType(string type)
		{
			ArgumentNullException.ThrowIfNull(type, nameof(type));

			return type.ToLowerInvariant() switch
			{
				"email" => EndpointType.EmailAddress,
				"phone" => EndpointType.PhoneNumber,
				"url" => EndpointType.Url,
				"topic" => EndpointType.Topic,
				"user-id" or "userid" => EndpointType.UserId,
				"app-id" or "appid" or "applicationid" => EndpointType.ApplicationId,
				"endpoint-id" or "id" or "endpointid" => EndpointType.Id,
				"device-id" or "device" or "deviceid" => EndpointType.DeviceId,
				"label" or "alphanumeric" => EndpointType.Label,
				_ => throw new ArgumentException($"Unknown endpoint type: {type}", nameof(type))
			};
		}
	}
}
