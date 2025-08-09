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
        public int MaxPacketSize { get; set; } = int.MaxValue - 1;
        public ITypeSerializer<SphynxPacket> PacketSerializer { get; set; }

        public Version Version { get; set; }

        public PacketTransporter(ITypeSerializer<SphynxPacket> packetSerializer)
        {
            PacketSerializer = packetSerializer ?? throw new ArgumentNullException(nameof(packetSerializer));
        }

        // NOTE: not async to reduce state machine overhead
        public ValueTask SendAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (!stream.CanWrite)
                return ValueTask.FromException(new ArgumentException("Stream must be writable", nameof(stream)));

            var sequenceRental = SequencePool.Shared.Rent();
            var sequence = sequenceRental.Value;

            SphynxPacketHeader header;

            bool successfullySerialized = false;
            try
            {
                PacketSerializer.Serialize(packet, sequence);

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                long contentSize = sequence.Length;

                if (contentSize < 0)
                    return ValueTask.FromException(new SerializationException(
                        $"Packet {packet} serialized with negative size ({contentSize} bytes)"));

                if (contentSize > MaxPacketSize)
                    return ValueTask.FromException(new PacketTooLargeException(packet, (int)contentSize,
                        $"Packet {packet} is too large to serialize ({contentSize} bytes)"));

                header = new SphynxPacketHeader(Version, packet.PacketType, (int)contentSize);
                successfullySerialized = true;
            }
            finally
            {
                if (!successfullySerialized)
                    sequenceRental.Dispose();
            }

            return SendAsyncCore(stream, header, sequenceRental, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask SendAsyncCore(Stream stream, SphynxPacketHeader header, SequencePool.Rental rental, CancellationToken token)
            {
                try
                {
                    await SphynxPacketHeader.SendAsync(in header, stream, cancellationToken: token).ConfigureAwait(false);

                    foreach (ReadOnlyMemory<byte> segment in rental.Value.AsReadOnlySequence)
                        await stream.WriteAsync(segment, cancellationToken: token).ConfigureAwait(false);
                }
                finally
                {
                    rental.Dispose();
                }
            }
        }

        public ValueTask<SphynxPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<SphynxPacket>(cancellationToken);

            if (!stream.CanRead)
                return ValueTask.FromException<SphynxPacket>(new ArgumentException("Stream must be readable", nameof(stream)));

            return ReceiveAsyncCore(this, stream, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<SphynxPacket> ReceiveAsyncCore(PacketTransporter transporter, Stream stream, CancellationToken token)
            {
                var header = await SphynxPacketHeader.ReceiveAsync(stream, token).ConfigureAwait(false);

                // TODO: Add proper version handling
                if (header.Version > transporter.Version)
                    throw new SerializationException(
                        $"Could not deserialize packet of type {header.PacketType} against new version ({header.Version} > {transporter.Version})");

                if (header.ContentSize < 0)
                    throw new SerializationException($"Packet {header.PacketType} deserialized with negative size ({header.ContentSize} bytes)");

                if (header.ContentSize > transporter.MaxPacketSize)
                    throw new PacketTooLargeException(null, header.ContentSize,
                        $"Packet {header.PacketType} is to large to deserialize ({header.ContentSize} bytes)");

                using (var sequenceRental = SequencePool.Shared.Rent())
                {
                    var sequence = sequenceRental.Value;

                    int bytesRead = 0;
                    do
                    {
                        var memory = sequence.GetMemory(sizeHint: 0);

                        Debug.Assert(memory.Length > 0);

                        int chunkSize = Math.Min(header.ContentSize - bytesRead, memory.Length);

                        await stream.FillAsync(memory[..chunkSize], token).ConfigureAwait(false);
                        sequence.Advance(chunkSize);

                        bytesRead += chunkSize;
                    } while (bytesRead < header.ContentSize);

                    return transporter.PacketSerializer.Deserialize(sequence.AsReadOnlySequence) ?? throw new SerializationException(
                        $"An error occured while deserializing packet of type {header.PacketType} (no information)");
                }
            }
        }
    }
}
