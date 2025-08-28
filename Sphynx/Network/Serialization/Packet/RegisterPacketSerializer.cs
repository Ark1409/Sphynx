// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    /// <remarks>
    /// Does not inherit <see cref="RequestSerializer{T}"/> in order to save bytes (since user and session
    /// id will always be zero).
    /// </remarks>
    public class RegisterRequestSerializer : PacketSerializer<RegisterRequest>
    {
        public override void Serialize(RegisterRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
        }

        public override RegisterRequest Deserialize(ref BinaryDeserializer deserializer)
        {
            string userName = deserializer.ReadString()!;
            string password = deserializer.ReadString()!;

            return new RegisterRequest(userName, password);
        }
    }

    public class RegisterResponseSerializer : ResponseSerializer<RegisterResponse>
    {
        private readonly ITypeSerializer<SphynxSelfInfo> _userSerializer;

        public RegisterResponseSerializer(ITypeSerializer<SphynxSelfInfo> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override void SerializeInternal(RegisterResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            serializer.WriteGuid(packet.SessionId.GetValueOrDefault());
            _userSerializer.Serialize(packet.UserInfo!, ref serializer);
        }

        protected override RegisterResponse? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new RegisterResponse(responseInfo.ErrorInfo);

            var sessionId = deserializer.ReadGuid();
            var userInfo = _userSerializer.Deserialize(ref deserializer)!;

            return new RegisterResponse(userInfo, sessionId);
        }
    }
}
