// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public interface IUserRepository
    {
        event Action<ISphynxUserInfo>? UserCreated;
        event Action<ISphynxUserInfo>? UserDeleted;

        Task<SphynxErrorInfo<ISphynxSelfInfo?>> InsertUserAsync(ISphynxSelfInfo user, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserAsync(ISphynxSelfInfo updatedUser, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ISphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ISphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ISphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(SnowflakeId userId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserFieldAsync<T>(SnowflakeId userId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
