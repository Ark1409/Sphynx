// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Sphynx.Core;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Server;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Middleware;
using Sphynx.Server.Infrastructure.Routing;

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

            Assert.That(orderTrackingPacket.ExecutionOrder.Count, Is.EqualTo(4));
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
        public async Task UseHandler_ShouldReplaceHandler_WhenInvokedTwiceOnSamePacketType()
        {
            // Arrange
            var router = new PacketRouter();
            router.UseHandler(new OrderedHandler(1));

            // Act
            router.UseHandler(new OrderedHandler(2));

            // Assert
            var orderTrackingPacket = new OrderTrackingPacket();
            await router.ExecuteAsync(new TestClient(), orderTrackingPacket);

            Assert.That(orderTrackingPacket.ExecutionOrder.Count, Is.EqualTo(1));
            Assert.That(orderTrackingPacket.ExecutionOrder[0], Is.EqualTo(2));
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
        public async Task ExecuteAsync_ShouldInvokeNonGenericHandler_WhenHandlerIsNotRegistered()
        {
            // Arrange
            var router = new PacketRouter { ThrowOnUnregistered = true };
            await Assert.ThatAsync(() => router.ExecuteAsync(new TestClient(), new TestPacket()), Throws.Exception);

            var nonGenericHandler = new TestHandler();
            router.UseHandler(nonGenericHandler);

            Assert.That(nonGenericHandler.IsExecuted, Is.False);

            // Act
            await router.ExecuteAsync(new TestClient(), new TestRequestPacket());

            // Assert
            Assert.That(nonGenericHandler.IsExecuted);
        }

        [Test]
        public async Task ExecuteAsync_ShouldAlwaysInvokeNonGenericMiddleware_WhenExecuted()
        {
            // Arrange
            var router = new PacketRouter { ThrowOnUnregistered = true };

            var nonGenericMiddleware = new TestMiddleware();
            router.UseMiddleware(nonGenericMiddleware);

            Assert.That(nonGenericMiddleware.IsExecuted, Is.False);

            // Act
            await router.ExecuteAsync(new TestClient(), new TestRequestPacket());

            // Assert
            Assert.That(nonGenericMiddleware.IsExecuted);
        }

        private class OrderedMiddleware : IPacketMiddleware<OrderTrackingPacket>
        {
            public int Order { get; }

            public OrderedMiddleware(int order)
            {
                Order = order;
            }

            public Task InvokeAsync(ISphynxClient client, OrderTrackingPacket packet, NextDelegate<OrderTrackingPacket> next,
                CancellationToken token = default)
            {
                packet.ExecutionOrder.Add(Order);
                return next(client, packet, token);
            }
        }

        private class OrderedHandler : IPacketHandler<OrderTrackingPacket>
        {
            public int Order { get; }

            public OrderedHandler(int order)
            {
                Order = order;
            }

            public Task HandlePacketAsync(ISphynxClient client, OrderTrackingPacket packet, CancellationToken cancellationToken = default)
            {
                packet.ExecutionOrder.Add(Order);
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

        private class TestHandler : IPacketHandler
        {
            public bool IsExecuted { get; private set; }

            public Task HandlePacketAsync(ISphynxClient client, SphynxPacket packet, CancellationToken cancellationToken = default)
            {
                IsExecuted = true;
                return Task.CompletedTask;
            }
        }

        private class TestMiddleware : IPacketMiddleware
        {
            public bool IsExecuted { get; private set; }

            public Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token = default)
            {
                IsExecuted = true;
                return next(client, packet, token);
            }
        }

        private class TestPacket : SphynxPacket
        {
            public override SphynxPacketType PacketType => SphynxPacketType.NOP;
        }

        private class TestRequestPacket : SphynxRequest
        {
            public override SphynxPacketType PacketType => SphynxPacketType.NOP;
            public override TestResponsePacket CreateResponse(SphynxErrorInfo errorInfo) => new TestResponsePacket(errorInfo);
        }

        private class TestResponsePacket : SphynxResponse
        {
            public TestResponsePacket(SphynxErrorInfo errorInfo) : base(errorInfo)
            {
            }

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
