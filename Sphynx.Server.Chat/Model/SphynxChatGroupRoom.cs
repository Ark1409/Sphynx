using Sphynx.Model.Room;
using Sphynx.Server.Chat.Persistence.Room;

namespace Sphynx.Server.Chat.Model
{
    public class SphynxChatGroupRoom : SphynxChatRoom, IEquatable<SphynxChatGroupRoom>
    {
        public override SphynxRoomType RoomType => SphynxRoomType.GROUP;

        public string Name { get; set; } = null!;
        public bool IsPublic { get; set; }
        public Guid OwnerId { get; set; }
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }

        public bool Equals(SphynxChatGroupRoom? other) => base.Equals(other) && OwnerId == other.OwnerId;
    }

    public static class SphynxGroupRoomExtensions
    {
        public static SphynxGroupRoomInfo ToDto(this SphynxChatGroupRoom room)
        {
            return new SphynxGroupRoomInfo
            {
                RoomId = room.RoomId,
                Name = room.Name,
                IsPublic = room.IsPublic,
                OwnerId = room.OwnerId,
                Password = room.PasswordHash,
                PasswordSalt = room.PasswordSalt,
                CreatedAt = room.CreatedAt,
            };
        }

        public static SphynxDbGroupRoom ToRecord(this SphynxChatGroupRoom room)
        {
            return new SphynxDbGroupRoom
            {
                RoomId = room.RoomId,
                Name = room.Name,
                IsPublic = room.IsPublic,
                OwnerId = room.OwnerId,
                PasswordHash = room.PasswordHash,
                PasswordSalt = room.PasswordSalt,
                CreatedAt = room.CreatedAt,
            };
        }

        public static SphynxChatGroupRoom ToDomain(this SphynxDbGroupRoom room)
        {
            return new SphynxChatGroupRoom
            {
                RoomId = room.RoomId,
                Name = room.Name,
                IsPublic = room.IsPublic,
                OwnerId = room.OwnerId,
                PasswordHash = room.PasswordHash,
                PasswordSalt = room.PasswordSalt,
                CreatedAt = room.CreatedAt,
            };
        }
    }
}
