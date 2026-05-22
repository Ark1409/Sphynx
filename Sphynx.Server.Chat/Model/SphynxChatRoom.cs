using Sphynx.Model.Room;

namespace Sphynx.Server.Chat.Model
{
    public abstract class SphynxChatRoom : IEquatable<SphynxChatRoom>
    {
        public Guid RoomId { get; set; }

        public abstract SphynxRoomType RoomType { get; }

        public DateTimeOffset CreatedAt { get; set; }

        public SphynxChatRoom()
        {
        }

        public SphynxChatRoom(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxChatRoom? other) => RoomId == other?.RoomId;
    }
}
