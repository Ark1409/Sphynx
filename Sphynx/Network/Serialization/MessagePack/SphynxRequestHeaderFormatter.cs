// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using FastEnumUtility;
using MessagePack;
using MessagePack.Formatters;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Request;
using Sphynx.Utils;

namespace Sphynx.Network.Serialization.MessagePack
{
    public class SphynxRequestHeaderFormatter : IMessagePackFormatter<SphynxRequestHeader>
    {
        public static readonly SphynxRequestHeaderFormatter Instance = new();

        public void Serialize(ref MessagePackWriter writer, SphynxRequestHeader value, MessagePackSerializerOptions options)
        {
            writer.WriteMapHeader(3);

            writer.WriteString("type"u8);
            writer.WriteUInt16((ushort)value.RequestType);

            writer.WriteString("id"u8);
            MessagePackHelper.WriteGuid(ref writer, value.RequestId);

            writer.WriteString("session_id"u8);
            MessagePackHelper.WriteGuid(ref writer, value.SessionId);
        }

        public SphynxRequestHeader Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!TryDeserialize(ref reader, out var header, out _, out _))
                throw new SerializationException($"Unable to deserialize {nameof(SphynxRequestHeader)} from MessagePack stream");

            return header;
        }

        public bool TryDeserialize(ref MessagePackReader reader, out SphynxRequestHeader header, out bool needMoreBytes, out int bytesRead)
        {
            if (!reader.TryReadMapHeader(out int count, out var result) || count != 3)
                goto Fail;

            if (!reader.TryReadString("type"u8, out result))
                goto Fail;

            if (!reader.TryReadUInt16(out ushort reqTypeValue, out result) || !FastEnum.IsDefined<SphynxRequestType>(reqTypeValue))
                goto Fail;

            if (!reader.TryReadString("id"u8, out result) || !MessagePackHelper.TryReadGuid(ref reader, out var requestId, out result))
                goto Fail;

            if (!reader.TryReadString("session_id"u8) || !MessagePackHelper.TryReadGuid(ref reader, out var sessionId, out result))
                goto Fail;

            header = new SphynxRequestHeader
            {
                RequestType = (SphynxRequestType)reqTypeValue,
                RequestId = requestId,
                SessionId = sessionId
            };
            needMoreBytes = false;
            bytesRead = (int)reader.Consumed;
            return true;

            Fail:
            {
                needMoreBytes = result is MessagePackPrimitives.DecodeResult.EmptyBuffer or MessagePackPrimitives.DecodeResult.InsufficientBuffer;
                bytesRead = (int)reader.Consumed;
                header = default;
                return false;
            }
        }
    }
}
