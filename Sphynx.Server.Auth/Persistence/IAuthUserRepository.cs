// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Persistence
{
    public interface IAuthUserRepository
    {
        Task<SphynxErrorInfo<SphynxAuthUser?>> InsertUserAsync(SphynxAuthUser user, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> UpdateUserAsync(SphynxAuthUser updatedUser, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> UpdateUserPasswordAsync(SnowflakeId userId, PasswordInfo password, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> UpdateUserPasswordAsync(string userName, PasswordInfo password, CancellationToken cancellationToken = default);
    }
}
