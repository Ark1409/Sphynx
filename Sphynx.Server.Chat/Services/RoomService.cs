// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Chat.Model;
using Sphynx.Server.Chat.Persistence.Room;
using Sphynx.Server.Extensions;

namespace Sphynx.Server.Chat.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;

        public RoomService(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<SphynxErrorInfo<SphynxChatDirectRoom?>> CreateDirectRoomAsync(Guid userA, Guid userB,
            CancellationToken cancellationToken = default)
        {
            if (userA == default || userB == default)
                return SphynxErrorCode.INVALID_USER;

            var room = new SphynxChatDirectRoom
            {
                CreatedAt = DateTimeOffset.UtcNow,
                UserA = userA,
                UserB = userB,
            };

            var insertResult = await _roomRepository.InsertDirectRoomAsync(room, cancellationToken).ConfigureAwait(false);
            return insertResult.MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom?>> CreateGroupRoomAsync(Guid ownerId,
            string? name = null,
            bool isPublic = true,
            string? password = null,
            string? passwordSalt = null,
            CancellationToken cancellationToken = default)
        {
            if (ownerId == default)
                return SphynxErrorCode.INVALID_USER;

            if (password == null && passwordSalt != null || passwordSalt == null && password == null)
                return SphynxErrorCode.INVALID_ROOM;

            var room = new SphynxChatGroupRoom
            {
                CreatedAt = DateTimeOffset.UtcNow,
                OwnerId = ownerId,
                Name = name ?? $"{ownerId}'s Awesome Room",
                IsPublic = isPublic,
                PasswordHash = password,
                PasswordSalt = passwordSalt,
            };

            var insertResult = await _roomRepository.InsertGroupRoomAsync(room, cancellationToken).ConfigureAwait(false);
            return insertResult.MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatRoom?>> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetRoomAsync(roomId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatDirectRoom?>> GetDirectRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetDirectRoomAsync(roomId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom?>> GetGroupRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetGroupRoomAsync(roomId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatRoom[]?>> GetRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetRoomsAsync(roomIds, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatDirectRoom[]?>> GetDirectRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetDirectRoomsAsync(roomIds, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom[]?>> GetGroupRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetGroupRoomsAsync(roomIds, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom?>> UpdateGroupRoomAsync(SphynxChatGroupRoom room,
            CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.UpdateGroupRoomAsync(room, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupMembership?>> AddMemberAsync(Guid groupId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            // TODO: Broadcast JoinedRoomBroadcast if added
            return (await _roomRepository.AddMemberAsync(groupId, userId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.RemoveMemberAsync(groupId, userId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupMembership?>> GetMemberAsync(Guid groupId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetMemberAsync(groupId, userId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupMembership[]?>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetMembersAsync(groupId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo<Guid[]?>> GetMemberIdsAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.GetMemberIdsAsync(groupId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            return _roomRepository.IsMemberAsync(groupId, userId, cancellationToken);
        }

        public async Task<int> CountMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            return (int)await _roomRepository.CountMembersAsync(groupId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SphynxErrorInfo> DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.DeleteRoomAsync(roomId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo> DeleteDirectRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.DeleteDirectRoomAsync(roomId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }

        public async Task<SphynxErrorInfo> DeleteGroupRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return (await _roomRepository.DeleteGroupRoomAsync(roomId, cancellationToken).ConfigureAwait(false)).MaskServerError();
        }
    }
}
