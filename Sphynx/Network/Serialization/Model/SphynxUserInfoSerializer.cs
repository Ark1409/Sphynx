// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.Serialization.Model
{
    public class SphynxUserInfoSerializer : TypeSerializer<SphynxUserInfo>
    {
        public override void Serialize(SphynxUserInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);
        }

        public override SphynxUserInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            string userName = deserializer.ReadString()!;
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new SphynxUserInfo { UserId = userId, UserName = userName, UserStatus = userStatus };
        }
    }

    public class SphynxSelfInfoSerializer : TypeSerializer<SphynxSelfInfo>
    {
        public override void Serialize(SphynxSelfInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);

            serializer.WriteCollection(model.Friends);
            serializer.WriteCollection(model.Rooms);
            serializer.WriteDictionary(model.LastReadMessages);
            serializer.WriteCollection(model.OutgoingFriendRequests);
            serializer.WriteCollection(model.IncomingFriendRequests);
        }

        public override SphynxSelfInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            string userName = deserializer.ReadString()!;
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            var friends = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();
            var rooms = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();

            var lastReadMessages = deserializer.ReadDictionary<SnowflakeId, SnowflakeId>();
            var lastReadMessagesInfo = new LastReadMessageInfo(lastReadMessages);

            var outgoingFriendReqs = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();
            var incomingFriendReqs = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();

            return new SphynxSelfInfo
            {
                UserId = userId,
                UserName = userName,
                UserStatus = userStatus,
                Friends = friends,
                Rooms = rooms,
                LastReadMessages = lastReadMessagesInfo,
                OutgoingFriendRequests = outgoingFriendReqs,
                IncomingFriendRequests = incomingFriendReqs
            };
        }
    }
}
