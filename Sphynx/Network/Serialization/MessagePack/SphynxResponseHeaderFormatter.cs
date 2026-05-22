// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using FastEnumUtility;
using MessagePack;
using MessagePack.Formatters;
using Sphynx.Core;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.Network.Serialization.MessagePack
{
    public class SphynxResponseHeaderFormatter : IMessagePackFormatter<SphynxResponseHeader>
    {
        public static readonly SphynxResponseHeaderFormatter Instance = new();

        public void Serialize(ref MessagePackWriter writer, SphynxResponseHeader value, MessagePackSerializerOptions options)
        {
            writer.WriteMapHeader(4);

            writer.WriteString("type"u8);
            writer.WriteUInt16((ushort)value.ResponseType);

            writer.WriteString("id"u8);
            MessagePackHelper.WriteGuid(ref writer, value.RequestId);

            writer.WriteString("err_code"u8);
            writer.WriteUInt16((ushort)value.ErrorInfo.ErrorCode);

            writer.WriteString("err_msg"u8);
            writer.Write(value.ErrorInfo.Message);
        }

        public SphynxResponseHeader Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!TryDeserialize(ref reader, out var header, out _, out _))
                throw new SerializationException($"Unable to deserialize {nameof(SphynxResponseHeader)} from MessagePack stream");

            return header;
        }

        public bool TryDeserialize(ref MessagePackReader reader, out SphynxResponseHeader header, out bool needMoreBytes, out int bytesRead)
        {
            if (!reader.TryReadMapHeader(out int count, out var result) || count != 4)
                goto Fail;

            if (!reader.TryReadString("type"u8, out result))
                goto Fail;

            if (!reader.TryReadUInt16(out ushort reqTypeValue, out result) || !FastEnum.IsDefined<SphynxRequestType>(reqTypeValue))
                goto Fail;

            if (!reader.TryReadString("id"u8, out result) || !MessagePackHelper.TryReadGuid(ref reader, out var requestId, out result))
                goto Fail;

            if (!reader.TryReadString("err_code"u8, out result))
                goto Fail;

            if (!reader.TryReadUInt16(out ushort errorCodeValue, out result) || !FastEnum.IsDefined<SphynxErrorCode>(errorCodeValue))
                goto Fail;

            if (!reader.TryReadString("err_msg"u8, out result) || !reader.TryReadString(out string? errorMsg, out result))
                goto Fail;

            header = new SphynxResponseHeader
            {
                ResponseType = (SphynxRequestType)reqTypeValue,
                RequestId = requestId,
                ErrorInfo = new SphynxErrorInfo((SphynxErrorCode)errorCodeValue, errorMsg)
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
