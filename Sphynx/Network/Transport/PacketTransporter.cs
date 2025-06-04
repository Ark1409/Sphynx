// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public sealed class PacketTransporter : IPacketTransporter
    {
        private readonly Dictionary<SphynxPacketType, IPacketSerializer<SphynxPacket>> _serializers = new();

        public Version Version { get; set; }

        public PacketTransporter()
        {
        }

        public PacketTransporter(PacketTransporter original)
        {
            foreach (var entry in original._serializers)
                AddSerializer(entry.Key, entry.Value);
        }

        public async Task SendAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));

            var serializer = _serializers[packet.PacketType];

            int bufferSize = SphynxPacketHeader.Size + serializer.GetMaxSize(packet);
            byte[] rentBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rentBuffer.AsMemory()[..bufferSize];

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                int packetSize = SerializePacket(packet, buffer, serializer);

                await stream.WriteAsync(buffer[..packetSize], cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
            }
        }

        private int SerializePacket(SphynxPacket packet, Memory<byte> buffer, IPacketSerializer<SphynxPacket> serializer)
        {
            int contentSize = SerializeContent(packet, buffer[SphynxPacketHeader.Size..], serializer);

            SerializeHeader(packet, contentSize, buffer[..SphynxPacketHeader.Size]);

            return SphynxPacketHeader.Size + contentSize;
        }

        private SphynxPacketHeader SerializeHeader(SphynxPacket packet, int contentSize, Memory<byte> buffer)
        {
            Debug.Assert(buffer.Length >= SphynxPacketHeader.Size);

            var header = new SphynxPacketHeader(Version, packet.PacketType, contentSize);

            if (!header.TrySerialize(buffer.Span))
                throw new SerializationException($"Could not serialize header for packet {packet.GetType()}");

            return header;
        }

        private int SerializeContent(SphynxPacket packet, Memory<byte> buffer, IPacketSerializer<SphynxPacket> serializer)
        {
            Debug.Assert(_serializers[packet.PacketType].Equals(serializer));
            Debug.Assert(serializer.GetMaxSize(packet) >= 0);

            if (!serializer.TrySerializeUnsafe(packet, buffer.Span, out int contentSize))
                throw new SerializationException(
                    $"Could not serialize packet {packet.GetType()} ({packet.PacketType}) with serializer {serializer.GetType()}.");

            return contentSize;
        }

        public async Task<SphynxPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            var header = await ReceiveHeaderAsync(stream, cancellationToken);

            // TODO: Length sanity check
            int contentBufferSize = header.ContentSize;
            byte[] rentContentBuffer = ArrayPool<byte>.Shared.Rent(contentBufferSize);
            var contentBuffer = rentContentBuffer.AsMemory()[..contentBufferSize];

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await stream.FillAsync(contentBuffer, cancellationToken).ConfigureAwait(false);

                return ReadContent(header, contentBuffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentContentBuffer);
            }
        }

        private async Task<SphynxPacketHeader> ReceiveHeaderAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var header = await SphynxPacketHeader.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);

            // TODO: Add proper version handling
            if (header.Version > Version)
            {
                throw new SerializationException(
                    $"Could not deserialize packet of type {header.PacketType} against newer" +
                    $"version ({header.Version} > {Version})");
            }

            return header;
        }

        private SphynxPacket ReadContent(SphynxPacketHeader header, Memory<byte> buffer)
        {
            var serializer = _serializers[header.PacketType];

            // TODO: Maybe just continue reading, instead of throwing,
            // to protect against the server being overflowed with random bytes
            if (!serializer.TryDeserialize(buffer.Span, out var packet, out _))
                throw new SerializationException($"Could not deserialize packet of type {header.PacketType}");

            return packet;
        }

        public IPacketSerializer<T> GetSerializer<T>(SphynxPacketType packetType)
            where T : SphynxPacket
        {
            var serializer = _serializers[packetType];

            if (serializer is SerializerAdapter<T> adapter)
                return adapter.InnerSerializer;

            return (IPacketSerializer<T>)serializer;
        }

        public PacketTransporter AddSerializer<T>(SphynxPacketType packetType, IPacketSerializer<T> serializer)
            where T : SphynxPacket
        {
            ref var existingSerializer = ref CollectionsMarshal.GetValueRefOrAddDefault(_serializers, packetType, out bool exists);

            // Avoid extra allocations
            if (exists && existingSerializer is SerializerAdapter<T> adapter)
            {
                adapter.InnerSerializer = serializer;
            }
            else if (serializer is IPacketSerializer<SphynxPacket> baseSerializer)
            {
                existingSerializer = baseSerializer;
            }
            else
            {
                existingSerializer = new SerializerAdapter<T>(serializer);
            }

            return this;
        }

        public PacketTransporter RemoveSerializer(SphynxPacketType packetType)
        {
            _serializers.Remove(packetType);
            return this;
        }

        public bool ContainsSerializer(SphynxPacketType packetType)
        {
            return _serializers.ContainsKey(packetType);
        }

        public PacketTransporter ClearSerializers()
        {
            _serializers.Clear();
            return this;
        }

        #region Generic Adaptation

        private class SerializerAdapter<T> : IPacketSerializer<SphynxPacket>
            where T : SphynxPacket
        {
            internal IPacketSerializer<T> InnerSerializer { get; set; }

            public SerializerAdapter(IPacketSerializer<T> innerSerializer)
            {
                InnerSerializer = innerSerializer;
            }

            public int GetMaxSize(SphynxPacket packet)
            {
                return InnerSerializer.GetMaxSize((T)packet);
            }

            public bool TrySerializeUnsafe(SphynxPacket packet, Span<byte> buffer, out int bytesWritten)
            {
                return InnerSerializer.TrySerializeUnsafe((T)packet, buffer, out bytesWritten);
            }

            public bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out SphynxPacket? packet, out int bytesRead)
            {
                if (InnerSerializer.TryDeserialize(buffer, out var instance, out bytesRead))
                {
                    packet = instance;
                    return true;
                }

                packet = null;
                return false;
            }
        }

        #endregion
    }
}
