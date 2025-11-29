// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Persistence.Friend
{
    public interface IFriendRepository
    {
        Task<SphynxErrorInfo<SphynxChatFriendRequest?>> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatFriendRequest[]?>> GetFriendRequestsAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<Guid[]?>> GetFriendRequestUsersAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> RemoveFriendRequestAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatFriend[]?>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<Guid[]?>> GetFriendIdsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> IsFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
        Task<long> CountFriendsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<long> CountFriendRequestsAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default);
    }

    public enum FriendRequestType
    {
        All,
        Outgoing,
        Incoming,
    }
}
