// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Model.User
{
    public class SphynxSelfInfo : SphynxUserInfo, IEquatable<SphynxSelfInfo?>
    {
        public SphynxSelfInfo()
        {
        }

        public SphynxSelfInfo(Guid userId, string userName, SphynxUserStatus userStatus, DateTimeOffset createdAt, DateTimeOffset lastLogin)
            : base(userId, userName, userStatus, createdAt, lastLogin)
        {
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxSelfInfo? other) => UserId == other?.UserId;
    }

    public class LastReadMessageInfo : Dictionary<Guid, SnowflakeId>
    {
        public LastReadMessageInfo()
        {
        }

        public LastReadMessageInfo(IDictionary<Guid, SnowflakeId> lastReadMessages) : base(lastReadMessages)
        {
        }

        public LastReadMessageInfo(IEnumerable<KeyValuePair<Guid, SnowflakeId>> lastReadMessages) : base(lastReadMessages)
        {
        }

        public void SetLastMessage(Guid roomId, SnowflakeId msgId) => this[roomId] = msgId;
        public bool RemoveRoom(Guid roomId) => Remove(roomId);
        public SnowflakeId GetLastMessage(Guid roomId) => this[roomId];
        public bool TryGetLastMessage(Guid roomId, out SnowflakeId msgId) => TryGetValue(roomId, out msgId);
    }
}
