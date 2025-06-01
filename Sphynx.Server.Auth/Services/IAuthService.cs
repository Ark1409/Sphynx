// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Services
{
    public interface IAuthService
    {
        Task<SphynxErrorInfo<SphynxAuthUser?>> AuthenticateUserAsync(string userName, string password, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxAuthUser?>> RegisterUserAsync(string userName, string password, CancellationToken cancellationToken = default);
    }
}
