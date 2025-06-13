// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Persistence
{
    public class NullUserRepository : IUserRepository
    {
        public event Action<SphynxAuthUser>? UserCreated;
        public Task<SphynxErrorInfo<SphynxAuthUser?>> InsertUserAsync(SphynxAuthUser user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateUserAsync(SphynxAuthUser updatedUser, CancellationToken cancellationToken = default)
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

        public Task<SphynxErrorCode> UpdateUserPasswordAsync(SnowflakeId userId, PasswordInfo password, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateUserPasswordAsync(string userName, PasswordInfo password, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
