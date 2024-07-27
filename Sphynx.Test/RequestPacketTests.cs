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

        [Test]
        public void MessageInfoRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new MessageInfoRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageInfoRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
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
            var samplePacket = new MessageRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), message);
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
        public void RoomCreateRequestPacket_Direct_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new RoomCreateRequestPacket.Direct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            samplePacket.TrySerialize(out byte[]? samplePacketBytes);

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomCreateRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase("BestRoom", null)]
        [TestCase("NewRoom", "pass123")]
        [TestCase("Group chat room", "nopass12345$")]
        public void RoomCreateRequestPacket_Group_ShouldSerializeWithCorrectFormat(string name, string? password)
        {
            var samplePacket = new RoomCreateRequestPacket.Group(Guid.NewGuid(), Guid.NewGuid(), name, password);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomCreateRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(null)]
        [TestCase("pass123")]
        [TestCase("rooompass987$")]
        public void RoomDeleteRequestPacket_ShouldSerializeWithCorrectFormat(string? password)
        {
            var samplePacket = new RoomDeleteRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), password);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomDeleteRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
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

        [TestCase(null)]
        [TestCase("jelly22fish")]
        [TestCase("d3ltagamm@$")]
        public void RoomJoinRequestPacket_ShouldSerializeWithCorrectFormat(string? password)
        {
            var samplePacket = new RoomJoinRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), password);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomJoinRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomKickRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new RoomKickRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomKickRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomLeaveRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new RoomLeaveRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomLeaveRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(ChatRoomType.DIRECT_MSG)]
        [TestCase(ChatRoomType.GROUP)]
        public void RoomSelectRequestPacket_ShouldSerializeWithCorrectFormat(ChatRoomType chatType)
        {
            var samplePacket = new RoomSelectRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomSelectRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void UserInfoRequestPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new UserInfoRequestPacket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(UserInfoRequestPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}