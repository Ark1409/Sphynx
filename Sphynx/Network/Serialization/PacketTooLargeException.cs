// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using Sphynx.Network.Packet;

namespace Sphynx.Network.Serialization
{
    public class PacketTooLargeException : SerializationException
    {
        public SphynxPacket? Packet { get; }
        public int Size { get; }

        public PacketTooLargeException(SphynxPacket? packet, int size)
            : this(packet, size, $"Packet {packet?.ToString() ?? string.Empty} is too large ({size} bytes)")
        {
        }

        public PacketTooLargeException(int size, string message)
            : this(null, size, message)
        {
        }

        public PacketTooLargeException(SphynxPacket? packet, int size, string? message)
            : this(packet, size, message, null)
        {
        }

        public PacketTooLargeException(SphynxPacket? packet, int size, string? message, Exception? innerException)
            : base(message, innerException)
        {
            Packet = packet;
            Size = size;
        }
    }
}
