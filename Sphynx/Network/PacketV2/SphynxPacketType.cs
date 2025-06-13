namespace Sphynx.Network.PacketV2
{
    /// <summary>
    /// An unsigned integer enum code representing the packet type.
    /// </summary>
    public enum SphynxPacketType : uint
    {
        /// <summary>
        /// Safeguard against corrupted or uninitialized data. Server simply discards packet contents.
        /// </summary>
        NOP = 0x00000000u,

        /// <summary>
        /// A login request packet sent from client to server with necessary authentication information.
        /// </summary>
        LOGIN_REQ = 0x00000001u,

        /// <summary>
        /// A packet sent from client to server when a user enters a message in a chat room.
        /// </summary>
        MSG_REQ = 0x00000002u,

        /// <summary>
        /// A chat creation request packet sent from client to server including necessary chat creation information.
        /// </summary>
        ROOM_CREATE_REQ = 0x00000003u,

        /// <summary>
        /// A packet sent from client to server when a user joins a chat room.
        /// </summary>
        ROOM_JOIN_REQ = 0x00000004u,

        /// <summary>
        /// A packet sent from client to server when a leaves joins a chat room.
        /// </summary>
        ROOM_LEAVE_REQ = 0x00000005u,

        /// <summary>
        /// A packet sent from client to server when a user attempts to kick another from a chat room.
        /// </summary>
        ROOM_KICK_REQ = 0x00000006u,

        /// <summary>
        /// A chat room deletion request packet sent from client to server.
        /// </summary>
        ROOM_DEL_REQ = 0x00000007u,

        /// <summary>
        /// A packet that is sent from the client to the server when a user selects to enter a specific chat room on their screen.
        /// </summary>
        ROOM_SELECT_REQ = 0x00000008u,

        /// <summary>
        /// A packet that is sent from client to server when a user attempts to logout.
        /// </summary>
        LOGOUT_REQ = 0x00000009u,

        /// <summary>
        /// A packet that is sent from the client to resolve room information.
        /// </summary>
        ROOM_INFO_REQ = 0x0000000Au,

        /// <summary>
        /// A packet that is sent from a client to resolve user information.
        /// </summary>
        USER_INFO_REQ = 0x0000000Bu,

        /// <summary>
        /// A packet that is sent from a client to resolve message information.
        /// </summary>
        MSG_INFO_REQ = 0x0000000Cu,

        /// <summary>
        /// A registration request packet sent from client to server with necessary authentication information.
        /// </summary>
        REGISTER_REQ = 0x0000000Du,

        /// <summary>
        /// A response sent from server to client for a <see cref="LOGIN_REQ"/> packet indicating whether the login
        /// was successful.
        /// </summary>
        LOGIN_RES = 0x01000001u,

        /// <summary>
        /// A response sent from server to client to a <see cref="LOGIN_REQ"/> packet.
        /// </summary>
        MSG_RES = 0x01000002u,

        /// <summary>
        /// A chat creation response packet sent from server to client which returns room chat information.
        /// </summary>
        ROOM_CREATE_RES = 0x01000003u,

        /// <summary>
        /// A packet sent from server to requesting client indicating whether the joining was successful.
        /// </summary>
        ROOM_JOIN_RES = 0x01000004u,

        /// <summary>
        /// A packet sent from server to requesting client indicating whether the leaving was successful.
        /// </summary>
        ROOM_LEAVE_RES = 0x01000005u,

        /// <summary>
        /// A confirmation packet sent from server to client indicating whether the user could be kicked.
        /// </summary>
        ROOM_KICK_RES = 0x01000006u,

        /// <summary>
        /// A confirmation packet sent from server to client indicating whether the chat room could be deleted.
        /// </summary>
        ROOM_DEL_RES = 0x01000007u,

        /// <summary>
        /// A packet sent from server to client containing all information required when loading a chat room.
        /// </summary>
        ROOM_SELECT_RES = 0x01000008u,

        /// <summary>
        /// A confirmation packet that is sent from sever to client indicating whether the logout was successful.
        /// </summary>
        LOGOUT_RES = 0x01000009u,

        /// <summary>
        /// A packet that is sent from server to the client containing resolved room information.
        /// </summary>
        ROOM_INFO_RES = 0x0100000Au,

        /// <summary>
        /// A packet that is sent from server to the client containing resolved user information.
        /// </summary>
        USER_INFO_RES = 0x0100000Bu,

        /// <summary>
        /// A packet that is sent from server to the client containing resolved message information.
        /// </summary>
        MSG_INFO_RES = 0x0100000Cu,

        /// <summary>
        /// A response sent from server to client for a <see cref="REGISTER_REQ"/> packet indicating whether the registration
        /// was successful.
        /// </summary>
        REGISTER_RES = 0x0100000Cu,

        /// <summary>
        /// A broadcast packet sent from server to all other friends of a user when said user goes online.
        /// </summary>
        LOGIN_BCAST = 0x02000001u,

        /// <summary>
        /// A broadcast packet sent from server to all other friends of a user when said user goes offline.
        /// </summary>
        LOGOUT_BCAST = 0x02000002u,

        /// <summary>
        /// A broadcast/notification packet sent from server to all other recipients containing the message ID of the message that was sent from
        /// a <see cref="MSG_REQ"/> packet.
        /// </summary>
        MSG_BCAST = 0x02000003u,

        /// <summary>
        /// A broadcast packet sent from server to all other clients within the chat room indicating that a user
        /// has joined.
        /// </summary>
        CHAT_JOIN_BCAST = 0x02000004u,

        /// <summary>
        /// A broadcast packet sent from server to all other clients within the chat room indicating that a user
        /// has left.
        /// </summary>
        CHAT_LEAVE_BCAST = 0x02000005u,

        /// <summary>
        /// A broadcast packet sent from server to all other clients in the chat room that a user has been kicked.
        /// </summary>
        CHAT_KICK_BCAST = 0x02000006u,

        /// <summary>
        /// A broadcast packet sent from server to all other clients in the chat room indicating that it has been deleted.
        /// </summary>
        CHAT_DEL_BCAST = 0x02000007u,
    }
}
