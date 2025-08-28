// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Persistence.Auth
{
    public interface ISessionRepository
    {
        Task<SphynxErrorInfo> InsertAsync(SphynxSessionInfo sessionInfo, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxSessionInfo[]?>> GetAsync(Guid[] sessionIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxSessionInfo[]?>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAndUpdateExpiry(Guid sessionId, DateTimeOffset expiryTime,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<bool>> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<bool>> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<long>> CountSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> UpdateExpiryAsync(Guid sessionId, DateTimeOffset expiryTime, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<int>> DeleteAsync(Guid[] sessionIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<long>> DeleteSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxSessionInfo?>> DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }

    public static class SessionRepositoryExtensions
    {
        public static async Task<SphynxErrorInfo<DateTimeOffset?>> UpdateExpiryAsync(this ISessionRepository repository,
            Guid sessionId,
            TimeSpan expiryTime,
            CancellationToken cancellationToken = default)
        {
            var newExpiryTime = DateTimeOffset.UtcNow + expiryTime;
            var updateResult = await repository.UpdateExpiryAsync(sessionId, newExpiryTime, cancellationToken).ConfigureAwait(false);

            if (updateResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<DateTimeOffset?>(updateResult.ErrorCode, updateResult.Message);

            return newExpiryTime;
        }

        public static Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAndUpdateExpiry(this ISessionRepository repository,
            Guid sessionId,
            TimeSpan expiryTime,
            CancellationToken cancellationToken = default)
        {
            var newExpiryTime = DateTimeOffset.UtcNow + expiryTime;
            return repository.GetAndUpdateExpiry(sessionId, newExpiryTime, cancellationToken);
        }
    }
}
