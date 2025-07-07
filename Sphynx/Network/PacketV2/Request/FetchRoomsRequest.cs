using Sphynx.Core;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_REQ"/>
    public sealed class FetchRoomsRequest : SphynxRequest, IEquatable<FetchRoomsRequest>
    {
        /// <summary>
        /// The maximum number of rooms which can be requested at once.
        /// </summary>
        public const int MAX_ROOM_COUNT = 10;

        /// <summary>
        /// The room IDs of the rooms for which to retrieve information.
        /// </summary>
        /// <remarks>If the provided value has a length greater than <see cref="MAX_ROOM_COUNT"/>, the first
        /// <see cref="MAX_ROOM_COUNT"/> IDs will be taken.</remarks>
        public SnowflakeId[] RoomIds
        {
            get => _roomIds;
            init
            {
                if (value.Length > MAX_ROOM_COUNT)
                {
                    if (_roomIds.Length != MAX_ROOM_COUNT)
                        _roomIds = new SnowflakeId[MAX_ROOM_COUNT];

                    Array.Copy(value, 0, _roomIds, 0, MAX_ROOM_COUNT);
                    return;
                }

                _roomIds = value;
            }
        }

        private SnowflakeId[] _roomIds = Array.Empty<SnowflakeId>();

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_REQ;

        /// <summary>
        /// Creates a new <see cref="FetchRoomsRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public FetchRoomsRequest(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchRoomsRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomIds">The ID of the room to get the information of.</param>
        public FetchRoomsRequest(SnowflakeId userId, Guid sessionId, params SnowflakeId[] roomIds)
            : base(userId, sessionId)
        {
            RoomIds = roomIds;
        }

        /// <inheritdoc/>
        public bool Equals(FetchRoomsRequest? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(RoomIds, other?.RoomIds);
    }
}
