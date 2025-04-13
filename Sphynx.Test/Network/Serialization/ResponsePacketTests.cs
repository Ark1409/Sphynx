// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Test.Model;
using Sphynx.Test.Model.Room;
using Sphynx.Test.Model.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Network.Serialization
{
    [TestFixture]
    public class ResponsePacketTests
    {
        [Test]
        public void LoginResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LoginResponsePacketSerializer(new SphynxSelfInfoSerializer());
            var packet = new LoginResponsePacket(new TestSphynxSelfInfo(), "test".AsGuid());
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
        public void LogoutResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LogoutResponsePacketSerializer();
            var packet = new LogoutResponsePacket();
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
        public void RegisterResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RegisterResponsePacketSerializer(new SphynxSelfInfoSerializer());
            var packet = new RegisterResponsePacket(new TestSphynxSelfInfo(), "test".AsGuid());
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
        public void GetMessagesResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new GetMessagesResponsePacketSerializer(new ChatMessageSerializer());
            var packet = new GetMessagesResponsePacket
            {
                // ReSharper disable once CoVariantArrayConversion
                Messages = TestChatMessage.FromArray("Hello World", "Test message", "Crème glacée")
            };
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize(packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket!, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void GetUsersResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new GetUsersResponsePacketSerializer(new SphynxUserInfoSerializer());
            var packet = new GetUsersResponsePacket
            {
                // ReSharper disable once CoVariantArrayConversion
                Users = TestSphynxSelfInfo.FromArray("user1", "user2", "user3")!
            };
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
        public void SendMessageResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new SendMessageResponsePacketSerializer();
            var packet = new SendMessageResponsePacket();
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
        public void CreateRoomResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new CreateRoomResponsePacketSerializer();
            var packet = new CreateRoomResponsePacket("room1".AsSnowflakeId());
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
        public void DeleteRoomResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new DeleteRoomResponsePacketSerializer();
            var packet = new DeleteRoomResponsePacket();
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
        public void JoinRoomResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new JoinRoomResponsePacketSerializer(new ChatRoomInfoSerializer());
            var packet = new JoinRoomResponsePacket { RoomInfo = new TestDirectChatRoomInfo() };
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
        public void KickUserResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new KickUserResponsePacketSerializer();
            var packet = new KickUserResponsePacket();
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
        public void LeaveRoomResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LeaveRoomResponsePacketSerializer();
            var packet = new LeaveRoomResponsePacket();
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
        public void GetRoomsResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new GetRoomsResponsePacketSerializer(new ChatRoomInfoSerializer());
            var packet = new GetRoomsResponsePacket { Rooms = new IChatRoomInfo[] { new TestDirectChatRoomInfo(), new TestGroupChatRoomInfo() } };
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
