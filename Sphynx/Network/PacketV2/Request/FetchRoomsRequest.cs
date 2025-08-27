using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_REQ"/>
    public sealed class FetchRoomsRequest : SphynxRequest, IEquatable<FetchRoomsRequest>
    {
        /// <summary>
        /// The maximum number of rooms which can be requested at once.
        /// </summary>
        public const int MAX_ROOM_COUNT = 25;

        /// <summary>
        /// The room IDs of the rooms for which to retrieve information.
        /// </summary>
        /// <remarks>If the provided value has a length greater than <see cref="MAX_ROOM_COUNT"/>, the first
        /// <see cref="MAX_ROOM_COUNT"/> IDs will be taken.</remarks>
        public Guid[] RoomIds
        {
            get => _roomIds;
            set
            {
                if (value.Length > MAX_ROOM_COUNT)
                {
                    if (_roomIds.Length != MAX_ROOM_COUNT)
                        _roomIds = new Guid[MAX_ROOM_COUNT];

                    Array.Copy(value, 0, _roomIds, 0, MAX_ROOM_COUNT);
                    return;
                }

                _roomIds = value;
            }
        }

        private Guid[] _roomIds = Array.Empty<Guid>();

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_REQ;

        public FetchRoomsRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchRoomsRequest"/>.
        /// </summary>
        /// <param name="sessionId">The JWT access token for this request.</param>
        public FetchRoomsRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchRoomsRequest"/>.
        /// </summary>
        /// <param name="sessionId">The JWT access token for this request.</param>
        /// <param name="roomIds">The ID of the room to get the information of.</param>
        public FetchRoomsRequest(Guid sessionId, params Guid[] roomIds) : base(sessionId)
        {
            RoomIds = roomIds;
        }

        /// <inheritdoc/>
        public bool Equals(FetchRoomsRequest? other) => base.Equals(other) && MemoryUtils.SequenceEqual(RoomIds, other?.RoomIds);

        public override FetchRoomsResponse CreateResponse(SphynxErrorInfo errorInfo) => new FetchRoomsResponse(errorInfo);
    }
}
