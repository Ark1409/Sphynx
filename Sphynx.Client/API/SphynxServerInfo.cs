namespace Sphynx.Client.API;

public abstract class SphynxServerInfo
{
    public abstract Task<SphynxServerConnection> ConnectAsync(CancellationToken token = default);
}
