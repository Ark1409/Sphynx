// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization
{
    public sealed class ArraySerializer<T> : ITypeSerializer<T[]> where T : notnull
    {
        private readonly ITypeSerializer<T> _innerSerializer;

        public ArraySerializer(ITypeSerializer<T> innerSerializer)
        {
            _innerSerializer = innerSerializer;
        }

        public int GetMaxSize(T[] instance)
        {
            int size = BinarySerializer.MaxSizeOf<int>();

            foreach (var item in instance)
                size += _innerSerializer.GetMaxSize(item);

            return size;
        }

        public bool TrySerialize(T[] instance, Span<byte> buffer, out int bytesWritten)
        {
            var serializer = new BinarySerializer(buffer);
            serializer.WriteInt32(instance.Length);

            foreach (var item in instance)
            {
                if (!_innerSerializer.TrySerializeUnsafe(item, ref serializer))
                {
                    bytesWritten = serializer.Offset;
                    return false;
                }
            }

            bytesWritten = serializer.Offset;
            return true;
        }

        public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T[]? instance, out int bytesRead)
        {
            var deserializer = new BinaryDeserializer(buffer);
            int size = deserializer.ReadInt32();

            instance = size == 0 ? Array.Empty<T>() : new T[size];

            for (int i = 0; i < size; i++)
            {
                if (!_innerSerializer.TryDeserialize(ref deserializer, out var item))
                {
                    bytesRead = deserializer.Offset;
                    instance = null;
                    return false;
                }

                instance[i] = item;
            }

            bytesRead = deserializer.Offset;
            return true;
        }
    }
}
