// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sphynx.Network.Serialization.Model
{
    public abstract class ModelSerializer<T> : ITypeSerializer<T> where T : notnull
    {
        public abstract int GetMaxSize(T model);

        public bool TrySerialize(T model, Span<byte> buffer, out int bytesWritten)
        {
            int tempBufferSize = GetMaxSize(model);
            byte[] rawTempBuffer = ArrayPool<byte>.Shared.Rent(tempBufferSize);
            var tempBuffer = rawTempBuffer.AsMemory()[..tempBufferSize];

            try
            {
                var tempSpan = tempBuffer.Span;

                if (TrySerializeUnsafe(model, tempSpan, out bytesWritten) && tempSpan.TryCopyTo(buffer))
                    return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawTempBuffer);
            }

            bytesWritten = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySerializeUnsafe(T model, Span<byte> buffer)
        {
            return TrySerializeUnsafe(model, buffer, out _);
        }

        /// <summary>
        /// Serializes the model directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="model">The model to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <param name="bytesWritten">Number of bytes written to the buffer.</param>
        /// <returns>true if serialization succeeded; false otherwise.</returns>
        public bool TrySerializeUnsafe(T model, Span<byte> buffer, out int bytesWritten)
        {
            var serializer = new BinarySerializer(buffer);

            try
            {
                Serialize(model, ref serializer);
                bytesWritten = serializer.Offset;
                return true;
            }
            catch
            {
                bytesWritten = serializer.Offset;
                return false;
            }
        }

        protected abstract void Serialize(T model, ref BinarySerializer serializer);

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? model, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer);

            try
            {
                model = Deserialize(ref deserializer);
                bytesRead = deserializer.Offset;
                return true;
            }
            catch
            {
                model = default;
                bytesRead = 0;
                return false;
            }
        }

        protected abstract T Deserialize(ref BinaryDeserializer deserializer);
    }

    internal static class ModelSerializerExtensions
    {
        /// <inheritdoc cref="TrySerializeUnsafe{T}(ITypeSerializer{T},T,Span{byte}, out int)"/>
        internal static bool TrySerializeUnsafe<T>(
            this ITypeSerializer<T> serializer,
            T model,
            ref BinarySerializer binarySerializer) where T : notnull
        {
            if (TrySerializeUnsafe(serializer, model, binarySerializer.CurrentSpan, out int bytesWritten))
            {
                binarySerializer.Offset += bytesWritten;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calls <see cref="ModelSerializer{T}.TrySerializeUnsafe(T, Span{byte}, out int)"/> if the underlying
        /// <paramref name="serializer"/> is a <see cref="ModelSerializer{T}"/>, else defaults to
        /// <see cref="ITypeSerializer{T}.TrySerialize(T, Span{byte}, out int)"/>.
        /// </summary>
        internal static bool TrySerializeUnsafe<T>(
            this ITypeSerializer<T> serializer,
            T model,
            Span<byte> buffer,
            out int bytesWritten) where T : notnull
        {
            if (serializer is ModelSerializer<T> modelSerializer)
            {
                return modelSerializer.TrySerializeUnsafe(model, buffer, out bytesWritten);
            }

            return serializer.TrySerialize(model, buffer, out bytesWritten);
        }
    }
}
