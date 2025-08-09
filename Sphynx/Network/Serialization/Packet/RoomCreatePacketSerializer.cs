// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class RoomCreateRequestSerializer<TRequest> : RequestSerializer<TRequest>
        where TRequest : RoomCreateRequest
    {
        protected sealed override void SerializeInternal(TRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteEnum(packet.RoomType);

            SerializeRoom(packet, ref serializer);
        }

        protected internal abstract void SerializeRoom(TRequest packet, ref BinarySerializer serializer);

        protected sealed override TRequest? DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            var roomInfo = new RoomCreateRequestInfo { Base = requestInfo, RoomType = roomType };

            return DeserializeRoom(ref deserializer, in roomInfo);
        }

        protected internal abstract TRequest? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomCreateRequestInfo requestInfo);
    }

    public readonly struct RoomCreateRequestInfo
    {
        public RequestInfo Base { get; init; }
        public ChatRoomType RoomType { get; init; }
    }

    public sealed class RoomCreateRequestSerializer : RoomCreateRequestSerializer<RoomCreateRequest>
    {
        private readonly Dictionary<ChatRoomType, RoomCreateRequestSerializer<RoomCreateRequest>>
            _serializers = new();

        public RoomCreateRequestSerializer()
        {
            WithSerializer(ChatRoomType.DIRECT_MSG, new Direct());
            WithSerializer(ChatRoomType.GROUP, new Group());
        }

        protected internal override void SerializeRoom(RoomCreateRequest packet, ref BinarySerializer serializer)
        {
            if (!_serializers.TryGetValue(packet.RoomType, out var roomSerializer))
                throw new SerializationException($"No serializer for {packet.PacketType} with room {packet} found");

            roomSerializer.SerializeRoom(packet, ref serializer);
        }

        protected internal override RoomCreateRequest? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomCreateRequestInfo requestInfo)
        {
            if (_serializers.TryGetValue(requestInfo.RoomType, out var roomDeserializer))
                return roomDeserializer.DeserializeRoom(ref deserializer, in requestInfo);

            throw new SerializationException($"No deserializer for room type {requestInfo.RoomType} found");
        }

        public RoomCreateRequestSerializer WithSerializer<T>(ChatRoomType roomType, RoomCreateRequestSerializer<T> serializer)
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

        public RoomCreateRequestSerializer WithoutSerializer(ChatRoomType roomType)
        {
            _serializers.Remove(roomType);
            return this;
        }

        private class SerializerAdapter<T> : RoomCreateRequestSerializer<RoomCreateRequest>
            where T : RoomCreateRequest
        {
            internal RoomCreateRequestSerializer<T> InnerSerializer { get; set; }

            public SerializerAdapter(RoomCreateRequestSerializer<T> innerSerializer)
            {
                InnerSerializer = innerSerializer;
            }

            protected internal override void SerializeRoom(RoomCreateRequest packet, ref BinarySerializer serializer)
            {
                InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override RoomCreateRequest? DeserializeRoom(ref BinaryDeserializer deserializer, in RoomCreateRequestInfo requestInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, in requestInfo);
            }
        }

        #region Concrete Implementations

        public class Direct : RoomCreateRequestSerializer<RoomCreateRequest.Direct>
        {
            protected internal override void SerializeRoom(RoomCreateRequest.Direct packet, ref BinarySerializer serializer)
            {
                serializer.WriteSnowflakeId(packet.OtherId);
            }

            protected internal override RoomCreateRequest.Direct DeserializeRoom(ref BinaryDeserializer deserializer,
                in RoomCreateRequestInfo requestInfo)
            {
                var otherId = deserializer.ReadSnowflakeId();

                return new RoomCreateRequest.Direct(requestInfo.Base.AccessToken, otherId);
            }
        }

        public class Group : RoomCreateRequestSerializer<RoomCreateRequest.Group>
        {
            protected internal override void SerializeRoom(RoomCreateRequest.Group packet, ref BinarySerializer serializer)
            {
                serializer.WriteString(packet.Name);
                serializer.WriteString(packet.Password);
                serializer.WriteBool(packet.Public);
            }

            protected internal override RoomCreateRequest.Group DeserializeRoom(ref BinaryDeserializer deserializer,
                in RoomCreateRequestInfo requestInfo)
            {
                string name = deserializer.ReadString()!;
                string? password = deserializer.ReadString();
                bool isPublic = deserializer.ReadBool();

                return new RoomCreateRequest.Group(requestInfo.Base.AccessToken, name, password, isPublic);
            }
        }

        #endregion
    }

    public class RoomCreateResponseSerializer : ResponseSerializer<RoomCreateResponse>
    {
        protected override void SerializeInternal(RoomCreateResponse packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            serializer.WriteSnowflakeId(packet.RoomId!.Value);
        }

        protected override RoomCreateResponse DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new RoomCreateResponse(responseInfo.ErrorInfo);

            var roomId = deserializer.ReadSnowflakeId();
            return new RoomCreateResponse(roomId);
        }
    }
}
