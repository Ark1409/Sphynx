// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using System.Net.Sockets;
using Nerdbank.Streams;
using Sphynx.Network.Transport;

namespace Sphynx.Test.Network.Transport
{
    [TestFixture]
    public class SphynxChannelTests
    {
        [Test]
        public async Task StartAsync_ShouldPopulateRunTask_WhenInvoked()
        {
            // Arrange
            using var stream = new MemoryStream();
            await using var channel = new SphynxChannel(stream);
            Assert.That(channel.RunTask == null);

            // Act
            channel.Start();

            // Assert
            Assert.That(channel.RunTask != null);
        }

        [TestFixture]
        public class ReaderTests
        {
            [Test]
            public async Task DisposeAsync_ShouldCauseSenderChannelToClose_WhenInvokedOnOpenedReceiverChannel()
            {
                // Arrange
                using var sock1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var sock2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var sock3 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var sock4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock3.Bind(IPEndPoint.Parse("127.0.0.1:25566"));
                sock3.Listen();
                var r3t = sock3.AcceptAsync();
                sock4.Bind(IPEndPoint.Parse("127.0.0.1:25567"));
                sock4.Listen();
                var r4t = sock4.AcceptAsync();
                await sock1.ConnectAsync(IPAddress.Loopback, 25566);
                await sock2.ConnectAsync(IPAddress.Loopback, 25567);
                using var stream1 = new NetworkStream(sock1, FileAccess.Write, ownsSocket: true);
                using var stream2 = new NetworkStream(sock2, FileAccess.Write, ownsSocket: true);
                using var stream3 = new NetworkStream(await r3t, FileAccess.Read, ownsSocket: true);
                using var stream4 = new NetworkStream(await r4t, FileAccess.Read, ownsSocket: true);
                await using var senderChannel = new SphynxChannel(FullDuplexStream.Splice(stream4, stream1));
                await using var receiverChannel = new SphynxChannel(FullDuplexStream.Splice(stream3, stream2));

                senderChannel.Start();
                receiverChannel.Start();
                var writerChannel = senderChannel.Writer.OpenChannel();

                var channelOpened = new SemaphoreSlim(0, 1);
                receiverChannel.Reader.OnChannelOpened(async (_, c) =>
                {
                    await c.DisposeAsync(new ChannelClosedException());
                    while (!writerChannel.IsDisposed)
                        await Task.Yield();
                    channelOpened.Release();
                });

                // Act
                await writerChannel.WriteAsync(new byte[] { 1, 2, 3 });
                await writerChannel.FlushAsync();
                await channelOpened.WaitAsync(TimeSpan.FromSeconds(10));

                // Assert
                Assert.That(writerChannel.IsDisposed);
            }
        }
    }
}
