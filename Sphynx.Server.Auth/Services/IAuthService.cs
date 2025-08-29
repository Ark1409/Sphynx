// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Sphynx.Core;
using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Services
{
    public interface IAuthService
    {
        Task<SphynxErrorInfo<SphynxAuthResult?>> LoginUserAsync(SphynxLoginInfo loginInfo, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxAuthResult?>> RegisterUserAsync(SphynxLoginInfo registerInfo, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<int>> LogoutUserAsync(Guid sessionId, LogoutPolicy policy = LogoutPolicy.Self,
            CancellationToken cancellationToken = default);
    }

    public readonly record struct SphynxLoginInfo(string UserName, string Password, IPAddress ClientAddress);

    public enum LogoutPolicy : byte
    {
        /// <summary>
        /// Logout of only this session.
        /// </summary>
        Self,

        /// <summary>
        /// Logout of all sessions for this user.
        /// </summary>
        Global
    }
}
