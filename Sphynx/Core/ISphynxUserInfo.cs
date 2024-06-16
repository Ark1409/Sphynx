namespace Sphynx.Core
{
    /// <summary>
    /// A type which holds information about a specific Sphynx user.
    /// </summary>
    public interface ISphynxUserInfo : IEquatable<ISphynxUserInfo>
    {
        /// <summary>
        /// The user ID for this Sphynx user.
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// The username for this Sphynx user.
        /// </summary>
        /// <remarks>Value should only be mutated on the server side.</remarks>
        public string UserName { get; }

        /// <summary>
        /// The activity status of this Sphynx user.
        /// </summary>
        /// <remarks>Value should only be mutated on the server side.</remarks>
        public SphynxUserStatus UserStatus { get; }
    }
}