﻿using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class JoinedRoomBroadcast : SphynxPacket, IEquatable<JoinedRoomBroadcast>
    {
        /// <summary>
        /// Room ID of the room the user has joined.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// The user ID of the user who joined the room.
        /// </summary>
        public SnowflakeId JoinerId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        /// <summary>
        /// Creates a new <see cref="JoinedRoomBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has joined.</param>
        /// <param name="joinerId">The user ID of the user who joined the room.</param>
        public JoinedRoomBroadcast(SnowflakeId roomId, SnowflakeId joinerId)
        {
            RoomId = roomId;
            JoinerId = joinerId;
        }

        /// <inheritdoc/>
        public bool Equals(JoinedRoomBroadcast? other) =>
            base.Equals(other) && RoomId == other?.RoomId && JoinerId == other?.JoinerId;
    }
}
