using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Core;
using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponsePacket : SphynxResponsePacket, IEquatable<LoginResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Holds the authenticated user's information.
        /// </summary>
        public SphynxUserInfo? UserInfo { get; set; }

        /// <summary>
        /// The session ID for the client.
        /// </summary>
        public Guid? SessionId { get; set; }

        private int ContentSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int contentSize = DEFAULT_CONTENT_SIZE;

                // We only serialize user info when authentication is successful
                if (SessionId.HasValue && UserInfo is not null)
                {
                    contentSize += GUID_SIZE; // SessionId

                    // UserInfo
                    UserInfo.GetPacketInfo(false, out _, out int userContentSize);
                    contentSize += userContentSize;
                }

                return contentSize;
            }
        }

        private const int SESSION_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int USER_OFFSET = SESSION_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public LoginResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public LoginResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The session ID for the client.</param>
        public LoginResponsePacket(SphynxUserInfo userInfo, Guid sessionId) : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            SessionId = sessionId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginResponsePacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + SphynxUserInfo.GetMinimumSize(); // SessionId, UserInfo

            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < minContentSize))
            {
                packet = null;
                return false;
            }

            // We only serialize user info when authentication is successful
            if (errorCode != SphynxErrorCode.SUCCESS)
            {
                packet = new LoginResponsePacket(errorCode.Value);
                return true;
            }

            // Deserialize session ID and user info
            var sessionId = new Guid(contents.Slice(SESSION_ID_OFFSET, GUID_SIZE));

            if (SphynxUserInfo.TryDeserialize(contents[USER_OFFSET..], out var userInfo))
            {
                packet = new LoginResponsePacket(userInfo, sessionId);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + ContentSize;

            try
            {
                if (!TrySerialize(packetBytes = new byte[bufferSize]))
                {
                    packetBytes = null;
                    return false;
                }
            }
            catch
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

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + ContentSize;
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
            catch
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // We only serialize user info when authentication is successful
            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            SessionId!.Value.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            return UserInfo!.TrySerialize(buffer);
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponsePacket? other) =>
            base.Equals(other) && SessionId == other?.SessionId && (UserInfo?.Equals(other?.UserInfo) ?? true);
    }
}