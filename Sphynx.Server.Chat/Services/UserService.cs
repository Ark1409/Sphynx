// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Server.Chat.Model;
using Sphynx.Server.Chat.Persistence;

namespace Sphynx.Server.Chat.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<SphynxErrorInfo> UpdateStatusAsync(Guid userId, SphynxUserStatus newStatus, CancellationToken cancellationToken = default)
        {
            return _userRepository.UpdateStatusAsync(userId, newStatus, cancellationToken);
        }

        public Task<SphynxErrorInfo<SphynxChatUser?>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetUserAsync(userId, cancellationToken);
        }

        public Task<SphynxErrorInfo<SphynxChatUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetUserAsync(userName, cancellationToken);
        }

        public Task<SphynxErrorInfo<SphynxChatUser[]?>> GetUsersAsync(Guid[] userIds, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetUsersAsync(userIds, cancellationToken);
        }

        public Task<SphynxErrorInfo<SphynxChatUser[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default)
        {
            return _userRepository.GetUsersAsync(userNames, cancellationToken);
        }
    }
}
