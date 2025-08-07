//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	/// <summary>
	/// An implementation of <see cref="IMessage"/> that is used to
	/// represent a message that can be sent or received.
	/// </summary>
	public class Message : IMessage {
		/// <summary>
		/// Constructs an empty message instance.
		/// </summary>
		public Message() {
		}

		/// <summary>
		/// Constructs a message instance from the given <paramref name="message"/>.
		/// </summary>
		/// <param name="message">
		/// The message that is used as source of the new instance.
		/// </param>
		public Message(IMessage message) {
			Id = message.Id;
			Sender = message.Sender != null ? new Endpoint(message.Sender) : null;
			Receiver = message.Receiver != null ? new Endpoint(message.Receiver) : null;
			Content = MessageContent.Create(message.Content);
			Properties = message.Properties?.ToDictionary(x => x.Key, x => new MessageProperty(x.Value));
		}

		/// <inheritdoc/>
		public string Id { get; set; } = "";

		/// <inheritdoc/>
		public Endpoint? Sender { get; set; }

		/// <inheritdoc/>
		public Endpoint? Receiver { get; set; }

		IEndpoint? IMessage.Sender => Sender;

		IEndpoint? IMessage.Receiver => Receiver;


		/// <inheritdoc/>
		public MessageContent? Content { get; set; }

		IMessageContent? IMessage.Content => Content;

		IDictionary<string, IMessageProperty>? IMessage.Properties 
			=> Properties?.ToDictionary(x => x.Key, y => (IMessageProperty)y.Value);

		/// <inheritdoc/>
		public IDictionary<string, MessageProperty>? Properties { get; set; }

		#region Builder Methods

		/// <summary>
		/// Sets the identifier for the message.
		/// </summary>
		/// <param name="id">The unique identifier to assign 
		/// to the message.</param>
		/// <returns>
		/// The current <see cref="Message"/> instance, 
		/// allowing for method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="id"/> is <c>null</c>.
		/// </exception>
		public Message WithId(string id)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(id, nameof(id));
			Id = id;
			return this;
		}

		/// <summary>
		/// Sets the sender of the message.
		/// </summary>
		/// <param name="sender">
		/// The endpoint that is the sender of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// building operations.
		/// </returns>
		public Message WithSender(IEndpoint sender)
		{
			ArgumentNullException.ThrowIfNull(sender, nameof(sender));
			Sender = new Endpoint(sender.Type, sender.Address);
			return this;
		}

		/// <summary>
		/// Sets the sender of the message to an email address.
		/// </summary>
		/// <param name="email">
		/// The email address that is the sender of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithEmailSender(string email)
			=> WithSender(Endpoint.EmailAddress(email));

		/// <summary>
		/// Sets the sender of the message to a phone number.
		/// </summary>
		/// <param name="phone">
		/// The phone number that is the sender of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithPhoneSender(string phone)
			=> WithSender(Endpoint.PhoneNumber(phone));

		/// <summary>
		/// Sets the receiver of the message.
		/// </summary>
		/// <param name="receiver">
		/// The endpoint that is the receiver of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithReceiver(IEndpoint receiver)
		{
			ArgumentNullException.ThrowIfNull(receiver, nameof(receiver));
			Receiver = new Endpoint(receiver.Type, receiver.Address);
			return this;
		}

		/// <summary>
		/// Sets the receiver of the message to an email address.
		/// </summary>
		/// <param name="email">
		/// The email address that is the receiver of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithEmailReceiver(string email)
			=> WithReceiver(Endpoint.EmailAddress(email));

		/// <summary>
		/// Sets the receiver of the message to a phone number.
		/// </summary>
		/// <param name="phone">
		/// The phone number that is the receiver of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithPhoneReceiver(string phone)
			=> WithReceiver(Endpoint.PhoneNumber(phone));

		/// <summary>
		/// Sets the content of the message.
		/// </summary>
		/// <param name="content">
		/// The content of the message to set.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the given content is <c>null</c>.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// Thrown if the given content is not supported by the
		/// construction of the message.
		/// </exception>
		/// <seealso cref="MessageContent.Create(IMessageContent?)"/>
		public Message WithContent(IMessageContent content)
		{
			ArgumentNullException.ThrowIfNull(content, nameof(content));
			Content = MessageContent.Create(content);
			return this;
		}

		/// <summary>
		/// Sets the HTML content of the message.
		/// </summary>
		/// <param name="html">
		/// The HTML content to set as the content of the message.
		/// </param>
		/// <param name="configure">
		/// A configuration action that can be used to
		/// further configure the HTML content.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithHtmlContent(string html, Action<HtmlContent>? configure = null)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(html, nameof(html));

			var content = new HtmlContent(html);
			configure?.Invoke(content);
			return WithContent(content);
		}

		/// <summary>
		/// Sets a text content to the message.
		/// </summary>
		/// <param name="text">
		/// The text to set as the content of the message.
		/// </param>
		/// <param name="encoding">
		/// An optional encoding to use to encode the text.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		/// <seealso cref="WithContent(IMessageContent)"/>
		public Message WithTextContent(string text, string? encoding = null)
			=> WithContent(new TextContent(text, encoding));

		/// <summary>
		/// Attaches a set of properties to the message.
		/// </summary>
		/// <param name="properties">
		/// The dictionary of key-value pairs that represent
		/// the properties to attach to the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message With(IDictionary<string, MessageProperty> properties)
		{
			if (Properties == null)
			{
				Properties = properties;
			} else
			{
				Properties = Properties.Merge(properties);
			}

			return this;
		}

		/// <summary>
		/// Attaches a set of properties to the message.
		/// </summary>
		/// <param name="properties">
		/// The dictionary of key-value pairs that represent
		/// the properties to attach to the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message With(IDictionary<string, object> properties)
		{
			var messageProperties = properties.ToDictionary(kvp => kvp.Key, kvp => new MessageProperty(kvp.Key, kvp.Value));
			return With(messageProperties);
		}

		/// <summary>
		/// Attaches a property to the message.
		/// </summary>
		/// <param name="propertyName">
		/// The name of the property to attach to the message.
		/// </param>
		/// <param name="value">
		/// The value of the property.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message With(string propertyName, object value)
			=> With(new Dictionary<string, MessageProperty> { { propertyName, new MessageProperty(propertyName, value) } });

		/// <summary>
		/// Extends the message with a property that
		/// carries the subject.
		/// </summary>
		/// <param name="subject">
		/// The subject of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		/// <seealso cref="With(IDictionary{string, MessageProperty})"/>
		/// <seealso cref="With(string, object)"/>
		/// <seealso cref="KnownMessageProperties.Subject"/>
		public Message WithSubject(string subject)
			=> With(KnownMessageProperties.Subject, subject);

		/// <summary>
		/// Sets the identifier of the message that is handled
		/// by the remote endpoint (sender or receiver), and that
		/// correlates the message with the remote one.
		/// </summary>
		/// <param name="messageId">
		/// The identifier of the message that is handled by the
		/// remote endpoint.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		/// <seealso cref="With(IDictionary{string, MessageProperty})"/>
		/// <seealso cref="With(string, object)"/>
		/// <seealso cref="KnownMessageProperties.RemoteMessageId"/>
		public Message WithRemoteId(string messageId)
			=> With(KnownMessageProperties.RemoteMessageId, messageId);

		/// <summary>
		/// Extends the message with the identifier of the message
		/// that is being replied to.
		/// </summary>
		/// <param name="messageId">
		/// The identifier of the message that is being replied to.
		/// </param>
		/// <returns>
		/// Returns the instance of the message to allow chaining
		/// construction operations.
		/// </returns>
		public Message WithReplyTo(string messageId)
			=> With(KnownMessageProperties.ReplyTo, messageId);

		#endregion
	}
}
