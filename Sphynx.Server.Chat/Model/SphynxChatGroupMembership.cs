using Sphynx.Server.Chat.Persistence.Room;

namespace Sphynx.Server.Chat.Model
{
    public class SphynxChatGroupMembership : IEquatable<SphynxChatGroupMembership>
    {
        public Guid MembershipId { get; set; }

        public Guid RoomId { get; set; }
        public Guid MemberId { get; set; }
        public bool IsOwner { get; set; }

        public DateTimeOffset JoinedAt { get; set; }

        public bool Equals(SphynxChatGroupMembership? other) =>
            MembershipId == other?.MembershipId && RoomId == other?.RoomId && MemberId == other?.MemberId;
    }

    public static class SphynxChatGroupMembershipExtensions
    {
        public static SphynxDbGroupMembership ToRecord(this SphynxChatGroupMembership mem)
        {
            return new SphynxDbGroupMembership
            {
                MembershipId = mem.MembershipId,
                RoomId = mem.RoomId,
                MemberId = mem.MemberId,
                IsOwner = mem.IsOwner,
                JoinedAt = mem.JoinedAt,
            };
        }

        public static SphynxChatGroupMembership ToDomain(this SphynxDbGroupMembership mem)
        {
            return new SphynxChatGroupMembership
            {
                MembershipId = mem.MembershipId,
                RoomId = mem.RoomId,
                MemberId = mem.MemberId,
                IsOwner = mem.IsOwner,
                JoinedAt = mem.JoinedAt,
            };
        }
    }
}
