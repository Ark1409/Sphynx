// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Model.User
{
    public class SphynxFriend
    {
        public Guid InitiatorId { get; set; }

        public Guid AcceptorId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
