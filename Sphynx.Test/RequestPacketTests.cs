using Sphynx.ChatRoom;
using Sphynx.Packet.Request;

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
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LoginRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void LogoutRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new LogoutRequestPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LogoutRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
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
            var samplePacket = new MessageRequestPacket(Guid.NewGuid(), Guid.NewGuid(), recipientType, Guid.NewGuid(), message);
            samplePacket.TrySerialize(out byte[]? samplePacketBytes);

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
            var samplePacket = new ChatCreateRequestPacket.Direct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            samplePacket.TrySerialize(out byte[]? samplePacketBytes);

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
            var samplePacket = new ChatCreateRequestPacket.Group(Guid.NewGuid(), Guid.NewGuid(), name, password);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatCreateRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(null)]
        [TestCase("pass123")]
        [TestCase("rooompass987$")]
        public void ChatDeleteRequestPacket_ShouldSerializeWithCorrectFormat(string? password)
        {
            var samplePacket = new ChatDeleteRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), password);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatDeleteRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(null)]
        [TestCase("jelly22fish")]
        [TestCase("d3ltagamm@$")]
        public void ChatJoinRequestPacket_ShouldSerializeWithCorrectFormat(string? password)
        {
            var samplePacket = new ChatJoinRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), password);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatJoinRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatKickRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatKickRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatKickRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatLeaveRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatLeaveRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatLeaveRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(ChatRoomType.DIRECT_MSG)]
        [TestCase(ChatRoomType.GROUP)]
        public void ChatSelectRequestPacket_ShouldSerializeWithCorrectFormat(ChatRoomType chatType)
        {
            var samplePacket = new ChatSelectRequestPacket(Guid.NewGuid(), Guid.NewGuid(), chatType, Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatSelectRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomInfoRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new RoomInfoRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomInfoRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}
