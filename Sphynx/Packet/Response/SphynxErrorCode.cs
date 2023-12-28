namespace Sphynx.Packet.Response
{
    /// <summary>
    /// Error code for packet responses.
    /// </summary>
    public enum SphynxErrorCode : byte
    {
        /// <summary>
        /// Action has been completed succesfully.
        /// </summary>
        SUCCESS = 0,

        /// <summary>
        /// Invalid (unknown) email address at login.
        /// </summary>
        INVALID_EMAIL,

        /// <summary>
        /// Invalid password for authentication (whether it be account email at login or room password).
        /// </summary>
        INVALID_PASSWORD
    }
}
