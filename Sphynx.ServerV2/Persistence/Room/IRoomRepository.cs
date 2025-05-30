// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.ServerV2.Persistence.Room
{
    public interface IRoomRepository
    {
        event Action<ChatRoomInfo>? RoomCreated;
        event Action<ChatRoomInfo>? RoomDeleted;

        Task<SphynxErrorInfo<ChatRoomInfo?>> InsertRoomAsync(ChatRoomInfo roomInfo, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateRoomAsync(ChatRoomInfo updatedRoom, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> DeleteRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ChatRoomInfo?>> GetRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<ChatRoomInfo[]?>> GetRoomsAsync(SnowflakeId[] roomIds, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
