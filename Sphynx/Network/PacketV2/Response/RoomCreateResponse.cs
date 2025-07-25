﻿using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_RES"/>
    public sealed class RoomCreateResponse : SphynxResponse, IEquatable<RoomCreateResponse>
    {
        /// <summary>
        /// Room ID assigned to the newly created room.
        /// </summary>
        public SnowflakeId? RoomId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_CREATE_RES;

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for room creation attempt.</param>
        public RoomCreateResponse(SphynxErrorCode errorCode) : base(errorCode)
        {
            // Assume the room is to be created
            if (errorCode == SphynxErrorCode.SUCCESS)
                RoomId = SnowflakeId.NewId();
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponse"/>.
        /// </summary>
        /// <param name="roomId">Room ID assigned to the newly created room.</param>
        public RoomCreateResponse(SnowflakeId roomId) : base(SphynxErrorCode.SUCCESS)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(RoomCreateResponse? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
