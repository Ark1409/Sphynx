// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FastEnumUtility;
using MessagePack;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network.Serialization.MessagePack
{
    internal interface ISphynxResponseFormatterCore
    {
        bool OwnsWriter { get; }
        bool OwnsReader { get; }
        ValueTask WriteResponseAsync(SphynxResponse response, SphynxChannelWriter.Channel channel, CancellationToken ct);
        ValueTask<SphynxResponse> ReadResponseAsync(SphynxResponseHeader header, SphynxChannelReader.Channel channel, CancellationToken ct);
    }

    public class SphynxResponseFormatter<T> : SphynxMessageFormatter<T>, ISphynxResponseFormatterCore where T : SphynxResponse
    {
        /// <summary>
        /// The request type this serializer accepts when serializing.
        /// <see langword="null"/> indicates that it accepts any request type.
        /// </summary>
        public SphynxRequestType? ResponseType { get; }

        // TODO: Read this from the T
        public SphynxResponseFormatter(SphynxRequestType? responseType) : base(SphynxMessageType.Response)
        {
            ResponseType = responseType;
        }

        protected sealed override ValueTask WriteMessageAsync(T message, SphynxChannelWriter.Channel channel,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (ResponseType != null && message.ResponseType != ResponseType)
                ThrowResponseTypeMismatch(message.ResponseType);

            // NOTE: AsBufferWriter here shouldn't allocate as the default impl uses an IBufferWriter itself
            WriteResponseHeader(message, channel.AsBufferWriter());
            return WriteResponseAsync(message, channel, cancellationToken);
        }

        protected virtual ValueTask WriteResponseAsync(T response, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken)
        {
            return base.WriteMessageAsync(response, channel, cancellationToken);
        }

        protected sealed override ValueTask<T> ReadMessageAsync(SphynxMessageType msgType, SphynxChannelReader.Channel channel,
            CancellationToken token) => base.ReadMessageAsync(msgType, channel, token);

        protected sealed override async ValueTask<T> ReadMessageAsync(SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            if (FastEnum.GetUnderlyingType<SphynxRequestType>() != typeof(ushort))
                ThrowResponseTypeMismatch(typeof(ushort));

            var resHeader = await ReadResponseHeaderAsync(channel, cancellationToken).ConfigureAwait(false);

            if (ResponseType != null && resHeader.ResponseType != ResponseType)
                ThrowResponseTypeMismatch(resHeader.ResponseType);

            return await ReadResponseAsync(resHeader, channel, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async ValueTask<T> ReadResponseAsync(SphynxResponseHeader header, SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            var response = await base.ReadMessageAsync(channel, cancellationToken).ConfigureAwait(false);
            response.Header = header;
            return response;
        }

        ValueTask ISphynxResponseFormatterCore.WriteResponseAsync(SphynxResponse response, SphynxChannelWriter.Channel channel, CancellationToken ct)
            => WriteResponseAsync((T)response, channel, ct);

        async ValueTask<SphynxResponse> ISphynxResponseFormatterCore.ReadResponseAsync(SphynxResponseHeader header,
            SphynxChannelReader.Channel channel, CancellationToken ct)
            => await ReadResponseAsync(header, channel, ct);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected static void ThrowResponseTypeMismatch(Type expected)
            => throw new MessagePackSerializationException($"Response type is not a {expected.Name}");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected static void ThrowResponseTypeMismatch(SphynxRequestType t)
            => throw new MessagePackSerializationException($"Unexpected response type: {t}");

        #region Header Serialization

        protected static void WriteResponseHeader(T response, IBufferWriter<byte> buffer)
        {
            if (FastEnum.GetUnderlyingType<SphynxRequestType>() != typeof(ushort))
                ThrowResponseTypeMismatch(typeof(byte));

            var writer = new MessagePackWriter(buffer);

            writer.WriteMapHeader(2);
            writer.WriteString("req_header"u8);
            SphynxResponseHeaderFormatter.Instance.Serialize(ref writer, response.Header, MessagePackSerializer.DefaultOptions);
            writer.WriteString("req_data"u8);

            writer.Flush();
        }

        private static async ValueTask<SphynxResponseHeader> ReadResponseHeaderAsync(SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Sadly, we require this since we don't know the size of the header. We could technically precompute it in this
            // specialized case, but that would not work as a general strategy. Where this matters (i.e. on the server-side),
            // we'll have proper pooling in place so it should be fine.
            var pipeReader = channel.AsPipeReader();

            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (TryReadResponseHeader(buffer, out var reqHeader, out bool needMoreBytes, out int bytesRead))
                {
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesRead));
                    return reqHeader;
                }

                if (result.IsCompleted || !needMoreBytes)
                    break;

                pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            throw new MessagePackSerializationException("Error occured while deserializing the response header from the stream");
        }

        protected static bool TryReadResponseHeader(ReadOnlySequence<byte> headerBytes, out SphynxResponseHeader header, out bool needMoreBytes,
            out int bytesRead)
        {
            var reader = new MessagePackReader(headerBytes);

            if (!reader.TryReadMapHeader(out int count, out var result) || count != 2)
                goto Fail;

            if (!reader.TryReadString("res_header"u8, out result))
                goto Fail;

            if (!SphynxResponseHeaderFormatter.Instance.TryDeserialize(ref reader, out header, out needMoreBytes, out _))
            {
                result = needMoreBytes ? MessagePackPrimitives.DecodeResult.InsufficientBuffer : result;
                goto Fail;
            }

            if (!reader.TryReadString("res_data"u8, out result))
                goto Fail;

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

        #endregion
    }
}
