// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using MessagePack;
using Sphynx.Network.Transport;

namespace Sphynx.Utils
{
    public static class MessagePackHelper
    {
        // ReSharper disable once InconsistentNaming
        private static readonly int GuidSize = Unsafe.SizeOf<Guid>();

        public static bool TryReadGuid(ref MessagePackReader reader, out Guid guid, out MessagePackPrimitives.DecodeResult result)
        {
            result = MessagePackPrimitives.TryReadBinHeader(reader.Sequence.FirstSpan, out uint length, out int tokenSize);

            if (result == MessagePackPrimitives.DecodeResult.InsufficientBuffer)
            {
                var sequence = reader.Sequence;

                if (sequence.Length < tokenSize)
                {
                    guid = default;
                    return false;
                }

                Span<byte> buffer = tokenSize <= 32 ? stackalloc byte[32] : new byte[tokenSize];

                if (sequence.Slice(0, tokenSize).TryCopyTo(buffer))
                    result = MessagePackPrimitives.TryReadBinHeader(buffer, out length, out tokenSize);
            }

            if (result != MessagePackPrimitives.DecodeResult.Success || length != GuidSize)
            {
                guid = default;
                return false;
            }

            if (reader.Sequence.Length - tokenSize < length)
            {
                result = MessagePackPrimitives.DecodeResult.InsufficientBuffer;
                guid = default;
                return false;
            }

            var guidSeq = reader.ReadBytes()!.Value;
            Span<byte> guidBytes = stackalloc byte[GuidSize];
            guidSeq.CopyTo(guidBytes);

            guid = new Guid(guidBytes);
            return true;
        }

        public static void WriteGuid(ref MessagePackWriter writer, Guid guid)
        {
            writer.WriteBinHeader(GuidSize);

            var guidBytes = writer.GetSpan(GuidSize);

            if (!guid.TryWriteBytes(guidBytes))
                ThrowSerializeException();

            writer.Advance(GuidSize);

            [DoesNotReturn]
            static void ThrowSerializeException() => throw new SerializationException($"Could not format {nameof(Guid)} for MessagePack");
        }

        // NOTE: Implementation modified from MessagePack's own De/SerializeAsync<T>
        // https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/2e8153e29ad4b4271e30d55aa1c1adb306db6932/src/MessagePack/MessagePackSerializer.cs

        /// <summary>
        /// Serializes a structure directly to a <see cref="SphynxChannelWriter.Channel"/>.
        /// </summary>
        internal static ValueTask SerializeAsync<T>(SphynxChannelWriter.Channel channel,
            T value,
            bool flushChannel = false,
            MessagePackSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            try
            {
                // NOTE: AsBufferWriter here shouldn't allocate as the default impl uses an IBufferWriter itself
                MessagePackSerializer.Serialize(channel.AsBufferWriter(), value, options, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }

            return flushChannel ? channel.FlushAsync(cancellationToken) : ValueTask.CompletedTask;
        }

        /// <summary>
        /// Deserializes a structure directly from a <see cref="SphynxChannelReader.Channel"/>.
        /// </summary>
        internal static async ValueTask<T> DeserializeAsync<T>(SphynxChannelReader.Channel channel,
            MessagePackSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Look into directly using a sequence pool if its the non-poolable one channel
            var pipeReader = channel.AsPipeReader();

            ReadOnlySequence<byte> sequence;

            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (result.IsCompleted)
                {
                    sequence = buffer;
                    break;
                }

                pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            return DeserializeFromSequence<T>(options, sequence, cancellationToken);
        }

        private static T DeserializeFromSequence<T>(MessagePackSerializerOptions? options, ReadOnlySequence<byte> sequence, CancellationToken ct)
        {
            var reader = new MessagePackReader(sequence)
            {
                CancellationToken = ct,
            };

            return MessagePackSerializer.Deserialize<T>(ref reader, options);
        }
    }
}
