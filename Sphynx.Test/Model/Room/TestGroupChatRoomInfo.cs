// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestGroupChatRoomInfo : TestChatRoomInfo, IGroupChatRoomInfo
    {
        public override ChatRoomType RoomType { get; set; } = ChatRoomType.GROUP;
        public SnowflakeId OwnerId { get; set; }
        public bool IsPublic { get; set; }

        public TestGroupChatRoomInfo(string name = "Test-Group-Room") : base(name)
        {
            OwnerId = $"owner+{name}".AsSnowflakeId();
            IsPublic = name.Length % 2 == 0;
        }

        public static TestGroupChatRoomInfo[] FromArray(params string[] names)
        {
            var users = new TestGroupChatRoomInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestGroupChatRoomInfo(names[i]);
            }

            return users;
        }

        public bool Equals(IGroupChatRoomInfo? other) =>
            base.Equals(other) && OwnerId == other.OwnerId && IsPublic == other.IsPublic;
    }
}
