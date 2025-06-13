// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Network.Serialization
{
    /// <summary>
    /// Serializes and deserializes <see cref="T"/>s to and from bytes.
    /// </summary>
    /// <typeparam name="T">The type supported by this serializer.</typeparam>
    public interface ITypeSerializer<T>
    {
        /// <summary>
        /// Returns the maximum serialization size (in bytes) of the specified <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance for which the maximum serialization size should be checked.</param>
        int GetMaxSize(T instance);

        /// <summary>
        /// Attempts to serialize this <paramref name="instance"/> into the provided <paramref name="buffer"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="buffer">This buffer to serialize this instance into.</param>
        bool TrySerialize(T instance, Span<byte> buffer) => TrySerialize(instance, buffer, out _);

        /// <summary>
        /// Attempts to serialize this <paramref name="instance"/> into the provided <paramref name="buffer"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="buffer">This buffer to serialize this instance into.</param>
        /// <param name="bytesWritten">Number of bytes written into the buffer.</param>
        bool TrySerialize(T instance, Span<byte> buffer, out int bytesWritten)
        {
            int tempBufferSize = GetMaxSize(instance);
            byte[] rentTempBuffer = ArrayPool<byte>.Shared.Rent(tempBufferSize);
            var tempBuffer = rentTempBuffer.AsMemory()[..tempBufferSize];

            try
            {
                var tempSpan = tempBuffer.Span;

                if (TrySerializeUnsafe(instance, tempSpan, out bytesWritten) && tempSpan[..bytesWritten].TryCopyTo(buffer))
                    return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentTempBuffer);
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        /// Serializes this <paramref name="instance"/> directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <returns>true if serialization succeeded; false otherwise.</returns>
        bool TrySerializeUnsafe(T instance, Span<byte> buffer) => TrySerializeUnsafe(instance, buffer, out _);

        /// <summary>
        /// Serializes this <paramref name="instance"/> directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <param name="bytesWritten">Number of bytes written to the buffer.</param>
        /// <returns>true if serialization succeeded; false otherwise.</returns>
        bool TrySerializeUnsafe(T instance, Span<byte> buffer, out int bytesWritten);

        /// <summary>
        /// Attempts to deserialize a <see cref="T"/>.
        /// </summary>
        /// <param name="buffer">The serialized <see cref="T"/> bytes.</param>
        /// <param name="instance">The deserialized instance.</param>
        bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? instance) => TryDeserialize(buffer, out instance, out _);

        /// <summary>
        /// Attempts to deserialize a <see cref="T"/>.
        /// </summary>
        /// <param name="buffer">The serialized <see cref="T"/> bytes.</param>
        /// <param name="instance">The deserialized instance.</param>
        /// <param name="bytesRead">Number of bytes read from the buffer.</param>
        bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? instance, out int bytesRead);
    }

    public static partial class TypeSerializerExtensions
    {
        public static bool TrySerialize<TSerializer, T>(this TSerializer serializer, T instance, Span<byte> buffer)
            where TSerializer : ITypeSerializer<T>
        {
            return serializer.TrySerialize(instance, buffer);
        }

        public static bool TrySerialize<TSerializer, T>(this TSerializer serializer, T instance, Span<byte> buffer, out int bytesWritten)
            where TSerializer : ITypeSerializer<T>
        {
            return serializer.TrySerialize(instance, buffer, out bytesWritten);
        }

        public static bool TryDeserialize<TSerializer, T>(
            this TSerializer serializer,
            ReadOnlySpan<byte> buffer,
            [NotNullWhen(true)] out T? instance,
            out int bytesRead)
            where TSerializer : ITypeSerializer<T>
        {
            return serializer.TryDeserialize(buffer, out instance, out bytesRead);
        }

        internal static bool TryDeserialize<T>(
            this ITypeSerializer<T> serializer,
            ref BinaryDeserializer deserializer,
            [NotNullWhen(true)] out T? instance)
        {
            if (!serializer.TryDeserialize(deserializer.CurrentSpan, out instance, out int bytesRead))
                return false;

            deserializer.Offset += bytesRead;
            return true;
        }
    }
}
