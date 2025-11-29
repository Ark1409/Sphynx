// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Services
{
    public interface IUserService
    {
        Task<SphynxErrorInfo> UpdateStatusAsync(Guid userId, SphynxUserStatus newStatus, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatUser?>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatUser[]?>> GetUsersAsync(Guid[] userIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatUser[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default);
    }
}
