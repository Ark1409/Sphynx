// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Packet.Broadcast
{
    public abstract class SphynxBroadcast : SphynxMessage, IEquatable<SphynxBroadcast>
    {
        public sealed override SphynxMessageType MessageType => SphynxMessageType.Broadcast;

        /// <summary>
        /// The type of this broadcast message.
        /// </summary>
        public abstract SphynxBroadcastType BroadcastType { get; }

        public virtual bool Equals(SphynxBroadcast? other) => base.Equals(other) && BroadcastType == other?.BroadcastType;
    }
}
