// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.Serialization.Model
{
    public class SphynxUserInfoSerializer : TypeSerializer<SphynxUserInfo>
    {
        public override void Serialize(SphynxUserInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);
        }

        public override SphynxUserInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadGuid();
            string userName = deserializer.ReadString()!;
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new SphynxUserInfo { UserId = userId, UserName = userName, UserStatus = userStatus };
        }
    }

    public class SphynxSelfInfoSerializer : TypeSerializer<SphynxSelfInfo>
    {
        public override void Serialize(SphynxSelfInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(model.UserId);
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
            var userId = deserializer.ReadGuid();
            string userName = deserializer.ReadString()!;
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            var friends = deserializer.ReadCollection<Guid, HashSet<Guid>>();
            var rooms = deserializer.ReadCollection<Guid, HashSet<Guid>>();
            var lastReadMessages = deserializer.ReadDictionary<Guid, SnowflakeId>();
            var lastReadMessagesInfo = new LastReadMessageInfo(lastReadMessages);
            var outgoingFriendReqs = deserializer.ReadCollection<Guid, HashSet<Guid>>();
            var incomingFriendReqs = deserializer.ReadCollection<Guid, HashSet<Guid>>();

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
