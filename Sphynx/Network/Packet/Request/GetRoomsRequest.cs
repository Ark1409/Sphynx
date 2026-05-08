using Sphynx.Core;
using Sphynx.Network.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    public class GetRoomsRequest : SphynxRequest<GetRoomsResponse>, IEquatable<GetRoomsRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.GET_ROOM_REQ;

        /// <summary>
        /// The maximum number of rooms which can be requested at once.
        /// </summary>
        public const int MAX_ROOM_COUNT = 10;

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


        public GetRoomsRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetRoomsRequest"/>.
        /// </summary>
        /// <param name="roomIds">The ID of the room to get the information of.</param>
        public GetRoomsRequest( params Guid[] roomIds)
        {
            RoomIds = roomIds;
        }

        /// <inheritdoc/>
        public bool Equals(GetRoomsRequest? other) => base.Equals(other) && MemoryUtils.SequenceEqual(RoomIds, other?.RoomIds);

        public override GetRoomsResponse CreateResponse(SphynxErrorInfo errorInfo) => new GetRoomsResponse(errorInfo);
    }
}
