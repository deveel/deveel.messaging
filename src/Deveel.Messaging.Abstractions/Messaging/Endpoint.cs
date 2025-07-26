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
		public Endpoint(string type, string address) {
			Type = type;
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
		public string Type { get; set; } = "";

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
		public static Endpoint Create(string type, string address) {
			ArgumentException.ThrowIfNullOrWhiteSpace(type, nameof(type));
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
		/// <seealso cref="KnownEndpointTypes.EndpointId"/>
		public static Endpoint Id(string endpointId)
			=> Create(KnownEndpointTypes.EndpointId, endpointId);

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
		/// <seealso cref="KnownEndpointTypes.Email"/>
		public static Endpoint EmailAddress(string address)
			=> Create(KnownEndpointTypes.Email, address);

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
			=> Create(KnownEndpointTypes.Phone, number);

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
			=> Create(KnownEndpointTypes.Url, address);

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
			=> Create(KnownEndpointTypes.Application, appId);

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
			=> Create(KnownEndpointTypes.UserId, userId);

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
			=> Create(KnownEndpointTypes.DeviceId, deviceId);

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
			=> Create(KnownEndpointTypes.Label, label);
	}
}
