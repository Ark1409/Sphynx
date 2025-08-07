// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Nerdbank.Streams;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Storage;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public class PacketTransporter : IPacketTransporter
    {
        public ITypeSerializer<SphynxPacket> PacketSerializer { get; set; }

        public Version Version { get; set; }

        public PacketTransporter(ITypeSerializer<SphynxPacket> packetSerializer)
        {
            PacketSerializer = packetSerializer;
        }

        [ThreadStatic] private static byte[]? _scratchHeaderArray;

        // NOTE: not async to reduce state machine overhead
        public ValueTask SendAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (!stream.CanWrite)
                return ValueTask.FromException(new ArgumentException("Stream must be writable", nameof(stream)));

            using (var rental = SequencePool.Shared.Rent())
            {
                var sequence = rental.Value;

                PacketSerializer.Serialize(packet, sequence);

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                long contentSize = sequence.Length;

                if (contentSize >= int.MaxValue)
                    return ValueTask.FromException(new SerializationException($"Packet {packet} is to large to serialize ({contentSize} bytes)"));

                var header = new SphynxPacketHeader(Version, packet.PacketType, (int)contentSize);

                if (!header.TrySerialize(_scratchHeaderArray ??= new byte[SphynxPacketHeader.Size]))
                    return ValueTask.FromException(new SerializationException($"Could not serialize header for packet {packet}"));

                return SendAsyncCore(stream, _scratchHeaderArray, sequence.AsReadOnlySequence, cancellationToken);
            }

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask SendAsyncCore(Stream stream, ReadOnlyMemory<byte> header, ReadOnlySequence<byte> content, CancellationToken token)
            {
                await stream.WriteAsync(header, token).ConfigureAwait(false);

                foreach (ReadOnlyMemory<byte> segment in content)
                    await stream.WriteAsync(segment, cancellationToken: token).ConfigureAwait(false);
            }
        }

        public async ValueTask<SphynxPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            var header = await SphynxPacketHeader.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);

            // TODO: Add proper version handling
            if (header.Version > Version)
            {
                throw new SerializationException(
                    $"Could not deserialize packet of type {header.PacketType} against newer" +
                    $"version ({header.Version} > {Version})");
            }

            // TODO: Length sanity check

            using (var rental = SequencePool.Shared.Rent())
            {
                var sequence = rental.Value;

                await ReceiveAsyncCore(stream, sequence, header.ContentSize, cancellationToken).ConfigureAwait(false);

                return PacketSerializer.Deserialize(sequence.AsReadOnlySequence) ?? throw new SerializationException("An error occured while " +
                    $"deserializing packet of type {header.PacketType} (no information)");
            }

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask ReceiveAsyncCore(Stream stream, Sequence<byte> sequence, int contentSize, CancellationToken token)
            {
                int bytesRead = 0;
                do
                {
                    var memory = sequence.GetMemory(sizeHint: 0);

                    Debug.Assert(memory.Length > 0);

                    int chunkSize = Math.Min(contentSize - bytesRead, memory.Length);

                    await stream.FillAsync(memory[..chunkSize], token).ConfigureAwait(false);
                    sequence.Advance(chunkSize);

                    bytesRead += chunkSize;
                } while (bytesRead < contentSize);
            }
        }
    }
}
