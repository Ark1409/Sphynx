// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class RoomCreateRequestSerializer<TRequest> : RequestSerializer<TRequest>
        where TRequest : RoomCreateRequest
    {
        protected sealed override int GetMaxSizeInternal(TRequest packet)
        {
            return BinarySerializer.MaxSizeOf<ChatRoomType>() + GetMaxRoomSize(packet);
        }

        protected internal abstract int GetMaxRoomSize(TRequest packet);

        protected sealed override bool SerializeInternal(TRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteEnum(packet.RoomType);

            return SerializeRoom(packet, ref serializer);
        }

        protected internal abstract bool SerializeRoom(TRequest packet, ref BinarySerializer serializer);

        protected sealed override TRequest? DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            var roomInfo = new RoomCreateRequestInfo { Base = requestInfo, RoomType = roomType };

            return DeserializeRoom(ref deserializer, roomInfo);
        }

        protected internal abstract TRequest? DeserializeRoom(ref BinaryDeserializer deserializer, RoomCreateRequestInfo requestInfo);
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

        protected internal override RoomCreateRequest? DeserializeRoom(ref BinaryDeserializer deserializer, RoomCreateRequestInfo requestInfo)
        {
            if (_serializers.TryGetValue(requestInfo.RoomType, out var roomDeserializer))
            {
                return roomDeserializer.DeserializeRoom(ref deserializer, requestInfo);
            }

            return null;
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

            protected internal override int GetMaxRoomSize(RoomCreateRequest packet)
            {
                return InnerSerializer.GetMaxRoomSize((T)packet);
            }

            protected internal override bool SerializeRoom(RoomCreateRequest packet, ref BinarySerializer serializer)
            {
                return InnerSerializer.SerializeRoom((T)packet, ref serializer);
            }

            protected internal override RoomCreateRequest? DeserializeRoom(ref BinaryDeserializer deserializer, RoomCreateRequestInfo requestInfo)
            {
                return InnerSerializer.DeserializeRoom(ref deserializer, requestInfo);
            }
        }

        #region Concrete Implementations

        public class Direct : RoomCreateRequestSerializer<RoomCreateRequest.Direct>
        {
            protected internal override int GetMaxRoomSize(RoomCreateRequest.Direct packet)
            {
                return BinarySerializer.MaxSizeOf<SnowflakeId>();
            }

            protected internal override bool SerializeRoom(RoomCreateRequest.Direct packet, ref BinarySerializer serializer)
            {
                serializer.WriteSnowflakeId(packet.OtherId);
                return true;
            }

            protected internal override RoomCreateRequest.Direct DeserializeRoom(
                ref BinaryDeserializer deserializer,
                RoomCreateRequestInfo requestInfo)
            {
                var otherId = deserializer.ReadSnowflakeId();

                return new RoomCreateRequest.Direct(requestInfo.Base.AccessToken, otherId);
            }
        }

        public class Group : RoomCreateRequestSerializer<RoomCreateRequest.Group>
        {
            protected internal override int GetMaxRoomSize(RoomCreateRequest.Group packet)
            {
                return BinarySerializer.MaxSizeOf(packet.Name) + BinarySerializer.MaxSizeOf(packet.Password) +
                       BinarySerializer.MaxSizeOf<bool>();
            }

            protected internal override bool SerializeRoom(RoomCreateRequest.Group packet, ref BinarySerializer serializer)
            {
                serializer.WriteString(packet.Name);
                serializer.WriteString(packet.Password);
                serializer.WriteBool(packet.Public);
                return true;
            }

            protected internal override RoomCreateRequest.Group DeserializeRoom(
                ref BinaryDeserializer deserializer,
                RoomCreateRequestInfo requestInfo)
            {
                string name = deserializer.ReadString();
                string password = deserializer.ReadString();
                bool isPublic = deserializer.ReadBool();

                return new RoomCreateRequest.Group(requestInfo.Base.AccessToken,
                    name, password, isPublic);
            }
        }

        #endregion
    }

    public class RoomCreateResponseSerializer : ResponseSerializer<RoomCreateResponse>
    {
        protected override int GetMaxSizeInternal(RoomCreateResponse packet)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool SerializeInternal(RoomCreateResponse packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return true;

            serializer.WriteSnowflakeId(packet.RoomId!.Value);
            return true;
        }

        protected override RoomCreateResponse DeserializeInternal(ref BinaryDeserializer deserializer, ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new RoomCreateResponse(responseInfo.ErrorInfo);

            var roomId = deserializer.ReadSnowflakeId();
            return new RoomCreateResponse(roomId);
        }
    }
}
