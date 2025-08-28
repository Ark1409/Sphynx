// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public abstract class TestChatRoomInfo : ChatRoomInfo
    {
        public TestChatRoomInfo(string name = "test-room")
        {
            Name = name;
            RoomId = name.AsSnowflakeId();
        }

        public override bool Equals(ChatRoomInfo? other) => RoomId == other?.RoomId && Name == other.Name;
    }
}
