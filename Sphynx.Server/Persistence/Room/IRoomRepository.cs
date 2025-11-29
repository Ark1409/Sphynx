// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.Room;

namespace Sphynx.Server.Persistence.Room
{
    public interface IRoomRepository
    {
        event Action<SphynxRoomInfo>? RoomCreated;
        event Action<SphynxRoomInfo>? RoomDeleted;

        Task<SphynxErrorInfo<SphynxRoomInfo?>> InsertRoomAsync(SphynxRoomInfo roomInfo, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> UpdateRoomAsync(SphynxRoomInfo updatedRoom, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> DeleteRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRoomInfo?>> GetRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRoomInfo[]?>> GetRoomsAsync(SnowflakeId[] roomIds, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo> UpdateRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, T value, CancellationToken cancellationToken = default);
    }
}
