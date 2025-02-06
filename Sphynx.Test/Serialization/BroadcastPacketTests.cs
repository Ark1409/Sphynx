// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.User;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Serialization
{
    [TestFixture]
    public class BroadcastPacketTests
    {
        [Test]
        public void LoginBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LoginBroadcastPacketSerializer();
            var packet = new LoginBroadcastPacket("user1".AsSnowflakeId(), SphynxUserStatus.ONLINE);
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead),
                "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void LogoutBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LogoutBroadcastPacketSerializer();
            var packet = new LogoutBroadcastPacket("user1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead),
                "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void SendMessageBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new SendMessageBroadcastPacketSerializer();
            var packet = new SendMessageBroadcastPacket("room1".AsSnowflakeId(), "msg1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead),
                "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void DeleteRoomBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new DeleteRoomBroadcastPacketSerializer();
            var packet = new DeleteRoomBroadcastPacket("room1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead),
                "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }
    }
}
