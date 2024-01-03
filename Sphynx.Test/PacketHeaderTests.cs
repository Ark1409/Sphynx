using System.Reflection.PortableExecutable;

using NUnit.Framework.Constraints;

namespace Sphynx.Test
{
    [TestFixture]
    public class PacketHeaderTests
    {
        [TestCase(SphynxPacketType.MSG_REQ, 10)]
        [TestCase(SphynxPacketType.MSG_RES, 10)]
        [TestCase(SphynxPacketType.LOGIN_RES, 1024)]
        [TestCase(SphynxPacketType.CHAT_JOIN_BCAST, 25565)]
        [TestCase(SphynxPacketType.CHAT_KICK_REQ, 1234)]
        [TestCase(SphynxPacketType.CHAT_CREATE_REQ, 32)]
        [TestCase(SphynxPacketType.CHAT_SELECT_REQ, 8421)]
        [TestCase(SphynxPacketType.MSG_REQ, 1024)]
        [TestCase(SphynxPacketType.LOGIN_RES, 512)]
        public void PacketHeader_ShouldSerializeWithCorrectFormat(SphynxPacketType packetType, int contentSize)
        {
            var sampleHeader = new SphynxPacketHeader(packetType, contentSize);
            var serializedHeader = sampleHeader.Serialize();

            Assert.Multiple(() =>
            {
                Assert.That(SphynxPacketHeader.TryDeserialize(serializedHeader, out var deserializedHeader));
                Assert.That(deserializedHeader, Is.EqualTo(sampleHeader));
            });
        }
    }
}