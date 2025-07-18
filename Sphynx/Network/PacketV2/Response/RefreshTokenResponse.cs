// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    public class RefreshTokenResponse : SphynxResponse, IEquatable<RefreshTokenResponse>
    {
        public override SphynxPacketType PacketType => SphynxPacketType.REFRESH_TOKEN_RES;

        public string? AccessToken { get; init; }
        public Guid? RefreshToken { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }

        public RefreshTokenResponse(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        public RefreshTokenResponse(string accessToken, Guid refreshToken, DateTimeOffset expiresAt) : this(SphynxErrorCode.SUCCESS)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ExpiresAt = expiresAt;
        }

        public bool Equals(RefreshTokenResponse? other)
        {
            return AccessToken == other?.AccessToken && RefreshToken == other?.RefreshToken && ExpiresAt == other?.ExpiresAt;
        }
    }
}
