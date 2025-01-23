// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.Serialization.Model
{
    public class DirectRoomInfoSerializer : ModelSerializer<IDirectChatRoomInfo>
    {
        public override int GetMaxSize(IDirectChatRoomInfo model)
        {
            int roomSize = BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<ChatRoomType>() +
                           BinarySerializer.MaxSizeOf(model.Name) + BinarySerializer.MaxSizeOf(model.Users);

            return roomSize;
        }

        protected override void Serialize(IDirectChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.RoomId);
            serializer.WriteEnum(model.RoomType);
            serializer.WriteString(model.Name);
            serializer.WriteCollection(model.Users);
        }

        protected override IDirectChatRoomInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            string name = deserializer.ReadString();
            var users = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();

            return new DummyDirectChatRoomInfo { RoomId = roomId, RoomType = roomType, Name = name, Users = users };
        }

        private class DummyDirectChatRoomInfo : IDirectChatRoomInfo
        {
            public SnowflakeId RoomId { get; set; }
            public ChatRoomType RoomType { get; set; }
            public string Name { get; set; } = null!;
            public ISet<SnowflakeId> Users { get; set; } = null!;

            public bool Equals(IChatRoomInfo? other) => other is IDirectChatRoomInfo direct && Equals(direct);
            public bool Equals(IDirectChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
            public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), RoomType.GetHashCode());
        }
    }

    public class GroupRoomInfoSerializer : ModelSerializer<IGroupChatRoomInfo>
    {
        public override int GetMaxSize(IGroupChatRoomInfo model)
        {
            int roomSize = BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<ChatRoomType>() +
                           BinarySerializer.MaxSizeOf(model.Name) + BinarySerializer.MaxSizeOf(model.Users);
            int groupSize = BinarySerializer.MaxSizeOf<bool>() + BinarySerializer.MaxSizeOf<SnowflakeId>();

            return roomSize + groupSize;
        }

        protected override void Serialize(IGroupChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.RoomId);
            serializer.WriteEnum(model.RoomType);
            serializer.WriteString(model.Name);
            serializer.WriteCollection(model.Users);

            serializer.WriteBool(model.Public);
            serializer.WriteSnowflakeId(model.OwnerId);
        }

        protected override IGroupChatRoomInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            string name = deserializer.ReadString();
            var users = deserializer.ReadCollection<SnowflakeId, HashSet<SnowflakeId>>();

            bool isPublic = deserializer.ReadBool();
            var ownerId = deserializer.ReadSnowflakeId();

            return new DummyGroupChatRoomInfo
            {
                RoomId = roomId,
                RoomType = roomType,
                Name = name,
                Users = users,
                Public = isPublic,
                OwnerId = ownerId
            };
        }

        private class DummyGroupChatRoomInfo : IGroupChatRoomInfo
        {
            public SnowflakeId RoomId { get; set; }
            public ChatRoomType RoomType { get; set; }
            public string Name { get; set; } = null!;
            public ISet<SnowflakeId> Users { get; set; } = null!;
            public bool Public { get; set; }
            public SnowflakeId OwnerId { get; set; }

            public bool Equals(IChatRoomInfo? other) => other is IGroupChatRoomInfo direct && Equals(direct);
            public bool Equals(IGroupChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
            public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), RoomType.GetHashCode());
        }
    }
}
