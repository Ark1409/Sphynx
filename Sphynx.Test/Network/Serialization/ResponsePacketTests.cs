// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization;
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
            var serializer = new LoginResponseSerializer(new SphynxSelfInfoSerializer());
            var packet = new LoginResponse(new TestSphynxSelfInfo(), "access-token", Guid.NewGuid(), DateTimeOffset.UtcNow);
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
            var serializer = new LogoutResponseSerializer();
            var packet = new LogoutResponse();
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
            var serializer = new RegisterResponseSerializer(new SphynxSelfInfoSerializer());
            var packet = new RegisterResponse(new TestSphynxSelfInfo(), "access-token", Guid.NewGuid(), DateTimeOffset.UtcNow);
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
        public void FetchMessagesResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchMessagesResponseSerializer(new ChatMessageSerializer());
            var packet = new FetchMessagesResponse
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
            var serializer = new FetchUsersResponseSerializer(new SphynxUserInfoSerializer());
            var packet = new FetchUsersResponse
            {
                // ReSharper disable once CoVariantArrayConversion
                Users = TestSphynxSelfInfo.FromNames("user1", "user2", "user3")!
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
        public void MessagePostResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new MessagePostResponseSerializer();
            var packet = new MessagePostResponse();
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
        public void RoomCreateResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RoomCreateResponseSerializer();
            var packet = new RoomCreateResponse("room1".AsSnowflakeId());
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
        public void RoomDeleteResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new RoomDeleteResponseSerializer();
            var packet = new RoomDeleteResponse();
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
            var serializer = new JoinRoomResponseSerializer(new ChatRoomInfoSerializer());
            var packet = new JoinRoomResponse { RoomInfo = new TestDirectChatRoomInfo() };
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
            var serializer = new KickUserResponseSerializer();
            var packet = new KickUserResponse();
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
        public void LeaveRoomResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new LeaveRoomResponseSerializer();
            var packet = new LeaveRoomResponse();
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
        public void FetchRoomsResponsePacket_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var serializer = new FetchRoomsResponseSerializer(new ChatRoomInfoSerializer());
            var packet = new FetchRoomsResponse { Rooms = new ChatRoomInfo[] { new TestDirectChatRoomInfo(), new TestGroupChatRoomInfo() } };
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
