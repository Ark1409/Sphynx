// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;

namespace Sphynx.Network.Serialization.Packet
{
    /// <summary>
    /// Serializes and deserializes <see cref="TPacket"/>s to and from bytes.
    /// </summary>
    /// <typeparam name="TPacket">The type of <see cref="SphynxPacket"/> supported by this serializer.</typeparam>
    public interface IPacketSerializer<TPacket> : ITypeSerializer<TPacket>
        where TPacket : SphynxPacket
    {
    }
}
