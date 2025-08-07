// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using Sphynx.Storage;

namespace Sphynx.Network.Serialization
{
    public abstract class TypeSerializer<T> : ITypeSerializer<T>
    {
        public void Serialize(T instance, IBufferWriter<byte> buffer)
        {
            var serializer = new BinarySerializer(buffer);
            Serialize(instance, ref serializer);
        }

        public void Serialize(T instance, Span<byte> buffer)
        {
            var serializer = new BinarySerializer(buffer);
            Serialize(instance, ref serializer);
        }

        public abstract void Serialize(T model, ref BinarySerializer serializer);

        public T? Deserialize(in ReadOnlySequence<byte> buffer, out long bytesRead)
        {
            var deserializer = new BinaryDeserializer(in buffer);
            var instance = Deserialize(ref deserializer);

            bytesRead = deserializer.Offset;

            return instance;
        }

        public abstract T? Deserialize(ref BinaryDeserializer deserializer);
    }

    public static partial class TypeSerializerExtensions
    {
        public static void Serialize<T>(this ITypeSerializer<T> serializer, T instance, ref BinarySerializer binarySerializer)
        {
            if (serializer is TypeSerializer<T> typeSerializer)
            {
                typeSerializer.Serialize(instance, ref binarySerializer);
                return;
            }

            // Note: Currently, tracking the number of bytes written isn't a requirement of the ITypeSerializer{T} API, sp
            // we could technically just pass the IBufferWriter, but let's keep it like this for now for correctness.

            // Slow path
            using (var rental = SequencePool.Shared.Rent())
            {
                var sequence = rental.Value;
                serializer.Serialize(instance, sequence);
                binarySerializer.WriteRaw(sequence.AsReadOnlySequence);
            }
        }

        public static T? Deserialize<T>(this ITypeSerializer<T> serializer, ref BinaryDeserializer deserializer)
        {
            if (serializer is TypeSerializer<T> typeSerializer)
                return typeSerializer.Deserialize(ref deserializer);

            if (deserializer.HasSequence)
            {
                var instance = serializer.Deserialize(deserializer.UnreadSequence, out long bytesRead);
                deserializer.Offset += bytesRead;
                return instance;
            }

            // Slow path
            byte[] rentArray = ArrayPool<byte>.Shared.Rent(deserializer.UnreadSpan.Length); // TODO: Sequence? Parition the span perhaps

            try
            {
                deserializer.UnreadSpan.CopyTo(rentArray.AsSpan());

                var instance = serializer.Deserialize(new ReadOnlySequence<byte>(rentArray.AsMemory()), out long bytesRead);
                deserializer.Offset += bytesRead;
                return instance;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentArray);
            }
        }
    }
}
