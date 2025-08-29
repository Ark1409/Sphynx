using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Client.API;

public class SphynxPacketRouter
{
    private IPacketTransporter _transporter;
    private SphynxServerConnection _connection;
    private readonly SemaphoreSlim _readLock = new(1, 1);
    private readonly Dictionary<Guid, TaskCompletionSource<SphynxResponse>> _sources = new();

    public SphynxPacketRouter(IPacketTransporter transporter, SphynxServerConnection connection)
    {
        _transporter = transporter;
        _connection = connection;
    }

    public async Task<TResponse> SendRequest<TResponse>(SphynxRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : SphynxResponse
    {
        using var _ = await _readLock.RentAsync(cancellationToken);

        await _transporter.SendAsync(_connection.Stream, request, cancellationToken).ConfigureAwait(false);

        var tsc = new TaskCompletionSource<SphynxResponse>();
        // _sources[request.Tag] = tsc;
        return (TResponse)await tsc.Task.ConfigureAwait(false);
    }

    private Task Start()
    {
        return Task.Run(async () =>
        {
            var isReadLoopRunning = true;
            while (isReadLoopRunning)
            {
                var res = await _transporter.ReceiveAsync(_connection.Stream, CancellationToken.None).ConfigureAwait(false);

                using var _ = await _readLock.RentAsync();

                // if (_sources.TryGetValue(res.Tag, out var val))
                // {
                //     val.SetResult((SphynxResponse)res);
                // }
                // else
                // {
                //     // Handle other types of packets normally (e.g. broadcast??)
                // }
            }

        });
    }

    private void Stop()
    {

    }
}
