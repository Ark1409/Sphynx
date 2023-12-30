using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Test
{
    [TestFixture]
    public class RequestPacketTests
    {
        [TestCase("Fred", "fredisbest123")]
        [TestCase("Timothy", "timtimtim222")]
        [TestCase("Bryan", "j$u3#mosq")]
        public void LoginRequestPacket_ShouldSerializeWithCorrectFormat(string userName, string password)
        {
            var packet = new LoginRequestPacket(userName, password);
            Span<byte> packetBytes = packet.Serialize();
            var deserializedPacket = new LoginRequestPacket(packetBytes.Slice(SphynxRequestHeader.HEADER_SIZE));

            Assert.Multiple(() =>
            {
                Assert.That(packet.Email, Is.EqualTo(deserializedPacket.Email));
                Assert.That(packet.Password, Is.EqualTo(deserializedPacket.Password));
            });
        }
    }
}
