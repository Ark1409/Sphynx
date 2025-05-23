// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public class NullUserRepository : IUserRepository
    {
        public event Action<ISphynxUserInfo>? UserCreated;
        public event Action<ISphynxUserInfo>? UserDeleted;

        public Task<SphynxErrorInfo<ISphynxSelfInfo?>> InsertUserAsync(ISphynxSelfInfo user, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Inserted user: {user.UserName}");
            return Task.FromResult(new SphynxErrorInfo<ISphynxSelfInfo?>(SphynxErrorCode.SERVER_ERROR));
        }

        public Task<SphynxErrorCode> UpdateUserAsync(ISphynxSelfInfo updatedUser, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Updated user : {updatedUser.UserName}");
            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        public Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        public Task<SphynxErrorInfo<ISphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(SphynxErrorInfo<ISphynxUserInfo?>));
        }

        public Task<SphynxErrorInfo<ISphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Getting user : {userName}");
            return Task.FromResult(default(SphynxErrorInfo<ISphynxUserInfo?>));
        }

        public Task<SphynxErrorInfo<ISphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Getting self : {userId}");
            return Task.FromResult(default(SphynxErrorInfo<ISphynxSelfInfo?>));
        }

        public Task<SphynxErrorInfo<ISphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Getting self : {userName}");
            return Task.FromResult(default(SphynxErrorInfo<ISphynxSelfInfo?>));
        }

        public Task<SphynxErrorInfo<ISphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<ISphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(SnowflakeId userId, string fieldName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateUserFieldAsync<T>(SnowflakeId userId, string fieldName, T value,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
