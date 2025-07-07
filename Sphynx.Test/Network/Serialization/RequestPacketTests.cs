// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Serialization;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Network.Serialization
{
    [TestFixture]
    public class RequestPacketTests
    {
        [Test]
        public void LoginRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LoginRequestPacketSerializer();
            var packet = new LoginRequest("Marry Jones", "password$123");
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
        public void LogoutRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LogoutRequestSerializer();
            var packet = new LogoutRequest("test".AsSnowflakeId(), "test".AsGuid());
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
        public void RegisterRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RegisterRequestSerializer();
            var packet = new RegisterRequest("John Doe", "stronger$#pwd*234");
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
        public void FetchMessagesRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchMessagesRequestSerializer();
            var packet = new FetchMessagesRequest("test".AsSnowflakeId(), "test".AsGuid())
            {
                BeforeId = "before".AsSnowflakeId(), Count = 123, Inclusive = true, RoomId = "room".AsSnowflakeId()
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
        public void FetchUsersRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchUsersRequestSerializer();
            var packet = new FetchUsersRequest(SnowflakeId.NewId(), Guid.NewGuid())
            {
                UserIds = new[] { "test user".AsSnowflakeId(), "test user 2".AsSnowflakeId(), "test user 3".AsSnowflakeId(), }
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
        public void MessagePostRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new MessagePostRequestSerializer();
            var packet = new MessagePostRequest("user".AsSnowflakeId(), "session".AsGuid())
            {
                RoomId = "test room id".AsSnowflakeId(),
                Message = "This is a test message. The quick brown fox jumps over... \r\n" +
                          "Let's also add in $some $sp4cia1 ch4r6t3rs: e, è, é, ê, ë\t\aç"
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
        public void RoomCreateRequestPacket_Direct_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RoomCreateRequestSerializer();
            var packet = new RoomCreateRequest.Direct("user".AsSnowflakeId(), "session".AsGuid()) { OtherId = "other-user".AsSnowflakeId() };
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize((RoomCreateRequest)packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void RoomCreateRequestPacket_Group_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RoomCreateRequestSerializer();
            var packet = new RoomCreateRequest.Group("user".AsSnowflakeId(), "session".AsGuid())
            {
                Name = "test-group", Password = "test-group-pwd", Public = true
            };
            Span<byte> buffer = stackalloc byte[serializer.GetMaxSize(packet)];

            // Act
            bool serialized = serializer.TrySerialize((RoomCreateRequest)packet, buffer, out int bytesWritten);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.TryDeserialize(buffer, out var newPacket, out int bytesRead), "Could not perform deserialization.");
            Assert.That(bytesWritten, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void RoomDeleteRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new DeleteRoomRequestSerializer();
            var packet = new RoomDeleteRequest("user".AsSnowflakeId(), "session".AsGuid())
            {
                RoomId = "room".AsSnowflakeId(), Password = "test-group-pwd"
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
        public void JoinRoomRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new JoinRoomRequestSerializer();
            var packet = new JoinRoomRequest("user".AsSnowflakeId(), "session".AsGuid())
            {
                RoomId = "room".AsSnowflakeId(), Password = "test-group-pwd"
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
        public void KickUserRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new KickUserRequestSerializer();
            var packet = new KickUserRequest("user".AsSnowflakeId(), "session".AsGuid())
            {
                RoomId = "room".AsSnowflakeId(), KickId = "kick-user".AsSnowflakeId()
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
        public void LeaveRoomRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LeaveRoomRequestSerializer();
            var packet = new LeaveRoomRequest("user".AsSnowflakeId(), "session".AsGuid()) { RoomId = "room".AsSnowflakeId() };
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
        public void FetchRoomsRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchRoomsRequestSerializer();
            var packet = new FetchRoomsRequest("user".AsSnowflakeId(), "session".AsGuid())
            {
                RoomIds = new[] { "room1".AsSnowflakeId(), "room2".AsSnowflakeId(), "room3".AsSnowflakeId() }
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
    }
}
