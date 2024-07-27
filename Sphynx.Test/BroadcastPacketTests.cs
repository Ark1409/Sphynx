﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sphynx.ChatRoom;
using Sphynx.Core;
using Sphynx.Packet.Broadcast;

namespace Sphynx.Test
{
    [TestFixture]
    public class BroadcastPacketTests
    {
        [TestCase(SphynxUserStatus.AWAY)]
        [TestCase(SphynxUserStatus.ONLINE)]
        [TestCase(SphynxUserStatus.DO_NOT_DISTURB)]
        [TestCase(SphynxUserStatus.OFFLINE)]
        public void LoginBroadcastPacket_ShouldSerializeWithCorrectFormat(SphynxUserStatus userStatus)
        {
            var samplePacket = new LoginBroadcastPacket(Guid.NewGuid(), userStatus);
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LoginBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void LogoutBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new LogoutBroadcastPacket(Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(LogoutBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        public void MessageBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new MessageBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(MessageBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatDeleteBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatDeleteBroadcastPacket(Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatDeleteBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatJoinBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatJoinBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatJoinBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatKickBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatKickBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatKickBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }

        [Test]
        public void ChatLeaveBroadcastPacket_ShouldSerializeWithCorrectFormat()
        {
            var samplePacket = new ChatLeaveBroadcastPacket(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(samplePacket.TrySerialize(out byte[]? samplePacketBytes));

            Assert.Multiple(() =>
            {
                var samplePacketSpan = new ReadOnlySpan<byte>(samplePacketBytes);
                Assert.That(ChatLeaveBroadcastPacket.TryDeserialize(samplePacketSpan[SphynxPacketHeader.HEADER_SIZE..],
                    out var deserializedPacket));
                Assert.That(deserializedPacket!, Is.EqualTo(samplePacket));
            });
        }
    }
}