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
        public override int GetMaxSize(LoginRequest packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserName) + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override bool Serialize(LoginRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.UserName);
            serializer.WriteString(packet.Password);
            return true;
        }

        protected override LoginRequest Deserialize(ref BinaryDeserializer deserializer)
        {
            string userName = deserializer.ReadString();
            string password = deserializer.ReadString();

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

        protected override int GetMaxSizeInternal(LoginResponse packet)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<Guid>() + _userSerializer.GetMaxSize(packet.UserInfo!);
        }

        protected override bool SerializeInternal(LoginResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize user info when authentication is successful
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return true;

            // TODO: Default?
            serializer.WriteString(packet.AccessToken);
            serializer.WriteGuid(packet.RefreshToken.GetValueOrDefault());
            serializer.WriteDateTimeOffset(packet.AccessTokenExpiry.GetValueOrDefault());

            return _userSerializer.TrySerializeUnsafe(packet.UserInfo!, ref serializer);
        }

        protected override LoginResponse? DeserializeInternal(ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new LoginResponse(responseInfo.ErrorInfo);

            string accessToken = deserializer.ReadString();
            var refreshToken = deserializer.ReadGuid();
            var accessTokenExpiry = deserializer.ReadDateTimeOffset();

            return _userSerializer.TryDeserialize(ref deserializer, out var userInfo)
                ? new LoginResponse(userInfo, accessToken, refreshToken, accessTokenExpiry)
                : null;
        }
    }

    public class LoginBroadcastSerializer : PacketSerializer<LoginBroadcast>
    {
        public override int GetMaxSize(LoginBroadcast packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SphynxUserStatus>();
        }

        protected override bool Serialize(LoginBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            serializer.WriteEnum(packet.UserStatus);
            return true;
        }

        protected override LoginBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new LoginBroadcast(userId, userStatus);
        }
    }
}
