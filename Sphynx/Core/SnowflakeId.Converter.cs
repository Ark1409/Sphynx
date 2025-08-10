// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sphynx.Core
{
    public class SnowflakeIdConverter : JsonConverter<SnowflakeId>
    {
        public override SnowflakeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // TODO: Copy string
            string? idString = reader.GetString();

            if (!SnowflakeId.TryParse(idString, out var id))
                throw new SerializationException($"Could not deserialize {nameof(SnowflakeId)} '{idString}'");

            return id.Value;
        }

        public override void Write(Utf8JsonWriter writer, SnowflakeId value, JsonSerializerOptions options)
        {
            // TODO: Copy string
            writer.WriteStringValue(value.ToString());
        }
    }
}
