// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ServerV2.Persistence.Auth;

namespace Sphynx.ServerV2.Auth
{
    public readonly record struct SphynxSessionInfo(
        Guid SessionId,
        Guid UserId,
        string IpAddress,
        DateTimeOffset ExpiresAt,
        DateTimeOffset CreatedAt);

    public static class SphynxSessionInfoExtensions
    {
        public static SphynxDbSession ToRecord(this SphynxSessionInfo domain)
        {
            return new SphynxDbSession
            {
                SessionId = domain.SessionId,
                UserId = domain.UserId,
                IpAddress = domain.IpAddress,
                ExpiresAt = domain.ExpiresAt,
                CreatedAt = domain.CreatedAt,
            };
        }

        public static SphynxSessionInfo ToDomain(this SphynxDbSession record)
        {
            return new SphynxSessionInfo
            {
                SessionId = record.SessionId,
                UserId = record.UserId,
                IpAddress = record.IpAddress,
                ExpiresAt = record.ExpiresAt,
                CreatedAt = record.CreatedAt,
            };
        }
    }
}
