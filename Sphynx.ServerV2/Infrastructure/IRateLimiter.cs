// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ServerV2.Infrastructure
{
    public interface IRateLimiter : IDisposable
    {
        int MaxOperations { get; }
        TimeSpan TimeWindow { get; }

        double RateSeconds => MaxOperations / TimeWindow.TotalSeconds;

        ValueTask<TimeSpan> ConsumeTokensAsync(int count = 1, CancellationToken cancellationToken = default);
    }
}
