namespace Sphynx.ModelV2.User
{
    /// <summary>
    /// Possible statuses for a Sphynx user.
    /// </summary>
    public enum SphynxUserStatus : byte
    {
        /// <summary>
        /// The user is offline.
        /// </summary>
        OFFLINE = 0,

        /// <summary>
        /// The user is online.
        /// </summary>
        ONLINE,

        /// <summary>
        /// The user is away (idle).
        /// </summary>
        AWAY,

        /// <summary>
        /// The user is set to Do Not Disturb.
        /// </summary>
        DO_NOT_DISTURB
    }
}
