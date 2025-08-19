// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.User
{
    public class TestSphynxSelfInfo : SphynxSelfInfo
    {
        public TestSphynxSelfInfo(string name = "test-self")
        {
            UserName = name;
            UserId = name.AsSnowflakeId();

            var statuses = Enum.GetValues<SphynxUserStatus>();
            UserStatus = statuses[name.Length % statuses.Length];

            Friends = new HashSet<SnowflakeId>
            {
                $"user1-{name}".AsSnowflakeId(), $"user2-{name}".AsSnowflakeId(), $"user3-{name}".AsSnowflakeId()
            };
            Rooms = new HashSet<SnowflakeId>
            {
                $"room1-{name}".AsSnowflakeId(), $"room2-{name}".AsSnowflakeId(), $"room3-{name}".AsSnowflakeId()
            };
            IncomingFriendRequests = new HashSet<SnowflakeId>
            {
                $"inc_user1-{name}".AsSnowflakeId(),
                $"inc_user2-{name}".AsSnowflakeId(),
                $"inc_user3-{name}".AsSnowflakeId()
            };
            OutgoingFriendRequests = new HashSet<SnowflakeId>
            {
                $"out_user1-{name}".AsSnowflakeId(),
                $"out_user2-{name}".AsSnowflakeId(),
                $"out_user3-{name}".AsSnowflakeId()
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
