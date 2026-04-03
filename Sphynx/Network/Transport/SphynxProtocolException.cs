// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Transport
{
    public class SphynxProtocolException : Exception
    {
        public SphynxProtocolException() : base()
        {
        }

        public SphynxProtocolException(Exception? innerException) : this(null, innerException)
        {
        }

        public SphynxProtocolException(string? message) : base(message)
        {
        }

        public SphynxProtocolException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
