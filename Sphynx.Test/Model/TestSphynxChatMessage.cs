// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model
{
    public class TestSphynxChatMessage : SphynxChatMessage
    {
        public TestSphynxChatMessage(string msg)
        {
            MessageId = msg.AsSnowflakeId();
            RoomId = $"room-{msg}".AsGuid();
            SenderId = $"sender-{msg}".AsGuid();
            Content = msg;
            EditTimestamp = string.IsNullOrEmpty(msg) ? null : new DateTime(1990, 10, 12).ToUniversalTime();
        }

        public static TestSphynxChatMessage[] FromArray(params string[] msgs)
        {
            var chatMessages = new TestSphynxChatMessage[msgs.Length];

            for (int i = 0; i < msgs.Length; i++)
            {
                chatMessages[i] = new TestSphynxChatMessage(msgs[i]);
            }

            return chatMessages;
        }

        public override bool Equals(SphynxChatMessage? other)
        {
            return MessageId.Equals(other?.MessageId) && RoomId.Equals(other?.RoomId) &&
                   SenderId.Equals(other?.SenderId) && Content == other?.Content &&
                   Nullable.Equals(EditTimestamp, other.EditTimestamp);
        }
    }
}
