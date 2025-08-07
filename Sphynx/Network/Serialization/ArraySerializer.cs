// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization
{
    public sealed class ArraySerializer<T> : TypeSerializer<T[]>
    {
        private readonly ITypeSerializer<T> _innerSerializer;

        public ArraySerializer(ITypeSerializer<T> innerSerializer)
        {
            _innerSerializer = innerSerializer;
        }

        public override void Serialize(T[] model, ref BinarySerializer serializer)
        {
            serializer.WriteInt32(model.Length);

            foreach (var item in model.AsSpan())
                _innerSerializer.Serialize(item, ref serializer);
        }

#pragma warning disable CS8609 // Nullability of reference types in return type doesn't match overridden member.
        public override T?[] Deserialize(ref BinaryDeserializer deserializer)
#pragma warning restore CS8609 // Nullability of reference types in return type doesn't match overridden member.
        {
            int size = deserializer.ReadInt32();

            if (size < 0)
                throw new SerializationException($"Cannot deserialize array of size {size}");

            if (size == 0)
                return Array.Empty<T>();

            var array = new T?[size];
            var span = array.AsSpan();

            for (int i = 0; i < span.Length; i++)
                span[i] = _innerSerializer.Deserialize(ref deserializer);

            return array;
        }
    }
}
