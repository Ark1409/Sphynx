// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.Room
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public interface IDirectChatRoomInfo : IChatRoomInfo, IEquatable<IDirectChatRoomInfo>
    {
        /// <summary>
        /// Returns the user ID of one of the users within this direct-message chat room.
        /// </summary>
        /// <remarks>Null if this information has not yet been populated.</remarks>
        public SnowflakeId? UserOne => Users.Count == 0 ? null : Users.First();

        /// <summary>
        /// Returns the other user within this direct-message chat room.
        /// </summary>
        /// <remarks>Null if this information has not yet been populated.</remarks>
        public SnowflakeId? UserTwo
        {
            get
            {
                if (Users.Count < 2) return null;

                using var enumerator = Users.GetEnumerator();
                enumerator.MoveNext();
                enumerator.MoveNext();

                return enumerator.Current;
            }
        }
    }
}
