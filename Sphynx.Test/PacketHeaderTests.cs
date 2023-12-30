using System.Reflection.PortableExecutable;

using NUnit.Framework.Constraints;

namespace Sphynx.Test
{
    [TestFixture]
    public class PacketHeaderTests
    {
        private const int SIGNATURE_SIZE = sizeof(ushort);

        [TestCase(SphynxPacketType.MSG_REQ, 10)]
        public void RequestPacketHeader_ShouldSerializeWithCorrectFormat(SphynxPacketType packetType, int contentSize)
        {
            if ((int)packetType > 0)
            {
                // Empty header test
                byte[] emptyHeader = SerializeRequestHeader(SphynxPacketType.NOP, 0);
                Assert.That(emptyHeader, Has.Length.EqualTo(SphynxRequestHeader.HEADER_SIZE));

                // Implicitly verifies signature
                Assert.DoesNotThrow(() => new SphynxRequestHeader(emptyHeader));
                for (int i = SIGNATURE_SIZE; i < emptyHeader.Length; i++)
                {
                    Assert.That(emptyHeader[i], Is.Zero);
                }

                // Sample test - we should be able to deserialize the serialization
                var sampleUserId = Guid.NewGuid();
                var sampleSessionId = Guid.NewGuid();
                Span<byte> sampleHeader = SerializeRequestHeader(packetType, sampleUserId, sampleSessionId, contentSize);
                var deserializedHeader = new SphynxRequestHeader(sampleHeader);

                Assert.That(deserializedHeader, Is.EqualTo(new SphynxRequestHeader(packetType, sampleUserId, sampleSessionId, contentSize)));
            }
            else
            {
                Assert.Throws(new InstanceOfTypeConstraint(typeof(Exception)), () => SerializeRequestHeader(packetType, contentSize));
            }
        }

        [TestCase(SphynxPacketType.CHAT_CREATE_REQ, 32)]
        [TestCase(SphynxPacketType.CHAT_SELECT_REQ, 8421)]
        [TestCase(SphynxPacketType.MSG_REQ, 1024)]
        [TestCase(SphynxPacketType.LOGIN_RES, 512)]
        public void RequestPacketHeader_ShouldDeserializeWithCorrectData(SphynxPacketType packetType, int contentSize)
        {
            if ((int)packetType > 0)
            {
                var sampleUserId = Guid.NewGuid();
                var sampleSessionId = Guid.NewGuid();
                ReadOnlySpan<byte> sampleHeader = SerializeRequestHeader(packetType, sampleUserId, sampleSessionId, contentSize);
                var deserializedHeader = new SphynxRequestHeader(sampleHeader);

                Assert.That(deserializedHeader, Is.EqualTo(new SphynxRequestHeader(packetType, sampleUserId, sampleSessionId, contentSize)));
            }
            else
            {
                // Force into request packet
                byte[] incorrectHeader = SerializeRequestHeader((SphynxPacketType)((uint)packetType & 0b01111111), contentSize);
                incorrectHeader[SIGNATURE_SIZE + sizeof(int) - 1] |= 1 << 7;

                Assert.Throws(new InstanceOfTypeConstraint(typeof(Exception)), () => new SphynxRequestHeader(incorrectHeader));
            }
        }

        [TestCase(SphynxPacketType.MSG_RES, 10)]
        [TestCase(SphynxPacketType.LOGIN_RES, 1024)]
        [TestCase(SphynxPacketType.CHAT_INV_RES, 25565)]
        [TestCase(SphynxPacketType.CHAT_KICK_REQ, 1234)]
        public void ResponsePacketHeader_ShouldSerializeWithCorrectFormat(SphynxPacketType packetType, int contentSize)
        {
            if ((int)packetType < 0)
            {
                // Empty header test
                byte[] emptyHeader = SerializeResponseHeader(SphynxPacketType.NOP, 0);
                Assert.That(emptyHeader, Has.Length.EqualTo(SphynxResponseHeader.HEADER_SIZE));

                // Implicitly verifies signature
                Assert.DoesNotThrow(() => new SphynxResponseHeader(emptyHeader));
                for (int i = SIGNATURE_SIZE; i < emptyHeader.Length; i++)
                {
                    Assert.That(emptyHeader[i], Is.Zero);
                }

                // Sample test - we should be able to deserialize the serialization
                Span<byte> sampleHeader = SerializeResponseHeader(packetType, contentSize);
                var deserializedHeader = new SphynxResponseHeader(sampleHeader);

                Assert.That(deserializedHeader, Is.EqualTo(new SphynxResponseHeader(packetType, contentSize)));
            }
            else
            {
                Assert.Throws(new InstanceOfTypeConstraint(typeof(Exception)), () => SerializeResponseHeader(packetType, contentSize));
            }
        }

        [TestCase(SphynxPacketType.CHAT_CREATE_RES, 32)]
        [TestCase(SphynxPacketType.LOGIN_RES, 32678)]
        [TestCase(SphynxPacketType.CHAT_SELECT_RES, 100)]
        [TestCase(SphynxPacketType.CHAT_KICK_REQ, 9999)]
        public void ResponsePacketHeader_ShouldDeserializeWithCorrectData(SphynxPacketType packetType, int contentSize)
        {
            if ((int)packetType < 0)
            {
                Span<byte> sampleHeader = SerializeResponseHeader(packetType, contentSize);
                var deserializedHeader = new SphynxResponseHeader(sampleHeader);

                Assert.That(deserializedHeader, Is.EqualTo(new SphynxResponseHeader(packetType, contentSize)));
            }
            else
            {
                // Force into response packet
                byte[] incorrectHeader = SerializeResponseHeader((SphynxPacketType)((uint)packetType | (1 << 31)), contentSize);
                incorrectHeader[SIGNATURE_SIZE + sizeof(int) - 1] &= 0b01111111;

                Assert.Throws(new InstanceOfTypeConstraint(typeof(Exception)), () => new SphynxResponseHeader(incorrectHeader));
            }
        }

        private byte[] SerializeRequestHeader(SphynxPacketType packetType, int contentSize)
        {
            var header = new SphynxRequestHeader(packetType, contentSize);

            byte[] serializedHeader = new byte[SphynxRequestHeader.HEADER_SIZE];
            header.Serialize(serializedHeader);
            return serializedHeader;
        }

        private byte[] SerializeRequestHeader(SphynxPacketType packetType, Guid userId, Guid sessionId, int contentSize)
        {
            var header = new SphynxRequestHeader(packetType, userId, sessionId, contentSize);

            byte[] serializedHeader = new byte[SphynxRequestHeader.HEADER_SIZE];
            header.Serialize(serializedHeader);
            return serializedHeader;
        }

        private byte[] SerializeResponseHeader(SphynxPacketType packetType, int contentSize)
        {
            var header = new SphynxResponseHeader(packetType, contentSize);

            byte[] serializedHeader = new byte[SphynxResponseHeader.HEADER_SIZE];
            header.Serialize(serializedHeader);
            return serializedHeader;
        }
    }
}