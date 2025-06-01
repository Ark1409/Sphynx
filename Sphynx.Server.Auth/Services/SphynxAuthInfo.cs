// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Server.Auth.Model;

namespace Sphynx.Server.Auth.Services
{
    public record struct SphynxAuthInfo(SphynxAuthUser User, Guid SessionId);
}
