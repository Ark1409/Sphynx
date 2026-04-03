using Sphynx.Network.Transport;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class SphynxFrameHeaderTests
    {
        [Test]
        public void SphynxFrameHeader_ShouldSerialize_WhenHeaderIsValid()
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
                    FrameType = SphynxFrameType.CHANNEL_REJECT,
                    Flags = 0,
                },
            };

            // Act
            byte[][] headerBytes = new byte[headers.Length][];

            for (int i = 0; i < headerBytes.Length; i++)
                Assert.DoesNotThrow(() => headerBytes[i] = headers[i].Serialize());

            // Assert
            for (int i = 0; i < headerBytes.Length; i++)
            {
                Assert.That(SphynxFrameHeader.TryDeserialize(headerBytes[i], out var deserializedHeader));
                Assert.That(deserializedHeader, Is.EqualTo(headers[i]));
            }
        }

        [Test]
        public void SphynxFrameHeader_ShouldNotSerialize_WhenHeaderIsInvalid()
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
                    ChannelId = ChannelId.MinValue,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_REJECT,
                    Flags = ChannelDataFlags.CHANNEL_START
                },
                new SphynxFrameHeader
                {
                    ChannelId = -10,
                    FrameSize = 1,
                    FrameType = SphynxFrameType.CHANNEL_REJECT,
                    Flags = 0,
                },
                new SphynxFrameHeader
                {
                    ChannelId = 5,
                    FrameSize = -1,
                    FrameType = SphynxFrameType.CHANNEL_REJECT,
                    Flags = 0,
                },
                new SphynxFrameHeader
                {
                    ChannelId = 5,
                    FrameSize = 0,
                    FrameType = SphynxFrameType.CHANNEL_REJECT,
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
                Assert.Throws(Is.InstanceOf<Exception>(), () => header.Serialize());
        }
    }
}
