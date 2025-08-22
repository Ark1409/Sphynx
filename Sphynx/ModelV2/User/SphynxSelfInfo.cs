// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.User
{
    /// <summary>
    /// A type which holds information about the current Sphynx user.
    /// </summary>
    public class SphynxSelfInfo : SphynxUserInfo, IEquatable<SphynxSelfInfo?>
    {
        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        public ISet<Guid> Friends { get; set; } = null!;

        /// <summary>
        /// Room IDs of chat rooms which this user is in (including DMs).
        /// </summary>
        public ISet<Guid> Rooms { get; set; } = null!;

        /// <summary>
        /// Collection of the last read message IDs for the messages in the rooms that the user is a part of.
        /// </summary>
        public LastReadMessageInfo LastReadMessages { get; set; } = null!;

        /// <summary>
        /// The user IDs of outgoing friend requests sent by this user.
        /// </summary>
        public ISet<Guid> OutgoingFriendRequests { get; set; } = null!;

        /// <summary>
        /// The user IDs of incoming friend requests sent to this user.
        /// </summary>
        public ISet<Guid> IncomingFriendRequests { get; set; } = null!;

        public SphynxSelfInfo()
        {
        }

        public SphynxSelfInfo(Guid userId, string userName, SphynxUserStatus userStatus) : base(userId, userName, userStatus)
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
