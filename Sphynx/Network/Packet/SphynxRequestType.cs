// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Packet
{
    /// <summary>
    /// An unsigned integer enum code representing the request type.
    /// </summary>
    public enum SphynxRequestType : ushort
    {
        LOGIN_REQ = 0,
        LOGOUT_REQ = 1,
        REGISTER_REQ = 2,

        GET_USER_REQ = 3,
        GET_FRIEND_REQ_REQ = 4,
        DEL_FRIEND_REQ_REQ = 5,
        GET_FRIEND_REQ = 6,
        ADD_FRIEND_REQ = 7,
        DEL_FRIEND_REQ = 8,

        SEND_MSG_REQ = 9,
        GET_MSG_REQ = 10,

        CREATE_ROOM_REQ = 11,
        DEL_ROOM_REQ = 12,
        GET_ROOM_REQ = 13,
        JOIN_ROOM_REQ = 14,
        LEAVE_ROOM_REQ = 15,

        ADD_MEMBER_REQ = 16,
        KICK_MEMBER_REQ = 17,
    }

    /// <summary>
    /// An unsigned integer enum code representing the broadcast type.
    /// </summary>
    public enum SphynxBroadcastType : ushort
    {
        LOGIN_BCAST = 0,
        LOGOUT_BCAST = 1,

        SEND_MSG_BCAST = 2,

        JOIN_ROOM_BCAST = 3,
        LEAVE_ROOM_BCAST = 4,
        DEL_ROOM_BCAST = 5,

        ADD_MEMBER_BCAST = 6,
        KICK_MEMBER_BCAST = 7,
    }
}
