// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Storage;

namespace Sphynx.Network.Serialization
{
    /// <summary>
    /// Serializes and deserializes <see cref="T"/>s to and from bytes.
    /// </summary>
    /// <typeparam name="T">The type supported by this serializer.</typeparam>
    public interface ITypeSerializer<T>
    {
        /// <summary>
        /// Serializes this <paramref name="instance"/> into the provided <paramref name="buffer"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="buffer">The buffer to serialize this instance into.</param>
        void Serialize(T instance, IBufferWriter<byte> buffer);

        /// <summary>
        /// Deserialize a <see cref="T"/>.
        /// </summary>
        /// <param name="buffer">The serialized <see cref="T"/> bytes.</param>
        /// <param name="bytesRead">The number of bytes read of <paramref cref="buffer"/> in order to
        /// deserialize <typeparamref name="T"/>.</param>
        /// <returns>The deserialized instance.</returns>
        T? Deserialize(in ReadOnlySequence<byte> buffer, out long bytesRead);
    }

    public static partial class TypeSerializerExtensions
    {
        /// <summary>
        /// Serializes this <paramref name="instance"/> into the provided <paramref name="stream"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="stream">The stream to serialize this instance into.</param>
        /// <param name="token">A cancellation token for the serialization operation.</param>
        public static ValueTask SerializeAsync<T>(this ITypeSerializer<T> serializer, T instance, Stream stream, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return ValueTask.FromCanceled(token);

            if (!stream.CanWrite)
                return ValueTask.FromException(new ArgumentException("Stream must be writable", nameof(stream)));

            using (var rental = SequencePool.Shared.Rent())
            {
                var sequence = rental.Value;

                serializer.Serialize(instance, sequence);

                if (token.IsCancellationRequested)
                    return ValueTask.FromCanceled(token);

                var readOnlySequence = sequence.AsReadOnlySequence;

                return readOnlySequence.IsSingleSegment
                    ? stream.WriteAsync(readOnlySequence.First, cancellationToken: token)
                    : SerializeAsyncSlow(stream, readOnlySequence, token);
            }
        }

        private static async ValueTask SerializeAsyncSlow(Stream stream, ReadOnlySequence<byte> readOnlySequence, CancellationToken token)
        {
            foreach (ReadOnlyMemory<byte> segment in readOnlySequence)
            {
                token.ThrowIfCancellationRequested();
                await stream.WriteAsync(segment, cancellationToken: token).ConfigureAwait(false);
            }
        }

        public static T? Deserialize<T>(this ITypeSerializer<T> serializer, in ReadOnlySequence<byte> buffer) =>
            serializer.Deserialize(in buffer, out _);
    }
}
