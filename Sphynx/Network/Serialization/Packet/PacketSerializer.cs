// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Network.PacketV2;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class PacketSerializer<T> : IPacketSerializer<T> where T : SphynxPacket
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMaxSize(T packet)
        {
            return sizeof(int) + GetMaxPacketSize(packet);
        }

        protected abstract int GetMaxPacketSize(T packet);

        public bool TrySerialize(T packet, Span<byte> buffer, out int bytesWritten)
        {
            int tempBufferSize = GetMaxSize(packet);
            byte[] rawTempBuffer = ArrayPool<byte>.Shared.Rent(tempBufferSize);
            var tempBuffer = rawTempBuffer.AsMemory()[..tempBufferSize];

            try
            {
                var tempSpan = tempBuffer.Span;
                bytesWritten = SerializeUnsafe(packet, tempSpan);

                if (tempSpan.TryCopyTo(buffer))
                    return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawTempBuffer);
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        /// Serializes the packet directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <returns>Number of bytes written to the buffer.</returns>
        public int SerializeUnsafe(T packet, Span<byte> buffer)
        {
            // Sanity check
            if (buffer.Length < sizeof(int))
                return 0;

            var serializer = new BinarySerializer(buffer[sizeof(int)..]);

            try
            {
                Serialize(packet, ref serializer);

                int bytesWritten = sizeof(int) + serializer.Offset;

                serializer = new BinarySerializer(buffer);
                serializer.WriteInt32(bytesWritten - sizeof(int));

                return bytesWritten;
            }
            catch
            {
                return sizeof(int) + serializer.Offset;
            }
        }

        protected abstract void Serialize(T packet, ref BinarySerializer serializer);

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? packet, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer[sizeof(int)..]);

            try
            {
                packet = Deserialize(ref deserializer);
                bytesRead = sizeof(int) + deserializer.Offset;

                return true;
            }
            catch
            {
                packet = null;
                bytesRead = 0;
                return false;
            }
        }

        protected abstract T Deserialize(ref BinaryDeserializer deserializer);
    }
}
