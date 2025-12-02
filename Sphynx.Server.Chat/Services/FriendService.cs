// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Chat.Model;
using Sphynx.Server.Chat.Persistence.Friend;
using Sphynx.Server.Extensions;

namespace Sphynx.Server.Chat.Services
{
    public class FriendService : IFriendService
    {
        private readonly IFriendRepository _friendService;

        public FriendService(IFriendRepository friendService)
        {
            _friendService = friendService;
        }

        public async Task<SphynxErrorInfo<SphynxChatFriendRequest?>> AddFriendAsync(Guid userId, Guid friendId,
            CancellationToken cancellationToken = default)
        {
            return (await _friendService.AddFriendAsync(userId, friendId, cancellationToken)).MaskServerError();
        }

        public async Task<SphynxErrorInfo> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            return (await _friendService.RemoveFriendAsync(userId, friendId, cancellationToken)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatFriendRequest[]?>> GetFriendRequestsAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default)
        {
            return (await _friendService.GetFriendRequestsAsync(userId, type, cancellationToken)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<Guid[]?>> GetFriendRequestUsersAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default)
        {
            return (await _friendService.GetFriendRequestUsersAsync(userId, type, cancellationToken)).MaskServerError();
        }

        public async Task<SphynxErrorInfo> RemoveFriendRequestAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            return (await _friendService.RemoveFriendRequestAsync(userId, friendId, cancellationToken)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatFriend[]?>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return (await _friendService.GetFriendsAsync(userId, cancellationToken)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<Guid[]?>> GetFriendIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return (await _friendService.GetFriendIdsAsync(userId, cancellationToken)).MaskServerError();
        }

        public Task<bool> IsFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            return _friendService.IsFriendAsync(userId, friendId, cancellationToken);
        }

        public Task<long> CountFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _friendService.CountFriendsAsync(userId, cancellationToken);
        }

        public Task<long> CountFriendRequestsAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default)
        {
            return _friendService.CountFriendRequestsAsync(userId, type, cancellationToken);
        }
    }
}
