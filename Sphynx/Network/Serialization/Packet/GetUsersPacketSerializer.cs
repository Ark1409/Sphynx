// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class GetUsersRequestPacketSerializer : RequestPacketSerializer<GetUsersRequestPacket>
    {
        protected override int GetMaxSizeInternal(GetUsersRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserIds);
        }

        protected override bool SerializeInternal(GetUsersRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteCollection(packet.UserIds);
            return true;
        }

        protected override GetUsersRequestPacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestPacketInfo requestInfo)
        {
            var userIds = deserializer.ReadArray<SnowflakeId>();
            return new GetUsersRequestPacket(requestInfo.UserId, requestInfo.SessionId, userIds);
        }
    }

    public class GetUsersResponsePacketSerializer : ResponsePacketSerializer<GetUsersResponsePacket>
    {
        private readonly ITypeSerializer<ISphynxUserInfo[]> _userSerializer;

        public GetUsersResponsePacketSerializer(ITypeSerializer<ISphynxUserInfo> userSerializer)
            : this(new ArraySerializer<ISphynxUserInfo>(userSerializer))
        {
        }

        public GetUsersResponsePacketSerializer(ITypeSerializer<ISphynxUserInfo[]> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override int GetMaxSizeInternal(GetUsersResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return _userSerializer.GetMaxSize(packet.Users!);
        }

        protected override bool SerializeInternal(GetUsersResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only serialize users on success
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            return _userSerializer.TrySerializeUnsafe(packet.Users!, ref serializer);
        }

        protected override GetUsersResponsePacket? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new GetUsersResponsePacket(responseInfo.ErrorCode);

            return _userSerializer.TryDeserialize(ref deserializer, out var users)
                ? new GetUsersResponsePacket(users)
                : null;
        }
    }
}
