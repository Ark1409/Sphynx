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

            serializer.WriteString(packet.AccessToken!);
            serializer.WriteGuid(packet.RefreshToken.GetValueOrDefault());
            serializer.WriteDateTimeOffset(packet.AccessTokenExpiry.GetValueOrDefault());

            _userSerializer.Serialize(packet.UserInfo!, ref serializer);
        }

        protected override LoginResponse? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new LoginResponse(responseInfo.ErrorInfo);

            string accessToken = deserializer.ReadString()!;
            var refreshToken = deserializer.ReadGuid();
            var accessTokenExpiry = deserializer.ReadDateTimeOffset();
            var userInfo = _userSerializer.Deserialize(ref deserializer)!;

            return new LoginResponse(userInfo, accessToken, refreshToken, accessTokenExpiry);
        }
    }

    public class LoginBroadcastSerializer : PacketSerializer<LoginBroadcast>
    {
        public override void Serialize(LoginBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            serializer.WriteEnum(packet.UserStatus);
        }

        public override LoginBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new LoginBroadcast(userId, userStatus);
        }
    }
}
