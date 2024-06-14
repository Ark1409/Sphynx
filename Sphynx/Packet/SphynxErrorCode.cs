namespace Sphynx.Packet
{
    /// <summary>
    /// Error codes used within the <see cref="Sphynx"/> application.
    /// </summary>
    public enum SphynxErrorCode : byte
    {
        /// <summary>
        /// Action has been completed successfully.
        /// </summary>
        SUCCESS = 0,

        /// <summary>
        /// Invalid (unknown/already exists) username.
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
        
        /// <summary>
        /// Rate limited.
        /// </summary>
        ENHANCE_YOUR_CALM,
        
        /// <summary>
        /// An error occured while attempting to read from the database. Could also happen due to an
        /// invalid query.
        /// </summary>
        DB_READ_ERROR,
        
        /// <summary>
        /// An error occured while attempting to write to the database.
        /// </summary>
        DB_WRITE_ERROR,
        
        /// <summary>
        /// Invalid (unknown/already exists) room ID.
        /// </summary>
        INVALID_ROOM,
        
        /// <summary>
        /// Invalid (unknown/already exists) message ID.
        /// </summary>
        INVALID_MSG,
        
        /// <summary>
        /// Attempt to authenticate when already logged in.
        /// </summary>
        ALREADY_LOGGED_IN
    }
}
