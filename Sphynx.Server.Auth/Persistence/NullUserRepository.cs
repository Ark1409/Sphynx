// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Persistence
{
    public class NullUserRepository : IAuthUserRepository
    {
        public event Action<SphynxAuthUser>? UserCreated;
        public Task<SphynxErrorInfo<SphynxAuthUser?>> InsertUserAsync(SphynxAuthUser user, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Insernting user: {user.UserName}");
            return Task.FromResult(new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.DB_WRITE_ERROR));
        }

        public Task<SphynxErrorInfo> UpdateUserAsync(SphynxAuthUser updatedUser, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(string userName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo> UpdateUserPasswordAsync(SnowflakeId userId, PasswordInfo password, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo> UpdateUserPasswordAsync(string userName, PasswordInfo password, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
