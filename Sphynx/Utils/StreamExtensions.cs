// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    internal static class StreamExtensions
    {
        public static ValueTask FillAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return Core(stream, buffer, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Core(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
            {
                int readCount = 0;

                while (readCount < buffer.Length)
                {
                    int bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (bytesRead <= 0)
                        throw new EndOfStreamException();

                    readCount += bytesRead;
                }
            }
        }
    }
}
