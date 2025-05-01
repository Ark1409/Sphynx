// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    internal static class StreamExtensions
    {
        public static async ValueTask FillAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
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
