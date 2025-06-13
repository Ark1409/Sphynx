// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model
{
    public class TestChatMessage : ChatMessage
    {
        public SnowflakeId MessageId { get; set; }
        public SnowflakeId RoomId { get; set; }
        public SnowflakeId SenderId { get; set; }
        public string Content { get; set; }
        public DateTimeOffset? EditTimestamp { get; set; }

        public TestChatMessage(string msg)
        {
            MessageId = msg.AsSnowflakeId();
            RoomId = $"room+{msg}".AsSnowflakeId();
            SenderId = $"sender+{msg}".AsSnowflakeId();
            Content = msg;
            EditTimestamp = string.IsNullOrEmpty(msg) ? null : new DateTime(1990, 10, 12).ToUniversalTime();
        }

        public static TestChatMessage[] FromArray(params string[] msgs)
        {
            var chatMessages = new TestChatMessage[msgs.Length];

            for (int i = 0; i < msgs.Length; i++)
            {
                chatMessages[i] = new TestChatMessage(msgs[i]);
            }

            return chatMessages;
        }

        public bool Equals(ChatMessage? other)
        {
            return MessageId.Equals(other?.MessageId) && RoomId.Equals(other?.RoomId) &&
                   SenderId.Equals(other?.SenderId) && Content == other?.Content &&
                   Nullable.Equals(EditTimestamp, other.EditTimestamp);
        }
    }
}
