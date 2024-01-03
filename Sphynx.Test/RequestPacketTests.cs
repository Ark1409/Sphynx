using Sphynx.ChatRoom;

namespace Sphynx.Test
{
    [TestFixture]
    public class RequestPacketTests
    {
        [TestCase("Fred", "fredisbest123")]
        [TestCase("Timothy", "timtimtim222")]
        [TestCase("Bryan", "j$u3#mo&sq")]
        public void LoginRequestPacket_ShouldSerializeWithCorrectFormat(string userName, string password)
        {
            var samplePacket = new LoginRequestPacket(userName, password);
            samplePacket.TrySerialize(out var samplePacketBytes);

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LoginRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(ChatRoomType.DIRECT_MSG, "This is a test message")]
        [TestCase(ChatRoomType.GROUP, "A normal group chat message")]
        [TestCase(ChatRoomType.DIRECT_MSG, "ب بـ ـبـ ـبSphynx is a shell-based chat client.")]
        [TestCase(ChatRoomType.GROUP, "音?!@#$%^&*()读写汉字")]
        public void MessageRequestPacket_ShouldSerializeWithCorrectFormat(ChatRoomType recipientType, string message)
        {
            var samplePacket = new MessageRequestPacket(recipientType, Guid.NewGuid(), message);
            samplePacket.TrySerialize(out var samplePacketBytes);

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatCreateRequestPacket_Direct_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatCreateRequestPacket.Direct(Guid.NewGuid());
            samplePacket.TrySerialize(out var samplePacketBytes);

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatCreateRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase("BestRoom", null)]
        [TestCase("NewRoom", "pass123")]
        [TestCase("Group chat room", "nopass12345$")]
        public void ChatCreateRequestPacket_Group_ShouldSerializeWithCorrectFormat(string name, string? password)
        {
            var samplePacket = new ChatCreateRequestPacket.Group(name, password);
            samplePacket.TrySerialize(out var samplePacketBytes);

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatCreateRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}
