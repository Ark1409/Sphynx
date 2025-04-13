// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.Serialization.Model
{
    public abstract class ChatRoomInfoSerializer<TRoom> : ModelSerializer<TRoom>
        where TRoom : IChatRoomInfo
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

    public sealed class ChatRoomInfoSerializer : ChatRoomInfoSerializer<IChatRoomInfo>
    {
        private readonly Dictionary<ChatRoomType, ChatRoomInfoSerializer<IChatRoomInfo>> _serializers = new();

        public ChatRoomInfoSerializer()
        {
            AddSerializer(ChatRoomType.DIRECT_MSG, new DirectChatRoomInfoSerializer());
            AddSerializer(ChatRoomType.GROUP, new GroupChatRoomInfoSerializer());
        }

        protected internal override int GetMaxRoomSize(IChatRoomInfo room)
        {
            return _serializers.TryGetValue(room.RoomType, out var serializer)
                ? serializer.GetMaxRoomSize(room)
                : 0;
        }

        protected internal override bool SerializeRoom(IChatRoomInfo room, ref BinarySerializer serializer)
        {
            if (_serializers.TryGetValue(room.RoomType, out var roomSerializer))
            {
                roomSerializer.SerializeRoom(room, ref serializer);
            }

            // We can allow it, but it may not deserialize
            return true;
        }

        protected internal override IChatRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo)
        {
            if (_serializers.TryGetValue(roomInfo.RoomType, out var roomDeserializer))
            {
                return roomDeserializer.DeserializeRoom(ref deserializer, roomInfo);
            }

            return null;
        }

        public ChatRoomInfoSerializer AddSerializer<T>(ChatRoomType roomType, ChatRoomInfoSerializer<T> serializer)
            where T : IChatRoomInfo
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

        private class SerializerAdapter<T> : ChatRoomInfoSerializer<IChatRoomInfo>
            where T : IChatRoomInfo
        {
            internal ChatRoomInfoSerializer<T> InnerSerializer { get; set; }

            public SerializerAdapter(ChatRoomInfoSerializer<T> innerSerializer)
            {
                InnerSerializer = innerSerializer;
            }

            protected internal override int GetMaxRoomSize(IChatRoomInfo packet)
            {
                return InnerSerializer.GetMaxRoomSize((T)packet);
            }

            protected internal override bool SerializeRoom(IChatRoomInfo packet, ref BinarySerializer serializer)
            {
                return InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override IChatRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, roomInfo);
            }
        }
    }

    public class DirectChatRoomInfoSerializer : ChatRoomInfoSerializer<IDirectChatRoomInfo>
    {
        protected internal override int GetMaxRoomSize(IDirectChatRoomInfo model)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected internal override bool SerializeRoom(IDirectChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserOne);
            serializer.WriteSnowflakeId(model.UserTwo);
            return true;
        }

        protected internal override IDirectChatRoomInfo DeserializeRoom(
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

        private class DummyDirectChatRoomInfo : IDirectChatRoomInfo
        {
            public SnowflakeId RoomId { get; set; }
            public ChatRoomType RoomType { get; set; }
            public string Name { get; set; } = null!;
            public SnowflakeId UserOne { get; set; }
            public SnowflakeId UserTwo { get; set; }

            public bool Equals(IChatRoomInfo? other) => other is IDirectChatRoomInfo direct && Equals(direct);
            public bool Equals(IDirectChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
            public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), RoomType.GetHashCode());
        }
    }

    public class GroupChatRoomInfoSerializer : ChatRoomInfoSerializer<IGroupChatRoomInfo>
    {
        protected internal override int GetMaxRoomSize(IGroupChatRoomInfo model)
        {
            return BinarySerializer.MaxSizeOf<bool>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected internal override bool SerializeRoom(IGroupChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteBool(model.IsPublic);
            serializer.WriteSnowflakeId(model.OwnerId);
            return true;
        }

        protected internal override IGroupChatRoomInfo DeserializeRoom(ref BinaryDeserializer deserializer, RoomInfo roomInfo)
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

        private class DummyGroupChatRoomInfo : IGroupChatRoomInfo
        {
            public SnowflakeId RoomId { get; set; }
            public ChatRoomType RoomType { get; set; }
            public string Name { get; set; } = null!;
            public bool IsPublic { get; set; }
            public SnowflakeId OwnerId { get; set; }

            public bool Equals(IChatRoomInfo? other) => other is IGroupChatRoomInfo direct && Equals(direct);
            public bool Equals(IGroupChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
            public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), RoomType.GetHashCode());
        }
    }
}
