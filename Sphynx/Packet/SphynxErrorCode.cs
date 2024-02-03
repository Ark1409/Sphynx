namespace Sphynx.Packet
{
    /// <summary>
    /// Error code for packet responses.
    /// </summary>
    public enum SphynxErrorCode : byte
    {
        /// <summary>
        /// Action has been completed successfully.
        /// </summary>
        SUCCESS = 0,

        /// <summary>
        /// Invalid (unknown) username address at login.
        /// </summary>
        INVALID_USERNAME,

        /// <summary>
        /// Invalid password for authentication (whether it be account username at login or room password).
        /// </summary>
        INVALID_PASSWORD,

        /// <summary>
        /// Invalid session ID for user when performing an action.
        /// </summary>
        INVALID_SESSION,

        /// <summary>
        /// Invalid user ID for user when performing an action.
        /// </summary>
        INVALID_USER,

        /// <summary>
        /// When the user attempts to complete an action but does not have sufficient permissions.
        /// </summary>
        INSUFFICIENT_PERMS,
    }
}
