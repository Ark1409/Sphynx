using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet
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
        protected static readonly unsafe int GUID_SIZE = sizeof(Guid);

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
        /// <returns>true if the <see cref="SphynxPacket"/> could be created successfully; false otherwise.</returns>
        public static bool TryCreate(SphynxPacketType packetType, ReadOnlySpan<byte> contents, [NotNullWhen(true)] out SphynxPacket? packet)
        {
            switch (packetType)
            {
                case SphynxPacketType.LOGIN_REQ:
                {
                    if (!LoginRequestPacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.LOGIN_RES:
                {
                    if (!LoginResponsePacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.MSG_REQ:
                {
                    if (!MessageRequestPacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.MSG_RES:
                {
                    if (!MessageResponsePacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.MSG_BCAST:
                {
                    if (!MessageBroadcastPacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_CREATE_REQ:
                {
                    if (!RoomCreateRequestPacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_CREATE_RES:
                {
                    if (!RoomCreateResponsePacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_JOIN_REQ:
                {
                    if (!RoomJoinRequestPacket.TryDeserialize(contents, out var p)) break;
                    
                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_JOIN_RES:
                {
                    if (!RoomJoinResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.CHAT_JOIN_BCAST:
                {
                    if (!RoomJoinBroadcastPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_LEAVE_REQ:
                {
                    if (!RoomLeaveRequestPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_LEAVE_RES:
                {
                    if (!RoomLeaveResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.CHAT_LEAVE_BCAST:
                {
                    if (!RoomLeaveBroadcastPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_KICK_REQ:
                {
                    if (!RoomKickRequestPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_KICK_RES:
                {
                    if (!RoomKickResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.CHAT_KICK_BCAST:
                {
                    if (!RoomKickBroadcastPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_DEL_REQ:
                {
                    if (!RoomDeleteRequestPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_DEL_RES:
                {
                    if (!RoomDeleteResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.CHAT_DEL_BCAST:
                {
                    if (!RoomDeleteBroadcastPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_SELECT_REQ:
                {
                    if (!RoomSelectRequestPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_SELECT_RES:
                {
                    if (!RoomSelectResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.LOGOUT_REQ:
                {
                    if (!LogoutRequestPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.LOGOUT_RES:
                {
                    if (!LogoutResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_INFO_REQ:
                {
                    if (!RoomInfoRequestPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.ROOM_INFO_RES:
                {
                    if (!RoomInfoResponsePacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }
                
                case SphynxPacketType.LOGIN_BCAST:
                {
                    if (!LoginBroadcastPacket.TryDeserialize(contents, out var p)) break;

                    packet = p;
                    return true;
                }

                case SphynxPacketType.USER_INFO_REQ:
                case SphynxPacketType.USER_INFO_RES:
                    break;

                case SphynxPacketType.NOP:
                default:
                    break;
            }

            packet = null;
            return false;
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
        /// <returns>true if the <see cref="SphynxPacket"/> could be created successfully; false otherwise.</returns>
        public static bool TryCreate(SphynxPacketHeader header, Stream contentStream, [NotNullWhen(true)] out SphynxPacket? packet)
        {
            return (packet = CreateAsync(header, contentStream).GetAwaiter().GetResult()) is not null;
        }

        /// <summary>
        /// Asynchronously creates the appropriate <see cref="SphynxPacket"/> (specified by the 
        /// <paramref name="header"/>'s <see cref="SphynxPacketHeader.PacketType"/>) by reading from the <paramref name="contentStream"/>. 
        /// Note that the stream must be positioned at the start of the packet contents (excluding the header).
        /// </summary>
        /// <param name="header">The header for the packet to create.</param>
        /// <param name="contentStream">The contents of the packet, excluding the header. Must be positioned at the start 
        /// of the packet contents (excluding the header)</param>
        /// <returns>The actual packet, or null if it could not be deserialized.</returns>
        public static async Task<SphynxPacket?> CreateAsync(SphynxPacketHeader header, Stream contentStream)
        {
            if (!contentStream.CanRead)
            {
                return null;
            }

            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(header.ContentSize);
            var buffer = rawBuffer.AsMemory()[..header.ContentSize];
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static async Task ReadBytesAsync(Stream stream, Memory<byte> buffer)
            {
                int readCount = 0;
                do
                {
                    readCount += await stream.ReadAsync(buffer[readCount..]).ConfigureAwait(false);
                } while (readCount < buffer.Length);
            }

            try
            {
                await ReadBytesAsync(contentStream, buffer).ConfigureAwait(false);
                _ = TryCreate(header.PacketType, buffer.Span, out var packet);
                return packet;
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
        public bool TrySerialize(Stream stream) => TrySerializeAsync(stream).GetAwaiter().GetResult();

        /// <summary>
        /// Attempts to asynchronously serialize this packet into the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize this packet into.</param>
        public abstract Task<bool> TrySerializeAsync(Stream stream);

        /// <summary>
        /// Serializes a packet header into the specified <paramref name="packetBuffer"/>, a tightly-packed
        /// span which is expected to contain only the contents of this packet along with its header.
        /// </summary>
        /// <param name="packetBuffer">The buffer to serialize the header into.</param>
        protected bool TrySerializeHeader(Span<byte> packetBuffer)
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