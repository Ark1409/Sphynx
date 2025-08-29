using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Transport;

namespace Sphynx.Client.API;

public class SphynxAuthServer : SphynxServer
{
    public SphynxAuthServer(SphynxPacketRouter router) : base(router) { }

    // public async Task<SphynxErrorInfo<SphynxSessionInfo>> Login(LoginInfo info, CancellationToken cancellationToken = default)
    // {
    //     var login = new LoginRequest(info.UserName, info.Password);
    //     var loginRes = await Router.SendRequest(login, cancellationToken).ConfigureAwait(false);
    //
    //     if (loginRes.ErrorInfo.ErrorCode != SphynxErrorCode.SUCCESS)
    //     {
    //         return new(loginRes.ErrorInfo.ErrorCode, loginRes.ErrorInfo.Message);
    //     }
    //
    //     return new(new(loginRes.SessionId!.Value, loginRes.UserInfo!));
    // }
    //
    // public Task<SphynxErrorCode> Logout(in SphynxSessionInfo info, CancellationToken cancellationToken = default)
    // {
    //     return Logout(info.SessionId, info.Self.UserId, cancellationToken);
    // }
    //
    // public async Task<SphynxErrorCode> Logout(Guid sessionId, SnowflakeId userId, CancellationToken cancellationToken = default)
    // {
    //     var logout = new LogoutRequest(userId, sessionId);
    //     LogoutResponse res = null!;
    //
    //     cancellationToken.ThrowIfCancellationRequested();
    //
    //     using (var stream = await Connection.Stream.RentAsync(cancellationToken).ConfigureAwait(false))
    //     {
    //         cancellationToken.ThrowIfCancellationRequested();
    //
    //         await Transporter.SendAsync(stream, logout, cancellationToken).ConfigureAwait(false);
    //         res = (LogoutResponse)await Transporter.ReceiveAsync(stream, CancellationToken.None).ConfigureAwait(false);
    //     }
    //
    //     return res.ErrorCode;
    // }
    //
    // public async Task<SphynxErrorInfo<SphynxSessionInfo>> Register(RegistrationInfo info, CancellationToken cancellationToken = default)
    // {
    //     var reg = new RegisterRequest(info.UserName, info.Password);
    //     RegisterResponse res = null!;
    //
    //     cancellationToken.ThrowIfCancellationRequested();
    //
    //     using (var stream = await Connection.Stream.RentAsync(cancellationToken).ConfigureAwait(false))
    //     {
    //         cancellationToken.ThrowIfCancellationRequested();
    //
    //         await Transporter.SendAsync(stream, reg, cancellationToken).ConfigureAwait(false);
    //         res = (RegisterResponse)await Transporter.ReceiveAsync(stream, CancellationToken.None).ConfigureAwait(false);
    //     }
    //
    //     if (res.ErrorCode != SphynxErrorCode.SUCCESS)
    //     {
    //         return new(res.ErrorCode);
    //     }
    //
    //     return new(new(res.SessionId!.Value, res.UserInfo!));
    // }
    //
    // public record struct LoginInfo(string UserName, string Password) { }
    // public record struct RegistrationInfo(string UserName, string Password) { }
    //
    // public readonly record struct SphynxSessionInfo(Guid SessionId, SphynxSelfInfo Self) { }
}
