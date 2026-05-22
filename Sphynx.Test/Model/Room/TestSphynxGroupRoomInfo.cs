// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestSphynxGroupRoomInfo : SphynxGroupRoomInfo
    {
        public TestSphynxGroupRoomInfo(string name = "test-group-room")
        {
            Name = name;
            RoomId = name.AsGuid();

            OwnerId = $"owner-{name}".AsGuid();
            IsPublic = name.Length % 2 == 0;
        }

        public static TestSphynxGroupRoomInfo[] FromNames(params string[] names)
        {
            var users = new TestSphynxGroupRoomInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestSphynxGroupRoomInfo(names[i]);
            }

            return users;
        }

        public override bool Equals(SphynxGroupRoomInfo? other) =>
            base.Equals(other) && OwnerId == other.OwnerId && IsPublic == other.IsPublic;
    }
}
