// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Network.Packet;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class PacketSerializer<T> : IPacketSerializer<T> where T : SphynxPacket
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int GetMaxSize(T packet)
        {
            return sizeof(int); // Content size
        }

        public bool TrySerialize(T packet, Span<byte> buffer, out int bytesWritten)
        {
            int tempBufferSize = GetMaxSize(packet);
            byte[] rawTempBuffer = ArrayPool<byte>.Shared.Rent(tempBufferSize);
            var tempBuffer = rawTempBuffer.AsMemory()[..tempBufferSize];

            try
            {
                var tempSpan = tempBuffer.Span;

                if (TrySerializeUnsafe(packet, tempSpan, out bytesWritten) && tempSpan.TryCopyTo(buffer))
                {
                    return true;
                }

                bytesWritten = 0;
                return false;
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
        }

        /// <summary>
        /// Serializes the packet directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <param name="bytesWritten">Number of bytes written to the buffer.</param>
        public bool TrySerializeUnsafe(T packet, Span<byte> buffer, out int bytesWritten)
        {
            var serializer = new BinarySerializer(buffer[sizeof(int)..]);

            if (TrySerialize(packet, ref serializer))
            {
                int contentByteCount = serializer.Count;
                bytesWritten = sizeof(int) + contentByteCount;

                serializer = new BinarySerializer(buffer);
                serializer.WriteInt32(contentByteCount);
                return true;
            }

            bytesWritten = serializer.Count;
            return false;
        }

        protected abstract bool TrySerialize(T packet, ref BinarySerializer serializer);

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? packet, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer[sizeof(int)..]);

            if (TryDeserialize(out packet, ref deserializer))
            {
                bytesRead = sizeof(int) + deserializer.Count;
                return true;
            }

            bytesRead = 0;
            return false;
        }

        protected abstract bool TryDeserialize([NotNullWhen(true)] out T? packet, ref BinaryDeserializer deserializer);
    }
}
