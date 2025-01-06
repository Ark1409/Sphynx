// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Packet;

namespace Sphynx.Network.Serialization.Packet
{
    /// <summary>
    /// Serializes and deserializes <see cref="TPacket"/>s to and from bytes.
    /// </summary>
    /// <typeparam name="TPacket">The type of <see cref="SphynxPacket"/> supported by this serializer.</typeparam>
    public interface IPacketSerializer<TPacket> where TPacket : SphynxPacket
    {
        /// <summary>
        /// Returns the maximum serialization size (in bytes) of the specified <paramref name="packet"/>.
        /// </summary>
        /// <param name="packet">The packet for which the maximum serialization size should be checked.</param>
        int GetMaxSize(TPacket packet);

        /// <summary>
        /// Attempts to serialize this packet into a tightly-packed byte array.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <param name="buffer">This buffer to serialize this packet into.</param>
        /// <param name="bytesWritten">Number of bytes written into the buffer.</param>
        bool TrySerialize(TPacket packet, Span<byte> buffer, out int bytesWritten);

        /// <summary>
        /// Attempts to deserialize a <see cref="TPacket"/>.
        /// </summary>
        /// <param name="buffer">The deserialized packet bytes.</param>
        /// <param name="packet">The deserialized packet.</param>
        /// <param name="bytesRead">Number of bytes read from the buffer.</param>
        bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out TPacket? packet, out int bytesRead);
    }
}
