// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Network.Serialization.Model
{
    public abstract class ModelSerializer<T> : ITypeSerializer<T> where T : notnull
    {
        public abstract int GetMaxSize(T model);

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

        protected abstract bool Serialize(T model, ref BinarySerializer serializer);

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? model, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer);

            try
            {
                model = Deserialize(ref deserializer);
                bytesRead = deserializer.Offset;
                return model is not null;
            }
            catch
            {
                model = default;
                bytesRead = 0;
                return false;
            }
        }

        protected abstract T? Deserialize(ref BinaryDeserializer deserializer);
    }

    internal static class ModelSerializerExtensions
    {
        internal static bool TrySerializeUnsafe<T>(this ITypeSerializer<T> serializer, T model, ref BinarySerializer binarySerializer)
            where T : notnull
        {
            if (!serializer.TrySerializeUnsafe(model, binarySerializer.CurrentSpan, out int bytesWritten))
                return false;

            binarySerializer.Offset += bytesWritten;
            return true;
        }
    }
}
