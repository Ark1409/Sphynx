// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Persistence
{
    public interface IUserRepository
    {
        event Action<SphynxAuthUser>? UserCreated;

        Task<SphynxErrorInfo<SphynxAuthUser?>> InsertUserAsync(SphynxAuthUser user, CancellationToken cancellationToken = default);

        Task<SphynxErrorCode> UpdateUserAsync(SphynxAuthUser updatedUser, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxPasswordInfo?>> GetUserPasswordAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxPasswordInfo?>> GetUserPasswordAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorCode> UpdateUserPasswordAsync(SnowflakeId userId, SphynxPasswordInfo password, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserPasswordAsync(string userName, SphynxPasswordInfo password, CancellationToken cancellationToken = default);
    }
}
