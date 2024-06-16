using Sphynx.Packet.Response;

namespace Sphynx.Test
{
    [TestFixture]
    public class ResponsePacketTests
    {
        [TestCase(SphynxErrorCode.INVALID_USERNAME)]
        [TestCase(SphynxErrorCode.INVALID_PASSWORD)]
        [TestCase(SphynxErrorCode.SUCCESS)]
        public void LoginResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new LoginResponsePacket(errorCode);
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

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void ChatCreateResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new ChatCreateResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatCreateResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void ChatJoinResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new ChatJoinResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatJoinResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void ChatKickResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new ChatKickResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatKickResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase(SphynxErrorCode.SUCCESS)]
        [TestCase(SphynxErrorCode.INVALID_SESSION)]
        [TestCase(SphynxErrorCode.INSUFFICIENT_PERMS)]
        public void ChatLeaveResponsePacket_ShouldSerializeWithCorrectFormat(SphynxErrorCode errorCode)
        {
            var samplePacket = new ChatLeaveResponsePacket(errorCode);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatLeaveResponsePacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}
