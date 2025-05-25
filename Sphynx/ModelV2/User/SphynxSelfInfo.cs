// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
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
        public ISet<SnowflakeId> Friends { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        /// <summary>
        /// Room IDs of chat rooms which this user is in (including DMs).
        /// </summary>
        public ISet<SnowflakeId> Rooms { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        /// <summary>
        /// Collection of the last read message IDs for the messages in the rooms that the user is a part of.
        /// </summary>
        public LastReadMessageInfo LastReadMessages { get; set; } = new LastReadMessageInfo();

        /// <summary>
        /// The user IDs of outgoing friend requests sent by this user.
        /// </summary>
        public ISet<SnowflakeId> OutgoingFriendRequests { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        /// <summary>
        /// The user IDs of incoming friend requests sent to this user.
        /// </summary>
        public ISet<SnowflakeId> IncomingFriendRequests { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        public SphynxSelfInfo()
        {
        }

        public SphynxSelfInfo(SnowflakeId userId, string userName, SphynxUserStatus userStatus) : base(userId, userName, userStatus)
        {
        }

        /// <inheritdoc/>
        public bool Equals(SphynxSelfInfo? other) => UserId == other?.UserId;
    }

    public class LastReadMessageInfo : Dictionary<SnowflakeId, SnowflakeId>
    {
        public LastReadMessageInfo()
        {
        }

        public LastReadMessageInfo(IDictionary<SnowflakeId, SnowflakeId> lastReadMessages) : base(lastReadMessages)
        {
        }

        public LastReadMessageInfo(IEnumerable<KeyValuePair<SnowflakeId, SnowflakeId>> lastReadMessages) : base(lastReadMessages)
        {
        }

        public void SetLastMessage(SnowflakeId roomId, SnowflakeId msgId) => this[roomId] = msgId;
        public bool RemoveRoom(SnowflakeId roomId) => Remove(roomId);
        public SnowflakeId GetLastMessage(SnowflakeId roomId) => this[roomId];
        public bool TryGetLastMessage(SnowflakeId roomId, out SnowflakeId msgId) => TryGetValue(roomId, out msgId);
    }
}
