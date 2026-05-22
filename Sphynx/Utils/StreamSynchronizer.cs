// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public readonly struct StreamSynchronizer : IDisposable, IAsyncDisposable
    {
        public SemaphoreSlim StreamLock { get; }
        public Stream Stream { get; }

        public StreamSynchronizer(Stream stream) : this(new SemaphoreSlim(1, 1), stream)
        {
        }

        public StreamSynchronizer(SemaphoreSlim streamLock, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(streamLock);
            ArgumentNullException.ThrowIfNull(stream);

            StreamLock = streamLock;
            Stream = stream;
        }

        public ValueInvokeOnDisposal<SemaphoreSlim> RentLock(CancellationToken cancellationToken = default)
        {
            return StreamLock.Rent();
        }

        public ValueTask<ValueInvokeOnDisposal<SemaphoreSlim>> RentLockAsync(CancellationToken cancellationToken = default)
        {
            return StreamLock.RentAsync(cancellationToken);
        }

        public void Lock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            StreamLock.Wait(timeout, cancellationToken);
        }

        public void Lock(CancellationToken cancellationToken = default)
        {
            StreamLock.Wait(cancellationToken);
        }

        public Task LockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return StreamLock.WaitAsync(timeout, cancellationToken);
        }

        public Task LockAsync(CancellationToken cancellationToken = default)
        {
            return StreamLock.WaitAsync(cancellationToken);
        }

        public void Unlock()
        {
            StreamLock.Release();
        }

        public void Dispose()
        {
            StreamLock.Dispose();
            Stream.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                StreamLock.Dispose();
                return Stream.DisposeAsync();
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }
        }
    }
}
