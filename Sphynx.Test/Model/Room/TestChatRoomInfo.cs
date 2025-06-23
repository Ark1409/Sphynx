// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.Room
{
    public abstract class TestChatRoomInfo : ChatRoomInfo
    {
        public SnowflakeId RoomId { get; set; }
        public abstract ChatRoomType RoomType { get; set; }
        public string Name { get; set; }

        public TestChatRoomInfo(string name = "Test-Room")
        {
            Name = name;
            RoomId = name.AsSnowflakeId();
        }

        public bool Equals(ChatRoomInfo? other) => RoomId == other?.RoomId && Name == other.Name;
    }
}
