using Sphynx.Core;
using Sphynx.Model.ChatRoom;
using Sphynx.Model.User;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Test
{
    [TestFixture]
    public class ResponsePacketTests
    {
        [Test]
        public void LoginResponsePacket_ShouldSerializeWithCorrectFormat()
        {
            var sampleUser = new SphynxUserInfo(Guid.NewGuid(), "foobar", SphynxUserStatus.ONLINE,
                new[] { Guid.NewGuid(), Guid.NewGuid() }.AsEnumerable(), new[] { Guid.NewGuid() }.AsEnumerable());
            var sampleSessionId = Guid.NewGuid();
            var samplePacket = new LoginResponsePacket(sampleUser, sampleSessionId);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LoginResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void LogoutResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new LogoutResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LogoutResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void MessageInfoResponsePacket_ShouldSerializeWithCorrectFormat()
        {
            var sampleMsg1 = new ChatRoomMessageInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "foobar");
            var sampleMsg2 = new ChatRoomMessageInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "bazqux");
            var samplePacket = new MessageInfoResponsePacket(sampleMsg1, sampleMsg2);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageInfoResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void MessageResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new MessageResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomCreateResponsePacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new RoomCreateResponsePacket(Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomCreateResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void RoomDeleteResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new RoomDeleteResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomDeleteResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomInfoResponsePacket_Direct_ShouldSerializeWithCorrectFormat()
        {
            var sampleRoom = new ChatRoomInfo.Direct(Guid.NewGuid(), "foobar", Guid.NewGuid(), Guid.NewGuid());
            var samplePacket = new RoomInfoResponsePacket(sampleRoom);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomInfoResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomInfoResponsePacket_Group_ShouldSerializeWithCorrectFormat()
        {
            var sampleRoom = new ChatRoomInfo.Group(Guid.NewGuid(), Guid.NewGuid(), "foobar", "password123", false,
                new[] { Guid.NewGuid(), Guid.NewGuid() });
            var samplePacket = new RoomInfoResponsePacket(sampleRoom);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomInfoResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void RoomJoinResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new RoomJoinResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomJoinResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void RoomKickResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new RoomKickResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomKickResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void RoomLeaveResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new RoomLeaveResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomLeaveResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void RoomSelectResponsePacket_ShouldSerializeWithCorrectFormat()
        {
            var sampleMsg1 = new ChatRoomMessageInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "foobar");
            var sampleMsg2 = new ChatRoomMessageInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "bazqux");
            var sampleMsg3 = new ChatRoomMessageInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "fooqux");
            var samplePacket = new RoomSelectResponsePacket(sampleMsg1, sampleMsg2, sampleMsg3);

            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(RoomSelectResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void UserInfoResponsePacket_ShouldSerializeWithCorrectFormat()
        {
            var sampleUser1 = new SphynxUserInfo(Guid.NewGuid(), "foobar", SphynxUserStatus.ONLINE);
            var sampleUser2 = new SphynxUserInfo(Guid.NewGuid(), "barbaz", SphynxUserStatus.OFFLINE);
            var sampleUser3 = new SphynxUserInfo(Guid.NewGuid(), "bazqux", SphynxUserStatus.AWAY);
            var samplePacket = new UserInfoResponsePacket(sampleUser1, sampleUser2, sampleUser3);

            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(UserInfoResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}
