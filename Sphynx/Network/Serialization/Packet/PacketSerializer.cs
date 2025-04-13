// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.PacketV2;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class PacketSerializer<T> : IPacketSerializer<T> where T : SphynxPacket
    {
        public abstract int GetMaxSize(T packet);

        public bool TrySerializeUnsafe(T packet, Span<byte> buffer, out int bytesWritten)
        {
            var serializer = new BinarySerializer(buffer);

            try
            {
                Serialize(packet, ref serializer);
                bytesWritten = serializer.Offset;

                return true;
            }
            catch
            {
                bytesWritten = serializer.Offset;
                return false;
            }
        }

        protected abstract bool Serialize(T packet, ref BinarySerializer serializer);

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? packet, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer);

            try
            {
                packet = Deserialize(ref deserializer);
                bytesRead = deserializer.Offset;

                return packet is not null;
            }
            catch
            {
                packet = null;
                bytesRead = 0;
                return false;
            }
        }

        protected abstract T? Deserialize(ref BinaryDeserializer deserializer);
    }
}
