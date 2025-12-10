// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Sphynx.Core;
using Sphynx.Model.Room;

namespace Sphynx.Network.Serialization.Model
{
    public abstract class ChatRoomInfoSerializer<TRoom> : TypeSerializer<TRoom> where TRoom : SphynxRoomInfo
    {
        public sealed override void Serialize(TRoom packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteEnum(packet.RoomType);
            serializer.WriteDateTimeOffset(packet.CreatedAt);

            SerializeRoom(packet, ref serializer);
        }

        protected internal abstract void SerializeRoom(TRoom packet, ref BinarySerializer serializer);

        public sealed override TRoom? Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadGuid();
            var roomType = deserializer.ReadEnum<SphynxRoomType>();
            var createdAt = deserializer.ReadDateTimeOffset()!;

            var roomInfo = new RoomInfo { RoomId = roomId, RoomType = roomType, CreatedAt = createdAt };

            return DeserializeRoom(ref deserializer, in roomInfo);
        }

        protected internal abstract TRoom? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo);
    }

    public readonly struct RoomInfo
    {
        public Guid RoomId { get; init; }
        public SphynxRoomType RoomType { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    public sealed class ChatRoomInfoSerializer : ChatRoomInfoSerializer<SphynxRoomInfo>
    {
        private readonly Dictionary<SphynxRoomType, ChatRoomInfoSerializer<SphynxRoomInfo>> _serializers = new();

        public ChatRoomInfoSerializer()
        {
            AddSerializer(SphynxRoomType.DIRECT_MSG, new DirectChatRoomInfoSerializer());
            AddSerializer(SphynxRoomType.GROUP, new GroupChatRoomInfoSerializer());
        }

        protected internal override void SerializeRoom(SphynxRoomInfo room, ref BinarySerializer serializer)
        {
            if (!_serializers.TryGetValue(room.RoomType, out var roomSerializer))
                throw new SerializationException($"No serializer for room {room} found");

            roomSerializer.SerializeRoom(room, ref serializer);
        }

        protected internal override SphynxRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
        {
            if (!_serializers.TryGetValue(roomInfo.RoomType, out var roomDeserializer))
                throw new SerializationException($"No deserializer for room {roomInfo.RoomType} found");

            return roomDeserializer.DeserializeRoom(ref deserializer, in roomInfo);
        }

        public ChatRoomInfoSerializer AddSerializer<T>(SphynxRoomType roomType, ChatRoomInfoSerializer<T> serializer)
            where T : SphynxRoomInfo
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

        public ChatRoomInfoSerializer RemoveSerializer(SphynxRoomType roomType)
        {
            _serializers.Remove(roomType);
            return this;
        }

        private class SerializerAdapter<T> : ChatRoomInfoSerializer<SphynxRoomInfo>
            where T : SphynxRoomInfo
        {
            internal ChatRoomInfoSerializer<T> InnerSerializer { get; set; }

            public SerializerAdapter(ChatRoomInfoSerializer<T> innerSerializer)
            {
                InnerSerializer = innerSerializer;
            }

            protected internal override void SerializeRoom(SphynxRoomInfo packet, ref BinarySerializer serializer)
            {
                InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override SphynxRoomInfo? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, in roomInfo);
            }
        }
    }

    public class DirectChatRoomInfoSerializer : ChatRoomInfoSerializer<SphynxDirectRoomInfo>
    {
        protected internal override void SerializeRoom(SphynxDirectRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(model.UserA);
            serializer.WriteGuid(model.UserB);
        }

        protected internal override SphynxDirectRoomInfo DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
        {
            if (roomInfo.RoomType != SphynxRoomType.DIRECT_MSG)
                throw new SerializationException($"Unknown room type for {nameof(SphynxDirectRoomInfo)} '{roomInfo.RoomType}'");

            var userOne = deserializer.ReadGuid();
            var userTwo = deserializer.ReadGuid();

            return new SphynxDirectRoomInfo
            {
                RoomId = roomInfo.RoomId,
                UserA = userOne,
                UserB = userTwo,
CreatedAt = roomInfo.CreatedAt,
            };
        }
    }

    public class GroupChatRoomInfoSerializer : ChatRoomInfoSerializer<SphynxGroupRoomInfo>
    {
        protected internal override void SerializeRoom(SphynxGroupRoomInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteBool(model.IsPublic);
            serializer.WriteGuid(model.OwnerId);
            serializer.WriteString(model.Name);
            serializer.WriteString(model.Password);
            serializer.WriteString(model.PasswordSalt);
        }

        protected internal override SphynxGroupRoomInfo DeserializeRoom(ref BinaryDeserializer deserializer, in RoomInfo roomInfo)
        {
            if (roomInfo.RoomType != SphynxRoomType.GROUP)
                throw new SerializationException($"Unknown room type for {nameof(SphynxGroupRoomInfo)} '{roomInfo.RoomType}'");

            bool isPublic = deserializer.ReadBool();
            var ownerId = deserializer.ReadGuid();
            string? name = deserializer.ReadString();
            string? password = deserializer.ReadString();
            string? passwordSalt = deserializer.ReadString();

            return new SphynxGroupRoomInfo
            {
                RoomId = roomInfo.RoomId,
                Name = name!,
                IsPublic = isPublic,
                OwnerId = ownerId,
                Password = password,
                PasswordSalt = passwordSalt,
                CreatedAt = roomInfo.CreatedAt,
            };
        }
    }
}
