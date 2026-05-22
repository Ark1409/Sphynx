namespace Sphynx.Network.Packet
{
    /// <summary>
    /// A class representing solely the content of a Sphynx message.
    /// </summary>
    public abstract class SphynxMessage : IEquatable<SphynxMessage>
    {
        /// <summary>
        /// The type of message for which this content is purposed.
        /// </summary>
        public abstract SphynxMessageType MessageType { get; }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxMessage? other) => MessageType == other?.MessageType;

        public override string ToString() => $"{MessageType}";
    }
}
