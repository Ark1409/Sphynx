// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Transport
{
    public class ChannelClosedException : Exception
    {
        public Exception? DisposeException => InnerException;

        public ChannelClosedException() :base()
        {
        }

        public ChannelClosedException(Exception? innerException) : this(null, innerException)
        {
        }

        public ChannelClosedException(string? message) : base(message)
        {
        }

        public ChannelClosedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
