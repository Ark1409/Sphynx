// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class FetchUsersRequestSerializer : RequestSerializer<FetchUsersRequest>
    {
        protected override int GetMaxSizeInternal(FetchUsersRequest packet)
        {
            return BinarySerializer.MaxSizeOf(packet.UserIds);
        }

        protected override bool SerializeInternal(FetchUsersRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteCollection(packet.UserIds);
            return true;
        }

        protected override FetchUsersRequest DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            var userIds = deserializer.ReadArray<SnowflakeId>();
            return new FetchUsersRequest(requestInfo.AccessToken, userIds);
        }
    }

    public class FetchUsersResponseSerializer : ResponseSerializer<FetchUsersResponse>
    {
        private readonly ITypeSerializer<SphynxUserInfo[]> _userSerializer;

        public FetchUsersResponseSerializer(ITypeSerializer<SphynxUserInfo> userSerializer)
            : this(new ArraySerializer<SphynxUserInfo>(userSerializer))
        {
        }

        public FetchUsersResponseSerializer(ITypeSerializer<SphynxUserInfo[]> userSerializer)
        {
            _userSerializer = userSerializer;
        }

        protected override int GetMaxSizeInternal(FetchUsersResponse packet)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return 0;

            return _userSerializer.GetMaxSize(packet.Users!);
        }

        protected override bool SerializeInternal(FetchUsersResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize users on success
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return true;

            return _userSerializer.TrySerializeUnsafe(packet.Users!, ref serializer);
        }

        protected override FetchUsersResponse? DeserializeInternal(ref BinaryDeserializer deserializer, ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new FetchUsersResponse(responseInfo.ErrorInfo);

            return _userSerializer.TryDeserialize(ref deserializer, out var users)
                ? new FetchUsersResponse(users)
                : null;
        }
    }
}
