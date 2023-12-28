using System.Data.SqlTypes;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using NUnit.Framework.Constraints;

namespace Sphynx.Test
{
    [TestFixture]
    public class RequestPacketTests
    {
        private const int GUID_SIZE = 16;

        [Test]
        public void RequestPacketHeader_ShouldSerializeWithCorrectFormat()
        {
            Span<byte> emptyHeader = SerializeRequestHeader(SphynxPacketType.NOP, 0);
            Assert.That(emptyHeader.Length, Is.EqualTo(SphynxRequestHeader.HEADER_SIZE));
            CheckEmptyHeader(emptyHeader);

            var samplePacketType = SphynxPacketType.MSG_REQ;
            var sampleUserId = Guid.NewGuid();
            var sampleSessionId = Guid.NewGuid();
            int sampleContentSize = 10;
            Span<byte> sampleHeader = SerializeRequestHeader(samplePacketType, sampleUserId, sampleSessionId, sampleContentSize);
            Assert.That(sampleHeader.Length, Is.EqualTo(SphynxRequestHeader.HEADER_SIZE));

            CheckSignature(sampleHeader.Slice(0, sizeof(ushort)));
            CheckPacketType(sampleHeader.Slice(sizeof(ushort), sizeof(SphynxPacketType)), samplePacketType);
            CheckGuid(sampleHeader.Slice(sizeof(ushort) + sizeof(SphynxPacketType), GUID_SIZE), in sampleUserId);
            CheckGuid(sampleHeader.Slice(sizeof(ushort) + sizeof(SphynxPacketType) + GUID_SIZE, GUID_SIZE), in sampleSessionId);
            CheckContentSize(sampleHeader.Slice(sizeof(ushort) + sizeof(SphynxPacketType) + 2 * GUID_SIZE, sizeof(int)), sampleContentSize);

            Assert.Throws(new InstanceOfTypeConstraint(typeof(Exception)), () => SerializeRequestHeader(SphynxPacketType.LOGIN_RES, 10));
        }

        private void CheckEmptyHeader(ReadOnlySpan<byte> header)
        {
            CheckSignature(header.Slice(0, sizeof(ushort)));

            for (int i = sizeof(ushort); i < header.Length; i++)
            {
                Assert.That(header[i], Is.Zero);
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

        [Test]
        public void RequestPacketHeader_ShouldDeserializeWithCorrectData()
        {
            var samplePacketType = SphynxPacketType.CHAT_CREATE_REQ;
            var sampleUserId = Guid.NewGuid();
            var sampleSessionId = Guid.NewGuid();
            int sampleContentSize = 32;
            Span<byte> sampleHeader = SerializeRequestHeader(samplePacketType, sampleUserId, sampleSessionId, sampleContentSize);

            var deserializedHeader = new SphynxRequestHeader(sampleHeader);
            Assert.Multiple(() =>
            {
                Assert.That(deserializedHeader.PacketType, Is.EqualTo(samplePacketType));
                Assert.That(deserializedHeader.UserId, Is.EqualTo(sampleUserId));
                Assert.That(deserializedHeader.SessionId, Is.EqualTo(sampleSessionId));
                Assert.That(deserializedHeader.ContentSize, Is.EqualTo(sampleContentSize));
            });
        }

        [Test]
        public void ResponsePacketHeader_ShouldSerializeWithCorrectFormat()
        {
            Span<byte> emptyHeader = SerializeResponseHeader(SphynxPacketType.NOP, 0);
            Assert.That(emptyHeader.Length, Is.EqualTo(SphynxResponseHeader.HEADER_SIZE));
            CheckEmptyHeader(emptyHeader);

            var samplePacketType = SphynxPacketType.MSG_RES;
            int sampleContentSize = 10;
            Span<byte> sampleHeader = SerializeResponseHeader(samplePacketType, sampleContentSize);
            Assert.That(sampleHeader.Length, Is.EqualTo(SphynxResponseHeader.HEADER_SIZE));

            CheckSignature(sampleHeader.Slice(0, sizeof(ushort)));
            CheckPacketType(sampleHeader.Slice(sizeof(ushort), sizeof(SphynxPacketType)), samplePacketType);
            CheckContentSize(sampleHeader.Slice(sizeof(ushort) + sizeof(SphynxPacketType), sizeof(int)), sampleContentSize);

            Assert.Throws(new InstanceOfTypeConstraint(typeof(Exception)), () => SerializeResponseHeader(SphynxPacketType.CHAT_DEL_REQ, 10));
        }

        [Test]
        public void ResponsePacketHeader_ShouldDeserializeWithCorrectData()
        {
            var samplePacketType = SphynxPacketType.CHAT_CREATE_RES;
            int sampleContentSize = 32;
            Span<byte> sampleHeader = SerializeResponseHeader(samplePacketType, sampleContentSize);

            var deserializedHeader = new SphynxResponseHeader(sampleHeader);
            Assert.Multiple(() =>
            {
                Assert.That(deserializedHeader.PacketType, Is.EqualTo(samplePacketType));
                Assert.That(deserializedHeader.ContentSize, Is.EqualTo(sampleContentSize));
            });
        }

        private byte[] SerializeResponseHeader(SphynxPacketType packetType, int contentSize)
        {
            var header = new SphynxResponseHeader(packetType, contentSize);

            byte[] serializedHeader = new byte[SphynxResponseHeader.HEADER_SIZE];
            header.Serialize(serializedHeader);
            return serializedHeader;
        }

        private void CheckSignature(ReadOnlySpan<byte> sigBytes)
        {
            var signatureSpan = MemoryMarshal.Cast<byte, ushort>(sigBytes);
            Assert.That(signatureSpan.Length, Is.EqualTo(1));
            Assert.That(signatureSpan[0], Is.EqualTo(SphynxPacketHeader.SIGNATURE));
        }

        private void CheckPacketType(ReadOnlySpan<byte> packetTypeBytes, SphynxPacketType packetType)
        {
            var packetTypeSpan = MemoryMarshal.Cast<byte, SphynxPacketType>(packetTypeBytes);
            Assert.That(packetTypeSpan.Length, Is.EqualTo(1));
            Assert.That(packetTypeSpan[0], Is.EqualTo(packetType));
        }

        private void CheckGuid(ReadOnlySpan<byte> guidBytes, in Guid guid)
        {
            var guidSpan = new Guid(guidBytes);
            Assert.That(guidSpan, Is.EqualTo(guid));
        }

        private void CheckContentSize(ReadOnlySpan<byte> contentSizeBytes, int contentSize)
        {
            var contentSizeSpan = MemoryMarshal.Cast<byte, int>(contentSizeBytes);
            Assert.That(contentSizeSpan.Length, Is.EqualTo(1));
            Assert.That(contentSizeSpan[0], Is.EqualTo(contentSize));
        }
    }
}