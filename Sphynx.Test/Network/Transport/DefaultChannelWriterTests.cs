// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework.Legacy;
using Sphynx.Network.Transport;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class DefaultChannelWriterTests
    {
        [Test]
        public async Task SendReleaseAsync_ShouldSendReleaseFrame_WhenWriterIsNotDisposed()
        {
            // Arrange
            using var stream = new MemoryStream();
            await using var writer = new DefaultChannelWriter(stream);

            // Act
            await writer.SendReleaseAsync(ChannelId.MaxValue);
            stream.Position = 0;

            // Assert
            var releaseFrame = new SphynxFrameHeader(SphynxFrameType.CHANNEL_RELEASE, ChannelId.MaxValue, 0);
            byte[] releaseBytes = releaseFrame.Serialize();

            CollectionAssert.AreEqual(stream.ToArray(), releaseBytes);
        }

        [Test]
        public async Task OpenChannel_ShouldThrow_WhenDuplicateChannelExists()
        {
            // Arrange
            await using var writer = new DefaultChannelWriter(new MemoryStream());
            await using var _ = writer.OpenChannel(ChannelId.MaxValue);

            // Act + Assert
            Assert.Throws(Is.InstanceOf<Exception>(), () => writer.OpenChannel(ChannelId.MaxValue));
        }

        [Test]
        public async Task DisposeAsync_ShouldNotAbortChannel_WhenNoDataHasBeenSent()
        {
            // Arrange
            using var stream = new MemoryStream();
            var writer = new DefaultChannelWriter(stream);

            // Don't dispose or flush the channel - don't send any data
            var channel = writer.OpenChannel(channelId: 1);
            await channel.WriteAsync(new byte[] { 1 });

            Assert.That(stream.Length, Is.Zero);

            // Act
            await writer.DisposeAsync();

            // Assert
            CollectionAssert.AreEqual(Array.Empty<byte>(), stream.ToArray());
        }

        [Test]
        public async Task DisposeAsync_ShouldAbortUnclosedChannels_WhenDataHasBeenSent()
        {
            // Arrange
            using var stream = new MemoryStream();
            var writer = new DefaultChannelWriter(stream);

            // Don't dispose the channel - leave it unfinished
            var channel = writer.OpenChannel(channelId: 1);
            await channel.WriteAsync(new byte[] { 1, 2, 3, 4, 5 });
            await channel.FlushAsync();

            Array.Clear(stream.GetBuffer());
            stream.SetLength(0);

            // Act
            await writer.DisposeAsync();

            // Assert
            byte[] expectedFrame = new SphynxFrameHeader
            {
                FrameType = SphynxFrameType.CHANNEL_ABORT,
                ChannelId = 1,
            }.Serialize();

            CollectionAssert.AreEqual(expectedFrame, stream.ToArray());
        }

        [TestFixture]
        public class ChannelTests
        {
            [Test]
            public async Task FlushAsync_ShouldFlushBuffer_WhenBufferIsNotEmpty()
            {
                // Arrange
                using var stream = new MemoryStream();
                await using var writer = new DefaultChannelWriter(stream);
                await using var channel = writer.OpenChannel();
                await channel.WriteAsync(new byte[] { 1, 2, 3 });

                Assert.That(stream.Length, Is.Zero);

                // Act
                await channel.FlushAsync();

                // Assert
                Assert.That(stream.Length, Is.GreaterThan(0));
            }

            [Test]
            public async Task WriteAsync_ShouldWriteDataFrame_WhenChannelFlushed()
            {
                // Arrange
                using var stream = new MemoryStream();
                await using var writer = new DefaultChannelWriter(stream);

                // Act
                await using (var channel = writer.OpenChannel(channelId: 1))
                {
                    await channel.WriteAsync(new byte[] { 1, 2, 3 });
                }

                // Assert
                var expectedFrame = new SphynxFrameHeader
                    {
                        FrameType = SphynxFrameType.CHANNEL_DATA,
                        Flags = ChannelDataFlags.CHANNEL_START | ChannelDataFlags.CHANNEL_END,
                        ChannelId = 1,
                        FrameSize = 3,
                    }.Serialize()
                    .Concat(new byte[] { 1, 2, 3 });

                CollectionAssert.AreEqual(expectedFrame, stream.ToArray());
            }

            [Test]
            public async Task DisposeAsync_ShouldWriteAbortFrame_WhenDisposingWithException()
            {
                // Arrange
                using var stream = new MemoryStream();
                await using var writer = new DefaultChannelWriter(stream);

                // Act
                var channel = writer.OpenChannel(channelId: 1);
                await channel.WriteAsync(new byte[] { 1 });
                await channel.FlushAsync();
                stream.SetLength(0);
                await channel.DisposeAsync(new ChannelClosedException());

                // Assert
                byte[] expectedFrame = new SphynxFrameHeader
                {
                    FrameType = SphynxFrameType.CHANNEL_ABORT,
                    ChannelId = 1,
                }.Serialize();

                CollectionAssert.AreEqual(expectedFrame, stream.ToArray());
            }
        }
    }
}
