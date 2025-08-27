// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Serialization;
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
            _transporter = new PacketTransporter(new JsonPacketSerializer())
            {
                Version = new Version(0, 0, 1)
            };
        }

        [Test]
        public void PacketTransporter_ShouldSendPacket_WhenSerializerRegistered()
        {
            // Arrange
            var packets = new SphynxPacket[]
            {
                new LoginRequest("username", "password"),
                new LogoutRequest("access-token".AsGuid(), Guid.NewGuid()),
                new MessagePostRequest("access-token".AsGuid(), "room-id".AsSnowflakeId(), "Test message")
            };

            using var stream = new MemoryStream();

            // Act + Assert
            foreach (var packet in packets)
            {
                Assert.DoesNotThrowAsync(async () => await _transporter.SendAsync(stream, packet).ConfigureAwait(false));
            }
        }

        [Test]
        public async Task PacketTransporter_ShouldReceivePacket_WhenSerializerRegistered()
        {
            // Arrange
            var packets = new SphynxPacket[]
            {
                new LoginRequest("username", "password"),
                new LogoutRequest("access-token".AsGuid(), Guid.NewGuid()),
                new MessagePostRequest("access-token".AsGuid(), "room-id".AsSnowflakeId(), "Test message")
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
    }
}
