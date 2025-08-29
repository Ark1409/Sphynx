// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.Serialization.Model
{
    public abstract class ChatRoomInfoSerializer<TRoom> : TypeSerializer<TRoom> where TRoom : ChatRoomInfo
    {
        public sealed override void Serialize(TRoom packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteEnum(packet.RoomType);
            serializer.WriteString(packet.Name);

            SerializeRoom(packet, ref serializer);
        }

        protected internal abstract void SerializeRoom(TRoom packet, ref BinarySerializer serializer);

        public sealed override TRoom? Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            string roomName = deserializer.ReadString()!;

            var roomInfo = new RoomInfo { RoomId = roomId, RoomType = roomType, Name = roomName };

            return DeserializeRoom(ref deserializer, in roomInfo);
        }

        protected internal abstract TRoom? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo);
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


        protected internal override void SerializeRoom(ChatRoomInfo room, ref BinarySerializer serializer)
        {
            if (!_serializers.TryGetValue(room.RoomType, out var roomSerializer))
                throw new SerializationException($"No serializer for room {room} found");

            roomSerializer.SerializeRoom(room, ref serializer);
        }

        protected internal override ChatRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
        {
            if (!_serializers.TryGetValue(roomInfo.RoomType, out var roomDeserializer))
                throw new SerializationException($"No deserializer for room {roomInfo.RoomType} found");

            return roomDeserializer.DeserializeRoom(ref deserializer, in roomInfo);
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

            protected internal override void SerializeRoom(ChatRoomInfo packet, ref BinarySerializer serializer)
            {
                InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override ChatRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, in roomInfo);
            }
        }
    }

    public class DirectChatRoomInfoSerializer : ChatRoomInfoSerializer<DirectChatRoomInfo>
    {
        protected internal override void SerializeRoom(DirectChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.UserOne);
            serializer.WriteSnowflakeId(model.UserTwo);
        }

        protected internal override DirectChatRoomInfo DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
        {
            if (roomInfo.RoomType != ChatRoomType.DIRECT_MSG)
                throw new SerializationException($"Unknown room type for {nameof(DirectChatRoomInfo)} '{roomInfo.RoomType}'");

            var userOne = deserializer.ReadSnowflakeId();
            var userTwo = deserializer.ReadSnowflakeId();

            return new DirectChatRoomInfo
            {
                RoomId = roomInfo.RoomId,
                Name = roomInfo.Name,
                UserOne = userOne,
                UserTwo = userTwo
            };
        }
    }

    public class GroupChatRoomInfoSerializer : ChatRoomInfoSerializer<GroupChatRoomInfo>
    {
        protected internal override void SerializeRoom(GroupChatRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteBool(model.IsPublic);
            serializer.WriteSnowflakeId(model.OwnerId);
        }

        protected internal override GroupChatRoomInfo DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
        {
            if (roomInfo.RoomType != ChatRoomType.GROUP)
                throw new SerializationException($"Unknown room type for {nameof(GroupChatRoomInfo)} '{roomInfo.RoomType}'");

            bool isPublic = deserializer.ReadBool();
            var ownerId = deserializer.ReadSnowflakeId();

            return new GroupChatRoomInfo
            {
                RoomId = roomInfo.RoomId,
                Name = roomInfo.Name,
                IsPublic = isPublic,
                OwnerId = ownerId
            };
        }
    }
}
