// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Persistence.User
{
    public interface IUserRepository
    {
        event Action<SphynxUserInfo>? UserCreated;
        event Action<SphynxUserInfo>? UserDeleted;

        Task<SphynxErrorInfo<SphynxSelfInfo?>> InsertUserAsync(SphynxSelfInfo user, CancellationToken cancellationToken = default);

        Task<SphynxErrorCode> UpdateUserAsync(SphynxSelfInfo updatedUser, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(SnowflakeId userId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserFieldAsync<T>(SnowflakeId userId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
