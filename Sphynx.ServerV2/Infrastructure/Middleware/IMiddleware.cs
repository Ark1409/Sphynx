// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;

namespace Sphynx.ServerV2.Infrastructure.Middleware
{
    public delegate Task NextDelegate<in TPacket>(ISphynxClient client, TPacket packet, CancellationToken ct) where TPacket : SphynxPacket;

    public interface IMiddleware<TPacket> where TPacket : SphynxPacket
    {
        Task InvokeAsync(ISphynxClient client, TPacket packet, NextDelegate<TPacket> next, CancellationToken token = default);
    }

    public interface IMiddleware : IMiddleware<SphynxPacket>
    {
    }
}
