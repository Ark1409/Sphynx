using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

using Sphynx.Packet.Broadcast;
using Sphynx.Packet.Request;
using Sphynx.Packet.Response;

namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a packet sent between nodes on a Sphynx network.
    /// </summary>
    public abstract class SphynxPacket
    {
        /// <summary>
        /// Encoding used for text.
        /// </summary>
        public static readonly Encoding TEXT_ENCODING = Encoding.UTF8;

        /// <summary>
        /// <see langword="sizeof"/>(<see cref="Guid"/>)
        /// </summary>
        protected static unsafe readonly int GUID_SIZE = sizeof(Guid);

        /// <summary>
        /// Packet type for this packet.
        /// </summary>
        public abstract SphynxPacketType PacketType { get; }

        /// <summary>
        /// Creates the appropriate <see cref="SphynxPacket"/> from the <paramref name="contents"/>.
        /// </summary>
        /// <param name="packetType">The packet type.</param>
        /// <param name="contents">The contents of the packet, excluding the header.</param>
        /// <param name="packet">The actual packet.</param>
        /// <returns>true if the <see cref="SphynxPacket"/> could be created succesfully; false otherwise.</returns>
        public static bool TryCreate(SphynxPacketType packetType, ReadOnlySpan<byte> contents, [NotNullWhen(true)] out SphynxPacket? packet)
        {
            packet = null;

            switch (packetType)
            {
                case SphynxPacketType.LOGIN_REQ:
                    if (LoginRequestPacket.TryDeserialize(contents, out var p1))
                    {
                        packet = p1;
                        return true;
                    }
                    return false;

                case SphynxPacketType.LOGIN_RES:
                    if (LoginResponsePacket.TryDeserialize(contents, out var p2))
                    {
                        packet = p2;
                        return true;
                    }
                    return false;

                case SphynxPacketType.MSG_REQ:
                    if (MessageRequestPacket.TryDeserialize(contents, out var p3))
                    {
                        packet = p3;
                        return true;
                    }
                    return false;

                case SphynxPacketType.MSG_RES:
                    if (MessageResponsePacket.TryDeserialize(contents, out var p4))
                    {
                        packet = p4;
                        return true;
                    }
                    return false;

                case SphynxPacketType.MSG_BCAST:
                    if (MessageBroadcastPacket.TryDeserialize(contents, out var p5))
                    {
                        packet = p5;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_CREATE_REQ:
                    if (ChatCreateRequestPacket.TryDeserialize(contents, out var p6))
                    {
                        packet = p6;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_CREATE_RES:
                    if (ChatCreateResponsePacket.TryDeserialize(contents, out var p7))
                    {
                        packet = p7;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_JOIN_REQ:
                    if (ChatJoinRequestPacket.TryDeserialize(contents, out var p8))
                    {
                        packet = p8;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_JOIN_RES:
                    if (ChatJoinResponsePacket.TryDeserialize(contents, out var p9))
                    {
                        packet = p9;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_JOIN_BCAST:
                    if (ChatJoinBroadcastPacket.TryDeserialize(contents, out var p10))
                    {
                        packet = p10;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_LEAVE_REQ:
                    if (ChatLeaveRequestPacket.TryDeserialize(contents, out var p11))
                    {
                        packet = p11;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_LEAVE_RES:
                    if (ChatLeaveResponsePacket.TryDeserialize(contents, out var p12))
                    {
                        packet = p12;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_LEAVE_BCAST:
                    if (ChatLeaveBroadcastPacket.TryDeserialize(contents, out var p13))
                    {
                        packet = p13;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_KICK_REQ:
                    if (ChatKickRequestPacket.TryDeserialize(contents, out var p14))
                    {
                        packet = p14;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_KICK_RES:
                    if (ChatKickResponsePacket.TryDeserialize(contents, out var p15))
                    {
                        packet = p15;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_KICK_BCAST:
                    if (ChatKickBroadcastPacket.TryDeserialize(contents, out var p16))
                    {
                        packet = p16;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_DEL_REQ:
                    if (ChatDeleteRequestPacket.TryDeserialize(contents, out var p17))
                    {
                        packet = p17;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_DEL_RES:
                    if (ChatDeleteResponsePacket.TryDeserialize(contents, out var p18))
                    {
                        packet = p18;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_DEL_BCAST:
                    if (ChatDeleteBroadcastPacket.TryDeserialize(contents, out var p19))
                    {
                        packet = p19;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_SELECT_REQ:
                    if (ChatSelectRequestPacket.TryDeserialize(contents, out var p20))
                    {
                        packet = p20;
                        return true;
                    }
                    return false;

                case SphynxPacketType.CHAT_SELECT_RES:
                    if (ChatSelectResponsePacket.TryDeserialize(contents, out var p21))
                    {
                        packet = p21;
                        return true;
                    }
                    return false;

                case SphynxPacketType.LOGOUT_REQ:
                    if (LogoutRequestPacket.TryDeserialize(contents, out var p22))
                    {
                        packet = p22;
                        return true;
                    }
                    return false;

                case SphynxPacketType.LOGOUT_RES:
                    if (LogoutResponsePacket.TryDeserialize(contents, out var p23))
                    {
                        packet = p23;
                        return true;
                    }
                    return false;

                case SphynxPacketType.ROOM_INFO_REQ:
                    if (RoomInfoRequestPacket.TryDeserialize(contents, out var p24))
                    {
                        packet = p24;
                        return true;
                    }
                    return false;

                case SphynxPacketType.ROOM_INFO_RES:
                    if (RoomInfoResponsePacket.TryDeserialize(contents, out var p25))
                    {
                        packet = p25;
                        return true;
                    }
                    return false;

                case SphynxPacketType.USER_INFO_REQ:
                case SphynxPacketType.USER_INFO_RES:
                    return false;

                case SphynxPacketType.NOP:
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates the appropriate <see cref="SphynxPacket"/> (specified by the 
        /// <paramref name="header"/>'s <see cref="SphynxPacketHeader.PacketType"/>) by reading from the <paramref name="contentStream"/>. 
        /// Note that the stream must be positioned at the start of the packet contents (excluding the header).
        /// </summary>
        /// <param name="header">The header for the packet to create.</param>
        /// <param name="contentStream">The contents of the packet, excluding the header. Must be positioned at the start 
        /// of the packet contents (excluding the header)</param>
        /// <param name="packet">The actual packet.</param>
        /// <returns>true if the <see cref="SphynxPacket"/> could be created succesfully; false otherwise.</returns>
        public static bool TryCreate(SphynxPacketHeader header, Stream contentStream, [NotNullWhen(true)] out SphynxPacket? packet)
        {
            if (!contentStream.CanRead)
            {
                packet = null;
                return false;
            }

            var rawBuffer = ArrayPool<byte>.Shared.Rent(header.ContentSize);
            var buffer = rawBuffer.AsSpan()[..header.ContentSize];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void ReadBytes(Stream stream, Span<byte> buffer)
            {
                int readCount = 0;
                do
                {
                    readCount += stream.Read(buffer[readCount..]);
                } while (readCount < buffer.Length);
            }

            try
            {
                ReadBytes(contentStream, buffer);
                return TryCreate(header.PacketType, buffer, out packet));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }
        }

        /// <summary>
        /// Attempts to serialize this packet into a tightly-packed byte array.
        /// </summary>
        /// <param name="packetBytes">This packet serialized as a byte array.</param>
        public abstract bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes);

        /// <summary>
        /// Attempts to serialize this packet into the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize this packet into.</param>
        public abstract bool TrySerialize(Stream stream);

        /// <summary>
        /// Serializes a packet header into the specified <paramref name="packetBuffer"/>, a tightly-packed
        /// span which is expected to containly only the contents of this packet along with its header.
        /// </summary>
        /// <param name="packetBuffer">The buffer to serialize the header into.</param>
        protected virtual bool TrySerializeHeader(Span<byte> packetBuffer)
        {
            var header = new SphynxPacketHeader(PacketType, packetBuffer.Length - SphynxPacketHeader.HEADER_SIZE);
            return header.TrySerialize(packetBuffer[..SphynxPacketHeader.HEADER_SIZE]);
        }

        /// <summary>
        /// Indicates whether the current packet has the same packet type as another packet.
        /// </summary>
        /// <param name="other">A packet to compare with this packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected virtual bool Equals(SphynxPacket? other) => PacketType == other?.PacketType;
    }
}