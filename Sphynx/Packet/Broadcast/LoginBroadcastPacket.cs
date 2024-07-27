using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_BCAST"/>
    public sealed class LoginBroadcastPacket : SphynxPacket, IEquatable<LoginBroadcastPacket>
    {
        /// <summary>
        /// User ID of the user who went online.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The status of the user who went online.
        /// </summary>
        public SphynxUserStatus UserStatus { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_BCAST;

        private const int USER_ID_OFFSET = 0;
        private static readonly int USER_STATUS_OFFSET = USER_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="LoginBroadcastPacket"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went online.</param>
        /// <param name="userStatus">The status of the user who went online.</param>
        public LoginBroadcastPacket(Guid userId, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserStatus = userStatus;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginBroadcastPacket? packet)
        {
            int contentSize = GUID_SIZE + sizeof(SphynxUserStatus); // UserId, UserStatus

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var userId = new Guid(contents.Slice(USER_ID_OFFSET, GUID_SIZE));
            var userStatus = (SphynxUserStatus)contents[USER_STATUS_OFFSET];
            packet = new LoginBroadcastPacket(userId, userStatus);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE + sizeof(SphynxUserStatus); // UserId, UserStatus
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize]))
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int contentSize = GUID_SIZE + sizeof(SphynxUserStatus); // UserId, UserStatus

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer)
        {
            if (TrySerializeHeader(buffer))
            {
                buffer = buffer[SphynxPacketHeader.HEADER_SIZE..];
                UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE));
                buffer[USER_STATUS_OFFSET] = (byte)UserStatus;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(LoginBroadcastPacket? other) => base.Equals(other) && UserId == other?.UserId && UserStatus == other?.UserStatus;
    }
}