// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Network.Serialization
{
    public abstract class TypeSerializer<T> : ITypeSerializer<T> where T : notnull
    {
        public abstract int GetMaxSize(T model);

        public bool TrySerializeUnsafe(T instance, Span<byte> buffer, out int bytesWritten)
        {
            var serializer = new BinarySerializer(buffer);

            try
            {
                Serialize(instance, ref serializer);
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

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? instance, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer);

            try
            {
                instance = Deserialize(ref deserializer);
                bytesRead = deserializer.Offset;
                return instance is not null;
            }
            catch
            {
                instance = default;
                bytesRead = 0;
                return false;
            }
        }

        protected abstract T? Deserialize(ref BinaryDeserializer deserializer);
    }

    public static partial class TypeSerializerExtensions
    {
        internal static bool TrySerializeUnsafe<T>(this ITypeSerializer<T> serializer, T instance, ref BinarySerializer binarySerializer)
            where T : notnull
        {
            if (!serializer.TrySerializeUnsafe(instance, binarySerializer.CurrentSpan, out int bytesWritten))
                return false;

            binarySerializer.Offset += bytesWritten;
            return true;
        }
    }
}
