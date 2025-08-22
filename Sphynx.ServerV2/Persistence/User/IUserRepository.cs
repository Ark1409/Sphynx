// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public interface IUserRepository
    {
        event Action<SphynxDbUser>? UserCreated;
        event Action<SphynxDbUser>? UserDeleted;

        Task<SphynxErrorInfo<SphynxDbUser?>> InsertUserAsync(SphynxDbUser user, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> UpdateUserAsync(SphynxDbUser updatedUser, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxDbUser?>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxDbUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxDbUser?>> GetSelfAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxDbUser?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxDbUser[]?>> GetUsersAsync(Guid[] userIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxDbUser[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(Guid userId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> UpdateUserFieldAsync<T>(Guid userId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
