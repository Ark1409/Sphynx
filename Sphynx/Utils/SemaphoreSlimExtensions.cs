// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils;

public static class SempahoreSlimExtensions
{
    public static async ValueTask<ValueInvokeOnDisposal<SemaphoreSlim>> RentAsync(this SemaphoreSlim sem, CancellationToken cancellationToken = default)
    {
        await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new(sem, static state => state.Release());
    }

    public static ValueInvokeOnDisposal<SemaphoreSlim> Rent(this SemaphoreSlim sem, CancellationToken cancellationToken = default)
    {
        sem.Wait(cancellationToken);
        return new(sem, static state => state.Release());
    }
}
