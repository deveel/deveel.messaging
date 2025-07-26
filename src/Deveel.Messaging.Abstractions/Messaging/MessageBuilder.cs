//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// An object that provides a fluent interface to build 
	/// a message.
	/// </summary>
	public sealed class MessageBuilder : IMessage
	{
		/// <summary>
		/// Constructs the builder with a message to extend.
		/// </summary>
		/// <param name="message">
		/// The instance of the message to extend.
		/// </param>
		/// <remarks>
		/// When using this constructor, the message passed
		/// as argument will be copied to the builder, and
		/// any changes made to the builder will not affect
		/// the original message.
		/// </remarks>
		public MessageBuilder(IMessage message)
		{
			if (!(message is Message msg))
				msg = new Message(message);

			Message = msg;
		}

		/// <summary>
		/// Constructs the builder with a new message.
		/// </summary>
		public MessageBuilder()
			: this(new Message())
		{
		}

		/// <summary>
		/// Gets the message that is being built.
		/// </summary>
		public Message Message { get; }

		string IMessage.Id => Message.Id;

		IEndpoint? IMessage.Sender => Message.Sender;

		IEndpoint? IMessage.Receiver => Message.Receiver;

		IMessageContent? IMessage.Content => Message.Content;

		IDictionary<string, IMessageProperty>? IMessage.Properties 
			=> Message.Properties?.ToDictionary(x => x.Key, y => (IMessageProperty)y.Value);

		/// <summary>
		/// Sets the identifier for the message being built.
		/// </summary>
		/// <param name="id">The unique identifier to assign 
		/// to the message.</param>
		/// <returns>
		/// The current <see cref="MessageBuilder"/> instance, 
		/// allowing for method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="id"/> is <c>null</c>.
		/// </exception>
		public MessageBuilder WithId(string id)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(id, nameof(id));
			Message.Id = id;
			return this;
		}


		/// <summary>
		/// Sets the sender of the message.
		/// </summary>
		/// <param name="sender">
		/// The endpoint that is the sender of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// building operations.
		/// </returns>
		public MessageBuilder WithSender(IEndpoint sender)
		{
			ArgumentNullException.ThrowIfNull(sender, nameof(sender));
			Message.Sender = Endpoint.Create(sender.Type, sender.Address);
			return this;
		}

		/// <summary>
		/// Sets the sender of the message to an email address.
		/// </summary>
		/// <param name="email">
		/// The email address that is the sender of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder WithEmailSender(string email)
			=> WithSender(Endpoint.EmailAddress(email));

		/// <summary>
		/// Sets the sender of the message to a phone number.
		/// </summary>
		/// <param name="phone">
		/// The phone number that is the sender of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder WithPhoneSender(string phone)
			=> WithSender(Endpoint.PhoneNumber(phone));

		/// <summary>
		/// Sets the receiver of the message.
		/// </summary>
		/// <param name="receiver">
		/// The endpoint that is the receiver of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder WithReceiver(IEndpoint receiver)
		{
			ArgumentNullException.ThrowIfNull(receiver, nameof(receiver));
			Message.Receiver = Endpoint.Create(receiver.Type, receiver.Address);
			return this;
		}

		/// <summary>
		/// Sets the receiver of the message to an email address.
		/// </summary>
		/// <param name="email">
		/// The email address that is the receiver of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder WithEmailReceiver(string email)
			=> WithReceiver(Endpoint.EmailAddress(email));

		/// <summary>
		/// Sets the receiver of the message to a phone number.
		/// </summary>
		/// <param name="phone">
		/// The phone number that is the receiver of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder WithPhoneReceiver(string phone)
			=> WithReceiver(Endpoint.PhoneNumber(phone));

		/// <summary>
		/// Sets the content of the message.
		/// </summary>
		/// <param name="content">
		/// The content of the message to set.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
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
		public MessageBuilder WithContent(IMessageContent content)
		{
			ArgumentNullException.ThrowIfNull(content, nameof(content));
			Message.Content = MessageContent.Create(content);
			return this;
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
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		/// <seealso cref="WithContent(IMessageContent)"/>
		public MessageBuilder WithTextContent(string text, string? encoding = null)
			=> WithContent(new TextContent(text, encoding));

		/// <summary>
		/// Attaches a set of properties to the message.
		/// </summary>
		/// <param name="properties">
		/// The dictionary of key-value pairs that represent
		/// the properties to attach to the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder With(IDictionary<string, MessageProperty> properties)
		{
			if (Message.Properties == null)
			{
				Message.Properties = properties;
			} else
			{
				Message.Properties = Message.Properties.Merge(properties);
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
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder With(IDictionary<string, object> properties)
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
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder With(string propertyName, object value)
			=> With(new Dictionary<string, MessageProperty> { { propertyName, new MessageProperty(propertyName, value) } });

		/// <summary>
		/// Extends the message with a property that
		/// carries the subject.
		/// </summary>
		/// <param name="subject">
		/// The subject of the message.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		/// <seealso cref="With(IDictionary{string, MessageProperty})"/>
		/// <seealso cref="With(string, object)"/>
		/// <seealso cref="KnownMessageProperties.Subject"/>
		public MessageBuilder WithSubject(string subject)
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
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		/// <seealso cref="With(IDictionary{string, MessageProperty})"/>
		/// <seealso cref="With(string, object)"/>
		/// <seealso cref="KnownMessageProperties.RemoteMessageId"/>
		public MessageBuilder WithRemoteId(string messageId)
			=> With(KnownMessageProperties.RemoteMessageId, messageId);

		/// <summary>
		/// Extends the message with the identifier of the message
		/// that is being replied to.
		/// </summary>
		/// <param name="messageId">
		/// The identifier of the message that is being replied to.
		/// </param>
		/// <returns>
		/// Returns the instance of the builder to allow chaining
		/// construction operations.
		/// </returns>
		public MessageBuilder WithReplyTo(string messageId)
			=> With(KnownMessageProperties.ReplyTo, messageId);
	}
}
