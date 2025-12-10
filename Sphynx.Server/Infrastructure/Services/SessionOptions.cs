// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Server.Infrastructure.Services
{
    public readonly record struct SessionOptions(TimeSpan ActiveExpiryTime, TimeSpan ExpiryTime)
    {
        public static readonly SessionOptions Default = new()
        {
            ActiveExpiryTime = TimeSpan.FromMinutes(5),
            ExpiryTime = TimeSpan.FromDays(90),
        };
    }
}
