// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Transport
{
    /// <summary>
    /// A byte code indicating the type of data present within the current frame.
    /// </summary>
    public enum SphynxFrameType : byte
    {
        /// <summary>
        /// Indicates the non-graceful termination of a channel from the sender side. The receiver should disregard the channel's
        /// existing content once this is received.
        /// </summary>
        CHANNEL_ABORT = 0x0,

        /// <summary>
        /// Indicates that this frame holds an application-level data chunk. This type of frame may also be purposed to send
        /// out-of-band data over a separate connection (e.g. UDP).
        /// </summary>
        CHANNEL_DATA = 0x1,

        /// <summary>
        /// Indicates that the receiver has finished consuming the channel's data (releasing the read-end of the channel).
        /// </summary>
        CHANNEL_RELEASE = 0x2,
    }

    /// <summary>
    /// The possible control flags for a <seealso cref="SphynxFrameType.CHANNEL_DATA"/> frame.
    /// </summary>
    /// <seealso cref="SphynxFrameHeader.Flags"/>
    public static class ChannelDataFlags
    {
        /// <summary>
        /// Indicates no control flags are specified.
        /// </summary>
        public const byte NONE = 0x0;

        /// <summary>
        /// Indicates that this frame acts as the initiator of a new data channel. The frame may still contain application
        /// data within its body.
        /// </summary>
        public const byte CHANNEL_START = 0x1;

        /// <summary>
        /// Indicates that this will be the last frame sent within the channel. The frame may still contain application
        /// data within its body.
        /// </summary>
        public const byte CHANNEL_END = 0x2;
    }

    /// <summary>
    /// The possible control flags for a <seealso cref="SphynxFrameType.CHANNEL_RELEASE"/> frame.
    /// </summary>
    /// <seealso cref="SphynxFrameHeader.Flags"/>
    public static class ChannelReleaseFlags
    {
        /// <summary>
        /// Indicates no control flags are specified.
        /// </summary>
        public const byte NONE = 0x0;

        /// <summary>
        /// Indicates that the receiver will not process any of the channel's data and that it will simply be rejected. If the write-side of the
        /// channel is still open when this is sent, it signifies that the receiver will not be accepting any more frames from the specified channel
        /// until it is reset (i.e. it receives a <see cref="SphynxFrameType.CHANNEL_DATA"/> with the <see cref="ChannelDataFlags.CHANNEL_START"/>
        /// flag).
        /// </summary>
        public const byte CHANNEL_REJECTED = 0x1;
    }
}
