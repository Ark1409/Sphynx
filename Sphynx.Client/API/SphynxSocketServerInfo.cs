using System.Net;
using System.Net.Sockets;

namespace Sphynx.Client.API;

public class SphynxSocketServerInfo : SphynxServerInfo
{
    public required EndPoint EndPoint { get; init; }

    public sealed override async Task<SphynxServerConnection> ConnectAsync(CancellationToken token = default)
    {
        var socket = CreateSocket();

        await socket.ConnectAsync(EndPoint, token).ConfigureAwait(false);

        var ret = new SphynxServerConnection(new NetworkStream(socket, false));

        ret.OnDispose += obj =>
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Dispose();
        };

        return ret;
    }

    protected virtual Socket CreateSocket()
    {
        return new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }
}
