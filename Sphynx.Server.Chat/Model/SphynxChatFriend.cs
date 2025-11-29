// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Server.Chat.Persistence.Friend;

namespace Sphynx.Server.Chat.Model
{
    public struct SphynxChatFriend
    {
        public Guid FriendshipId { get; set; }

        public Guid InitiatorId { get; set; }

        public Guid AcceptorId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public static class SphynxChatFriendExtensions
    {
        public static SphynxDbFriend ToRecord(this SphynxChatFriend friend)
        {
            return new SphynxDbFriend
            {
                FriendshipId = friend.FriendshipId,
                InitiatorId = friend.InitiatorId,
                AcceptorId = friend.AcceptorId,
                CreatedAt = friend.CreatedAt
            };
        }

        public static SphynxChatFriend ToDomain(this SphynxDbFriend friend)
        {
            return new SphynxChatFriend
            {
                FriendshipId = friend.FriendshipId,
                InitiatorId = friend.InitiatorId,
                AcceptorId = friend.AcceptorId,
                CreatedAt = friend.CreatedAt
            };
        }
    }
}
