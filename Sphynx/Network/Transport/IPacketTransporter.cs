// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;

namespace Sphynx.Network.Transport
{
    /// <summary>
    /// Represents a type that marshals and unmarshals <see cref="SphynxPacket">packets</see> to be
    /// sent and received over streams.
    /// </summary>
    public interface IPacketTransporter
    {
        /// <summary>
        /// Sends a single <paramref name="packet"/> to the underlying <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to send the packet to.</param>
        /// <param name="packet">The packet to send.</param>
        /// <param name="cancellationToken">A cancellation token for the send request.</param>
        /// <returns>A task representing the send operation.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="stream"/> is not writable.</exception>
        /// <exception cref="TaskCanceledException">If the send request is cancelled.</exception>
        ValueTask SendAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives a single packet from the underlying <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to receive the packet from.</param>
        /// <param name="cancellationToken">A cancellation token for the reception request.</param>
        /// <returns>A task representing the reception operation.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="stream"/> is not readable.</exception>
        /// <exception cref="TaskCanceledException">If the reception request is cancelled.</exception>
        ValueTask<SphynxPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
