// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// Attempts to serialize this instance into the provided <paramref name="buffer"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="buffer">This buffer to serialize this instance into.</param>
        /// <param name="bytesWritten">Number of bytes written into the buffer.</param>
        bool TrySerialize(T instance, Span<byte> buffer, out int bytesWritten);

        /// <summary>
        /// Attempts to deserialize a <see cref="T"/>.
        /// </summary>
        /// <param name="buffer">The serialized <see cref="T"/> bytes.</param>
        /// <param name="packet">The deserialized instance.</param>
        /// <param name="bytesRead">Number of bytes read from the buffer.</param>
        bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out T? packet, out int bytesRead);
    }
}
