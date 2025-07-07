// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ModelV2.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestGroupChatRoomInfo : GroupChatRoomInfo
    {
        public TestGroupChatRoomInfo(string name = "test-group-room")
        {
            Name = name;
            RoomId = name.AsSnowflakeId();

            OwnerId = $"owner-{name}".AsSnowflakeId();
            IsPublic = name.Length % 2 == 0;
        }

        public static TestGroupChatRoomInfo[] FromNames(params string[] names)
        {
            var users = new TestGroupChatRoomInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestGroupChatRoomInfo(names[i]);
            }

            return users;
        }

        public override bool Equals(GroupChatRoomInfo? other) =>
            base.Equals(other) && OwnerId == other.OwnerId && IsPublic == other.IsPublic;
    }
}
