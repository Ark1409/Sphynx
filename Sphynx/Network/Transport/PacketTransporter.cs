// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization.Model;
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

        public async ValueTask SendAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default)
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

                var contentBuffer = buffer[SphynxPacketHeader.Size..];

                if (!serializer.TrySerialize(packet, contentBuffer.Span, out int contentSize))
                    throw new SerializationException(
                        $"Could not serialize packet {packet.GetType()} ({packet.PacketType}) with serializer {serializer.GetType()}.");

                var header = new SphynxPacketHeader(Version, packet.PacketType, contentSize);
                var headerBuffer = buffer[..SphynxPacketHeader.Size];

                if (!header.TrySerialize(headerBuffer.Span))
                    throw new SerializationException($"Could not serialize header for packet {packet.GetType()}");

                cancellationToken.ThrowIfCancellationRequested();

                int packetSize = SphynxPacketHeader.Size + contentSize;
                await stream.WriteAsync(buffer[..packetSize], cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
            }
        }

        public async ValueTask<SphynxPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var header = await SphynxPacketHeader.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);

            // TODO: Add proper version handling
            if (header.Version > Version)
                throw new SerializationException(
                    $"Could not deserialize packet of type {header.PacketType} against newer" +
                    $"version ({header.Version} > {Version})");

            int contentBufferSize = header.ContentSize;
            byte[] rentContentBuffer = ArrayPool<byte>.Shared.Rent(contentBufferSize);
            var contentBuffer = rentContentBuffer.AsMemory()[..contentBufferSize];

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await stream.FillAsync(contentBuffer, cancellationToken).ConfigureAwait(false);

                var serializer = _serializers[header.PacketType];

                if (!serializer.TryDeserialize(contentBuffer.Span, out var packet, out _))
                    throw new SerializationException($"Could not deserialize packet of type {header.PacketType}");

                return packet;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentContentBuffer);
            }
        }

        public IPacketSerializer<T> GetSerializer<T>(SphynxPacketType packetType)
            where T : SphynxPacket
        {
            SerializerAdapter<T> serializerAdapter = (SerializerAdapter<T>)_serializers[packetType];
            return serializerAdapter.InnerSerializer;
        }

        public PacketTransporter AddSerializer<T>(SphynxPacketType packetType, IPacketSerializer<T> serializer)
            where T : SphynxPacket
        {
            ref var existingAdapter =
                ref CollectionsMarshal.GetValueRefOrAddDefault(_serializers, packetType, out bool exists);

            // TODO: Check if `T` is SphynxPacket and perform optimization.
            //  Also check if `T` is already a serializer adapter

            // Avoid extra allocations
            if (exists && existingAdapter is SerializerAdapter<T> adapter)
            {
                adapter.InnerSerializer = serializer;
            }
            else
            {
                existingAdapter = new SerializerAdapter<T>(serializer);
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

            public bool TrySerialize(SphynxPacket packet, Span<byte> buffer, out int bytesWritten)
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
