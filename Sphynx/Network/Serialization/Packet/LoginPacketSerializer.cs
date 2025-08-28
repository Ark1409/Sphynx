// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    /// <remarks>
    /// Does not inherit <see cref="RequestSerializer{T}"/> in order to save bytes (since user and session
    /// id will always be zero).
    /// </remarks>
    public class LoginRequestPacketSerializer : PacketSerializer<LoginRequest>
    {
        public override void Serialize(LoginRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
        }

        public override LoginRequest Deserialize(ref BinaryDeserializer deserializer)
        {
            string userName = deserializer.ReadString()!;
            string password = deserializer.ReadString()!;

            return new LoginRequest(userName, password);
        }
    }

    public class LoginResponseSerializer : ResponseSerializer<LoginResponse>
    {
        private readonly ITypeSerializer<SphynxSelfInfo> _userSerializer;

        public LoginResponseSerializer(ITypeSerializer<SphynxSelfInfo> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override void SerializeInternal(LoginResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            serializer.WriteGuid(packet.SessionId.GetValueOrDefault());
            _userSerializer.Serialize(packet.UserInfo!, ref serializer);
        }

        protected override LoginResponse? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new LoginResponse(responseInfo.ErrorInfo);

            var sessionId = deserializer.ReadGuid();
            var userInfo = _userSerializer.Deserialize(ref deserializer)!;

            return new LoginResponse(userInfo, sessionId);
        }
    }

    public class LoginBroadcastSerializer : PacketSerializer<LoginBroadcast>
    {
        public override void Serialize(LoginBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.UserId);
            serializer.WriteEnum(packet.UserStatus);
        }

        public override LoginBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadGuid();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new LoginBroadcast(userId, userStatus);
        }
    }
}
