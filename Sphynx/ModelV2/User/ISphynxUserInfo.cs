// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.User
{
    /// <summary>
    /// A type which holds information about a specific Sphynx user.
    /// </summary>
    public interface ISphynxUserInfo : IEquatable<ISphynxUserInfo>
    {
        /// <summary>
        /// The user ID for this Sphynx user.
        /// </summary>
        SnowflakeId UserId { get; set; }

        /// <summary>
        /// The username for this Sphynx user.
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// The activity status of this Sphynx user.
        /// </summary>
        SphynxUserStatus UserStatus { get; set; }
    }
}
