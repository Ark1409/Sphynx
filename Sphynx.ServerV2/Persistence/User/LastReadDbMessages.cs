// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;

namespace Sphynx.ServerV2.Persistence.User
{
    [BsonIgnoreExtraElements]
    public class LastReadDbMessages : Dictionary<SnowflakeId, SnowflakeId>
    {
        private readonly IDictionary<SnowflakeId, SnowflakeId> _lastReadMessages;

        public LastReadDbMessages() : this(new Dictionary<SnowflakeId, SnowflakeId>())
        {
        }

        public LastReadDbMessages(IDictionary<SnowflakeId, SnowflakeId> lastReadMessages)
        {
            _lastReadMessages = lastReadMessages;
        }

        public void SetLastMessage(SnowflakeId roomId, SnowflakeId msgId) => this[roomId] = msgId;
        public bool RemoveRoom(SnowflakeId roomId) => Remove(roomId);
        public SnowflakeId GetLastMessage(SnowflakeId roomId) => this[roomId];
        public bool TryGetLastMessage(SnowflakeId roomId, out SnowflakeId msgId) => TryGetValue(roomId, out msgId);
    }
}
