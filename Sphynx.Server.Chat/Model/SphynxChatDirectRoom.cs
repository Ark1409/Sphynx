using Sphynx.Model.Room;
using Sphynx.Server.Chat.Persistence.Room;

namespace Sphynx.Server.Chat.Model
{
    public class SphynxChatDirectRoom : SphynxChatRoom, IEquatable<SphynxChatDirectRoom>
    {
        public override SphynxRoomType RoomType => SphynxRoomType.DIRECT_MSG;

        public Guid UserA { get; set; }
        public Guid UserB { get; set; }

        public bool Equals(SphynxChatDirectRoom? other) => base.Equals(other) && UserA == other?.UserA && UserB == other?.UserB;
    }

    public static class SphynxChatDirectRoomExtensions
    {
        public static SphynxDirectRoomInfo ToDto(this SphynxChatDirectRoom room)
        {
            return new SphynxDirectRoomInfo
            {
                RoomId = room.RoomId,
                UserA = room.UserA,
                UserB = room.UserB,
                CreatedAt = room.CreatedAt,
            };
        }

        public static SphynxDbDirectRoom ToRecord(this SphynxChatDirectRoom room)
        {
            return new SphynxDbDirectRoom
            {
                RoomId = room.RoomId,
                UserA = room.UserA,
                UserB = room.UserB,
                CreatedAt = room.CreatedAt
            };
        }

        public static SphynxChatDirectRoom ToDomain(this SphynxDbDirectRoom room)
        {
            return new SphynxChatDirectRoom
            {
                RoomId = room.RoomId,
                UserA = room.UserA,
                UserB = room.UserB,
                CreatedAt = room.CreatedAt
            };
        }
    }
}
