// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public interface ISessionService
    {
        Task<SphynxErrorInfo<bool>> IsActiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<bool>> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxSessionInfo?>> CreateSessionAsync(Guid userId, IPAddress clientAddress,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<long>> CountActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<long>> CountSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxSessionInfo?>> ReviveSessionAsync(Guid sessionId, SessionUpdatePolicy updatePolicy = SessionUpdatePolicy.Ephemeral,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxSessionInfo?>> RevokeSessionAsync(Guid sessionId, SessionUpdatePolicy updatePolicy = SessionUpdatePolicy.WriteThrough,
            CancellationToken cancellationToken = default);
    }

    public enum SessionUpdatePolicy : byte
    {
        /// <summary>
        /// Only writes to cache.
        /// </summary>
        Ephemeral,

        /// <summary>
        /// Writes through the cache and database.
        /// </summary>
        WriteThrough,

        /// <summary>
        /// Writes to the cache and Performs a write-behind on the database.
        /// </summary>
        WriteBehind
    }
}
