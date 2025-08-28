// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.User
{
    public class TestSphynxSelfInfo : SphynxSelfInfo
    {
        public TestSphynxSelfInfo(string name = "test-self")
        {
            UserName = name;
            UserId = name.AsGuid();

            var statuses = Enum.GetValues<SphynxUserStatus>();
            UserStatus = statuses[name.Length % statuses.Length];

            Friends = new HashSet<Guid>
            {
                $"user1-{name}".AsGuid(), $"user2-{name}".AsGuid(), $"user3-{name}".AsGuid()
            };
            Rooms = new HashSet<Guid>
            {
                $"room1-{name}".AsGuid(), $"room2-{name}".AsGuid(), $"room3-{name}".AsGuid()
            };
            IncomingFriendRequests = new HashSet<Guid>
            {
                $"inc_user1-{name}".AsGuid(),
                $"inc_user2-{name}".AsGuid(),
                $"inc_user3-{name}".AsGuid()
            };
            OutgoingFriendRequests = new HashSet<Guid>
            {
                $"out_user1-{name}".AsGuid(),
                $"out_user2-{name}".AsGuid(),
                $"out_user3-{name}".AsGuid()
            };
            LastReadMessages = new TestLastReadMessagesInfo($"msg1-{name}", $"msg2-{name}", $"msg3-{name}");
        }

        public static TestSphynxSelfInfo[] FromNames(params string[] names)
        {
            var users = new TestSphynxSelfInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestSphynxSelfInfo(names[i]);
            }

            return users;
        }

        public override bool Equals(SphynxSelfInfo? other)
        {
            return base.Equals(other) &&
                   CollectionUtils.AreEquivalent(Friends, other?.Friends) &&
                   CollectionUtils.AreEquivalent(Rooms, other?.Rooms) &&
                   CollectionUtils.AreEquivalent(LastReadMessages, other?.LastReadMessages) &&
                   CollectionUtils.AreEquivalent(OutgoingFriendRequests, other?.OutgoingFriendRequests) &&
                   CollectionUtils.AreEquivalent(IncomingFriendRequests, other?.IncomingFriendRequests);
        }
    }
}
