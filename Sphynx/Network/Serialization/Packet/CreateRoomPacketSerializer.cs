// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class CreateRoomRequestPacketSerializer<TPacket> : RequestPacketSerializer<TPacket>
        where TPacket : RoomCreateRequest
    {
        protected sealed override int GetMaxSizeInternal(TPacket packet)
        {
            return BinarySerializer.MaxSizeOf<ChatRoomType>() + GetMaxRoomSize(packet);
        }

        protected internal abstract int GetMaxRoomSize(TPacket packet);

        protected sealed override bool SerializeInternal(TPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteEnum(packet.RoomType);

            return SerializeRoom(packet, ref serializer);
        }

        protected internal abstract bool SerializeRoom(TPacket packet, ref BinarySerializer serializer);

        protected sealed override TPacket? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestInfo requestInfo)
        {
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            var roomInfo = new CreateRoomRequestInfo { Base = requestInfo, RoomType = roomType };

            return DeserializeRoom(ref deserializer, roomInfo);
        }

        protected internal abstract TPacket? DeserializeRoom(
            ref BinaryDeserializer deserializer,
            CreateRoomRequestInfo requestInfo);
    }

    public readonly struct CreateRoomRequestInfo
    {
        public RequestInfo Base { get; init; }
        public ChatRoomType RoomType { get; init; }
    }

    public sealed class CreateRoomRequestPacketSerializer : CreateRoomRequestPacketSerializer<RoomCreateRequest>
    {
        private readonly Dictionary<ChatRoomType, CreateRoomRequestPacketSerializer<RoomCreateRequest>>
            _serializers = new();

        public CreateRoomRequestPacketSerializer()
        {
            WithSerializer(ChatRoomType.DIRECT_MSG, new Direct());
            WithSerializer(ChatRoomType.GROUP, new Group());
        }

        protected internal override int GetMaxRoomSize(RoomCreateRequest packet)
        {
            return _serializers.TryGetValue(packet.RoomType, out var serializer)
                ? serializer.GetMaxRoomSize(packet)
                : 0;
        }

        protected internal override bool SerializeRoom(RoomCreateRequest packet, ref BinarySerializer serializer)
        {
            if (_serializers.TryGetValue(packet.RoomType, out var roomSerializer))
            {
                return roomSerializer.SerializeRoom(packet, ref serializer);
            }

            // We can allow it, but deserialization may not work
            return true;
        }

        protected internal override RoomCreateRequest? DeserializeRoom(
            ref BinaryDeserializer deserializer,
            CreateRoomRequestInfo requestInfo)
        {
            if (_serializers.TryGetValue(requestInfo.RoomType, out var roomDeserializer))
            {
                return roomDeserializer.DeserializeRoom(ref deserializer, requestInfo);
            }

            return null;
        }

        public CreateRoomRequestPacketSerializer WithSerializer<T>(
            ChatRoomType roomType,
            CreateRoomRequestPacketSerializer<T> serializer)
            where T : RoomCreateRequest
        {
            ref var existingAdapter =
                ref CollectionsMarshal.GetValueRefOrAddDefault(_serializers, roomType, out bool exists);

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

        public CreateRoomRequestPacketSerializer WithoutSerializer(ChatRoomType roomType)
        {
            _serializers.Remove(roomType);
            return this;
        }

        private class SerializerAdapter<T> : CreateRoomRequestPacketSerializer<RoomCreateRequest>
            where T : RoomCreateRequest
        {
            internal CreateRoomRequestPacketSerializer<T> InnerSerializer { get; set; }

            public SerializerAdapter(CreateRoomRequestPacketSerializer<T> innerSerializer)
            {
                InnerSerializer = innerSerializer;
            }

            protected internal override int GetMaxRoomSize(RoomCreateRequest packet)
            {
                return InnerSerializer.GetMaxRoomSize((T)packet);
            }

            protected internal override bool SerializeRoom(
                RoomCreateRequest packet,
                ref BinarySerializer serializer)
            {
                return InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override RoomCreateRequest? DeserializeRoom(
                ref BinaryDeserializer deserializer,
                CreateRoomRequestInfo requestInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, requestInfo);
            }
        }

        #region Concrete Implementations

        public class Direct : CreateRoomRequestPacketSerializer<RoomCreateRequest.Direct>
        {
            protected internal override int GetMaxRoomSize(RoomCreateRequest.Direct packet)
            {
                return BinarySerializer.MaxSizeOf<SnowflakeId>();
            }

            protected internal override bool SerializeRoom(
                RoomCreateRequest.Direct packet,
                ref BinarySerializer serializer)
            {
                serializer.WriteSnowflakeId(packet.OtherId);
                return true;
            }

            protected internal override RoomCreateRequest.Direct DeserializeRoom(
                ref BinaryDeserializer deserializer,
                CreateRoomRequestInfo requestInfo)
            {
                var otherId = deserializer.ReadSnowflakeId();

                return new RoomCreateRequest.Direct(requestInfo.Base.UserId, requestInfo.Base.SessionId, otherId);
            }
        }

        public class Group : CreateRoomRequestPacketSerializer<RoomCreateRequest.Group>
        {
            protected internal override int GetMaxRoomSize(RoomCreateRequest.Group packet)
            {
                return BinarySerializer.MaxSizeOf(packet.Name) + BinarySerializer.MaxSizeOf(packet.Password) +
                       BinarySerializer.MaxSizeOf<bool>();
            }

            protected internal override bool SerializeRoom(
                RoomCreateRequest.Group packet,
                ref BinarySerializer serializer)
            {
                serializer.WriteString(packet.Name);
                serializer.WriteString(packet.Password);
                serializer.WriteBool(packet.Public);
                return true;
            }

            protected internal override RoomCreateRequest.Group DeserializeRoom(
                ref BinaryDeserializer deserializer,
                CreateRoomRequestInfo requestInfo)
            {
                string name = deserializer.ReadString();
                string password = deserializer.ReadString();
                bool isPublic = deserializer.ReadBool();

                return new RoomCreateRequest.Group(requestInfo.Base.UserId, requestInfo.Base.SessionId,
                    name, password, isPublic);
            }
        }

        #endregion
    }

    public class CreateRoomResponsePacketSerializer : ResponsePacketSerializer<CreateRoomResponsePacket>
    {
        protected override int GetMaxSizeInternal(CreateRoomResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool SerializeInternal(CreateRoomResponsePacket packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            serializer.WriteSnowflakeId(packet.RoomId!.Value);
            return true;
        }

        protected override CreateRoomResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new CreateRoomResponsePacket(responseInfo.ErrorCode);

            var roomId = deserializer.ReadSnowflakeId();
            return new CreateRoomResponsePacket(roomId);
        }
    }
}
