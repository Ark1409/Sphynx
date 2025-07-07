// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.Serialization.Model
{
    public abstract class ChatRoomInfoSerializer<TRoom> : TypeSerializer<TRoom>
        where TRoom : ChatRoomInfo
    {
        public sealed override int GetMaxSize(TRoom packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<ChatRoomType>() +
                   BinarySerializer.MaxSizeOf(packet.Name) + GetMaxRoomSize(packet);
        }

        protected internal abstract int GetMaxRoomSize(TRoom packet);

        protected sealed override bool Serialize(TRoom packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteEnum(packet.RoomType);
            serializer.WriteString(packet.Name);

            return SerializeRoom(packet, ref serializer);
        }

        protected internal abstract bool SerializeRoom(TRoom packet, ref BinarySerializer serializer);

        protected sealed override TRoom? Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            string roomName = deserializer.ReadString();

            var roomInfo = new RoomInfo { RoomId = roomId, RoomType = roomType, Name = roomName };

            return DeserializeRoom(ref deserializer, roomInfo);
        }

        protected internal abstract TRoom? DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo);
    }

    public readonly struct RoomInfo
    {
        public SnowflakeId RoomId { get; init; }
        public ChatRoomType RoomType { get; init; }
        public string Name { get; init; }
    }

    public sealed class ChatRoomInfoSerializer : ChatRoomInfoSerializer<ChatRoomInfo>
    {
        private readonly Dictionary<ChatRoomType, ChatRoomInfoSerializer<ChatRoomInfo>> _serializers = new();

        public ChatRoomInfoSerializer()
        {
            AddSerializer(ChatRoomType.DIRECT_MSG, new DirectChatRoomInfoSerializer());
            AddSerializer(ChatRoomType.GROUP, new GroupChatRoomInfoSerializer());
        }

        protected internal override int GetMaxRoomSize(ChatRoomInfo room)
        {
            return _serializers.TryGetValue(room.RoomType, out var serializer)
                ? serializer.GetMaxRoomSize(room)
                : 0;
        }

        protected internal override bool SerializeRoom(ChatRoomInfo room, ref BinarySerializer serializer)
        {
            if (_serializers.TryGetValue(room.RoomType, out var roomSerializer))
            {
                roomSerializer.SerializeRoom(room, ref serializer);
            }

            // We can allow it, but it may not deserialize
            return true;
        }

        protected internal override ChatRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo)
        {
            if (_serializers.TryGetValue(roomInfo.RoomType, out var roomDeserializer))
            {
                return roomDeserializer.DeserializeRoom(ref deserializer, roomInfo);
            }

            return null;
        }

        public ChatRoomInfoSerializer AddSerializer<T>(ChatRoomType roomType, ChatRoomInfoSerializer<T> serializer)
            where T : ChatRoomInfo
        {
            ref var existingAdapter = ref CollectionsMarshal.GetValueRefOrAddDefault(_serializers, roomType, out bool exists);

            // Avoid extra allocations
            if (exists && existingAdapter is SerializerAdapter<T> adapter)
            {
                adapter.InnerSerializer = serializer;
            }
            else
            {
                existingAdapter = new SerializerAdapter<T>(serializer);
            }

            return this;
        }

        public ChatRoomInfoSerializer RemoveSerializer(ChatRoomType roomType)
        {
            _serializers.Remove(roomType);
            return this;
        }

        private class SerializerAdapter<T> : ChatRoomInfoSerializer<ChatRoomInfo>
            where T : ChatRoomInfo
        {
            internal ChatRoomInfoSerializer<T> InnerSerializer { get; set; }

            public SerializerAdapter(ChatRoomInfoSerializer<T> innerSerializer)
            {
                InnerSerializer = innerSerializer;
            }

            protected internal override int GetMaxRoomSize(ChatRoomInfo packet)
            {
                return InnerSerializer.GetMaxRoomSize((T)packet);
            }

            protected internal override bool SerializeRoom(ChatRoomInfo packet, ref BinarySerializer serializer)
            {
                return InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override ChatRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, roomInfo);
            }
        }
    }

    public class DirectChatRoomInfoSerializer : ChatRoomInfoSerializer<DirectChatRoomInfo>
    {
        protected internal override int GetMaxRoomSize(DirectChatRoomInfo model)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected internal override bool SerializeRoom(DirectChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserOne);
            serializer.WriteSnowflakeId(model.UserTwo);
            return true;
        }

        protected internal override DirectChatRoomInfo DeserializeRoom(
            ref BinaryDeserializer deserializer,
            RoomInfo roomInfo)
        {
            var userOne = deserializer.ReadSnowflakeId();
            var userTwo = deserializer.ReadSnowflakeId();

            return new DummyDirectChatRoomInfo
            {
                RoomId = roomInfo.RoomId,
                RoomType = roomInfo.RoomType,
                Name = roomInfo.Name,
                UserOne = userOne,
                UserTwo = userTwo
            };
        }

        private class DummyDirectChatRoomInfo : DirectChatRoomInfo
        {
            public SnowflakeId RoomId { get; set; }
            public ChatRoomType RoomType { get; set; }
            public string Name { get; set; } = null!;
            public SnowflakeId UserOne { get; set; }
            public SnowflakeId UserTwo { get; set; }

            public bool Equals(ChatRoomInfo? other) => other is DirectChatRoomInfo direct && Equals(direct);
            public bool Equals(DirectChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
            public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), RoomType.GetHashCode());
        }
    }

    public class GroupChatRoomInfoSerializer : ChatRoomInfoSerializer<GroupChatRoomInfo>
    {
        protected internal override int GetMaxRoomSize(GroupChatRoomInfo model)
        {
            return BinarySerializer.MaxSizeOf<bool>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected internal override bool SerializeRoom(GroupChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteBool(model.IsPublic);
            serializer.WriteSnowflakeId(model.OwnerId);
            return true;
        }

        protected internal override GroupChatRoomInfo DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo)
        {
            bool isPublic = deserializer.ReadBool();
            var ownerId = deserializer.ReadSnowflakeId();

            return new DummyGroupChatRoomInfo
            {
                RoomId = roomInfo.RoomId,
                RoomType = roomInfo.RoomType,
                Name = roomInfo.Name,
                IsPublic = isPublic,
                OwnerId = ownerId
            };
        }

        private class DummyGroupChatRoomInfo : GroupChatRoomInfo
        {
            public SnowflakeId RoomId { get; set; }
            public ChatRoomType RoomType { get; set; }
            public string Name { get; set; } = null!;
            public bool IsPublic { get; set; }
            public SnowflakeId OwnerId { get; set; }

            public bool Equals(ChatRoomInfo? other) => other is GroupChatRoomInfo direct && Equals(direct);
            public bool Equals(GroupChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
            public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), RoomType.GetHashCode());
        }
    }
}
