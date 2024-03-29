using Sphynx.Core;

namespace Sphynx.Client.State
{
    internal class SphynxLobbyState : ISphynxState
    {
        private SphynxSessionUser _user;
        private SphynxClient _client;

        public SphynxLobbyState(SphynxClient client, SphynxSessionUser user)
        {
            _client = client;
            _user = user;
        }

        public ISphynxState? Run()
        {
            ClearConsole();
            Console.WriteLine($"Lobby state for {_client.Server!.GetUsername(_user)}");
            Thread.Sleep(5000);
            return null;
        }

        private void ClearConsole()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = Console.ForegroundColor == Console.BackgroundColor ? (ConsoleColor)((int)Console.ForegroundColor + 1) : Console.ForegroundColor;

            Console.WriteLine(SphynxClient.GetHeader());
        }

        public void Dispose()
        {
            
        }
    }
}
