// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

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
                bytesWritten = SerializeUnsafe(model, tempSpan);

                if (tempSpan.TryCopyTo(buffer))
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

        /// <summary>
        /// Serializes the model directly into the <paramref name="buffer"/>, without resetting its contents
        /// on failure.
        /// </summary>
        /// <param name="model">The model to serialize.</param>
        /// <param name="buffer">The output buffer.</param>
        /// <returns>Number of bytes written to the buffer.</returns>
        public int SerializeUnsafe(T model, Span<byte> buffer)
        {
            var serializer = new BinarySerializer(buffer);

            try
            {
                Serialize(model, ref serializer);
                return serializer.Offset;
            }
            catch
            {
                return serializer.Offset;
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
}
