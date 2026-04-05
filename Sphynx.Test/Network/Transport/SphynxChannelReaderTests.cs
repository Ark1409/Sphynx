// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

            reader.OnFrameDropped((_, header) =>
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
        public async Task OnChannelRejected_ShouldBeInvoked_WhenChannelIsPrematurelyDisposed()
        {
            throw new NotImplementedException();
        }

        [Test]
        public async Task OnChannelRejectReceived_ShouldBeInvoked_WhenRejectIsReceived()
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

            reader.OnChannelRejectReceived((_, channelId) =>
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
        public async Task SphynxChannelReader_ShouldNotDisposeChannel_WhenChannelCompleted()
        {
            // Arrange
            // TODO: We need to use finishedData (throws ex) and unfinishedData (does not throw ex)
            //  finishedData: START+END
            //  unfinsehdData: START
            using var stream = new MemoryStream();
            byte[] data = [1, 2, 3, 4, 5];

            await using (var writer = new SphynxChannelWriter(stream))
            {
                await using var channel = writer.OpenChannel();
                channel.MaxFrameSize = (short)(data.Length / 2);

                await channel.WriteAsync(data.AsMemory());
            }

            var doneReading = new SemaphoreSlim(0, 1);
            data = new byte[data.Length];
            stream.Position = 0;

            // Act
            var reader = new SphynxChannelReader(stream);

            reader.OnChannelOpened(async (_, channel) =>
            {
                await channel.ReadExactlyAsync(new Memory<byte>(data, 0, data.Length));
                doneReading.Release();
            });

            await reader.RunAsync();
            await doneReading.WaitAsync();
            await reader.DisposeAsync();
        }

        [TestFixture]
        public class ChannelTests
        {
        }
    }
}
