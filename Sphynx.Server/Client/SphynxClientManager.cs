using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Sphynx.Server.User;
using Sphynx.Server.Utils;

namespace Sphynx.Server.Client
{
    public static class SphynxClientManager
    {
        private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Socket, SphynxClient>> _authenticatedClients;
        private static readonly ConcurrentDictionary<Socket, Guid> _clientIds;
        private static readonly ConcurrentDictionary<Socket, Guid> _sessionIds;
        private static readonly ConcurrentDictionary<Socket, SphynxClient> _anonymousClients;

        public static event Action<SphynxClient>? ClientRemoved;
        public static event Action<SphynxClient>? ClientAdded;
        public static event Action<SphynxClient>? ClientAuthenticated;
        public static event Action<SphynxClient>? ClientUnauthenticated;

        static SphynxClientManager()
        {
            _authenticatedClients = new ConcurrentDictionary<Guid, ConcurrentDictionary<Socket, SphynxClient>>();
            _anonymousClients = new ConcurrentDictionary<Socket, SphynxClient>();
            _sessionIds = new ConcurrentDictionary<Socket, Guid>();
            _clientIds = new ConcurrentDictionary<Socket, Guid>();
        }
        
        /// <summary>
        /// Checks whether the <paramref name="client"/> is an anonymous (i.e. unauthenticated) <see cref="SphynxClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="SphynxClient"/> to check.</param>
        /// <returns>true if the <paramref name="client"/> is unauthenticated; false otherwise.</returns>
        public static bool IsAnonymous(SphynxClient client) => !TryGetUserId(client, out _);

        /// <summary>
        /// Attempts to add an anonymous (i.e. unauthenticated) client to this manager, but returns an existing client instance if it has already
        /// been registered.
        /// </summary>
        /// <param name="server">The server that this <see cref="SphynxClient"/> is connected to.</param>
        /// <param name="clientSocket">The client socket to register.</param>
        /// <returns>A newly-registered <see cref="SphynxClient"/> or an existing one if the <paramref name="clientSocket"/> has already been
        /// registered.</returns>
        public static SphynxClient AddAnonymousClient(SphynxServer server, Socket clientSocket)
        {
            if (TryGetClient(clientSocket, out var existingClient))
            {
                return existingClient;
            }

            // Client registers itself safely
            return new SphynxClient(server, clientSocket);
        }

        /// <summary>
        /// Attempts to add an anonymous <see cref="SphynxClient"/> if it has not already been registered with this manager.
        /// </summary>
        /// <param name="client">The client to add.</param>
        /// <returns>The <paramref name="client"/> or an existing <see cref="SphynxClient"/> if a client with the same
        /// <see cref="SphynxClient.Socket"/> has already been registered.</returns>
        public static SphynxClient AddAnonymousClient(SphynxClient client)
        {
            if (TryGetClient(client.Socket, out var existingClient) || (existingClient = _anonymousClients.GetOrAdd(client.Socket, client)) != client)
            {
                return existingClient;
            }
            
            ClientAdded?.Invoke(client);
            return client;
        }

