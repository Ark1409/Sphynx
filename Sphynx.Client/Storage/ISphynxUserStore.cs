using System.Diagnostics.CodeAnalysis;
using Sphynx.Bindables;
using Sphynx.Model.User;

namespace Sphynx.Client.Storage
{
    public interface ISphynxUserStore
    {
        sealed Task<IReadOnlyBindable<SphynxUserInfo>> this[Guid id] => GetUser(id);

        Task<IReadOnlyBindable<SphynxUserInfo>> GetUser(Guid userId, CancellationToken cancellationToken = default);

        bool GetUserCached(Guid userId, [NotNullWhen(true)] out IReadOnlyBindable<SphynxUserInfo>? val);
    }
}
