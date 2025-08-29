// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Runtime.Serialization;
using System.Text.Json;
using Sphynx.Core;
using Sphynx.Network.Packet;
using Sphynx.Network.Serialization.Packet;

namespace Sphynx.Network.Serialization
{
    public class JsonPacketSerializer : IPacketSerializer<SphynxPacket>
    {
        public JsonWriterOptions WriterOptions { get; set; }
        public JsonReaderOptions ReaderOptions { get; set; }

        public JsonSerializerOptions SerializerOptions { get; set; } = new();

        public JsonPacketSerializer()
        {
            SerializerOptions.Converters.Add(new SnowflakeIdConverter());
        }

        public void Serialize(SphynxPacket instance, IBufferWriter<byte> buffer)
        {
            // TODO: Pool writers
            using var writer = new Utf8JsonWriter(buffer, WriterOptions);

            writer.WriteStartObject();

            // TODO: Refactor away this dangerous code
            writer.WriteString("$type", instance.GetType().AssemblyQualifiedName);
            writer.WritePropertyName("$payload");
            JsonSerializer.Serialize(writer, instance, instance.GetType(), SerializerOptions);

            writer.WriteEndObject();
        }

        public SphynxPacket? Deserialize(in ReadOnlySequence<byte> buffer, out long bytesRead)
        {
            var reader = new Utf8JsonReader(buffer, ReaderOptions);

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new SerializationException("Could not deserialize packet as JSON");

            if (!reader.Read() || reader.GetString() != "$type" || !reader.Read())
                throw new SerializationException("Could not deserialize packet as JSON (could not find type identifier)");

            string typeName = reader.GetString()!;
            var type = Type.GetType(typeName);

            if (type is null)
                throw new SerializationException($"Could not deserialize packet as JSON (unknown type: '{typeName}')");

            if (!reader.Read() || reader.GetString() != "$payload")
                throw new SerializationException("Could not deserialize packet as JSON (could not find payload)");

            var packet = (SphynxPacket?)JsonSerializer.Deserialize(ref reader, type, SerializerOptions);

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
                throw new SerializationException("Could not deserialize packet as JSON");

            bytesRead = reader.BytesConsumed;
            return packet;
        }
    }
}
