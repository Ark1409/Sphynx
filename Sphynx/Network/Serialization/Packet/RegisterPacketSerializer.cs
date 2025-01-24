// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
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
    public class RegisterRequestPacketSerializer : PacketSerializer<RegisterRequestPacket>
    {
        public override int GetMaxSize(RegisterRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserName) + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override void Serialize(RegisterRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
        }

        protected override RegisterRequestPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            string userName = deserializer.ReadString();
            string password = deserializer.ReadString();

            return new RegisterRequestPacket(userName, password);
        }
    }

    public class ResponseRequestPacketSerializer : ResponsePacketSerializer<RegisterResponsePacket>
    {
        private readonly ITypeSerializer<ISphynxSelfInfo> _userSerializer;

        public ResponseRequestPacketSerializer(ITypeSerializer<ISphynxSelfInfo> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override int GetMaxPacketSizeInternal(RegisterResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<Guid>() + _userSerializer.GetMaxSize(packet.UserInfo!);
        }

        protected override void SerializeInternal(RegisterResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return;

            serializer.WriteGuid(packet.SessionId!.Value);

            if (!_userSerializer.TrySerializeUnsafe(packet.UserInfo!, ref serializer))
            {
                throw new SerializationException(
                    $"Could not serialize user {packet.UserInfo!.UserId} with session id {packet.SessionId}");
            }
        }

        protected override RegisterResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new RegisterResponsePacket(responseInfo.ErrorCode);

            var sessionId = deserializer.ReadGuid();

            if (_userSerializer.TryDeserialize(ref deserializer, out var userInfo))
                return new RegisterResponsePacket(userInfo, sessionId);

            throw new SerializationException($"Could not deserialize user with session id {sessionId}");
        }
    }
}
