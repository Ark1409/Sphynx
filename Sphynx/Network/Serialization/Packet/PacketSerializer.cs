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
        public abstract int GetMaxSize(T packet);

        public bool TrySerialize(T packet, Span<byte> buffer, out int bytesWritten)
        {
            int tempBufferSize = GetMaxSize(packet);
            byte[] rawTempBuffer = ArrayPool<byte>.Shared.Rent(tempBufferSize);
            var tempBuffer = rawTempBuffer.AsMemory()[..tempBufferSize];

            try
            {
                var tempSpan = tempBuffer.Span;

                if (TrySerializeUnsafe(packet, tempSpan, out bytesWritten) && tempSpan.TryCopyTo(buffer))
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

        // TODO: Bring the unsafe serialization all the way up

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySerializeUnsafe(T packet, Span<byte> buffer)
        {
            return TrySerializeUnsafe(packet, buffer, out _);
        }

        /// <summary>
        /// Serializes the packet directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <param name="bytesWritten">Number of bytes written to the buffer.</param>
        /// <returns>true if serialization succeeded; false otherwise.</returns>
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
