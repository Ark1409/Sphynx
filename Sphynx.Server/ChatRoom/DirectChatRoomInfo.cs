using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.ChatRoom;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public sealed class DirectChatRoomInfo : ChatRoomInfo, IDirectChatRoomInfo
    {
        /// <inheritdoc/>
        [BsonElement("name")]
        public override string Name { get; internal set; }
        
        /// <inheritdoc/>
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public override Guid RoomId { get; internal set; }
        
        /// <inheritdoc/>
        [BsonIgnore]
        public override Guid Id => RoomId;
        
        /// <inheritdoc/>
        [BsonElement("users")]
        public override ICollection<Guid> Users { get; internal set; }
        
        /// <inheritdoc/>
        [BsonElement("room_type")]
        public override ChatRoomType RoomType { get; internal set; }
        
        /// <summary>
        /// Creates a new <see cref="ChatRoomType.DIRECT_MSG"/>
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="roomType"></param>
        /// <param name="name"></param>
        /// <param name="userIds"></param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DirectChatRoomInfo(Guid roomId, ChatRoomType roomType, string name, IEnumerable<Guid>? userIds = null) : base(roomId, roomType, name, userIds)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }
    }
}
