using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FastEnumUtility;
using MessagePack;
using Sphynx.Network.Packet;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network.Serialization.MessagePack
{
    internal interface ISphynxMessageFormatterCore
    {
        bool OwnsWriter { get; }
        bool OwnsReader { get; }
        ValueTask WriteMessageAsync(SphynxMessage message, SphynxChannelWriter.Channel channel, CancellationToken ct);
        ValueTask<SphynxMessage> ReadMessageAsync(SphynxMessageType msgType, SphynxChannelReader.Channel channel, CancellationToken ct);
    }

    public class SphynxMessageFormatter<T> : MessageFormatter<T>, ISphynxMessageFormatterCore where T : SphynxMessage
    {
        public override bool OwnsWriter { get; }
        public override bool OwnsReader { get; }

        /// <summary>
        /// The message type this serializer accepts when serializing.
        /// <see langword="null"/> indicates that it accepts any message type.
        /// </summary>
        public SphynxMessageType? MessageType { get; }

        // TODO: Read this from the T
        public SphynxMessageFormatter(SphynxMessageType? messageType)
            : this(messageType, false, false)
        {
        }

        public SphynxMessageFormatter(SphynxMessageType? messageType, bool ownsWriter, bool ownsReader)
        {
            MessageType = messageType;
            OwnsWriter = ownsWriter;
            OwnsReader = ownsReader;
        }

        public sealed override ValueTask SerializeAsync(T message, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (MessageType != null && message.MessageType != MessageType)
                return ValueTask.FromException(GetMessageTypeMismatchException(message.MessageType));

            // NOTE: AsBufferWriter here shouldn't allocate as the default impl uses an IBufferWriter itself
            WriteMessageHeader(message, channel.AsBufferWriter());
            return WriteMessageAsync(message, channel, cancellationToken);
        }

        protected virtual ValueTask WriteMessageAsync(T message, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (MessageType != null && message.MessageType != MessageType)
                return ValueTask.FromException(GetMessageTypeMismatchException(message.MessageType));

            var serializeTask = MessagePackHelper.SerializeAsync(channel, message, cancellationToken: cancellationToken);

            Debug.Assert(serializeTask.IsCompleted);

            if (!serializeTask.IsCompletedSuccessfully)
                return serializeTask;

            return OwnsWriter ? channel.DisposeAsync() : serializeTask;
        }

        public sealed override async ValueTask<T> DeserializeAsync(SphynxChannelReader.Channel channel, CancellationToken cancellationToken = default)
        {
            if (FastEnum.GetUnderlyingType<SphynxMessageType>() != typeof(byte))
                ThrowMessageTypeMismatch(typeof(byte));

            var msgType = await ReadMessageHeaderAsync(channel, cancellationToken).ConfigureAwait(false);

            if (msgType != MessageType)
                ThrowMessageTypeMismatch(msgType);

            return await ReadMessageAsync(msgType, channel, cancellationToken).ConfigureAwait(false);
        }

        protected virtual ValueTask<T> ReadMessageAsync(SphynxMessageType msgType, SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            if (MessageType != null && msgType != MessageType)
                return ValueTask.FromException<T>(GetMessageTypeMismatchException(msgType));

            return ReadMessageAsync(channel, cancellationToken);
        }

        protected virtual ValueTask<T> ReadMessageAsync(SphynxChannelReader.Channel channel, CancellationToken cancellationToken)
        {
            if (!OwnsReader)
                return MessagePackHelper.DeserializeAsync<T>(channel, cancellationToken: cancellationToken);

            return Core(channel, cancellationToken);

            static async ValueTask<T> Core(SphynxChannelReader.Channel channel, CancellationToken token)
            {
                Exception? deserializeEx = null;
                T message;

                try
                {
                    message = await MessagePackHelper.DeserializeAsync<T>(channel, cancellationToken: token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    deserializeEx = ex;
                    throw;
                }
                finally
                {
                    await channel.DisposeAsync(deserializeEx).ConfigureAwait(false);
                }

                return message;
            }
        }

        ValueTask ISphynxMessageFormatterCore.WriteMessageAsync(SphynxMessage message, SphynxChannelWriter.Channel channel, CancellationToken ct)
            => WriteMessageAsync((T)message, channel, ct);

        async ValueTask<SphynxMessage> ISphynxMessageFormatterCore.ReadMessageAsync(SphynxMessageType msgType, SphynxChannelReader.Channel channel,
            CancellationToken ct)
            => await ReadMessageAsync(msgType, channel, ct).ConfigureAwait(false);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected static void ThrowMessageTypeMismatch(Type expectedType)
            => throw new MessagePackSerializationException($"Message type is not a {expectedType.Name}");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected static void ThrowMessageTypeMismatch(SphynxMessageType unexpectedType)
            => throw GetMessageTypeMismatchException(unexpectedType);

        private protected static Exception GetMessageTypeMismatchException(SphynxMessageType unexpectedType)
            => new MessagePackSerializationException($"Unexpected message type: {unexpectedType}");

        #region Header serialization

        protected static void WriteMessageHeader(T message, IBufferWriter<byte> buffer)
        {
            if (FastEnum.GetUnderlyingType<SphynxMessageType>() != typeof(byte))
                ThrowMessageTypeMismatch(typeof(byte));

            var writer = new MessagePackWriter(buffer);

            writer.WriteMapHeader(2);
            writer.WriteString("msg_type"u8);
            writer.WriteUInt8((byte)message.MessageType);
            writer.WriteString("msg_data"u8);

            writer.Flush();
        }

        private static async ValueTask<SphynxMessageType> ReadMessageHeaderAsync(SphynxChannelReader.Channel channel,
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

                if (TryReadMessageHeader(buffer, out var msgType, out bool needMoreBytes, out int bytesRead))
                {
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesRead));
                    return msgType;
                }

                if (result.IsCompleted || !needMoreBytes)
                    break;

                pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            throw new MessagePackSerializationException("Error occured while deserializing the message header from the stream");
        }

        protected static bool TryReadMessageHeader(ReadOnlySequence<byte> headerBytes, out SphynxMessageType messageType, out bool needMoreBytes,
            out int bytesRead)
        {
            var reader = new MessagePackReader(headerBytes);

            if (!reader.TryReadMapHeader(out int count, out var result) || count != 2)
                goto Fail;

            if (!reader.TryReadString("msg_type"u8, out result))
                goto Fail;

            if (!reader.TryReadByte(out byte messageTypeValue, out result) || !FastEnum.IsDefined<SphynxMessageType>(messageTypeValue))
                goto Fail;

            if (!reader.TryReadString("msg_data"u8, out result))
                goto Fail;

            // Success:
            needMoreBytes = true;
            messageType = (SphynxMessageType)messageTypeValue;
            bytesRead = (int)reader.Consumed;
            return true;

            Fail:
            {
                needMoreBytes = result is MessagePackPrimitives.DecodeResult.EmptyBuffer or MessagePackPrimitives.DecodeResult.InsufficientBuffer;
                messageType = default;
                bytesRead = (int)reader.Consumed;
                return false;
            }
        }

        #endregion
    }
}
