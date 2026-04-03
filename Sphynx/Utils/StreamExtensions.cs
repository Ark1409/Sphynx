// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    internal static class StreamExtensions
    {
        public static ValueTask<int> SkipAsync(this Stream stream, int count, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<int>(cancellationToken);

            if (count < 0)
                return ValueTask.FromException<int>(new ArgumentOutOfRangeException(nameof(count)));

            if (count == 0)
                return ValueTask.FromResult(0);

            // Usually, seek operations are fast
            if (stream.CanSeek)
            {
                try
                {
                    long startPos = stream.Position;
                    int skipSize = (int)(stream.Seek(count, SeekOrigin.Current) - startPos);
                    return ValueTask.FromResult(skipSize);
                }
                catch (Exception ex)
                {
                    return ValueTask.FromException<int>(ex);
                }
            }

            return Core(stream, count, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<int> Core(Stream stream, int count, CancellationToken cancellationToken)
            {
                int skipSize = Math.Min(count, 1024);
                byte[] skipBuffer = ArrayPool<byte>.Shared.Rent(skipSize);
                var skipMemory = skipBuffer.AsMemory()[..skipSize];

                int skipCount = 0;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    while (skipCount < count)
                    {
                        int skipChunk = Math.Min(count - skipCount, skipSize);
                        int bytesRead = await stream.ReadAsync(skipMemory[..skipChunk], CancellationToken.None)
                            .ConfigureAwait(false);

                        if (bytesRead <= 0)
                            break;

                        skipCount += bytesRead;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(skipBuffer);
                }

                return skipCount;
            }
        }
    }
}
