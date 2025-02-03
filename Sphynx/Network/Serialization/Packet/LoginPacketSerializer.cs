// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;
using SphynxUserStatus = Sphynx.Model.User.SphynxUserStatus;

namespace Sphynx.Network.Serialization.Packet
{
    /// <remarks>
    /// Does not inherit <see cref="RequestPacketSerializer{T}"/> in order to save bytes (since user and session
    /// id will always be zero).
    /// </remarks>
    public class LoginRequestPacketSerializer : PacketSerializer<LoginRequestPacket>
    {
        public override int GetMaxSize(LoginRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserName) + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override bool Serialize(LoginRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
            return true;
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

        protected override int GetMaxSizeInternal(LoginResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<Guid>() + _userSerializer.GetMaxSize(packet.UserInfo!);
        }

        protected override bool SerializeInternal(LoginResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            serializer.WriteGuid(packet.SessionId!.Value);

            return _userSerializer.TrySerializeUnsafe(packet.UserInfo!, ref serializer);
        }

        protected override LoginResponsePacket? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new LoginResponsePacket(responseInfo.ErrorCode);

            var sessionId = deserializer.ReadGuid();

            return _userSerializer.TryDeserialize(ref deserializer, out var userInfo)
                ? new LoginResponsePacket(userInfo, sessionId)
                : null;
        }
    }

    public class LoginBroadcastPacketSerializer : PacketSerializer<LoginBroadcastPacket>
    {
        public override int GetMaxSize(LoginBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SphynxUserStatus>();
        }

        protected override bool Serialize(LoginBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            serializer.WriteEnum(packet.PacketType);
            return true;
        }

        protected override LoginBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new LoginBroadcastPacket(userId, userStatus);
        }
    }
}
