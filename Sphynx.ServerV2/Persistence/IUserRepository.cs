// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Persistence
{
    public interface IUserRepository
    {
        event Action<ISphynxUserInfo>? UserCreated;
        event Action<ISphynxUserInfo>? UserDeleted;

        Task<SphynxErrorInfo<ISphynxSelfInfo?>> CreateUserAsync(SphynxUserCredentials credentials, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserAsync(ISphynxSelfInfo updatedUser, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ISphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ISphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default);
    }
}
