// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework.Legacy;
using Sphynx.Network.Transport;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class SphynxChannelReaderTests
    {
        [Test]
        public async Task OnFrameDropped_ShouldBeInvoked_WhenChannelDataIsSentWithoutStart()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] channelBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, 1, 0).Serialize();
            await stream.WriteAsync(channelBytes);

            stream.Position = 0;

            // Act
            await using var reader = new SphynxChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelFrameDropped((_, header) =>
            {
                invoked = header.ChannelId == 1 && header.FrameSize == 0;
                channelDone.Release();
            });

            await reader.RunAsync();
            await channelDone.WaitAsync();

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task OnChannelRejecting_ShouldBeInvoked_WhenChannelIsPrematurelyDisposed()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] dataBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, ChannelDataFlags.CHANNEL_START, channelId: 1, 0).Serialize();
            await stream.WriteAsync(dataBytes);

            stream.Position = 0;

            // Act
            await using var reader = new SphynxChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelRejecting((_, channelId) =>
            {
                invoked = channelId == 1;
                channelDone.Release();
            });

            reader.OnChannelOpened(async (_, channel) =>
            {
                await channel.DisposeAsync();
            });

            await reader.RunAsync();
            await channelDone.WaitAsync();

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task OnChannelRejected_ShouldBeInvoked_WhenRejectIsReceived()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] rejectBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_REJECT, 1, 0).Serialize();
            await stream.WriteAsync(rejectBytes);

            stream.Position = 0;

            // Act
            await using var reader = new SphynxChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelRejected((_, channelId) =>
            {
                invoked = channelId == 1;
                channelDone.Release();
            });

            await reader.RunAsync();
            await channelDone.WaitAsync();

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task OnChannelOpened_ShouldBeInvoked_WhenChannelStartIsReceived()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] startBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, ChannelDataFlags.CHANNEL_START, 1, 0).Serialize();
            await stream.WriteAsync(startBytes);

            stream.Position = 0;

            // Act
            await using var reader = new SphynxChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelOpened((_, channel) =>
            {
                invoked = channel.ChannelId == 1;
                channelDone.Release();
                return ValueTask.CompletedTask;
            });

            await reader.RunAsync();
            await channelDone.WaitAsync();

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task RunAsync_ShouldNotDisposeChannel_WhenChannelCompleted()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] frameData = [1, 2, 4, 8];
            int frameCount = 3;

            for (int i = 0; i < frameCount; i++)
            {
                var frameHeader = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, 1, (short)frameData.Length);

                if (i == 0)
                    frameHeader = frameHeader.WithFlags(ChannelDataFlags.CHANNEL_START);

                if (i == frameCount - 1)
                    frameHeader = frameHeader.WithFlags(ChannelDataFlags.CHANNEL_END);

                stream.Write(frameHeader.Serialize());
                stream.Write(frameData);
            }

            stream.Position = 0;
            frameData = new byte[frameData.Length * frameCount];

            var channelDone = new SemaphoreSlim(0, 1);

            // Act
            await using var reader = new SphynxChannelReader(stream);
            SphynxChannelReader.Channel? channel = null;

            reader.OnChannelOpened(async (_, ch) =>
            {
                channel = ch;
                await channel.ReadExactlyAsync(frameData.AsMemory());
                channelDone.Release();
            });

            await reader.RunAsync();
            await channelDone.WaitAsync();

            // Assert
            Assert.That(channel, Is.Not.Null);
            Assert.That(channel.IsDisposed, Is.False);
            CollectionAssert.AreEqual(Enumerable.Repeat(new[] { 1, 2, 4, 8 }, frameCount).SelectMany(x => x), frameData.ToArray());

            await channel.DisposeAsync();
        }
    }
}
