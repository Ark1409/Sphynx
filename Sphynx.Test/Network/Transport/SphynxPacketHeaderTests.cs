using Sphynx.Network.Packet;
using Sphynx.Network.Transport;
using Version = Sphynx.Core.Version;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class SphynxPacketHeaderTests
    {
        [Theory]
        public void SphynxPacketHeader_ShouldSerializeCorrectly(SphynxPacketType packetType)
        {
            // Arrange
            const int CONTENT_SIZE = 1024;
            var sampleHeader = new SphynxPacketHeader(new Version(1, 2, 3), packetType, CONTENT_SIZE);

            // Act
            byte[] serializedHeader = sampleHeader.Serialize();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(SphynxPacketHeader.TryDeserialize(serializedHeader, out var deserializedHeader));
                Assert.That(deserializedHeader, Is.EqualTo(sampleHeader));
            });
        }
    }
}
