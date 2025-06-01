// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Persistence;

namespace Sphynx.Server.Auth.Services
{
    public interface IAuthService
    {
        Task<SphynxErrorInfo<SphynxAuthInfo?>> AuthenticateUserAsync(string userName, string password, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxAuthInfo?>> RegisterUserAsync(string userName, string password, CancellationToken cancellationToken = default);
    }
}
