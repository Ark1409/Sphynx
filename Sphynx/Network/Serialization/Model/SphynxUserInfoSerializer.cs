// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

using System.Collections;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.Serialization.Model
{
    public class SphynxUserInfoSerializer : ModelSerializer<ISphynxUserInfo>
    {
        public override int GetMaxSize(ISphynxUserInfo model)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(model.UserName) +
                   BinarySerializer.MaxSizeOf<SphynxUserStatus>();
        }

        protected override void Serialize(ISphynxUserInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);
        }

        protected override ISphynxUserInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            string userName = deserializer.ReadString();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            return new DummySphynxUserInfo { UserId = userId, UserName = userName, UserStatus = userStatus };
        }

        private class DummySphynxUserInfo : ISphynxUserInfo
        {
            public SnowflakeId UserId { get; set; }
            public string UserName { get; set; }
            public SphynxUserStatus UserStatus { get; set; }

            public override int GetHashCode() => UserId.GetHashCode();
            public bool Equals(ISphynxUserInfo? other) => other is not null && UserId == other.UserId;
        }
    }

    public class SphynxSelfInfoSerializer : ModelSerializer<ISphynxSelfInfo>
    {
        public override int GetMaxSize(ISphynxSelfInfo model)
        {
            int userInfoSize = BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(model.UserName) +
                               BinarySerializer.MaxSizeOf<SphynxUserStatus>();

            int selfInfoSize = BinarySerializer.MaxSizeOf(model.Friends) + BinarySerializer.MaxSizeOf(model.Rooms) +
                               BinarySerializer.MaxSizeOf(model.LastReadMessages) +
                               BinarySerializer.MaxSizeOf(model.OutgoingFriendRequests) +
                               BinarySerializer.MaxSizeOf(model.IncomingFriendRequests);

            return userInfoSize + selfInfoSize;
        }

        protected override void Serialize(ISphynxSelfInfo model, ref BinarySerializer serializer)
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

        protected override ISphynxSelfInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            string userName = deserializer.ReadString();
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();

            var friends = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();
            var rooms = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();

            var lastReadMessages = deserializer.ReadDictionary<SnowflakeId, SnowflakeId>();
            var lastReadMessagesInfo = new DummyLastReadMessageInfo(lastReadMessages);

            var outgoingFriendReqs = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();
            var incomingFriendReqs = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();

            return new DummySphynxSelfInfo
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

        private class DummySphynxSelfInfo : ISphynxSelfInfo
        {
            public SnowflakeId UserId { get; set; }
            public string UserName { get; set; }
            public SphynxUserStatus UserStatus { get; set; }
            public ISet<SnowflakeId> Friends { get; set; }
            public ISet<SnowflakeId> Rooms { get; set; }
            public ILastReadMessageInfo LastReadMessages { get; set; }
            public ISet<SnowflakeId> OutgoingFriendRequests { get; set; }
            public ISet<SnowflakeId> IncomingFriendRequests { get; set; }

            public override int GetHashCode() => UserId.GetHashCode();
            public bool Equals(ISphynxUserInfo? other) => other is not null && UserId == other.UserId;
        }

        private class DummyLastReadMessageInfo : ILastReadMessageInfo
        {
            private readonly IDictionary<SnowflakeId, SnowflakeId> _lastReadMessages;

            public DummyLastReadMessageInfo() : this(new Dictionary<SnowflakeId, SnowflakeId>())
            {
            }

            public DummyLastReadMessageInfo(IDictionary<SnowflakeId, SnowflakeId> lastReadMessages)
            {
                _lastReadMessages = lastReadMessages;
            }

            public IEnumerator<KeyValuePair<SnowflakeId, SnowflakeId>> GetEnumerator() =>
                _lastReadMessages.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _lastReadMessages.GetEnumerator();

            public void Add(KeyValuePair<SnowflakeId, SnowflakeId> item) => _lastReadMessages.Add(item);

            public void Clear() => _lastReadMessages.Clear();

            public bool Contains(KeyValuePair<SnowflakeId, SnowflakeId> item) => _lastReadMessages.Contains(item);

            public void CopyTo(KeyValuePair<SnowflakeId, SnowflakeId>[] array, int arrayIndex) =>
                _lastReadMessages.CopyTo(array, arrayIndex);

            public bool Remove(KeyValuePair<SnowflakeId, SnowflakeId> item) => _lastReadMessages.Remove(item);

            public int Count => _lastReadMessages.Count;

            public bool IsReadOnly => _lastReadMessages.IsReadOnly;

            public void Add(SnowflakeId key, SnowflakeId value) => _lastReadMessages.Add(key, value);

            public bool ContainsKey(SnowflakeId key) => _lastReadMessages.ContainsKey(key);

            public bool Remove(SnowflakeId key) => _lastReadMessages.Remove(key);

            public bool TryGetValue(SnowflakeId key, out SnowflakeId value) =>
                _lastReadMessages.TryGetValue(key, out value);

            public SnowflakeId this[SnowflakeId key]
            {
                get => _lastReadMessages[key];
                set => _lastReadMessages[key] = value;
            }

            public ICollection<SnowflakeId> Keys => _lastReadMessages.Keys;

            public ICollection<SnowflakeId> Values => _lastReadMessages.Values;
        }
    }
}
