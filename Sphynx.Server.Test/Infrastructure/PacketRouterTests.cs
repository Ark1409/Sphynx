// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Sphynx.Network.PacketV2;
using Sphynx.ServerV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Middleware;
using Sphynx.ServerV2.Infrastructure.Routing;

namespace Sphynx.Server.Test.Infrastructure
{
    [TestFixture]
    public class PacketRouterTests
    {
        [Test]
        public async Task UseMiddleware_ShouldRegisterMiddleware_InOrder()
        {
            // Arrange
            var router = new PacketRouter();

            // Act
            router.UseMiddleware(new OrderedMiddleware(1))
                .UseMiddleware(new OrderedMiddleware(2))
                .UseHandler(new OrderedHandler(4))
                .UseMiddleware(new OrderedMiddleware(3));

            // Assert
            var orderTrackingPacket = new OrderTrackingPacket();
            await router.ExecuteAsync(new TestClient(), orderTrackingPacket);

            Assert.That(orderTrackingPacket.ExecutionOrder.Count == 4);
            Assert.That(orderTrackingPacket.IsExecutionOrdered);
        }

        [Test]
        public async Task UseMiddleware_ShouldImplicitlyRegisterHandler_WhenPacketIsUnregistered()
        {
            // Arrange
            var router = new PacketRouter { ThrowOnUnregistered = true };
            await Assert.ThatAsync(() => router.ExecuteAsync(new TestClient(), new TestPacket()), Throws.Exception);

            // Act
            router.UseMiddleware(new TestMiddleware());

            // Assert
            Assert.DoesNotThrowAsync(() => router.ExecuteAsync(new TestClient(), new TestPacket()));
        }

        [Test]
        public void UseHandler_ShouldReplaceHandler_WhenInvokedTwiceOnSamePacketType()
        {
            // Arrange
            var router = new PacketRouter { ThrowOnUnregistered = true };

            // Act

            // Assert
            Assert.Fail();
        }

        [Test]
        public async Task ExecuteAsync_ShouldThrowException_WhenInvokedWithUnregisteredPacket()
        {
            // Arrange
            var router = new PacketRouter { ThrowOnUnregistered = true };

            // Act
            AsyncTestDelegate executeTask = Task () => router.ExecuteAsync(new TestClient(), new TestPacket());

            // Assert
            await Assert.ThatAsync(executeTask, Throws.Exception);
        }

        [Test]
        public void ExecuteAsync_ShouldInvokeNonGenericHandler_WhenHandlerIsNotRegistered()
        {
            // Arrange
            var router = new PacketRouter();

            // Act

            // Assert
            Assert.Fail();
        }

        [Test]
        public void ExecuteAsync_ShouldAlwaysInvokeNonGenericMiddleware_WhenExecuted()
        {
            // Arrange
            var router = new PacketRouter();

            // Act

            // Assert
            Assert.Fail();
        }

        private class NonGenericMiddleware : IPacketMiddleware
        {
            public Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }
        }

        private class OrderedMiddleware : IPacketMiddleware<OrderTrackingPacket>
        {
            private readonly int _order;

            public OrderedMiddleware(int order)
            {
                _order = order;
            }

            public Task InvokeAsync(ISphynxClient client, OrderTrackingPacket packet, NextDelegate<OrderTrackingPacket> next,
                CancellationToken token = default)
            {
                packet.ExecutionOrder.Add(_order);
                return next(client, packet, token);
            }
        }

        private class TestMiddleware : IPacketMiddleware
        {
            public Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token = default)
            {
                return next(client, packet, token);
            }
        }

        private class OrderedHandler : IPacketHandler<OrderTrackingPacket>
        {
            private readonly int _order;

            public OrderedHandler(int order)
            {
                _order = order;
            }

            public Task HandlePacketAsync(ISphynxClient client, OrderTrackingPacket packet, CancellationToken cancellationToken = default)
            {
                packet.ExecutionOrder.Add(_order);
                return Task.CompletedTask;
            }
        }

        private class OrderTrackingPacket : TestPacket
        {
            public List<int> ExecutionOrder { get; } = new();

            public bool IsExecutionOrdered
            {
                get
                {
                    int previous = ExecutionOrder[0];

                    foreach (int current in ExecutionOrder)
                    {
                        if (current < previous)
                            return false;

                        previous = current;
                    }

                    return true;
                }
            }
        }

        private class TestPacket : SphynxPacket
        {
            public override SphynxPacketType PacketType => SphynxPacketType.NOP;
        }

        private class TestClient : ISphynxClient
        {
            public Guid ClientId { get; } = Guid.NewGuid();
            public IPEndPoint EndPoint { get; } = new(IPAddress.Any, SphynxServerProfile.DEFAULT_PORT);

            public ValueTask SendAsync(SphynxPacket packet, CancellationToken cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
