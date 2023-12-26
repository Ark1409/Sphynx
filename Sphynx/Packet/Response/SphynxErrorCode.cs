namespace Sphynx.Packet.Response
{
    /// <summary>
    /// Error code for packet responses.
    /// </summary>
    public enum SphynxErrorCode : sbyte
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
        /// Invalid password for account email at login.
        /// </summary>
        INVALID_PASSWORD
    }
}
