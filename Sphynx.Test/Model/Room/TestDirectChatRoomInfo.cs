// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ModelV2.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestDirectChatRoomInfo : DirectChatRoomInfo
    {
        public TestDirectChatRoomInfo(string name = "test-room")
        {
            Name = name;
            RoomId = name.AsSnowflakeId();

            UserOne = "user-1".AsSnowflakeId();
            UserTwo = "user-2".AsSnowflakeId();
        }

        public static TestDirectChatRoomInfo[] FromNames(params string[] names)
        {
            var users = new TestDirectChatRoomInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestDirectChatRoomInfo(names[i]);
            }

            return users;
        }

        public override bool Equals(DirectChatRoomInfo? other) =>
            base.Equals(other) && UserOne == other.UserOne && UserTwo == other.UserTwo;
    }
}
