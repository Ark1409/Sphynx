// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.User;
using Sphynx.Server.Chat.Persistence.Friend;

namespace Sphynx.Server.Chat.Model
{
    public record struct SphynxChatFriendRequest
    {
        public Guid RequestId { get; set; }

        public Guid InitiatorId { get; set; }

        public Guid OtherId { get; set; }

        public DateTimeOffset SentAt { get; set; }
    }

    public static class SphynxChatFriendRequestExtensions
    {
        public static SphynxChatFriendRequest ToDomain(this SphynxDbFriendRequest friendReq)
        {
            return new SphynxChatFriendRequest
            {
                RequestId = friendReq.RequestId,
                InitiatorId = friendReq.InitiatorId,
                OtherId = friendReq.OtherId,
                SentAt = friendReq.SentAt
            };
        }

        public static SphynxFriendRequest ToDto(this SphynxChatFriendRequest friendReq)
        {
            return new SphynxFriendRequest
            {
                InitiatorId = friendReq.InitiatorId,
                OtherId = friendReq.OtherId,
                SentAt = friendReq.SentAt
            };
        }

        public static SphynxDbFriendRequest ToRecord(this SphynxChatFriendRequest friendReq)
        {
            return new SphynxDbFriendRequest
            {
                RequestId = friendReq.RequestId,
                InitiatorId = friendReq.InitiatorId,
                OtherId = friendReq.OtherId,
                SentAt = friendReq.SentAt
            };
        }
    }
}
