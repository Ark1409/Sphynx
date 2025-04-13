// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    /// <remarks>
    /// Does not inherit <see cref="RequestPacketSerializer{T}"/> in order to save bytes (since user and session
    /// id will always be zero).
    /// </remarks>
    public class RegisterRequestPacketSerializer : PacketSerializer<RegisterRequest>
    {
        public override int GetMaxSize(RegisterRequest packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserName) + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override bool Serialize(RegisterRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
            return true;
        }

        protected override RegisterRequest Deserialize(ref BinaryDeserializer deserializer)
        {
            string userName = deserializer.ReadString();
            string password = deserializer.ReadString();

            return new RegisterRequest(userName, password);
        }
    }

    public class RegisterResponsePacketSerializer : ResponsePacketSerializer<RegisterResponsePacket>
    {
        private readonly ITypeSerializer<ISphynxSelfInfo> _userSerializer;

        public RegisterResponsePacketSerializer(ITypeSerializer<ISphynxSelfInfo> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override int GetMaxSizeInternal(RegisterResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<Guid>() + _userSerializer.GetMaxSize(packet.UserInfo!);
        }

        protected override bool SerializeInternal(RegisterResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            serializer.WriteGuid(packet.SessionId!.Value);
            return _userSerializer.TrySerializeUnsafe(packet.UserInfo!, ref serializer);
        }

        protected override RegisterResponsePacket? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new RegisterResponsePacket(responseInfo.ErrorCode);

            var sessionId = deserializer.ReadGuid();

            return _userSerializer.TryDeserialize(ref deserializer, out var userInfo)
                ? new RegisterResponsePacket(userInfo, sessionId)
                : null;
        }
    }
}
