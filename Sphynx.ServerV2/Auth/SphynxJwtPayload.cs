// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Json;
using System.Text.Json.Serialization;
using Sphynx.Core;

namespace Sphynx.ServerV2.Auth
{
    public struct SphynxJwtPayload
    {
        [JsonPropertyName("iss")]
        public string Issuer { get; set; }

        [JsonPropertyName("aud")]
        public string Audience { get; set; }

        [JsonPropertyName("sub")]
        public Guid Subject { get; set; }

        [JsonPropertyName("iat")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset IssuedAt { get; set; }

        [JsonPropertyName("exp")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset ExpiresAt { get; set; }

        private sealed class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
        {
            public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());
            }

            public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.ToUnixTimeSeconds());
            }
        }
    }
}
