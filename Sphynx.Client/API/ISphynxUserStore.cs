using System.Diagnostics.CodeAnalysis;
using Sphynx.Bindables;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Client.API;

public interface ISphynxUserStore
{
    sealed Task<IReadOnlyBindable<SphynxUserInfo>> this[SnowflakeId id] => GetUser(id);

    Task<IReadOnlyBindable<SphynxUserInfo>> GetUser(SnowflakeId userId, CancellationToken cancellationToken = default);

    bool GetUserCached(SnowflakeId userId, [NotNullWhen(true)] out IReadOnlyBindable<SphynxUserInfo>? val);
}
