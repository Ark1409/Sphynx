// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Sockets;

namespace Sphynx.Server.Auth.Extensions
{
    public static class ExceptionExtensions
    {
        public static bool IsTransient(this Exception ex)
        {
            SocketException? socketException = ex as SocketException ?? ex.InnerException as SocketException;

            if (socketException is null)
                return false;

            switch (socketException.SocketErrorCode)
            {
                case SocketError.NetworkDown:
                case SocketError.TryAgain:
                case SocketError.HostUnreachable:
                    return true;

                default:
                    return false;
            }
        }
    }
}
