//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

namespace Deveel.Messaging
{
	/// <summary>
	/// Represents a type of content that is used to
	/// merge a template with a set of parameters
	/// and generate the final content of a message
	/// on the provider.
	/// </summary>
	public interface ITemplateContent : IMessageContent
	{
		/// <summary>
		/// Gets the template identifier that is used to
		/// generate the content of the message.
		/// </summary>
		string TemplateId { get; }

		/// <summary>
		/// Gets a dictionary of parameters that are used
		/// to fill the template.
		/// </summary>
		IDictionary<string, object?> Parameters { get; }
	}
}
