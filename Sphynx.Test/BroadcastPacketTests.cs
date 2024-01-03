using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sphynx.ChatRoom;
using Sphynx.Packet.Broadcast;

namespace Sphynx.Test
{
    [TestFixture]
    public class BroadcastPacketTests
    {
        [TestCase("This is a test message")]
        [TestCase("A normal direct chat message")]
        [TestCase("ب بـ ـبـ ـبSphynx is a shell-based chat client.")]
        public void MessageBroadcastPacket_Direct_ShouldSerializeWithCorrectFormat(string message)
        {
            var samplePacket = new MessageBroadcastPacket.Direct(Guid.NewGuid(), message);
            Assert.That(samplePacket.TrySerialize(out var samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageBroadcastPacket.Direct.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [TestCase("This is a test message")]
        [TestCase("A normal group chat message")]
        [TestCase("ب بـ ـبـ ـبSphynx is a shell-based chat client.")]
        public void MessageBroadcastPacket_Group_ShouldSerializeWithCorrectFormat(string message)
        {
            var samplePacket = new MessageBroadcastPacket.Group(Guid.NewGuid(), Guid.NewGuid(), message);
            Assert.That(samplePacket.TrySerialize(out var samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageBroadcastPacket.Group.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatJoinBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatJoinBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out var samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatJoinBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatLeaveBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatLeaveBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out var samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatLeaveBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatKickBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatKickBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out var samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatKickBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatDeleteBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatDeleteBroadcastPacket(Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out var samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatDeleteBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}
