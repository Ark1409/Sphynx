// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public abstract class TestSphynxRoomInfo : SphynxRoomInfo
    {
        public TestSphynxRoomInfo()
        {
            RoomId = Guid.NewGuid();
        }

        public override bool Equals(SphynxRoomInfo? other) => RoomId == other?.RoomId ;
    }
}
