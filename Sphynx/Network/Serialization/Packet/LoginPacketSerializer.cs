// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using SphynxUserStatus = Sphynx.Model.User.SphynxUserStatus;

namespace Sphynx.Network.Serialization.Packet
{
    /// <remarks>
    /// Does not inherit <see cref="RequestPacketSerializer{T}"/> in order to save bytes (since user and session
    /// id will always be zero).
    /// </remarks>
    public class LoginRequestPacketSerializer : PacketSerializer<LoginRequestPacket>
    {
        protected override int GetMaxPacketSize(LoginRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserName) + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override void Serialize(LoginRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
        }

        protected override LoginRequestPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            string userName = deserializer.ReadString();
            string password = deserializer.ReadString();

            return new LoginRequestPacket(userName, password);
        }
    }

    public class LoginResponsePacketSerializer : ResponsePacketSerializer<LoginResponsePacket>
    {
        private readonly ITypeSerializer<ISphynxSelfInfo> _userSerializer;

        public LoginResponsePacketSerializer(ITypeSerializer<ISphynxSelfInfo> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override int GetMaxPacketSizeInternal(LoginResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<Guid>() + _userSerializer.GetMaxSize(packet.UserInfo!);
        }

        protected override void SerializeInternal(LoginResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return;

            serializer.WriteGuid(packet.SessionId!.Value);

            if (_userSerializer.TrySerialize(packet.UserInfo!, serializer.CurrentSpan, out int bytesWritten))
            {
                serializer.Offset += bytesWritten;
                return;
            }

            throw new SerializationException(
                $"Could not serialize user {packet.UserInfo!.UserId} with session id {packet.SessionId}");
        }

        protected override LoginResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new LoginResponsePacket(responseInfo.ErrorCode);

            var sessionId = deserializer.ReadGuid();

            if (_userSerializer.TryDeserialize(deserializer.CurrentSpan, out var userInfo, out int bytesRead))
            {
                deserializer.Offset += bytesRead;
                return new LoginResponsePacket(userInfo, sessionId);
            }

            throw new SerializationException($"Could not deserialize user with session id {sessionId}");
        }
    }

    public class LoginBroadcastPacketSerializer : PacketSerializer<LoginBroadcastPacket>
    {
        protected override int GetMaxPacketSize(LoginBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SphynxUserStatus>();
        }

        protected override void Serialize(LoginBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            serializer.WriteEnum(packet.PacketType);
        }

        protected override LoginBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new LoginBroadcastPacket(userId, userStatus);
        }
    }
}
