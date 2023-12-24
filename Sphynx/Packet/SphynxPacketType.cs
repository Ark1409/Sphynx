namespace Sphynx.Packet
{
    /// <summary>
    /// An unsigned integer enum code representing the packet type.
    /// </summary>
    public enum SphynxPacketType : uint
    {
        /// <summary>
        /// Safe-guard against corrupted or uninitialized data. Server simply discards packet contents.
        /// </summary>
        NOP = 0x00000000,

        /// <summary>
        /// A login request packet sent from the client to the server with the necessary authentication information.
        /// </summary>
        LOGIN_REQ = 0x00000001,

        /// <summary>
        /// A login response packet sent from the server to the client with authentication status information.
        /// </summary>
        LOGIN_RES = 0x80000001,

        /// <summary>
        /// A packet that is sent from the client to the server when a user enters a message in a chat room. 
        /// </summary>
        MSG_REQ = 0x00000002,

        /// <summary>
        /// Broadcast packet sent from server to all other clients which includes the message that was contained within 
        /// a <see cref="MSG_REQ"/> packet.
        /// </summary>
        MSG_RES = 0x80000002,

        /// <summary>
        /// Chat creation request packet sent from client to server including necessary chat creation information.
        /// </summary>
        CHAT_CREATE_REQ = 0x00000004,

        /// <summary>
        /// Chat creation response packet sent from server to client which returns room chat information.
        /// </summary>
        CHAT_CREATE_RES = 0x80000004,

        /// <summary>
        /// A packet that is sent from the client to the server when a user decides to add another user to a chat room.
        /// </summary>
        CHAT_INV_REQ = 0x00000008,

        /// <summary>
        /// A packet sent from server to requesting client indicating whether or not the invitation was successful.
        /// </summary>
        // TOOD: Perhaps later allow users to choose whether they can want to join the chat room after they've been added
        CHAT_INV_RES = 0x80000008,

        /// <summary>
        /// A packet that is sent from the client to server when a user attempts to kick another from a chat room.
        /// </summary>
        CHAT_KICK_REQ = 0x00000010,

        /// <summary>
        /// A confirmation packet sent from server to client indicating whether the user could be kicked.
        /// </summary>
        CHAT_KICK_RES = 0x80000010,

        /// <summary>
        /// A chat room deletion request packet sent from client to server.
        /// </summary>
        CHAT_DEL_REQ = 0x00000020,

        /// <summary>
        /// A confirmation packet sent from server to client indicating whether the chat room could be deleted.
        /// </summary>
        CHAT_DEL_RES = 0x80000020,

        /// <summary>
        /// A packet that is sent from the client to the server when a user selects to enter a specific chat room on their screen.
        /// </summary>
        CHAT_SELECT_REQ = 0x00000040,

        /// <summary>
        /// A packet sent from server to client containing all information required when loading a chat room.
        /// </summary>
        CHAT_SELECT_RES = 0x80000040,
    }
}
