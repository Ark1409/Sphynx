// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.ServerV2.Persistence.Room
{
    public interface IRoomRepository
    {
        event Action<IChatRoomInfo>? RoomCreated;
        event Action<IChatRoomInfo>? RoomDeleted;

        Task<SphynxErrorInfo<IChatRoomInfo?>> CreateRoomAsync(IChatRoomInfo roomInfo, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateRoomAsync(IChatRoomInfo updatedRoom, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> DeleteRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<IChatRoomInfo?>> GetRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<IChatRoomInfo[]?>> GetRoomsAsync(SnowflakeId[] roomIds, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorCode> UpdateRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