        /// <summary>
        /// Attempts to remove a <see cref="SphynxClient"/> if it has already been registered with this manager.
        /// </summary>
        /// <param name="client">The client to remove.</param>
        /// <param name="dispose">Whether or not to dispose the <paramref name="client"/>. Should be true in almost every case.</param>
        /// <returns>true if the <paramref name="client"/> was successfully removed; false otherwise.</returns>
        public static bool RemoveClient(SphynxClient client, bool dispose = true)
        {
            if (IsAnonymous(client))
            {
                // Remove first to prevent packet-sending errors
                Debug.Assert(_anonymousClients.Remove(client.Socket, out _));
                if (dispose) client.Dispose();
                
                ClientRemoved?.Invoke(client);
                return true;
            }

            if (TryGetUserId(client, out var userId))
            {
                Debug.Assert(_authenticatedClients.TryGetValue(userId.Value, out var clients));
                
                // Remove first to prevent packet-sending errors
                Debug.Assert(clients.Remove(client.Socket, out _));
                Debug.Assert(_sessionIds.Remove(client.Socket, out _));
                if (dispose) client.Dispose();
                
                ClientRemoved?.Invoke(client);
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Attempts to authenticates the <paramref name="client"/> with the given <paramref name="credentials"/>, if they are valid;
        /// else will return error information.
        /// </summary>
        /// <param name="client">The client to authenticate.</param>
        /// <param name="credentials">The user credentials with which to authenticate the <paramref name="client"/>.</param>
        /// <returns>Error information describing whether the client could be successfully authenticated.</returns>
        public static async Task<SphynxErrorInfo<Guid>> AuthenticateClient(SphynxClient client, SphynxUserCredentials credentials)
        {
            // TODO: Query database
            // TODO: Assign session ID
            
            ClientAuthenticated?.Invoke(client);
            return default;
        }
        
        /// <summary>
        /// Disassociates the previously authenticated <paramref name="client"/> from the user with which it was authenticated, effectively
        /// transforming it into an anonymous user.
        /// </summary>
        /// <param name="client">The client to disassociate.</param>
        /// <returns>true if the client could be successfully unauthenticated; false otherwise.</returns>
        public static bool UnauthenticateClient(SphynxClient client)
        {
            if (_clientIds.Remove(client.Socket, out var clientId))
            {
                Debug.Assert(_sessionIds.Remove(client.Socket, out _));
                Debug.Assert(_authenticatedClients.TryGetValue(clientId, out var clients));
                Debug.Assert(clients.Remove(client.Socket, out _));
                Debug.Assert(_anonymousClients.TryAdd(client.Socket, client));
                
                ClientUnauthenticated?.Invoke(client);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the user ID that the <paramref name="client"/> is authenticated with.
        /// </summary>
        /// <param name="client">The client to retrieve the user ID of.</param>
        /// <param name="userId">The user ID of the <paramref name="client"/>, or null if it is currently anonymous or unregistered with
        /// this manager.</param>
        /// <returns>true if the <paramref name="client"/> has been authenticated; false otherwise.</returns>
        public static bool TryGetUserId(SphynxClient client, [NotNullWhen(true)] out Guid? userId)
        {
            if (_clientIds.TryGetValue(client.Socket, out var id))
            {
                userId = id;
                return true;
            }

            userId = null;
            return false;
        }
        
        /// <summary>
        /// Attempts to retrieve the session ID for the authenticated <paramref name="client"/>.
        /// </summary>
        /// <param name="client">The client to retrieve the session ID of.</param>
        /// <param name="sessionId">The session ID of the <paramref name="client"/>, or null if it is currently anonymous or unregistered with
        /// this manager.</param>
        /// <returns>true if the <paramref name="client"/> has been authenticated; false otherwise.</returns>
        public static bool TryGetSessionId(SphynxClient client, [NotNullWhen(true)] out Guid? sessionId)
        {
            if (_sessionIds.TryGetValue(client.Socket, out var id))
            {
                sessionId = id;
                return true;
            }

            sessionId = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="SphynxClient"/> for the given <paramref name="clientSocket"/>.
        /// </summary>
        /// <param name="clientSocket">The socket the check from</param>
        /// <param name="client">The existing <see cref="SphynxClient"/> representing that <paramref name="clientSocket"/> connection.</param>
        /// <returns>true if a <see cref="SphynxClient"/> has been registered with that <paramref name="clientSocket"/>.</returns>
        public static bool TryGetClient(Socket clientSocket, [NotNullWhen(true)] out SphynxClient? client)
        {
            if (_anonymousClients.TryGetValue(clientSocket, out client))
            {
                return true;
            }

            if (_clientIds.TryGetValue(clientSocket, out var clientId) && _authenticatedClients.TryGetValue(clientId, out var clients))
            {
                return clients.TryGetValue(clientSocket, out client);
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the connected and authenticated clients for the given <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user ID of the (authenticated) clients to retrieve.</param>
        /// <param name="clients">An enumeration of the authenticated clients associated with the given <paramref name="userId"/></param>
        /// <returns>true if authenticated clients associated with the given <paramref name="userId"/> could be found; false otherwise.</returns>
        public static bool TryGetClients(Guid userId, [NotNullWhen(true)] out IEnumerable<SphynxClient>? clients)
        {
            if (_authenticatedClients.TryGetValue(userId, out var clientSet))
            {
                clients = clientSet.Values;
                return true;
            }

            clients = null;
            return false;
        }
    }
}