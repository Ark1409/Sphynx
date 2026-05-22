// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Services
{
    public interface IRoomService
    {
        Task<SphynxErrorInfo<SphynxChatDirectRoom?>> CreateDirectRoomAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatGroupRoom?>> CreateGroupRoomAsync(Guid ownerId,
            string? name = null,
            bool isPublic = true,
            string? password = null,
            string? passwordSalt = null,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatRoom?>> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatDirectRoom?>> GetDirectRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatGroupRoom?>> GetGroupRoomAsync(Guid roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatRoom[]?>> GetRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatDirectRoom[]?>> GetDirectRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatGroupRoom[]?>> GetGroupRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatGroupRoom?>> UpdateGroupRoomAsync(SphynxChatGroupRoom room, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatGroupMembership?>> AddMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatGroupMembership?>> GetMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatGroupMembership[]?>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<Guid[]?>> GetMemberIdsAsync(Guid groupId, CancellationToken cancellationToken = default);

        Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
        Task<int> CountMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> DeleteDirectRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> DeleteGroupRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
    }
}
