// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LitJWT.Algorithms;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public struct JwtOptions
    {
        public static JwtOptions Default => new()
        {
            Issuer = "Sphynx",
            Audience = "Sphynx",
            Secret = HS256Algorithm.GenerateRandomRecommendedKey()
        };

        public string Issuer { get; set; }
        public string Audience { get; set; }
        public byte[] Secret { get; set; }
        public TimeSpan ExpiryTime { get; set; }
    }
}
