// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Sockets;

namespace Sphynx.Server.Extensions
{
    public static class ExceptionExtensions
    {
        public static bool IsCancellationException(this Exception ex)
        {
            switch (ex)
            {
                case TaskCanceledException:
                case OperationCanceledException:
                    return true;

                case SocketException se when se.SocketErrorCode is SocketError.OperationAborted or SocketError.ConnectionAborted:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsConnectionResetException(this Exception ex)
        {
            SocketException? se = ex as SocketException ?? (ex as IOException)?.InnerException as SocketException;

            return se?.SocketErrorCode == SocketError.ConnectionReset;
        }
    }
}
