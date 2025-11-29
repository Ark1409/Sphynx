// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Model.User
{
    public class SphynxFriendRequest
    {
        public Guid SenderId { get; set; }

        public Guid ReceiverId { get; set; }

        public DateTimeOffset SentAt { get; set; }
    }
}
