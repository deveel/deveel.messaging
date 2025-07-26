//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System;

namespace Deveel.Messaging
{
	/// <summary>
	/// Provides information about the health and status 
	/// of a messaging connector.
	/// </summary>
	public class ConnectorHealth
	{
		/// <summary>
		/// Gets or sets the current state of the connector.
		/// </summary>
		public ConnectorState State { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the system is in a healthy state.
		/// </summary>
		public bool IsHealthy { get; set; }

		/// <summary>
		/// Gets or sets the timestamp of the last health check performed.
		/// </summary>
		public DateTime LastHealthCheck { get; set; }

		/// <summary>
		/// Gets or sets the duration for which the system has been running.
		/// </summary>
		public TimeSpan Uptime { get; set; }

		/// <summary>
		/// Gets or sets a collection of metrics for the connector.
		/// </summary>
		public Dictionary<string, object> Metrics { get; set; } = new();

		/// <summary>
		/// Gets or sets the list of issues.
		/// </summary>
		public List<string> Issues { get; set; } = new();
	}
}
