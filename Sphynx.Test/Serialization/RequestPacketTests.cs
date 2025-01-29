// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework.Internal;
using Sphynx.Core;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Serialization.Packet;

namespace Sphynx.Test.Serialization
{
    [TestFixture]
    public class RequestPacketTests
    {
        [Test]
        public void LoginRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LoginRequestPacketSerializer();
            var packet = new LoginRequestPacket("Marry Jones", "password$123");
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
        public void LogoutRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LogoutRequestPacketSerializer();
            var packet = new LogoutRequestPacket(new SnowflakeId("0000a194b22bd8bc4071bf32"),
                new Guid("62FA647C-AD54-4BCC-A860-E5A2664B019D"));
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
        public void RegisterRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RegisterRequestPacketSerializer();
            var packet = new RegisterRequestPacket("John Doe", "strngr$#pwd*234");
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
        public void GetMessagesRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new GetMessagesRequestPacketSerializer();
            var packet = new GetMessagesRequestPacket(new SnowflakeId("0000a194b22bd8bc4071bf32"),
                new Guid("62FA647C-AD54-4BCC-A860-E5A2664B019D"))
            {
                SinceId = new SnowflakeId("0000a194ba52e1bc4071bf32"),
                Count = 123,
                Inclusive = true,
                RoomId = new SnowflakeId("0000a194b22bd8bc99aec132")
            };
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
        public void GetUsersRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new GetUsersRequestPacketSerializer();
            var packet = new GetUsersRequestPacket(SnowflakeId.NewId(), Guid.NewGuid())
            {
                UserIds = new[]
                {
                    new SnowflakeId("00000194b22bddbc0000bff2"), new SnowflakeId("00001194b24bedac0220bee2"),
                    new SnowflakeId("00002194b21bddbc0220bee2"), new SnowflakeId("00010194b21bdd5c0230be72")
                }
            };
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
        public void SendMessageRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new SendMessageRequestPacketSerializer();
            var packet = new SendMessageRequestPacket(SnowflakeId.NewId(), Guid.NewGuid())
            {
                RoomId = new SnowflakeId("00010194b21bdd5c0230be72"),
                Message = "This is a test message. The quick brown fox jumps over... \r\n" +
                          "Let's also add in $some $sp4cia1 ch4r6t3rs: e, è, é, ê, ë\t\aç"
            };
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
