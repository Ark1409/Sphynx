using NUnit.Framework.Legacy;
using Sphynx.Network.Transport;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class SphynxFrameHeaderTests
    {
        [Test]
        public void Serialize_ShouldNotThrow_WhenHeaderIsValid()
        {
            // Arrange
            var headers = new[]
            {
                new SphynxFrameHeader
                {
                    ChannelId = ChannelId.MaxValue,
                    FrameSize = short.MaxValue,
                    FrameType = SphynxFrameType.CHANNEL_DATA,
                    Flags = ChannelDataFlags.CHANNEL_END
                },
                new SphynxFrameHeader
                {
                    ChannelId = 10,
                    FrameSize = 123,
                    FrameType = SphynxFrameType.CHANNEL_DATA,
                    Flags = ChannelDataFlags.CHANNEL_START | ChannelDataFlags.CHANNEL_END,
                },
                new SphynxFrameHeader
                {
                    ChannelId = 10,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_DATA,
                    Flags = 0,
                },
                new SphynxFrameHeader
                {
                    ChannelId = ChannelId.MinValue,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_ABORT,
                    Flags = 0,
                },
                new SphynxFrameHeader
                {
                    ChannelId = ChannelId.MaxValue / 2,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_RELEASE,
                    Flags = 0,
                },
            };

            // Act + Assert
            foreach (var header in headers)
                Assert.DoesNotThrow(() => header.Serialize());
        }

        [Test]
        public void Serialize_ShouldThrow_WhenHeaderIsInvalid()
        {
            // Arrange
            var headers = new[]
            {
                new SphynxFrameHeader
                {
                    ChannelId = ChannelId.MinValue,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_ABORT,
                    Flags = ChannelDataFlags.CHANNEL_END
                },
                new SphynxFrameHeader
                {
                    ChannelId = 1234,
                    FrameSize = 1,
                    FrameType = SphynxFrameType.CHANNEL_ABORT,
                    Flags = 0,
                },
                new SphynxFrameHeader
                {
                    ChannelId = 5,
                    FrameSize = -1,
                    FrameType = SphynxFrameType.CHANNEL_RELEASE,
                    Flags = 0,
                },
                new SphynxFrameHeader
                {
                    ChannelId = 5,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_RELEASE,
                    Flags = 10,
                },
                new SphynxFrameHeader
                {
                    ChannelId = 5,
                    FrameSize = 10,
                    FrameType = SphynxFrameType.CHANNEL_DATA,
                    Flags = 123,
                },
            };

            // Act + Assert
            foreach (var header in headers)
                Assert.Throws(Is.InstanceOf<InvalidOperationException>(), () => header.Serialize());
        }

        [Test]
        public void TryDeserialize_ShouldNotFail_WhenHeaderIsValid()
        {
            // Arrange
            var header = new SphynxFrameHeader
            {
                ChannelId = ChannelId.MaxValue,
                FrameSize = short.MaxValue,
                FrameType = SphynxFrameType.CHANNEL_DATA,
                Flags = ChannelDataFlags.CHANNEL_END
            };

            Assert.That(header.IsValid());
            byte[] headerBytes = header.Serialize();

            // Act
            bool deserialized = SphynxFrameHeader.TryDeserialize(headerBytes, out var deserializedHeader);

            // Assert
            Assert.That(deserialized);
            Assert.That(deserializedHeader, Is.EqualTo(header));
        }

        [Test]
        public void Serialize_ShouldProduceExpectedByteSequence_WhenInvoked()
        {
            // Arrange
            var header = new SphynxFrameHeader
            {
                FrameType = SphynxFrameType.CHANNEL_DATA,
                Flags = ChannelDataFlags.CHANNEL_START | ChannelDataFlags.CHANNEL_END,
                ChannelId = ChannelId.MaxValue,
                FrameSize = 0x00FF,
            };

            var expectedByteSequence = SphynxFrameHeader.Signature.ToArray().Concat(new byte[]
            {
                SphynxFrameHeader.ProtocolVersion.Major,
                ((byte)SphynxFrameType.CHANNEL_DATA << 4) | (ChannelDataFlags.CHANNEL_START | ChannelDataFlags.CHANNEL_END),
                0xFF, 0xFF,
                0x00, 0xFF
            });

            // Act
            byte[] byteSequence = header.Serialize();

            // Assert
            CollectionAssert.AreEqual(expectedByteSequence, byteSequence);
        }
    }
}
