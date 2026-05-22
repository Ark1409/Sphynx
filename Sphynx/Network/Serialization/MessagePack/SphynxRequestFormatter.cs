// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FastEnumUtility;
using MessagePack;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network.Serialization.MessagePack
{
    internal interface ISphynxRequestFormatterCore
    {
        bool OwnsWriter { get; }
        bool OwnsReader { get; }
        ValueTask WriteRequestAsync(SphynxRequest request, SphynxChannelWriter.Channel channel, CancellationToken ct);
        ValueTask<SphynxRequest> ReadRequestAsync(SphynxRequestHeader header, SphynxChannelReader.Channel channel, CancellationToken ct);
    }

    public class SphynxRequestFormatter<T> : SphynxMessageFormatter<T>, ISphynxRequestFormatterCore where T : SphynxRequest
    {
        /// <summary>
        /// The request type this serializer accepts when serializing.
        /// <see langword="null"/> indicates that it accepts any request type.
        /// </summary>
        public SphynxRequestType? RequestType { get; }

        // TODO: Read this from the T
        public SphynxRequestFormatter(SphynxRequestType? requestType) : base(SphynxMessageType.Request)
        {
            RequestType = requestType;
        }

        public SphynxRequestFormatter(SphynxRequestType? messageType, bool ownsWriter, bool ownsReader)
            : base(SphynxMessageType.Request, ownsWriter, ownsReader)
        {
            RequestType = messageType;
        }

        protected sealed override ValueTask WriteMessageAsync(T message, SphynxChannelWriter.Channel channel,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (MessageType != null && message.RequestType != RequestType)
                return ValueTask.FromException(GetRequestTypeMismatchException(message.RequestType));

            // NOTE: AsBufferWriter here shouldn't allocate as the default impl uses an IBufferWriter itself
            WriteRequestHeader(message, channel.AsBufferWriter());
            return WriteRequestAsync(message, channel, cancellationToken);
        }

        protected virtual ValueTask WriteRequestAsync(T request, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken)
        {
            return base.WriteMessageAsync(request, channel, cancellationToken);
        }

        protected sealed override ValueTask<T> ReadMessageAsync(SphynxMessageType msgType, SphynxChannelReader.Channel channel,
            CancellationToken token) => base.ReadMessageAsync(msgType, channel, token);

        protected sealed override async ValueTask<T> ReadMessageAsync(SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            if (FastEnum.GetUnderlyingType<SphynxRequestType>() != typeof(ushort))
                ThrowRequestTypeMismatch(typeof(ushort));

            var reqHeader = await ReadRequestHeaderAsync(channel, cancellationToken).ConfigureAwait(false);

            if (RequestType != null && reqHeader.RequestType != RequestType)
                ThrowRequestTypeMismatch(reqHeader.RequestType);

            return await ReadRequestAsync(reqHeader, channel, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async ValueTask<T> ReadRequestAsync(SphynxRequestHeader header, SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            var request = await base.ReadMessageAsync(channel, cancellationToken).ConfigureAwait(false);
            request.Header = header;
            return request;
        }

        ValueTask ISphynxRequestFormatterCore.WriteRequestAsync(SphynxRequest request, SphynxChannelWriter.Channel channel, CancellationToken ct) =>
            WriteRequestAsync((T)request, channel, ct);

        async ValueTask<SphynxRequest> ISphynxRequestFormatterCore.ReadRequestAsync(SphynxRequestHeader header, SphynxChannelReader.Channel channel,
            CancellationToken ct) =>
            await ReadRequestAsync(header, channel, ct).ConfigureAwait(false);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected static void ThrowRequestTypeMismatch(Type expectedType)
            => throw new MessagePackSerializationException($"Request type is not a {expectedType.Name}");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected static void ThrowRequestTypeMismatch(SphynxRequestType unexpectedType)
            => throw GetRequestTypeMismatchException(unexpectedType);

        private protected static Exception GetRequestTypeMismatchException(SphynxRequestType unexpectedType)
            => new MessagePackSerializationException($"Unexpected request type: {unexpectedType}");

        #region Header Serialization

        protected static void WriteRequestHeader(T request, IBufferWriter<byte> buffer)
        {
            if (FastEnum.GetUnderlyingType<SphynxRequestType>() != typeof(ushort))
                ThrowRequestTypeMismatch(typeof(byte));

            var writer = new MessagePackWriter(buffer);

            writer.WriteMapHeader(2);
            writer.WriteString("req_header"u8);
            SphynxRequestHeaderFormatter.Instance.Serialize(ref writer, request.Header, MessagePackSerializer.DefaultOptions);
            writer.WriteString("req_data"u8);

            writer.Flush();
        }

        private static async ValueTask<SphynxRequestHeader> ReadRequestHeaderAsync(SphynxChannelReader.Channel channel,
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

                if (TryReadRequestHeader(buffer, out var reqHeader, out bool needMoreBytes, out int bytesRead))
                {
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesRead));
                    return reqHeader;
                }

                if (result.IsCompleted || !needMoreBytes)
                    break;

                pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            throw new MessagePackSerializationException("Error occured while deserializing the request header from the stream");
        }

        protected static bool TryReadRequestHeader(ReadOnlySequence<byte> headerBytes, out SphynxRequestHeader header, out bool needMoreBytes,
            out int bytesRead)
        {
            var reader = new MessagePackReader(headerBytes);

            if (!reader.TryReadMapHeader(out int count, out var result) || count != 2)
                goto Fail;

            if (!reader.TryReadString("req_header"u8, out result))
                goto Fail;

            if (!SphynxRequestHeaderFormatter.Instance.TryDeserialize(ref reader, out header, out bool needMore, out _))
            {
                result = needMore ? MessagePackPrimitives.DecodeResult.InsufficientBuffer : result;
                goto Fail;
            }

            if (!reader.TryReadString("req_data"u8, out result))
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
