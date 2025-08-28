// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Persistence.Auth
{
    public readonly record struct SphynxRedisSession
    {
        public Guid SessionId { get; init; }
        public Guid UserId { get; init; }
        public string IpAddress { get; init;  }
        public DateTimeOffset CreatedAt { get; init; }

        public SphynxRedisSession(in SphynxSessionInfo sessionInfo)
            : this(sessionInfo.SessionId, sessionInfo.UserId, sessionInfo.IpAddress, sessionInfo.CreatedAt)
        {
        }

        [JsonConstructor]
        public SphynxRedisSession(Guid SessionId, Guid UserId, string IpAddress, DateTimeOffset CreatedAt)
        {
            this.SessionId = SessionId;
            this.UserId = UserId;
            this.IpAddress = IpAddress;
            this.CreatedAt = CreatedAt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphynxSessionInfo WithExpiry(TimeSpan expiry) => WithExpiry(DateTime.UtcNow + expiry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphynxSessionInfo WithExpiry(DateTimeOffset expiry) => new SphynxSessionInfo(SessionId, UserId, IpAddress, expiry, CreatedAt);

        public static implicit operator SphynxRedisSession(in SphynxSessionInfo sessionInfo) => new SphynxRedisSession(in sessionInfo);
    }
}
