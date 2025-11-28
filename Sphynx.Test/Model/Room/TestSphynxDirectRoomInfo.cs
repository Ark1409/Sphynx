// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestSphynxDirectRoomInfo : SphynxDirectRoomInfo
    {
        public TestSphynxDirectRoomInfo(string name = "test-room")
        {
            Name = name;
            RoomId = name.AsGuid();

            UserA = "user-1".AsGuid();
            UserB = "user-2".AsGuid();
        }

        public static TestSphynxDirectRoomInfo[] FromNames(params string[] names)
        {
            var users = new TestSphynxDirectRoomInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestSphynxDirectRoomInfo(names[i]);
            }

            return users;
        }

        public override bool Equals(SphynxDirectRoomInfo? other) =>
            base.Equals(other) && UserA == other.UserA && UserB == other.UserB;
    }
}
