// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Transport
{
    public class SphynxProtocolException : Exception
    {
        public ChannelId? ChannelId { get; }

        public SphynxProtocolException() : base()
        {
        }

        public SphynxProtocolException(ChannelId? channelId) : this()
        {
            ChannelId = channelId;
        }

        public SphynxProtocolException(Exception? innerException) : base(null, innerException)
        {
        }

        public SphynxProtocolException(ChannelId? channelId, Exception? innerException) : this(innerException)
        {
            ChannelId = channelId;
        }

        public SphynxProtocolException(string? message) : base(message)
        {
        }

        public SphynxProtocolException(ChannelId? channelId, string? message) : this(GetMessage(channelId, message))
        {
            ChannelId = channelId;
        }

        public SphynxProtocolException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public SphynxProtocolException(ChannelId? channelId, string? message, Exception? innerException)
            : this(GetMessage(channelId, message), innerException)
        {
            ChannelId = channelId;
        }

        private static string? GetMessage(ChannelId? channelId, string? message)
        {
            if (channelId == null)
                return message;

            return message + $" ({nameof(ChannelId)}: {channelId})";
        }
    }
}
