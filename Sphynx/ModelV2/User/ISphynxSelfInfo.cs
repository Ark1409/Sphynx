// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.User
{
    /// <summary>
    /// A type which holds information about the current Sphynx user.
    /// </summary>
    public interface ISphynxSelfInfo : ISphynxUserInfo
    {
        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        ISet<SnowflakeId> Friends { get; set; }

        /// <summary>
        /// Room IDs of chat rooms which this user is in (including DMs).
        /// </summary>
        ISet<SnowflakeId> Rooms { get; set; }

        /// <summary>
        /// Rooms which contain pending (unread) messages, along with the message IDs of said messages.
        /// </summary>
        IList<PendingRoomMessageInfo> UnreadMessages { get; set; }

        /// <summary>
        /// The user IDs of outgoing friend requests sent by this user.
        /// </summary>
        ISet<SnowflakeId> OutgoingFriendRequests { get; set; }

        /// <summary>
        /// The user IDs of incoming friend requests sent to this user.
        /// </summary>
        ISet<SnowflakeId> IncomingFriendRequests { get; set; }
    }
}
