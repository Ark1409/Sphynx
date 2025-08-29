// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;

namespace Sphynx.ServerV2.Persistence.User
{
    [BsonIgnoreExtraElements]
    public class LastReadDbMessages : Dictionary<Guid, SnowflakeId>
    {
        public LastReadDbMessages()
        {
        }

        public LastReadDbMessages(IDictionary<Guid, SnowflakeId> lastReadMessages)
        {
            foreach(var kvp in lastReadMessages)
                Add(kvp.Key, kvp.Value);
        }

        public void SetLastMessage(Guid roomId, SnowflakeId msgId) => this[roomId] = msgId;
        public bool RemoveRoom(Guid roomId) => Remove(roomId);
        public SnowflakeId GetLastMessage(Guid roomId) => this[roomId];
        public bool TryGetLastMessage(Guid roomId, out SnowflakeId msgId) => TryGetValue(roomId, out msgId);
    }
}
