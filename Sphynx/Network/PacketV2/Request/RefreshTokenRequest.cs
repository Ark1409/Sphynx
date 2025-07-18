// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.REFRESH_TOKEN_REQ"/>
    public class RefreshTokenRequest : SphynxRequest
    {
        public override SphynxPacketType PacketType => SphynxPacketType.REFRESH_TOKEN_REQ;

        public Guid RefreshToken { get; init; }
    }
}
