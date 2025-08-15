// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sphynx.Core
{
    public class SnowflakeIdConverter : JsonConverter<SnowflakeId>
    {
        public override SnowflakeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? idString = reader.GetString();

            if (!SnowflakeId.TryParse(idString, out var id))
                throw new SerializationException($"Could not deserialize {nameof(SnowflakeId)} '{idString}'");

            return id.Value;
        }

        public override void Write(Utf8JsonWriter writer, SnowflakeId value, JsonSerializerOptions options)
        {
            Span<char> valueString = stackalloc char[20];

            bool formatted = value.TryFormat(valueString, out int charsWritten);
            Debug.Assert(formatted);
            Debug.Assert(charsWritten == valueString.Length);

            writer.WriteStringValue(valueString);
        }
    }
}
