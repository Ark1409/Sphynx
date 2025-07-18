// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Network.Transport;
using Sphynx.Test.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class PacketTransporterTests
    {
        private PacketTransporter _transporter;

        [SetUp]
        public void SetUp()
        {
            _transporter = new PacketTransporter();

            _transporter.AddSerializer(SphynxPacketType.LOGIN_REQ, new LoginRequestPacketSerializer())
                .AddSerializer(SphynxPacketType.LOGOUT_REQ, new LogoutRequestSerializer())
                .AddSerializer(SphynxPacketType.MSG_REQ, new MessagePostRequestSerializer());

            _transporter.Version = new Version(0, 0, 1);
        }

        [Test]
        public void PacketTransporter_ShouldSendPacket_WhenSerializerRegistered()
        {
            // Arrange
            var packets = new SphynxPacket[]
            {
                new LoginRequest("username", "password"),
                new LogoutRequest("access-token"),
                new MessagePostRequest("access-token",  "room-id".AsSnowflakeId(), "Test message")
            };

            using var stream = new MemoryStream();

            // Act + Assert
            foreach (var packet in packets)
            {
                Assert.DoesNotThrowAsync(async () => await _transporter.SendAsync(stream, packet).ConfigureAwait(false));
            }
        }

        [Test]
        public void PacketTransporter_ShouldNotSendPacket_WhenSerializerNotRegistered()
        {
            // Arrange
            var packets = new SphynxPacket[]
            {
                new LoginResponse(SphynxErrorCode.INVALID_USER),
                new LogoutResponse(),
                new MessagePostResponse()
            };

            using var stream = new MemoryStream();

            // Act + Assert
            foreach (var packet in packets)
            {
                Assert.CatchAsync(async () => await _transporter.SendAsync(stream, packet).ConfigureAwait(false));
            }
        }

        [Test]
        public async Task PacketTransporter_ShouldReceivePacket_WhenSerializerRegistered()
        {
            // Arrange
            var packets = new SphynxPacket[]
            {
                new LoginRequest("username", "password"),
                new LogoutRequest("access-token"),
                new MessagePostRequest("access-token", "room-id".AsSnowflakeId(), "Test message")
            };

            using var stream = new MemoryStream();

            foreach (var packet in packets)
                await _transporter.SendAsync(stream, packet).ConfigureAwait(false);

            stream.Seek(0, SeekOrigin.Begin);

            // Act + Assert
            for (int i = 0; i < packets.Length; i++)
            {
                SphynxPacket packet = null!;

                Assert.DoesNotThrowAsync(async () => packet = await _transporter.ReceiveAsync(stream).ConfigureAwait(false));
                Assert.That(packet, Is.EqualTo(packets[i]));
            }
        }

        [Test]
        public async Task PacketTransporter_ShouldNotReceivePacket_WhenSerializerNotRegistered()
        {
            // Arrange
            var packets = new SphynxPacket[]
            {
                new LoginRequest("username", "password"),
                new LogoutRequest("access-token"),
                new MessagePostRequest("access-token", "room-id".AsSnowflakeId(), "Test message")
            };

            using var stream = new MemoryStream();

            foreach (var packet in packets)
                await _transporter.SendAsync(stream, packet).ConfigureAwait(false);

            stream.Seek(0, SeekOrigin.Begin);
            _transporter.ClearSerializers();

            // Act + Assert
            for (int i = 0; i < packets.Length; i++)
            {
                Assert.CatchAsync(async () => await _transporter.ReceiveAsync(stream).ConfigureAwait(false));
            }
        }
    }
}
