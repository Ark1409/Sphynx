// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public static class SemaphoreSlimExtensions
    {
        public static ValueTask<ValueInvokeOnDisposal<SemaphoreSlim>> RentAsync(this SemaphoreSlim sem, CancellationToken cancellationToken = default)
        {
            var waitAsync = sem.WaitAsync(cancellationToken);

            if (waitAsync.IsCompleted)
            {
                if (waitAsync.IsCompletedSuccessfully)
                    return ValueTask.FromResult(new ValueInvokeOnDisposal<SemaphoreSlim>(sem, static state => state.Release()));

                return waitAsync.IsCanceled
                    ? ValueTask.FromCanceled<ValueInvokeOnDisposal<SemaphoreSlim>>(cancellationToken)
                    : ValueTask.FromException<ValueInvokeOnDisposal<SemaphoreSlim>>(waitAsync.Exception!);
            }

            return Core(sem, waitAsync);

            static async ValueTask<ValueInvokeOnDisposal<SemaphoreSlim>> Core(SemaphoreSlim sem, Task waitTask)
            {
                await waitTask.ConfigureAwait(false);
                return new ValueInvokeOnDisposal<SemaphoreSlim>(sem, static state => state.Release());
            }
        }

        public static ValueInvokeOnDisposal<SemaphoreSlim> Rent(this SemaphoreSlim sem, CancellationToken cancellationToken = default)
        {
            sem.Wait(cancellationToken);
            return new ValueInvokeOnDisposal<SemaphoreSlim>(sem, static state => state.Release());
        }
    }
}
