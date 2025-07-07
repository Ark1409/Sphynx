// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.Serialization.Model
{
    public class SphynxUserInfoSerializer : TypeSerializer<SphynxUserInfo>
    {
        public override int GetMaxSize(SphynxUserInfo model)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(model.UserName) +
                   BinarySerializer.MaxSizeOf<SphynxUserStatus>();
        }

        protected override bool Serialize(SphynxUserInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);
            return true;
        }

        protected override SphynxUserInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            string userName = deserializer.ReadString();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new DummySphynxUserInfo { UserId = userId, UserName = userName, UserStatus = userStatus };
        }

        private class DummySphynxUserInfo : SphynxUserInfo
        {
            public SnowflakeId UserId { get; set; }
            public string UserName { get; set; } = null!;
            public SphynxUserStatus UserStatus { get; set; }

            public override int GetHashCode() => UserId.GetHashCode();
            public bool Equals(SphynxUserInfo? other) => other is not null && UserId == other.UserId;
        }
    }

    public class SphynxSelfInfoSerializer : TypeSerializer<SphynxSelfInfo>
    {
        public override int GetMaxSize(SphynxSelfInfo model)
        {
            int userInfoSize = BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(model.UserName) +
                               BinarySerializer.MaxSizeOf<SphynxUserStatus>();

            int selfInfoSize = BinarySerializer.MaxSizeOf(model.Friends) + BinarySerializer.MaxSizeOf(model.Rooms) +
                               BinarySerializer.MaxSizeOf(model.LastReadMessages) +
                               BinarySerializer.MaxSizeOf(model.OutgoingFriendRequests) +
                               BinarySerializer.MaxSizeOf(model.IncomingFriendRequests);

            return userInfoSize + selfInfoSize;
        }

        protected override bool Serialize(SphynxSelfInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);

            serializer.WriteCollection(model.Friends);
            serializer.WriteCollection(model.Rooms);
            serializer.WriteDictionary(model.LastReadMessages);
            serializer.WriteCollection(model.OutgoingFriendRequests);
            serializer.WriteCollection(model.IncomingFriendRequests);
            return true;
        }

        protected override SphynxSelfInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            string userName = deserializer.ReadString();
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
