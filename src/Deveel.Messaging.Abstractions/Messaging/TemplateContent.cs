//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//


namespace Deveel.Messaging
{
	/// <summary>
	/// A message content that represents a template with parameters.
	/// </summary>
	public class TemplateContent : MessageContent, ITemplateContent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateContent"/> class 
		/// with the specified template identifier and parameters.
		/// </summary>
		/// <param name="templateId">The unique identifier for the template.</param>
		/// <param name="parameters">A dictionary containing the parameters to be used 
		/// with the template for the merge process on the remote service.</param>
		public TemplateContent(string templateId, IDictionary<string, object?> parameters)
		{
			TemplateId = templateId;
			Parameters = parameters != null ? new Dictionary<string, object?>(parameters) : new Dictionary<string, object?>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateContent"/> class 
		/// from a given source.
		/// </summary>
		/// <param name="content"></param>
		public TemplateContent(ITemplateContent content)
		{
			TemplateId= content.TemplateId;
			Parameters = content.Parameters != null ? new Dictionary<string, object?>(content.Parameters) : new Dictionary<string, object?>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateContent"/> class.
		/// </summary>
		public TemplateContent()
		{
		}

		/// <inheritdoc/>
		public string TemplateId { get; set; } = string.Empty;

		/// <inheritdoc/>
		public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();

		/// <inheritdoc/>
		public override MessageContentType ContentType => MessageContentType.Template;
	}
}
