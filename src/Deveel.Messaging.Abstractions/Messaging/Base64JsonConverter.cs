//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deveel.Messaging {
	class Base64JsonConverter : JsonConverter<string> {
		public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			string? s;
			if (reader.TokenType != JsonTokenType.String || (s = reader.GetString()) == null)
				return null;

			var buffer = new byte[((s.Length * 3) + 3) / 4 -
				(s.Length > 0 && s[s.Length - 1] == '=' ?
				s.Length > 1 && s[s.Length - 2] == '=' ?
				2 : 1 : 0)];

			if (!Convert.TryFromBase64String(s, buffer, out var bytes))
				return null;

			return Encoding.UTF8.GetString(buffer);
		}

		public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) {
			var bytes = Encoding.UTF8.GetBytes(value);
			var base64 = Convert.ToBase64String(bytes);

			writer.WriteStringValue(base64);
		}
	}
}
