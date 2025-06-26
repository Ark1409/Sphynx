using System.Runtime.Loader;

namespace Sphynx.Server.Auth
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await using var server = new SphynxAuthServer();

            RegisterCleanupHandlers(server);

            await server.StartAsync();
        }

        private static void RegisterCleanupHandlers(SphynxAuthServer server)
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => server.DisposeAsync().AsTask().Wait();
            AssemblyLoadContext.Default.Unloading += (_) => server.DisposeAsync().AsTask().Wait();
            Console.CancelKeyPress += (_, _) => server.DisposeAsync().AsTask().Wait();
        }
    }
}
