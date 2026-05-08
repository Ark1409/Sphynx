// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework.Legacy;
using Sphynx.Network.Transport;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class DefaultChannelReaderTests
    {
        [Test]
        public async Task OnChannelFrameDropped_ShouldBeInvoked_WhenChannelDataIsSentWithoutAStart()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] channelBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, 1, 0).Serialize();
            await stream.WriteAsync(channelBytes);

            stream.Position = 0;

            // Act
            await using var reader = new DefaultChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelFrameDropped((_, header) =>
            {
                invoked = header.ChannelId == 1 && header.FrameSize == 0;
                channelDone.Release();
                return ValueTask.CompletedTask;
            });

            await reader.RunAsync();
            await channelDone.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task OnChannelReleasing_ShouldBeInvoked_WhenChannelHasEnded()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] dataBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, ChannelDataFlags.CHANNEL_START | ChannelDataFlags.CHANNEL_END,
                channelId: 1, 0).Serialize();
            await stream.WriteAsync(dataBytes);

            stream.Position = 0;

            // Act
            await using var reader = new DefaultChannelReader(stream);
            bool invoked = false;
            var invokeDone = new SemaphoreSlim(0, 1);

            reader.OnChannelReleasing((_, channelId, _) =>
            {
                invoked = channelId == 1;
                invokeDone.Release();
                return ValueTask.CompletedTask;
            });

            reader.OnChannelOpened((_, channel) => channel.DisposeAsync());

            await reader.RunAsync();
            await invokeDone.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task OnChannelReleasing_ShouldBeInvokedAndMarkedAsRejecting_WhenChannelIsPrematurelyClosed()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] dataBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, ChannelDataFlags.CHANNEL_START, channelId: 1, 0).Serialize();
            await stream.WriteAsync(dataBytes);

            stream.Position = 0;

            // Act
            await using var reader = new DefaultChannelReader(stream);
            bool invoked = false;
            var invokeDone = new SemaphoreSlim(0, 1);

            reader.OnChannelReleasing((_, channelId, flags) =>
            {
                invoked = (flags & ChannelReleaseFlags.CHANNEL_REJECTED) > 0 && channelId == 1;
                invokeDone.Release();
                return ValueTask.CompletedTask;
            });

            reader.OnChannelOpened((_, channel) => channel.DisposeAsync());

            await reader.RunAsync();
            await invokeDone.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task OnChannelReleased_ShouldBeInvoked_WhenReleaseIsReceived()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] releaseBytes = new SphynxFrameHeader(SphynxFrameType.CHANNEL_RELEASE, 1, 0).Serialize();
            await stream.WriteAsync(releaseBytes);

            stream.Position = 0;

            // Act
            await using var reader = new DefaultChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelReleased((_, channelId, _) =>
            {
                invoked = channelId == 1;
                channelDone.Release();
                return ValueTask.CompletedTask;
            });

            await reader.RunAsync();
            await channelDone.WaitAsync(TimeSpan.FromSeconds(15));

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
            await using var reader = new DefaultChannelReader(stream);
            bool invoked = false;
            var channelDone = new SemaphoreSlim(0, 1);

            reader.OnChannelOpened((_, channel) =>
            {
                invoked = channel.ChannelId == 1;
                channelDone.Release();
                return ValueTask.CompletedTask;
            });

            await reader.RunAsync();
            await channelDone.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.That(invoked);
        }

        [Test]
        public async Task RunAsync_ShouldNotDisposeChannel_WhenChannelIsAlreadyReleased()
        {
            // Arrange
            using var stream = new MemoryStream();
            byte[] frameData = [1, 2, 4, 8];
            const int FRAME_COUNT = 3;

            for (int i = 0; i < FRAME_COUNT; i++)
            {
                var frameHeader = new SphynxFrameHeader(SphynxFrameType.CHANNEL_DATA, 1, (short)frameData.Length);

                if (i == 0)
                    frameHeader = frameHeader.WithFlags(ChannelDataFlags.CHANNEL_START);

                if (i == FRAME_COUNT - 1)
                    frameHeader = frameHeader.WithFlags(ChannelDataFlags.CHANNEL_END);

                stream.Write(frameHeader.Serialize());
                stream.Write(frameData);
            }

            stream.Position = 0;
            frameData = new byte[frameData.Length * FRAME_COUNT];
            var readingDone = new SemaphoreSlim(0, 1);

            // Act
            await using var reader = new DefaultChannelReader(stream);
            SphynxChannelReader.Channel? channel = null;

            reader.OnChannelOpened(async (_, ch) =>
            {
                channel = ch;
                await channel.ReadAtLeastAsync(frameData.AsMemory());
                readingDone.Release();
            });

            await reader.RunAsync();
            await readingDone.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.That(channel, Is.Not.Null);
            Assert.That(channel.IsDisposed, Is.False);
            CollectionAssert.AreEqual(Enumerable.Repeat(new[] { 1, 2, 4, 8 }, FRAME_COUNT).SelectMany(x => x), frameData.ToArray());

            await channel.DisposeAsync();
        }
    }
}
