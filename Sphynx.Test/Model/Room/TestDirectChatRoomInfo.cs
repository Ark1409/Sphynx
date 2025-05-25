// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestDirectChatRoomInfo : TestChatRoomInfo, DirectChatRoomInfo
    {
        public override ChatRoomType RoomType { get; set; } = ChatRoomType.DIRECT_MSG;
        public SnowflakeId UserOne { get; set; }
        public SnowflakeId UserTwo { get; set; }

        public TestDirectChatRoomInfo(string name = "Test-Room") : base(name)
        {
            UserOne = "user1".AsSnowflakeId();
            UserTwo = "user2".AsSnowflakeId();
        }

        public static TestDirectChatRoomInfo[] FromArray(params string[] names)
        {
            var users = new TestDirectChatRoomInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestDirectChatRoomInfo(names[i]);
            }

            return users;
        }

        public bool Equals(DirectChatRoomInfo? other) =>
            base.Equals(other) && UserOne == other.UserOne && UserTwo == other.UserTwo;
    }
}
