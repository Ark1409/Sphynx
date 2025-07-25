// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.User;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Test.Utils;
using Sphynx.Network.Serialization;

namespace Sphynx.Test.Network.Serialization
{
    [TestFixture]
    public class BroadcastPacketTests
    {
        [Test]
        public void LoginBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LoginBroadcastSerializer();
            var packet = new LoginBroadcast("user1".AsSnowflakeId(), SphynxUserStatus.ONLINE);
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void LogoutBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LogoutBroadcastSerializer();
            var packet = new LogoutBroadcast("user1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void SendMessageBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new MessagePostedBroadcastSerializer();
            var packet = new MessagePostedBroadcast("room1".AsSnowflakeId(), "msg1".AsSnowflakeId());
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
            var serializer = new RoomDeletedBroadcastSerializer();
            var packet = new RoomDeletedBroadcast("room1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void JoinRoomBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new JoinedRoomBroadcastSerializer();
            var packet = new JoinedRoomBroadcast("room1".AsSnowflakeId(), "user1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void KickUserBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new UserKickedBroadcastSerializer();
            var packet = new UserKickedBroadcast("room1".AsSnowflakeId(), "user1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void LeaveRoomBroadcastPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LeftRoomBroadcastSerializer();
            var packet = new LeftRoomBroadcast("room1".AsSnowflakeId(), "user1".AsSnowflakeId());
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }
    }
}
