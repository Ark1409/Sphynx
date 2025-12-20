// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public class TestSphynxDirectRoomInfo : SphynxDirectRoomInfo
    {
        public TestSphynxDirectRoomInfo()
        {
            UserA =Guid.NewGuid();
            UserB =Guid.NewGuid();
        }

        public static TestSphynxDirectRoomInfo[] FromCount(int count)
        {
            var users = new TestSphynxDirectRoomInfo[count];

            for (int i = 0; i < count; i++)
            {
                users[i] = new TestSphynxDirectRoomInfo();
            }

            return users;
        }

        public override bool Equals(SphynxDirectRoomInfo? other) =>
            base.Equals(other) && UserA == other.UserA && UserB == other.UserB;
    }
}
