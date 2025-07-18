// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public class NullUserRepository : IUserRepository
    {
        public event Action<SphynxDbUser>? UserCreated;
        public event Action<SphynxDbUser>? UserDeleted;

        public Task<SphynxErrorInfo<SphynxDbUser?>> InsertUserAsync(SphynxDbUser user, CancellationToken cancellationToken = default)
        {
            UserCreated?.Invoke(user);

            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser?>(user));
        }

        public Task<SphynxErrorCode> UpdateUserAsync(SphynxDbUser updatedUser, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        public Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var newUser = GetNullUser();
            newUser.UserId = userId;

            UserDeleted?.Invoke(newUser);

            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        public Task<SphynxErrorInfo<SphynxDbUser?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var user = GetNullUser();
            user.UserId = userId;

            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser?>(user));
        }

        public Task<SphynxErrorInfo<SphynxDbUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            var user = GetNullUser();
            user.UserName = userName;

            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser?>(user));
        }

        public Task<SphynxErrorInfo<SphynxDbUser?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var user = GetNullUser();
            user.UserId = userId;

            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser?>(user));
        }

        public Task<SphynxErrorInfo<SphynxDbUser?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser?>(GetNullUser()));
        }

        public Task<SphynxErrorInfo<SphynxDbUser[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default)
        {
            // var user = GetNullUser();
            // user.UserId = userId;
            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser[]?>(new[] { GetNullUser() }));
        }

        public Task<SphynxErrorInfo<SphynxDbUser[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo<SphynxDbUser[]?>(new[] { GetNullUser() }));
        }

        public Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(SnowflakeId userId, string fieldName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo<T?>(default));
        }

        public Task<SphynxErrorCode> UpdateUserFieldAsync<T>(SnowflakeId userId, string fieldName, T value,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        private static SphynxDbUser GetNullUser() => new SphynxDbUser(default, string.Empty, default);
    }
}
