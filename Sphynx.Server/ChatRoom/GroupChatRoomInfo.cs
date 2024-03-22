using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.ChatRoom;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    public sealed class GroupChatRoomInfo : ChatRoomInfo, IGroupChatRoomInfo
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
        /// Whether this room is public.
        /// </summary>
        [BsonElement("is_public")]
        public bool Public { get; }
        
        /// <summary>
        /// Creates new <see cref="ChatRoomType.GROUP"/> room info.
        /// </summary>
        /// <inheritdoc cref="ChatRoomInfo(Guid, ChatRoomType, string, IEnumerable{Guid}?)"/>
        /// <param name="public">Whether this room is public.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
        public GroupChatRoomInfo(Guid roomId, ChatRoomType roomType, string name, bool @public, IEnumerable<Guid>? userIds = null)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(roomId, roomType, name, userIds)
        {
            Public = @public;
        }
    }
}
