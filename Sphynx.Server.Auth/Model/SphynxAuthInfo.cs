// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Server.Auth.Model
{
    public record struct SphynxAuthInfo(SphynxAuthUser User, Guid SessionId);
}
