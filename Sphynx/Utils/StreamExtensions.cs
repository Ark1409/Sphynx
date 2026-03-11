// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    internal static class StreamExtensions
    {
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public static async ValueTask FillAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int readCount = 0;

            while (readCount < buffer.Length)
            {
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (bytesRead <= 0)
                    ThrowEndOfStreamException();

                readCount += bytesRead;
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowEndOfStreamException() => throw new EndOfStreamException();
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<int> TryFillAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int readCount = 0;

            while (readCount < buffer.Length)
            {
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (bytesRead <= 0)
                    return readCount;

                readCount += bytesRead;
            }

            return readCount;
        }

        public static ValueTask<int> SkipAsync(this Stream stream, int count, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<int>(cancellationToken);

            if (count == 0)
                return ValueTask.FromResult(0);

            if (stream.CanSeek)
            {
                // Usually, seek operations are fast
                long startPos = stream.Position;
                int skipSize = (int)(stream.Seek(count, SeekOrigin.Current) - startPos);

                return ValueTask.FromResult(skipSize);
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
