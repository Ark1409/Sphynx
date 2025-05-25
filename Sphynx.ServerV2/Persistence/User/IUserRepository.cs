// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public interface IUserRepository
    {
        event Action<ModelV2.User.SphynxUserInfo>? UserCreated;
        event Action<ModelV2.User.SphynxUserInfo>? UserDeleted;

        Task<SphynxErrorInfo<ModelV2.User.SphynxSelfInfo?>> InsertUserAsync(ModelV2.User.SphynxSelfInfo user, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserAsync(ModelV2.User.SphynxSelfInfo updatedUser, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ModelV2.User.SphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ModelV2.User.SphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ModelV2.User.SphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ModelV2.User.SphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ModelV2.User.SphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<ModelV2.User.SphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(SnowflakeId userId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateUserFieldAsync<T>(SnowflakeId userId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
