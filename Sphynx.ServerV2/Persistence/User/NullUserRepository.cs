// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public class NullUserRepository : IUserRepository
    {
        public event Action<SphynxUserInfo>? UserCreated;
        public event Action<SphynxUserInfo>? UserDeleted;

        public Task<SphynxErrorInfo<SphynxSelfInfo?>> InsertUserAsync(SphynxSelfInfo user, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Inserted user: {user.UserName}");
            return Task.FromResult(new SphynxErrorInfo<SphynxSelfInfo?>(SphynxErrorCode.SERVER_ERROR));
        }

        public Task<SphynxErrorCode> UpdateUserAsync(SphynxSelfInfo updatedUser, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Updated user : {updatedUser.UserName}");
            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        public Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SphynxErrorCode.SUCCESS);
        }

        public Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(SphynxErrorInfo<SphynxUserInfo?>));
        }

        public Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Getting user : {userName}");
            return Task.FromResult(default(SphynxErrorInfo<SphynxUserInfo?>));
        }

        public Task<SphynxErrorInfo<SphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Getting self : {userId}");
            return Task.FromResult(default(SphynxErrorInfo<SphynxSelfInfo?>));
        }

        public Task<SphynxErrorInfo<SphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Getting self : {userName}");
            return Task.FromResult(default(SphynxErrorInfo<SphynxSelfInfo?>));
        }

        public Task<SphynxErrorInfo<SphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<SphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default)
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
