using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Sphynx.Bindables;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Transport;

namespace Sphynx.Client.API;

public class SphynxUserStore : ISphynxUserStore
{
    private readonly Dictionary<Guid, UserInfo> _userCache = new();

    private readonly SphynxServerConnection _connection;
    private readonly IPacketTransporter _transporter = null!;

    public SphynxUserStore(SphynxServerConnection connection)
    {
        _connection = connection;
    }

    private void cleanup()
    {
        foreach (var (id, info) in _userCache)
        {
        }
    }

    public async Task<IReadOnlyBindable<SphynxUserInfo>> GetUser(Guid userId, CancellationToken cancellationToken = default)
    {
        // if (GetUserCached(userId, out var val))
        // {
        //     return val;
        // }
        //
        // var fetchReq = new FetchUsersRequest(, userId);
        // FetchUsersResponse fetchRes = null!;
        //
        // cancellationToken.ThrowIfCancellationRequested();
        // using (var stream = await _connection.Stream.RentAsync(cancellationToken).ConfigureAwait(false))
        // {
        //     await _transporter.SendAsync(stream, fetchReq, cancellationToken);
        //     fetchRes = (FetchUsersResponse)await _transporter.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);
        // }
        //
        // if(fetchRes.)
        return null!;
    }

    public bool GetUserCached(Guid userId, [NotNullWhen(true)] out IReadOnlyBindable<SphynxUserInfo>? val)
    {
        if (!_userCache.TryGetValue(userId, out var info))
        {
            val = null;
            return false;
        }

        if (isExpired(in info))
        {
            var userTask = refetchUser(ref info);

            if (!((IAsyncResult)userTask).CompletedSynchronously)
            {
                val = null;
                return false;
            }
        }

        val = info.Info;
        return true;
    }

    private void upsertUser(SphynxUserInfo info)
    {
        if (_userCache.TryGetValue(info.UserId, out var fullInfo))
        {
            _userCache[info.UserId] = new(fullInfo.Info);
            fullInfo.Info.Value = info;
        }
        else
        {
            fullInfo = _userCache[info.UserId] = new(info);
        }
    }

    private Task<SphynxUserInfo> refetchUser(ref UserInfo info, CancellationToken token = default)
    {
        return null!;
    }

    private static bool isExpired(in UserInfo info)
    {
        const long expireSeconds = 60 * 5;
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - info.InsertTimeSeconds > expireSeconds;
    }

    private readonly record struct UserInfo(IBindable<SphynxUserInfo> Info, long InsertTimeSeconds)
    {
        public UserInfo(IBindable<SphynxUserInfo> oldInfo) : this(oldInfo, DateTimeOffset.UtcNow.ToUnixTimeSeconds()) { }
        public UserInfo(SphynxUserInfo info) : this(new Bindable<SphynxUserInfo>(info)) { }
    }
}
