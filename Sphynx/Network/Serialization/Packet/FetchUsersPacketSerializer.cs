// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class FetchUsersRequestSerializer : RequestSerializer<FetchUsersRequest>
    {
        protected override void SerializeRequest(FetchUsersRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteCollection(packet.UserIds);
        }

        protected override FetchUsersRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var userIds = deserializer.ReadArray<Guid>();
            return new FetchUsersRequest(requestInfo.SessionId, userIds)
            {
                RequestTag = requestInfo.RequestTag
            };
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

        protected override void SerializeResponse(FetchUsersResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize users on success
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            _userSerializer.Serialize(packet.Users!, ref serializer);
        }

        protected override FetchUsersResponse? DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
            {
                return new FetchUsersResponse(responseInfo.ErrorInfo)
                {
                    RequestTag = responseInfo.RequestTag
                };
            }

            var users = _userSerializer.Deserialize(ref deserializer)!;

            return new FetchUsersResponse(users)
            {
                RequestTag = responseInfo.RequestTag
            };
        }
    }
}
