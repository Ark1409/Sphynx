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
        protected override void SerializeInternal(FetchUsersRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteCollection(packet.UserIds);
        }

        protected override FetchUsersRequest DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
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

        protected override void SerializeInternal(FetchUsersResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize users on success
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            _userSerializer.Serialize(packet.Users!, ref serializer);
        }

        protected override FetchUsersResponse? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new FetchUsersResponse(responseInfo.ErrorInfo);

            var users = _userSerializer.Deserialize(ref deserializer)!;

            return new FetchUsersResponse(users);
        }
    }
}
