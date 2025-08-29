// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Serialization;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Network.Serialization
{
    [TestFixture]
    public class RequestPacketTests : SerializerTest
    {
        [Test]
        public void LoginRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LoginRequestPacketSerializer();
            var packet = new LoginRequest("Marry Jones", "password$123");

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void LogoutRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LogoutRequestSerializer();
            var packet = new LogoutRequest("access-token".AsGuid());

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void RegisterRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RegisterRequestSerializer();
            var packet = new RegisterRequest("John Doe", "stronger$#pwd*234");

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void FetchMessagesRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchMessagesRequestSerializer();
            var packet = new FetchMessagesRequest("access-token".AsGuid())
            {
                BeforeId = "before".AsSnowflakeId(), Count = 123, Inclusive = true, RoomId = "room".AsGuid()
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void FetchUsersRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchUsersRequestSerializer();
            var packet = new FetchUsersRequest("access-token".AsGuid())
            {
                UserIds = new[] { "test user".AsGuid(), "test user 2".AsGuid(), "test user 3".AsGuid(), }
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void MessagePostRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new MessagePostRequestSerializer();
            var packet = new MessagePostRequest("access-token".AsGuid())
            {
                RoomId = "test room id".AsSnowflakeId(),
                Message = "This is a test message. The quick brown fox jumps over... \r\n" +
                          "Let's also add in $some $sp4cia1 ch4r6t3rs: e, è, é, ê, ë\t\aç"
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void RoomCreateRequestPacket_Direct_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RoomCreateRequestSerializer();
            var packet = new RoomCreateRequest.Direct("access-token".AsGuid()) { OtherId = "other-user".AsGuid() };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void RoomCreateRequestPacket_Group_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RoomCreateRequestSerializer();
            var packet = new RoomCreateRequest.Group("access-token".AsGuid())
            {
                Name = "test-group", Password = "test-group-pwd", Public = true
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void RoomDeleteRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new DeleteRoomRequestSerializer();
            var packet = new RoomDeleteRequest("access-token".AsGuid())
            {
                RoomId = "room".AsSnowflakeId(), Password = "test-group-pwd"
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void JoinRoomRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new JoinRoomRequestSerializer();
            var packet = new JoinRoomRequest("access-token".AsGuid())
            {
                RoomId = "room".AsGuid(), Password = "test-group-pwd"
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void KickUserRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new KickUserRequestSerializer();
            var packet = new KickUserRequest("access-token".AsGuid())
            {
                RoomId = "room".AsGuid(), KickId = "kick-user".AsGuid()
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void LeaveRoomRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LeaveRoomRequestSerializer();
            var packet = new LeaveRoomRequest("access-token".AsGuid()) { RoomId = "room".AsGuid() };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }

        [Test]
        public void FetchRoomsRequestPacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchRoomsRequestSerializer();
            var packet = new FetchRoomsRequest("access-token".AsGuid())
            {
                RoomIds = new[] { "room1".AsGuid(), "room2".AsGuid(), "room3".AsGuid() }
            };

            // Act
            serializer.Serialize(packet, Sequence);
            var newPacket = serializer.Deserialize(Sequence.AsReadOnlySequence, out long bytesRead);

            // Assert
            Assert.That(Sequence.Length, Is.EqualTo(bytesRead));
            Assert.That(newPacket, Is.EqualTo(packet).UsingPropertiesComparer());
        }
    }
}
